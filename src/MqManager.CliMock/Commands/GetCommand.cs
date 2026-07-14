using System.CommandLine;
using System.Text.Json;
using MqManager.CliMock.Services;
using MqManager.CliMock.Models;

namespace MqManager.CliMock.Commands;

public static class GetCommand
{
    public static Command Create(Func<MqConnectionSettings> getSettings, MockDataStore store)
    {
        var queueArg = new Argument<string>("queue", "Source queue name");
        var countOption = new Option<int>("--count", () => 1, "Number of messages to receive");

        var command = new Command("get", "Receive and consume messages from a queue")
        {
            queueArg, countOption
        };

        command.SetHandler((queue, count) =>
        {
            var settings = getSettings();
            using var mq = new MockMqService(settings, store);
            mq.Connect();

            var messages = mq.GetMessages(queue, count);
            var output = messages.Select(m => new
            {
                messageId = m.MessageId,
                correlationId = m.CorrelationId,
                putDateTime = m.PutDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                body = m.Body
            }).ToList();

            Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
        }, queueArg, countOption);

        return command;
    }
}
