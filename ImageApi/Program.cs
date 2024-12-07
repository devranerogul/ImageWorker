using System.Text;
using System.Text.Json;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Google.Cloud.Firestore;
using RabbitMQ.Client;
using ImageCore;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client.Events;
using ImageApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builderI =>
    {
        builderI.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS policy
app.UseCors("AllowAll");

var settings = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();
var firebaseConfig = new FirebaseAuthConfig
{
    // Get api key from appsettings.json
    ApiKey = settings.GetValue("Firebase:ApiKey", ""),
    AuthDomain = settings.GetValue("Firebase:AuthDomain", ""),
    Providers =
    [
        new EmailProvider()
    ]
};

app.MapGet("/template/{id}", async (string id) =>
    {
        var db = new FirestoreDbBuilder
        {
            ProjectId = settings.GetValue("Firebase:ProjectId", ""),
            ApiKey = settings.GetValue("Firebase:ApiKey", ""),
            ConverterRegistry = new ConverterRegistry
            {
                new FirestoreImageLayerConverter(),
                new FirestoreTextLayerConverter(),
                new FirestoreTemplateUpdateRequestConverter()
            }
        }.Build();

        DocumentReference docRef = db.Collection("user-image-template").Document(id);
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        if (!snapshot.Exists)
        {
            return Results.NotFound();
        }

        var template = snapshot.ConvertTo<TemplateUpdateRequest>();
        
        template.TemplateId = id;

        return Results.Ok(template);
    }).WithName("GetTemplate")
    .Produces<TemplateUpdateRequest>(200)
    .Produces(404);

app.MapPut("/template/create", async ([FromBody] TemplateUpdateRequest templateRequest) =>
    {
        var db = new FirestoreDbBuilder
        {
            ProjectId = settings.GetValue("Firebase:ProjectId", ""),
            ApiKey = settings.GetValue("Firebase:ApiKey", ""),
            ConverterRegistry = new ConverterRegistry
            {
                new FirestoreImageLayerConverter(),
                new FirestoreTextLayerConverter()
            }
        }.Build();

        var templateData = new Dictionary<string, object>
        {
            { "templateId", templateRequest.TemplateId },
            { "templateName", templateRequest.TemplateName },
            { "canvasWidth", templateRequest.CanvasWidth },
            { "canvasHeight", templateRequest.CanvasHeight },
            { "description", templateRequest.Description },
            { "createdAt", Timestamp.GetCurrentTimestamp() },
            {
                "imageLayers",
                templateRequest.ImageLayers.Select(l => new FirestoreImageLayerConverter().ToFirestore(l)).ToList()
            },
            {
                "textLayers",
                templateRequest.TextLayers.Select(l => new FirestoreTextLayerConverter().ToFirestore(l)).ToList()
            }
        };

        DocumentReference docRef = db.Collection("user-image-template").Document(templateRequest.TemplateId);

        await docRef.SetAsync(templateData);

        return Results.Ok(new { Id = docRef.Id, message = "Template updated successfully" });
    }).WithName("UpdateTemplate")
    .WithOpenApi();

app.MapPost("/template/create", async ([FromBody] TemplateCreationRequest templateRequest) =>
    {
        var db = new FirestoreDbBuilder
        {
            ProjectId = settings.GetValue("Firebase:ProjectId", ""),
            ApiKey = settings.GetValue("Firebase:ApiKey", ""),
            ConverterRegistry = new ConverterRegistry
            {
                new FirestoreImageLayerConverter(),
                new FirestoreTextLayerConverter()
            }
        }.Build();

        var templateData = new Dictionary<string, object>
        {
            { "templateName", templateRequest.TemplateName },
            { "description", templateRequest.Description },
            { "canvasWidth", templateRequest.CanvasWidth },
            { "canvasHeight", templateRequest.CanvasHeight },
            { "createdAt", Timestamp.GetCurrentTimestamp() },
            {
                "imageLayers",
                templateRequest.ImageLayers.Select(l => new FirestoreImageLayerConverter().ToFirestore(l)).ToList()
            },
            {
                "textLayers",
                templateRequest.TextLayers.Select(l => new FirestoreTextLayerConverter().ToFirestore(l)).ToList()
            }
        };

        // Save to Firestore
        var docRef = await db.Collection("user-image-template").AddAsync(templateData);

        return Results.Ok(new { id = docRef.Id, message = "Template created successfully" });
    }).WithName("CreateTemplate")
    .WithOpenApi();

app.MapPost("/image/generate", async (ImageTask imageGenTask) =>
    {
        // validate request body content has a base image and at least one layer
        if (imageGenTask.ImageLayers.Count == 0 && imageGenTask.TextLayers.Count == 0)
        {
            return Results.BadRequest("Request body must contain a base image and at least one layer");
        }

        var taskId = Guid.NewGuid().ToString();
        var completionSource = new TaskCompletionSource<string>();

        // Create connection and channel
        ConnectionFactory factory = new ConnectionFactory();
        factory.Uri = new Uri("amqp://guest:guest@localhost:5672/");

        IConnection conn = await factory.CreateConnectionAsync();
        IChannel channel = await conn.CreateChannelAsync();

        // Setup response queue with unique name for this consumer
        string responseQueueName = $"response_queue_{taskId}";
        await channel.QueueDeclareAsync(responseQueueName, false, true, true, null);
        await channel.QueueBindAsync(responseQueueName, "direct_exchange", "gen_task_completed", null);

        // Setup consumer for responses
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (model, ea) =>
        {
            if (ea.BasicProperties.CorrelationId == taskId)
            {
                var response = Encoding.UTF8.GetString(ea.Body.ToArray());
                completionSource.TrySetResult(response);
            }

            return Task.CompletedTask;
        };

        await channel.BasicConsumeAsync(responseQueueName, true, consumer);

        // Send the task
        await channel.ExchangeDeclareAsync("direct_exchange", ExchangeType.Direct);
        await channel.QueueDeclareAsync("gen_task", true, false, false, null);
        await channel.QueueBindAsync("gen_task", "direct_exchange", "gen_task", null);

        // SET OPTIONS
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        var json = JsonSerializer.Serialize(imageGenTask, options);
        var body = Encoding.UTF8.GetBytes(json);

        BasicProperties properties = new BasicProperties
        {
            ReplyTo = responseQueueName,
            CorrelationId = taskId,
        };

        await channel.BasicPublishAsync("direct_exchange", "gen_task", false, properties, body);

        // Wait for response with timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try
        {
            var response = await completionSource.Task.WaitAsync(cts.Token);
            await conn.CloseAsync();
            return Results.Ok(response);
        }
        catch (OperationCanceledException)
        {
            await conn.CloseAsync();
            return Results.StatusCode(statusCode: 408); // Request Timeout
        }
    }).Accepts<ImageTask>("application/json")
    .WithName("GenerateImage")
    .WithOpenApi();

//Generate a method for Signing Up using Firebase Authentication
app.MapPost("/signup", async (SignupRequest signUp) =>
{
    var auth = new FirebaseAuthClient(firebaseConfig);

    var response = await auth.CreateUserWithEmailAndPasswordAsync(signUp.Email, signUp.Password);

    return Results.Ok(response);
}).Accepts<SignupRequest>("application/json");

// Generate a method for Signing In using Firebase Authentication
app.MapPost("/signin", async (SignupRequest signIn) =>
{
    var auth = new FirebaseAuthClient(firebaseConfig);
    try
    {
        var response = await auth.SignInWithEmailAndPasswordAsync(signIn.Email, signIn.Password);
        return Results.Ok(response);
    }
    catch (FirebaseAuthException e)
    {
        return Results.Unauthorized();
    }
}).Accepts<SignupRequest>("application/json");


app.Run();

public class SignupRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class TemplateCreationRequest
{
    public string TemplateName { get; set; }
    public string Description { get; set; }

    public int CanvasWidth { get; set; }

    public int CanvasHeight { get; set; }
    public List<ImageLayer> ImageLayers { get; set; } = new();
    public List<TextLayer> TextLayers { get; set; } = new();
}

public class TemplateUpdateRequest : TemplateCreationRequest
{
    public string TemplateId { get; set; }
}