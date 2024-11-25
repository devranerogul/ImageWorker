using System.Text;
using ImageWorker;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

// Send a task to ImageWorker using 'gen_task' queue at RabbitMQ.Client
ConnectionFactory factory = new ConnectionFactory();
factory.Uri = new Uri("amqp://guest:guest@localhost:5672/");
Console.WriteLine("Connecting to RabbitMQ");
IConnection conn = await factory.CreateConnectionAsync();
IChannel channel = await conn.CreateChannelAsync();

// Listen gen_task queue
await channel.ExchangeDeclareAsync("direct_exchange", ExchangeType.Direct);
await channel.QueueDeclareAsync("gen_task", true, false, false, null);
await channel.QueueBindAsync("gen_task", "direct_exchange", "gen_task", null);

// create a consumer
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (ch, ea) =>
{
    Console.WriteLine("Connecting to RabbitMQ");
    var body = ea.Body.ToArray();
    var taskFile = Encoding.UTF8.GetString(body);
    Console.WriteLine("Received: " + taskFile);
    
    var taskId = ea.BasicProperties.CorrelationId;
    Console.WriteLine("Task ID: " + taskId);
    
    await channel.BasicAckAsync(ea.DeliveryTag, false);

    var imgGenResult = ImageGenerator.GenerateImage(taskFile, taskId ?? Guid.NewGuid().ToString());
    
    var responseProps = new BasicProperties
    {
        CorrelationId = taskId
    };
    
    // Desearialize the result to byte array
    string strResult = JsonConvert.SerializeObject(imgGenResult);
    var response = Encoding.UTF8.GetBytes(strResult);
    
    // Add response publishing
    await channel.BasicPublishAsync(
        exchange: "direct_exchange",
        routingKey: "gen_task_completed",  // New routing key for completion messages
        mandatory: true,
        basicProperties: responseProps,
        body: response
    );
    
    Console.WriteLine($"Sent completion message for task: {taskId}");
};

// Add new queue binding for completion messages
await channel.QueueDeclareAsync("gen_task_completed", true, false, false, null);
await channel.QueueBindAsync("gen_task_completed", "direct_exchange", "gen_task_completed", null);

// this consumer tag identifies the subscription
// when it has to be cancelled
string consumerTag = await channel.BasicConsumeAsync("gen_task", false, consumer);
Console.WriteLine("Listening to RabbitMQ");
Console.ReadKey();

await conn.CloseAsync();