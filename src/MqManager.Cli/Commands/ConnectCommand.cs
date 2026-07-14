using System.CommandLine;
using MqManager.Core.Models;
using MqManager.Core.Services;

namespace MqManager.Cli.Commands;

public static class ConnectCommand
{
    public static Command Create(Func<MqConnectionSettings> getSettings)
    {
        var command = new Command("connect", "Test connection to Queue Manager");

        command.SetHandler(() =>
        {
            var settings = getSettings();
            Console.WriteLine($"Connecting to {settings.HostName}:{settings.Port} QM={settings.QueueManager} Channel={settings.Channel}...");

            using var mq = new MqService(settings);
            try
            {
                mq.Connect();
                var qmName = mq.GetConnectedQueueManager();
                Console.WriteLine($"Connected successfully to Queue Manager: {qmName}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Connection failed: {ex.Message}");
                Console.ResetColor();
                throw;
            }
        });

        return command;
    }
}
