using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using ImageCore;
using RabbitMQ.Client.Events;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureHttpJsonOptions(options => {
    options.SerializerOptions.Converters.Add(new ImageApi.LayerConverter());
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

/// <summary>
/// Generate an image using the provided task file
/// </summary>
/// <param name="imageGenTask">Task details for image generation</param>
app.MapPost("/image/generate", async (ImageTask imageGenTask) =>
    {
        // validate request body content has a base image and at least one layer
        if (imageGenTask.BaseImage == null || imageGenTask.Layers == null || imageGenTask.Layers.Count == 0)
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
            Converters = { new ImageApi.LayerConverter() },
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

app.Run();