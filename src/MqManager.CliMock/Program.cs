using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Text;
using MqManager.CliMock.Commands;
using MqManager.CliMock.Config;
using MqManager.CliMock.Models;
using MqManager.CliMock.Services;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// Shared in-memory data store (seeded with defaults)
var store = new MockDataStore();
store.SeedDefaults();

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

var rootCommand = new RootCommand("IBM MQ Mock CLI - Drop-in mock for IBM MQ CLI (no real server needed)");

// Add global options
rootCommand.AddGlobalOption(hostOption);
rootCommand.AddGlobalOption(portOption);
rootCommand.AddGlobalOption(qmOption);
rootCommand.AddGlobalOption(channelOption);
rootCommand.AddGlobalOption(userOption);
rootCommand.AddGlobalOption(passwordOption);

// Register subcommands
rootCommand.AddCommand(ConnectCommand.Create(GetSettings, store));
rootCommand.AddCommand(PutCommand.Create(GetSettings, store));
rootCommand.AddCommand(GetCommand.Create(GetSettings, store));
rootCommand.AddCommand(BrowseCommand.Create(GetSettings, store));
rootCommand.AddCommand(ListCommand.Create(GetSettings, store));
rootCommand.AddCommand(QueueCommand.Create(GetSettings, store));
rootCommand.AddCommand(ChannelCommand.Create(GetSettings, store));
rootCommand.AddCommand(ConnectionCommand.Create(GetSettings, store));

// Build command line pipeline
var builder = new CommandLineBuilder(rootCommand)
    .UseDefaults()
    .AddMiddleware(async (context, next) =>
    {
        // Resolve settings with CLI overrides before any command handler runs
        // No validation - mock works with zero args
        pendingSettings = GetSettingsWithOverrides(context.ParseResult);
        await next(context);
    });

return await builder.Build().InvokeAsync(args);
