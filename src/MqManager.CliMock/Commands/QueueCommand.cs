using System.CommandLine;
using MqManager.CliMock.Services;
using MqManager.CliMock.Models;

namespace MqManager.CliMock.Commands;

public static class QueueCommand
{
    public static Command Create(Func<MqConnectionSettings> getSettings, MockDataStore store)
    {
        var command = new Command("queue", "Queue management operations");

        command.AddCommand(CreateDepthCommand(getSettings, store));
        command.AddCommand(CreatePurgeCommand(getSettings, store));
        command.AddCommand(CreateCreateCommand(getSettings, store));
        command.AddCommand(CreateDeleteCommand(getSettings, store));

        return command;
    }

    private static Command CreateDepthCommand(Func<MqConnectionSettings> getSettings, MockDataStore store)
    {
        var queueArg = new Argument<string>("queue", "Queue name");

        var command = new Command("depth", "Get the current depth (message count) of a queue")
        {
            queueArg
        };

        command.SetHandler((queue) =>
        {
            var settings = getSettings();
            using var admin = new MockMqAdminService(settings, store);
            var depth = admin.GetQueueDepth(queue);
            Console.WriteLine(depth);
        }, queueArg);

        return command;
    }

    private static Command CreatePurgeCommand(Func<MqConnectionSettings> getSettings, MockDataStore store)
    {
        var queueArg = new Argument<string>("queue", "Queue name");
        var forceOption = new Option<bool>("--force", "Skip confirmation prompt");

        var command = new Command("purge", "Remove all messages from a queue")
        {
            queueArg, forceOption
        };

        command.SetHandler((queue, force) =>
        {
            if (!force)
            {
                Console.Write($"Are you sure you want to purge all messages from '{queue}'? [y/N] ");
                var response = Console.ReadLine();
                if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Cancelled.");
                    return;
                }
            }

            var settings = getSettings();
            using var mq = new MockMqService(settings, store);
            mq.Connect();

            Console.WriteLine($"Purging messages from '{queue}'...");
            var purged = mq.PurgeQueue(queue);
            Console.WriteLine($"Purged {purged} message(s).");
        }, queueArg, forceOption);

        return command;
    }

    private static Command CreateCreateCommand(Func<MqConnectionSettings> getSettings, MockDataStore store)
    {
        var queueArg = new Argument<string>("queue", "Queue name to create");
        var typeOption = new Option<string>("--type", () => "local", "Queue type: local, alias, model");

        var command = new Command("create", "Create a new queue")
        {
            queueArg, typeOption
        };

        command.SetHandler((queue, type) =>
        {
            var settings = getSettings();
            using var admin = new MockMqAdminService(settings, store);
            admin.CreateQueue(queue, type);
            Console.WriteLine($"Queue '{queue}' created (type: {type}).");
        }, queueArg, typeOption);

        return command;
    }

    private static Command CreateDeleteCommand(Func<MqConnectionSettings> getSettings, MockDataStore store)
    {
        var queueArg = new Argument<string>("queue", "Queue name to delete");
        var forceOption = new Option<bool>("--force", "Skip confirmation prompt");

        var command = new Command("delete", "Delete a queue")
        {
            queueArg, forceOption
        };

        command.SetHandler((queue, force) =>
        {
            if (!force)
            {
                Console.Write($"Are you sure you want to delete queue '{queue}'? [y/N] ");
                var response = Console.ReadLine();
                if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Cancelled.");
                    return;
                }
            }

            var settings = getSettings();
            using var admin = new MockMqAdminService(settings, store);
            admin.DeleteQueue(queue);
            Console.WriteLine($"Queue '{queue}' deleted.");
        }, queueArg, forceOption);

        return command;
    }
}
