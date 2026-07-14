using System.CommandLine;
using System.Text.Json;
using MqManager.Core.Models;
using MqManager.Core.Serialization;
using MqManager.Core.Services;

namespace MqManager.Cli.Commands;

public static class BrowseCommand
{
    public static Command Create(Func<MqConnectionSettings> getSettings)
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
            using var mq = new MqService(settings);
            mq.Connect();

            var messages = mq.BrowseMessages(queue, count);
            Console.WriteLine(JsonSerializer.Serialize(messages, DefaultJsonContext.Default.ListMessageInfo));
        }, queueArg, countOption);

        return command;
    }
}
