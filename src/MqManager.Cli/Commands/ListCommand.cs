using System.CommandLine;
using System.Text.Json;
using MqManager.Core.Models;
using MqManager.Core.Serialization;
using MqManager.Core.Services;

namespace MqManager.Cli.Commands;

public static class ListCommand
{
    public static Command Create(Func<MqConnectionSettings> getSettings)
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
            try
            {
                using var admin = new MqAdminService(settings);
                var queues = admin.ListQueues(filter, includeDepth: depth, typeFilter: type, maxResults: max);

                var json = JsonSerializer.Serialize(queues, CamelCaseJsonContext.Default.ListQueueInfo);

                Console.WriteLine(json);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex);
                Console.ResetColor();
            }
        }, filterOption, depthOption, typeOption, maxOption);

        return command;
    }
}
