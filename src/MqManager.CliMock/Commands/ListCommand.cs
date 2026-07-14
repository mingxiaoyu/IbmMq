using System.CommandLine;
using System.Text.Json;
using MqManager.CliMock.Services;
using MqManager.CliMock.Models;

namespace MqManager.CliMock.Commands;

public static class ListCommand
{
    public static Command Create(Func<MqConnectionSettings> getSettings, MockDataStore store)
    {
        var filterOption = new Option<string>("--filter", () => "*", "Queue name filter pattern (e.g. 'TEST.*')");
        var depthOption = new Option<bool>("--depth", "Include queue depth in output (slower for large result sets)");
        var typeOption = new Option<string?>("--type", "Filter by queue type: Local, Alias, Model");
        var maxOption = new Option<int?>("--max", "Maximum number of queues to return");

        var command = new Command("list", "List all queues on the Queue Manager (JSON output)")
        {
            filterOption, depthOption, typeOption, maxOption
        };

        command.SetHandler((filter, depth, type, max) =>
        {
            var settings = getSettings();
            using var admin = new MockMqAdminService(settings, store);

            var queues = admin.ListQueues(filter, includeDepth: depth, typeFilter: type, maxResults: max);

            var json = JsonSerializer.Serialize(queues, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Console.WriteLine(json);
        }, filterOption, depthOption, typeOption, maxOption);

        return command;
    }
}
