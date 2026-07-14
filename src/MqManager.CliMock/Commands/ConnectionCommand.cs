using System.CommandLine;
using System.Text.Json;
using MqManager.CliMock.Services;
using MqManager.CliMock.Models;

namespace MqManager.CliMock.Commands;

public static class ConnectionCommand
{
    public static Command Create(Func<MqConnectionSettings> getSettings, MockDataStore store)
    {
        var command = new Command("connection", "Application connection operations");

        command.AddCommand(CreateListCommand(getSettings, store));

        return command;
    }

    private static Command CreateListCommand(Func<MqConnectionSettings> getSettings, MockDataStore store)
    {
        var filterOption = new Option<string?>("--filter", "Filter by channel name (supports wildcards, client-side)");

        var command = new Command("list", "List application connections")
        {
            filterOption
        };

        command.SetHandler((filter) =>
        {
            var settings = getSettings();
            using var admin = new MockMqAdminService(settings, store);
            var connections = admin.ListApplicationConnections();

            if (!string.IsNullOrEmpty(filter))
            {
                var pattern = new System.Text.RegularExpressions.Regex(
                    "^" + System.Text.RegularExpressions.Regex.Escape(filter).Replace("\\*", ".*").Replace("\\?", ".") + "$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                connections = connections.Where(c => pattern.IsMatch(c.ChannelName)).ToList();
            }

            Console.WriteLine(JsonSerializer.Serialize(connections, new JsonSerializerOptions { WriteIndented = true }));
        }, filterOption);

        return command;
    }
}
