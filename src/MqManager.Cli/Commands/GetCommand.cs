using System.CommandLine;
using System.Text.Json;
using MqManager.Core.Models;
using MqManager.Core.Serialization;
using MqManager.Core.Services;

namespace MqManager.Cli.Commands;

public static class GetCommand
{
    public static Command Create(Func<MqConnectionSettings> getSettings)
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
            using var mq = new MqService(settings);
            mq.Connect();

            var messages = mq.GetMessages(queue, count);
            Console.WriteLine(JsonSerializer.Serialize(messages, DefaultJsonContext.Default.ListMessageInfo));
        }, queueArg, countOption);

        return command;
    }
}
