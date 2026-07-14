using System.CommandLine;
using System.Text.Json;
using MqManager.Core.Models;
using MqManager.Core.Serialization;
using MqManager.Core.Services;

namespace MqManager.Cli.Commands;

public static class ChannelCommand
{
    public static Command Create(Func<MqConnectionSettings> getSettings)
    {
        var command = new Command("channel", "Channel management operations");

        command.AddCommand(CreateListCommand(getSettings));
        command.AddCommand(CreateStatusCommand(getSettings));
        command.AddCommand(CreateConnInfoCommand(getSettings));
        command.AddCommand(CreateStartCommand(getSettings));
        command.AddCommand(CreateStopCommand(getSettings));
        command.AddCommand(CreateResetCommand(getSettings));

        return command;
    }

    private static Command CreateListCommand(Func<MqConnectionSettings> getSettings)
    {
        var filterOption = new Option<string>("--filter", () => "*", "Channel name filter (supports wildcards)");

        var command = new Command("list", "List channels")
        {
            filterOption
        };

        command.SetHandler((filter) =>
        {
            var settings = getSettings();
            using var admin = new MqAdminService(settings);
            var channels = admin.ListChannels(filter);
            Console.WriteLine(JsonSerializer.Serialize(channels, DefaultJsonContext.Default.ListChannelInfo));
        }, filterOption);

        return command;
    }

    private static Command CreateStatusCommand(Func<MqConnectionSettings> getSettings)
    {
        var channelArg = new Argument<string>("channel", "Channel name");

        var command = new Command("status", "Get the status of a channel")
        {
            channelArg
        };

        command.SetHandler((channel) =>
        {
            var settings = getSettings();
            using var admin = new MqAdminService(settings);
            var status = admin.GetChannelStatus(channel);
            Console.WriteLine(JsonSerializer.Serialize(status, DefaultJsonContext.Default.ChannelStatus));
        }, channelArg);

        return command;
    }

    private static Command CreateConnInfoCommand(Func<MqConnectionSettings> getSettings)
    {
        var channelArg = new Argument<string>("channel", "Channel name");

        var command = new Command("conninfo", "Get connection info for a channel")
        {
            channelArg
        };

        command.SetHandler((channel) =>
        {
            var settings = getSettings();
            using var admin = new MqAdminService(settings);
            var info = admin.GetChannelConnectionInfo(channel);
            Console.WriteLine(JsonSerializer.Serialize(info, DefaultJsonContext.Default.ListChannelConnectionInfo));
        }, channelArg);

        return command;
    }

    private static Command CreateStartCommand(Func<MqConnectionSettings> getSettings)
    {
        var channelArg = new Argument<string>("channel", "Channel name to start");

        var command = new Command("start", "Start a channel")
        {
            channelArg
        };

        command.SetHandler((channel) =>
        {
            var settings = getSettings();
            using var admin = new MqAdminService(settings);
            admin.StartChannel(channel);
            Console.WriteLine($"Channel '{channel}' start request sent.");
        }, channelArg);

        return command;
    }

    private static Command CreateStopCommand(Func<MqConnectionSettings> getSettings)
    {
        var channelArg = new Argument<string>("channel", "Channel name to stop");
        var forceOption = new Option<bool>("--force", "Skip confirmation prompt");

        var command = new Command("stop", "Stop a channel")
        {
            channelArg, forceOption
        };

        command.SetHandler((channel, force) =>
        {
            if (!force)
            {
                Console.Write($"Are you sure you want to stop channel '{channel}'? [y/N] ");
                var response = Console.ReadLine();
                if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Cancelled.");
                    return;
                }
            }

            var settings = getSettings();
            using var admin = new MqAdminService(settings);
            admin.StopChannel(channel);
            Console.WriteLine($"Channel '{channel}' stop request sent.");
        }, channelArg, forceOption);

        return command;
    }

    private static Command CreateResetCommand(Func<MqConnectionSettings> getSettings)
    {
        var channelArg = new Argument<string>("channel", "Channel name to reset");
        var forceOption = new Option<bool>("--force", "Skip confirmation prompt");

        var command = new Command("reset", "Reset a channel")
        {
            channelArg, forceOption
        };

        command.SetHandler((channel, force) =>
        {
            if (!force)
            {
                Console.Write($"Are you sure you want to reset channel '{channel}'? [y/N] ");
                var response = Console.ReadLine();
                if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Cancelled.");
                    return;
                }
            }

            var settings = getSettings();
            using var admin = new MqAdminService(settings);
            admin.ResetChannel(channel);
            Console.WriteLine($"Channel '{channel}' reset.");
        }, channelArg, forceOption);

        return command;
    }
}
