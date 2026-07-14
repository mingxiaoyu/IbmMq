using System.CommandLine;
using System.Text.Json;
using MqManager.CliMock.Services;
using MqManager.CliMock.Models;

namespace MqManager.CliMock.Commands;

public static class BrowseCommand
{
    public static Command Create(Func<MqConnectionSettings> getSettings, MockDataStore store)
    {
        var queueArg = new Argument<string>("queue", "Queue name to browse");
        var countOption = new Option<int>("--count", () => 10, "Maximum number of messages to browse");

        var command = new Command("browse", "Browse messages on a queue (non-destructive)")
        {
            queueArg, countOption
        };

        command.SetHandler((queue, count) =>
        {
            var settings = getSettings();
            using var mq = new MockMqService(settings, store);
            mq.Connect();

            var messages = mq.BrowseMessages(queue, count);
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
