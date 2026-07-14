using System.CommandLine;
using MqManager.CliMock.Services;
using MqManager.CliMock.Models;

namespace MqManager.CliMock.Commands;

public static class PutCommand
{
    public static Command Create(Func<MqConnectionSettings> getSettings, MockDataStore store)
    {
        var queueArg = new Argument<string>("queue", "Target queue name");
        var messageArg = new Argument<string>("message", () => "", "Message body text");
        var fileOption = new Option<FileInfo?>("-f", "Read message body from file");
        var rawOption = new Option<bool>("--raw", "Read message from stdin (pipe-safe, avoids shell escaping)");
        rawOption.AddAlias("-r");

        var command = new Command("put", "Send a message to a queue")
        {
            queueArg, messageArg, fileOption, rawOption
        };

        command.SetHandler((queue, message, file, raw) =>
        {
            var settings = getSettings();

            string body;
            if (file != null)
            {
                body = File.ReadAllText(file.FullName);
                Console.WriteLine($"Reading message from file: {file.FullName}");
            }
            else if (raw)
            {
                body = Console.In.ReadToEnd();
            }
            else if (!string.IsNullOrEmpty(message))
            {
                body = message;
            }
            else
            {
                Console.WriteLine("Enter message body (Ctrl+Z on Windows / Ctrl+D on Unix to finish):");
                body = Console.In.ReadToEnd();
            }

            if (string.IsNullOrEmpty(body))
            {
                Console.Error.WriteLine("Error: No message body provided.");
                return;
            }

            using var mq = new MockMqService(settings, store);
            mq.Connect();
            mq.PutMessage(queue, body);
            Console.WriteLine($"Message sent to queue '{queue}' ({body.Length} chars)");
        }, queueArg, messageArg, fileOption, rawOption);

        return command;
    }
}
