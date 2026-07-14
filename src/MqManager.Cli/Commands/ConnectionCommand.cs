using System.CommandLine;
using System.Text.Json;
using MqManager.Core.Models;
using MqManager.Core.Serialization;
using MqManager.Core.Services;

namespace MqManager.Cli.Commands;

public static class ConnectionCommand
{
    public static Command Create(Func<MqConnectionSettings> getSettings)
    {
        var command = new Command("connection", "Application connection operations");

        command.AddCommand(CreateListCommand(getSettings));

        return command;
    }

    private static Command CreateListCommand(Func<MqConnectionSettings> getSettings)
    {
        var filterOption = new Option<string?>("--filter", "Filter by channel name (supports wildcards, client-side)");

        var command = new Command("list", "List application connections")
        {
            filterOption
        };

        command.SetHandler((filter) =>
        {
            var settings = getSettings();
            using var admin = new MqAdminService(settings);
            var connections = admin.ListApplicationConnections();

            if (!string.IsNullOrEmpty(filter))
            {
                var pattern = new System.Text.RegularExpressions.Regex(
                    "^" + System.Text.RegularExpressions.Regex.Escape(filter).Replace("\\*", ".*").Replace("\\?", ".") + "$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                connections = connections.Where(c => pattern.IsMatch(c.ChannelName)).ToList();
            }

            Console.WriteLine(JsonSerializer.Serialize(connections, DefaultJsonContext.Default.ListApplicationConnectionInfo));
        }, filterOption);

        return command;
    }
}
