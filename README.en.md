# MqManager

[中文](README.md)

IBM MQ command-line tool and desktop GUI built on the XMS .NET Client for everyday Queue Manager administration.

## Project Structure

```
MqManager.sln
├── src/MqManager.Core/       # Shared MQ service layer (XMS + MQI/PCF)
├── src/MqManager.Cli/        # CLI tool → mqmanager
├── src/MqManager.CliMock/    # Mock CLI (no real MQ server needed)
├── src/MqManager.Desktop/    # Avalonia desktop GUI → MqManagerDesktop
└── tests/MqManager.Tests/    # Integration tests
```

## CLI Commands

| Command | Description |
|---------|-------------|
| `connect` | Test connection to Queue Manager |
| `put <queue> <message>` | Send a message (`-f <file>` from file, `--raw` from stdin) |
| `get <queue> [--count N]` | Receive and consume messages |
| `browse <queue> [--count N]` | Browse messages (non-destructive) |
| `list [--filter "PAT*"] [--depth] [--type Local\|Alias\|Model] [--max N]` | List queues (JSON output) |
| `queue depth <queue>` | Get queue depth |
| `queue create <queue> [--type local]` | Create a queue |
| `queue delete <queue> [--force]` | Delete a queue |
| `queue purge <queue> [--force]` | Purge a queue |
| `channel list [--filter "PAT*"]` | List channels |
| `channel status <channel>` | Get channel status |
| `channel start <channel>` | Start a channel |
| `channel stop <channel> [--force]` | Stop a channel |
| `channel reset <channel> [--force]` | Reset a channel |
| `channel conninfo <channel>` | Get channel connection info |
| `connection list [--filter "PAT*"]` | List application connections (SVRCONN) |

## Quick Start

Requires .NET 9 SDK.

```bash
dotnet build
```

### Run CLI

```bash
dotnet run --project src/MqManager.Cli -- connect
dotnet run --project src/MqManager.Cli -- put TEST.QUEUE "hello world"
dotnet run --project src/MqManager.Cli -- list --filter "APP.*"
```

### Run Desktop GUI

```bash
dotnet run --project src/MqManager.Desktop
```

## Configuration

Connection settings via `appsettings.json`:

```json
{
  "MqConnection": {
    "HostName": "your-mq-host.example.com",
    "Port": 1414,
    "QueueManager": "QM1",
    "Channel": "DEV.APP.SVRCONN",
    "Username": "app",
    "Password": ""
  }
}
```

Override with environment variables: `MQ_HOSTNAME`, `MQ_PORT`, `MQ_QUEUEMANAGER`, `MQ_CHANNEL`, `MQ_USERNAME`, `MQ_PASSWORD`.

Or pass CLI flags: `--host`, `--port`, `--qm`, `--channel`, `--user`, `--password`.

Priority: CLI flags > environment variables > appsettings.json

## Sending XML/JSON Messages

Use `--raw` from stdin when messages contain special characters:

```bash
# From file
dotnet run --project src/MqManager.Cli -- put TEST.QUEUE -f message.xml

# Via pipe
echo "<root>hello</root>" | dotnet run --project src/MqManager.Cli -- put TEST.QUEUE --raw
```

## Mock Mode

Experience all commands without a real IBM MQ server. Zero external dependencies.

```bash
dotnet run --project src/MqManager.CliMock -- connect
dotnet run --project src/MqManager.CliMock -- list
dotnet run --project src/MqManager.CliMock -- channel conninfo APP.SVRCONN
```

## Multi-Platform Build

```bash
# CLI — all platforms
.\build-cli.ps1

# Desktop — all platforms
.\build-desktop.ps1

# Specific RIDs
.\build-cli.ps1 -Runtimes "win-x64,linux-x64"
.\build-desktop.ps1 -Runtimes "win-x64"
```

Output: `publish/` (CLI), `publish-desktop/` (Desktop).

## Performance

The `list` command uses a two-tier PCF query strategy:

| Scenario | PCF Command | Returns |
|----------|-------------|---------|
| Default (no `--depth`, no `--type`) | `MQCMD_INQUIRE_Q_NAMES` | Queue names only |
| With `--depth` or `--type` | `MQCMD_INQUIRE_Q` + `MQIACF_Q_ATTRS` | Name + type + depth (on demand) |

## Dependencies

- [IBMXMSDotnetClient](https://www.nuget.org/packages/IBMXMSDotnetClient) — IBM MQ XMS .NET client
- [System.CommandLine](https://www.nuget.org/packages/System.CommandLine) — CLI framework
- Microsoft.Extensions.Configuration — Configuration
- [Avalonia](https://github.com/AvaloniaUI/Avalonia) — Cross-platform desktop GUI
