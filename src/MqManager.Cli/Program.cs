using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Text;
using MqManager.Cli.Commands;
using MqManager.Core.Config;
using MqManager.Core.Models;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// Global options for MQ connection overrides
var hostOption = new Option<string?>("--host", "MQ host name (overrides config)");
var portOption = new Option<int?>("--port", "MQ port (overrides config)");
var qmOption = new Option<string?>("--qm", "Queue Manager name (overrides config)");
var channelOption = new Option<string?>("--channel", "MQ channel name (overrides config)");
var userOption = new Option<string?>("--user", "MQ username (overrides config)");
var passwordOption = new Option<string?>("--password", "MQ password (overrides config)");

// Load base settings from config file + environment variables
var baseSettings = ConfigLoader.Load();

// Factory that applies CLI overrides on top of base settings
MqConnectionSettings GetSettingsWithOverrides(ParseResult parseResult)
{
    return ConfigLoader.ApplyOverrides(
        baseSettings,
        parseResult.GetValueForOption(hostOption),
        parseResult.GetValueForOption(portOption),
        parseResult.GetValueForOption(qmOption),
        parseResult.GetValueForOption(channelOption),
        parseResult.GetValueForOption(userOption),
        parseResult.GetValueForOption(passwordOption));
}

// Deferred settings factory - will be resolved after parsing
MqConnectionSettings? pendingSettings = null;
MqConnectionSettings GetSettings() => pendingSettings!;

var rootCommand = new RootCommand("IBM MQ CLI - Command line tool for IBM MQ administration");

// Add global options
rootCommand.AddGlobalOption(hostOption);
rootCommand.AddGlobalOption(portOption);
rootCommand.AddGlobalOption(qmOption);
rootCommand.AddGlobalOption(channelOption);
rootCommand.AddGlobalOption(userOption);
rootCommand.AddGlobalOption(passwordOption);

// Register subcommands
rootCommand.AddCommand(ConnectCommand.Create(GetSettings));
rootCommand.AddCommand(PutCommand.Create(GetSettings));
rootCommand.AddCommand(GetCommand.Create(GetSettings));
rootCommand.AddCommand(BrowseCommand.Create(GetSettings));
rootCommand.AddCommand(ListCommand.Create(GetSettings));
rootCommand.AddCommand(QueueCommand.Create(GetSettings));
rootCommand.AddCommand(ChannelCommand.Create(GetSettings));
rootCommand.AddCommand(ConnectionCommand.Create(GetSettings));

// Build command line pipeline
var builder = new CommandLineBuilder(rootCommand)
    .UseDefaults()
    .AddMiddleware(async (context, next) =>
    {
        // Resolve settings with CLI overrides before any command handler runs
        pendingSettings = GetSettingsWithOverrides(context.ParseResult);

        // Validate required connection settings
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(pendingSettings.HostName) || pendingSettings.HostName == "localhost")
            missing.Add("--host");
        if (string.IsNullOrWhiteSpace(pendingSettings.QueueManager))
            missing.Add("--qm");
        if (string.IsNullOrWhiteSpace(pendingSettings.Channel))
            missing.Add("--channel");

        if (missing.Count > 0)
        {
            Console.Error.WriteLine($"Error: Missing required connection settings: {string.Join(", ", missing)}");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Provide them via CLI flags or appsettings.json:");
            Console.Error.WriteLine("  MqManager.Cli.exe --host <hostname> --port <port> --qm <qmgr> --channel <channel> <command>");
            context.ExitCode = 1;
            return; // short-circuit — don't run the command handler
        }

        await next(context);
    });

return await builder.Build().InvokeAsync(args);
