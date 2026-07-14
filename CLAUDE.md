# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Structure

```
IbmMqCli/
├── src/IbmMqCli/              # Main CLI project
│   ├── Commands/              # CLI commands (System.CommandLine)
│   ├── Config/                # Configuration resolution
│   ├── Models/                # Data models (MqConnectionSettings)
│   ├── Serialization/         # JsonContexts.cs — source-gen contexts, camelCase output
│   ├── Services/              # MQ service layer (MqService, MqAdminService)
│   └── Program.cs             # Entry point
├── src/IbmMqCli.Mock/        # Mock project — drop-in mock with no real MQ dependency
│   ├── Commands/              # Same command structure, uses MockMqAdminService
│   ├── Config/                # Standalone ConfigLoader.cs (re-declared, not referenced from main)
│   ├── Models/                # Standalone MqConnectionSettings.cs (re-declared)
│   ├── Services/              # MockMqService, MockMqAdminService, MockDataStore
│   └── Program.cs             # Entry point (skips connection validation)
├── desktop/                   # Avalonia desktop app (IbmMqDesktop)
│   ├── i18n/                  # Multilingual strings
│   │   ├── Strings.axaml      # English resource dictionary (default)
│   │   ├── Strings.zh.axaml   # Chinese resource dictionary
│   │   ├── Strings.cs         # Static helper: Strings.Get(key), Strings.Format(key, args)
│   │   └── CultureHelper.cs   # Singleton, CultureChanged event, language switching
│   ├── Models/                # Desktop models with [JsonPropertyName] (camelCase)
│   │   ├── ChannelConnectionInfo.cs, ChannelInfo.cs, ChannelStatus.cs
│   │   ├── ConnectionInfo.cs, MessageInfo.cs, QueueInfo.cs, ServerConfig.cs
│   ├── Services/              # IMqService, MqService (CLI subprocess), JsonContexts.cs
│   │   └── AuditLogService.cs # AuditLogEntry record + AuditLogService singleton
│   ├── ViewModels/            # MVVM ViewModels (CommunityToolkit.Mvvm)
│   │   ├── ViewModelBase.cs   # Abstract base (ObservableObject)
│   │   ├── LoginViewModel.cs, ServerSettingsViewModel.cs, MainWindowViewModel.cs
│   │   ├── QueueListViewModel.cs, QueueActionsViewModel.cs, MessagePanelViewModel.cs
│   │   ├── ChannelListViewModel.cs, ChannelStatusViewModel.cs
│   │   ├── ConnectionListViewModel.cs, ConnectionDetailViewModel.cs
│   │   └── AuditLogViewModel.cs  # Audit log tab VM
│   ├── Views/                 # Avalonia AXAML views
│   │   ├── MainWindow.axaml   # Root window
│   │   ├── LoginView.axaml, ServerSettingsView.axaml
│   │   ├── QueueListView.axaml, MessagePanelView.axaml
│   │   ├── ChannelListView.axaml, ChannelStatusView.axaml
│   │   ├── ConnectionListView.axaml, ConnectionDetailView.axaml
│   │   └── AuditLogView.axaml    # Audit log DataGrid view (4th tab)
│   ├── Data/                  # Data persistence (pure ADO.NET / Microsoft.Data.Sqlite)
│   │   ├── AppDbContext.cs    # SqliteConnection wrapper, creates tables on Initialize()
│   │   ├── ConfigRepository.cs # CRUD for ServerConfig (GetAll/GetById/Save/Delete)
│   │   ├── CacheRepository.cs  # Queue name cache keyed by host:port:queueManager
│   │   └── PreferencesRepository.cs  # AppSettings key-value store (e.g. dark mode)
│   ├── Properties/PublishProfiles/  # Trim publish profiles
│   │   ├── win-x64.pubxml, linux-x64.pubxml, osx-x64.pubxml
│   ├── Converters/            # ValueConverters.cs
│   ├── App.axaml / App.axaml.cs
│   └── Program.cs
└── tests/IbmMqCli.Tests/     # Test project (xUnit + FluentAssertions)
```

## Architecture

Two-layer service design for IBM MQ operations:

- **`src/IbmMqCli/Services/MqService.cs`** — XMS API layer (`IBM.XMS`). Handles messaging: put, get, browse, purge. Uses `XMSFactoryFactory` → `IConnectionFactory` → `IConnection` → `ISession` → `IProducer`/`IConsumer`/`IBrowser`.
- **`src/IbmMqCli/Services/MqAdminService.cs`** — MQI + PCF layer (`IBM.WMQ`, `IBM.WMQ.PCF`). Handles admin ops: list queues, get depth, create/delete queues; list channels, get channel status, start/stop/reset channels. Uses `MQQueueManager` for MQI and `PCFMessageAgent` for PCF commands.

Both services take `MqConnectionSettings` and implement `IDisposable`. Each command creates its own service instance (no shared connection state).

## Mock Project

`src/IbmMqCli.Mock/` is a standalone .NET 9 console app that mirrors all CLI commands without requiring a real IBM MQ server. It has zero IBM MQ dependencies — `MockDataStore` holds in-memory queues and channels, and mock services operate on that data.

The Mock project re-declares its own model classes (`QueueInfo`, `ChannelInfo`, `ChannelStatus`, `ChannelConnectionInfo`, `ApplicationConnectionInfo`) in the same namespaces as the main project. Commands share the `IbmMqCli.Commands` namespace. Mock services live in `IbmMqCli.Mock.Services`. `Config/ConfigLoader.cs` and `Models/MqConnectionSettings.cs` are also re-declared as standalone copies.

Command factory signature differs from the main project: `Create(Func<MqConnectionSettings> getSettings, MockDataStore store)` — the extra `store` parameter is the shared in-memory data source.

All commands from the main project are covered: `connect`, `put`, `get`, `browse`, `list`, `queue` (depth/purge/create/delete), `channel` (list/status/conninfo/start/stop/reset), `connection list`.

Run with `dotnet run --project src/IbmMqCli.Mock -- <args>`.

## Configuration Priority

CLI flags (`--host`, `--port`, `--qm`, `--channel`, `--user`, `--password`) > environment variables (`MQ_HOSTNAME`, `MQ_PORT`, `MQ_QUEUEMANAGER`, `MQ_CHANNEL`, `MQ_USERNAME`, `MQ_PASSWORD`) > `appsettings.json` under `MqConnection` section.

Resolution happens in `src/IbmMqCli/Config/ConfigLoader.cs`. `Program.cs` uses a middleware to apply CLI overrides before command handlers run.

## Command Pattern

Each command is a static class in `src/IbmMqCli/Commands/` with a `Create(Func<MqConnectionSettings> getSettings)` factory method. The `getSettings` delegate resolves after CLI parsing via the middleware in `Program.cs`.

`System.CommandLine` beta4 is used — `Option<T>`, `Argument<T>`, `SetHandler`, `AddGlobalOption`.

## Key IBM.XMS / IBM.WMQ API Notes

- XMS message properties: use JMS-style names (`JMSMessageID`, `JMSCorrelationID`, `JMSTimestamp`) via `GetStringProperty`/`GetLongProperty` with try-catch (not all properties are available in all versions).
- MQI constants: all prefixed with `MQC.` (e.g., `MQC.MQOO_INQUIRE`, `MQC.MQCMD_CREATE_Q`, `MQC.MQIA_CURRENT_Q_DEPTH`). Queue/queue-manager attributes use `MQIA_` prefix; channel attributes use `MQIACH_` prefix; channel char attributes use `MQCACH_` prefix.
- `MQQueueManager` constructor uses `Hashtable` for connection properties, not positional args. Auth uses `MQC.USE_MQCSP_AUTHENTICATION_PROPERTY` = `"YES"` + `MQC.USER_ID_PROPERTY` + `MQC.PASSWORD_PROPERTY`.
- `MQManagedObject.Inquire(int[] selectors, int[] intAttrs, byte[] charAttrs)` — three-param signature, not single-param.
- PCF agent must be explicitly disconnected after use.

## NuGet Packages

**src/IbmMqCli:**
- `IBMXMSDotnetClient` 9.4.5.1 — bundles both `amqmxmsstd.dll` (XMS) and `amqmdnetstd.dll` (MQI+PCF)
- `System.CommandLine` 2.0.0-beta4.22272.1
- `Microsoft.Extensions.Configuration.Json` + `.EnvironmentVariables` + `.Binder` 9.0.0
- `TrimmerRootAssemblies`: `amqmdnetstd`, `amqmxmsstd` (prevents trim from removing IBM reflection-heavy assemblies)

**desktop/IbmMqDesktop:**
- `Avalonia` 11.3.7, `Avalonia.Desktop` 11.3.7, `Avalonia.Fonts.Inter` 11.3.7
- `Semi.Avalonia` 11.3.7 — replaces `Avalonia.Themes.Fluent`; provides `<semi:SemiTheme>` + light/dark theme variants
- `Semi.Avalonia.DataGrid` 11.3.7 — replaces `Avalonia.Controls.DataGrid`; styled DataGrid via `avares://Semi.Avalonia.DataGrid/Index.axaml`
- `Irihi.Ursa` 1.15.1 — Toast, Dialog overlays (1.x family for Avalonia 11.x)
- `Irihi.Ursa.Themes.Semi` 1.15.1 — Semi theme for Ursa; loaded via `StyleInclude` in `App.axaml.cs`
- `CommunityToolkit.Mvvm` 8.4.0
- `Microsoft.Data.Sqlite` 9.0.0 — raw ADO.NET only, no EF Core
- Trim settings: `PublishTrimmed=true`, `TrimMode=full`, `EnableTrimAnalyzer=true`
- `NoWarn`: `IL2104` (upstream `Avalonia.Controls.DataGrid` assembly — dependency of `Semi.Avalonia.DataGrid`)
- `TrimmerRootAssembly`: `Semi.Avalonia.DataGrid`
- `RuntimeIdentifiers`: `win-x64;linux-x64;osx-x64`
- `AvaloniaUseCompiledBindingsByDefault=true`

**Version matrix (must match):** Semi.Avalonia 11.x → Avalonia 11.x; Semi.Avalonia 12.x → Avalonia 12.x. Ursa 1.x → Avalonia 11.x; Ursa 2.x → Avalonia 12.x.

## Testing

Tests are in `tests/IbmMqCli.Tests/` using xUnit and FluentAssertions. Run with:

```bash
dotnet test tests/IbmMqCli.Tests
```

## Desktop App (Avalonia)

`desktop/` is a standalone .NET 9 Avalonia app. It spawns `IbmMqCli.exe` as a subprocess and parses its stdout JSON — no direct IBM MQ SDK dependency. Focus of active development; CLI is stable.

### Architecture

- **`Services/MqService.cs`** — implements `IMqService`. Wraps all CLI commands as async methods. `IsConnected` bool property tracks connection state.
- **`Services/AuditLogService.cs`** — singleton (`AuditLogService.Instance`). Holds `AuditLogEntry` records in-memory (session-only). All ViewModels call `AuditLogService.Instance.Log(category, operation, target, success, details)`. Fires `EntryAdded` event for UI updates.
- **`Services/JsonContexts.cs`** — `MqJsonContext` source-gen context for trimming-safe deserialization. No naming policy (relies on `[JsonPropertyName]` on each model).
- **`Models/`** — all properties carry explicit `[JsonPropertyName("camelCase")]` to match CLI's camelCase JSON output (`PropertyNamingPolicy = CamelCase` in `src/IbmMqCli/Serialization/JsonContexts.cs`).
- **`Data/AppDbContext.cs`** — `SqliteConnection` wrapper (pure ADO.NET). `Initialize()` creates three tables: `ServerConfigs`, `CacheQueues`, `AppSettings`. Default DB path: `%LOCALAPPDATA%/IbmMqDesktop/app.db`.
- **`Data/ConfigRepository.cs`** — CRUD for `ServerConfig` (GetAll, GetById, Save with upsert via `Id == 0`, Delete).
- **`Data/CacheRepository.cs`** — queue name cache keyed by `host:port:queueManager`. Methods: `GetCachedQueues`, `SaveQueues` (delete+insert), `ClearCache`. Stores names only, not depth.
- **`Data/PreferencesRepository.cs`** — `AppSettings` table CRUD. `Get(key)` / `Set(key, value)`. Used by `MainWindowViewModel` to persist the dark/light theme preference (`key="theme"`, value `"dark"` or `"light"`).

### Theme System

`App.axaml` uses `<semi:SemiTheme Locale="zh-CN"/>` (replaces `<FluentTheme/>`). Ursa themes loaded at runtime via `StyleInclude` in `App.axaml.cs:Initialize()`.

`Application.Resources` contains `ResourceDictionary.ThemeDictionaries` with `"Light"` and `"Dark"` variants. Custom brush keys (all prefixed `App*`):

| Key | Purpose |
|---|---|
| `AppHeaderBgBrush` | Top bar background |
| `AppTabBarBgBrush` | Tab bar background |
| `AppSidebarBgBrush` | Left sidebar background |
| `AppSidebarBorderBrush` | Sidebar/content divider |
| `AppContentBgBrush` | Right content area background |
| `AppBorderBrush` | General borders |
| `AppConsoleBgBrush` | Message log console background |
| `AppConsoleFgBrush` | Message log console text |
| `AppConsoleBorderBrush` | Message log console border |
| `AppSuccessBgBrush` | Success state background |
| `AppSuccessFgBrush` | Success state text |
| `AppSecondaryTextBrush` | Secondary/muted text |

Dark mode toggled via `Application.Current.RequestedThemeVariant = ThemeVariant.Dark/Light`. Persisted to `AppSettings` table. Restored on startup in `MainWindowViewModel` constructor.

**Do NOT use `DisplayMemberBinding`** in any AXAML — it is always reflection-based and breaks trim. Always use an explicit `DataTemplate` with `x:DataType`.

`App.axaml.cs:OnLanguageChanged()` uses `[UnconditionalSuppressMessage("Trimming", "IL2026")]` — runtime `ResourceInclude` for i18n is intentional and safe.

### CLI Discovery Order (MqService)

`MqService` searches for `IbmMqCli.exe` in this order:
1. App directory (published scenario)
2. `src/IbmMqCli/bin/Debug/net9.0/` (framework-dependent debug)
3. `bin/Release/net9.0/<rid>/` (self-contained release)
4. `bin/Release/net9.0/` (framework-dependent release)
5. `bin/Development/net9.0/<rid>/` (self-contained development)
6. `CLI_PATH` environment variable

Subprocess timeout is 30,000 ms.

### IMqService Methods

All methods map directly to CLI subcommands:

| Method | CLI command |
|---|---|
| `Connect(ServerConfig)` | `connect` + connection args |
| `Disconnect()` | clears `_config` only (no CLI call) |
| `ListQueues(filter?)` | `list [--filter F]` |
| `GetQueueDepth(name)` | `queue depth <name>` |
| `CreateQueue(name, type)` | `queue create <name> --type T` |
| `DeleteQueue(name, force)` | `queue delete <name> [--force]` |
| `PurgeQueue(name, force)` | `queue purge <name> [--force]` |
| `PutMessage(queue, text)` | `put <queue> <text>` |
| `PutMessageFromFile(queue, path)` | `put <queue> -f <path>` |
| `GetMessages(queue, count)` | `get <queue> [--count N]` |
| `BrowseMessages(queue, count)` | `browse <queue> [--count N]` |
| `ListChannels(filter?)` | `channel list [--filter F]` |
| `GetChannelStatus(name)` | `channel status <name>` |
| `GetChannelConnInfo(name)` | `channel conninfo <name>` |
| `StartChannel(name)` | `channel start <name>` (no force flag) |
| `StopChannel(name, force)` | `channel stop <name> [--force]` |
| `ResetChannel(name, force)` | `channel reset <name> [--force]` |
| `ListConnections(filter?)` | `connection list [--filter F]` |

Note: `StartChannel` has no force parameter — IBM MQ `START CHANNEL` does not support force. Only stop and reset support force.

### ViewModels

All ViewModels inherit `ViewModelBase : ObservableObject`. All subscribe to `CultureHelper.Instance.CultureChanged` and call `UpdateStrings()`. Views are instantiated directly in `MainWindowViewModel` (not via DI or Avalonia view locator) — `SidebarContent` and `ContentPanel` hold typed `Control` references.

**All view instances are cached** in `MainWindowViewModel` private fields (e.g., `_queueListView`, `_channelStatusView`) and created lazily via `??=`. This prevents re-render flash on tab switch.

**All VM operations log to `AuditLogService.Instance.Log(...)`** — every queue, message, channel, connection, and system operation (connect/disconnect) is recorded.

### Navigation state model (`MainWindowViewModel`)

Three private arrays drive the navigation state:

| Field | Type | Purpose |
|---|---|---|
| `_tabContent[4]` | `Control?[]` | Remembers last `ContentPanel` per tab index |
| `_tabInitialized[4]` | `bool[]` | Load-once guard — data is fetched only on first tab visit |
| `_previousTabIndex` | `int` | Tracks outgoing tab so its content is saved before switching |

**`OnActiveTabIndexChanged(int value)`** flow:
1. Guard: `if (!IsConnected) return`
2. Save `ContentPanel` to `_tabContent[_previousTabIndex]`
3. Update `_previousTabIndex = value`
4. Tab 3 (Audit): clear sidebar, show full-width `AuditLogView`, return early
5. Call `LoadSidebarCommand` (uses `??=` cached sidebar views)
6. Restore `ContentPanel = _tabContent[value]` (null = nothing selected yet)
7. If `!_tabInitialized[value]`: set flag, fire the appropriate load command

**`QueueSelected` / `ChannelSelected` / `ConnectionSelected`**: after setting `ContentPanel`, write to `_tabContent[0/1/2]` so the content survives tab switches.

**`OnConnected`**: calls `Array.Clear` on `_tabContent` and `_tabInitialized`, resets `_previousTabIndex = -1`, then bootstraps tab 0 directly — sets `ActiveTabIndex = 0`, marks `_tabInitialized[0] = true`, wires `SidebarContent = _queueListView ??= ...`, sets `ContentPanel = _messagePanelView ??= ...`, seeds `_tabContent[0] = _messagePanelView`, calls `QueueListVm.LoadQueuesCommand.Execute(null)`. This bypasses `OnActiveTabIndexChanged` so no double-load occurs on connect.

**`Disconnect`**: calls `Array.Clear` on `_tabContent` and `_tabInitialized`, resets `_previousTabIndex = -1`, sets `ActiveTabIndex = 0`, clears `SidebarContent` and `ContentPanel`.

Key behavioral notes:
- **`MainWindowViewModel`** — 4 tabs: Queues (0), Channels (1), Connections (2), Log (3). Tab 3 sets `SidebarContent = null` (full-width audit log). Dark mode toggle via `ToggleThemeCommand`. Reads saved theme from `PreferencesRepository` on startup.
- **`QueueListViewModel`** — client-side glob filter wraps bare text with `*...*` for fuzzy matching. Uses `CacheRepository` to show cached queue names immediately, then refreshes from CLI.
- **`QueueActionsViewModel`** — `StatusMessage` observable property shows create/delete error feedback. `SetRefreshAction(Action)` wires the refresh callback.
- **`MessagePanelViewModel`** — three send modes: text (0), file (1), folder (2). Folder mode iterates all files and puts each as a message. `ConsumeAllAsync` drains in 100-message batches until queue is empty.
- **`ChannelStatusViewModel`** — two-tab panel (Status + ConnInfo); ConnInfo loaded lazily on tab switch. `IsForceStop` and `IsForceReset` toggle force flags; no `IsForceStart` (StartChannel has no force).
- **`ConnectionListViewModel`** — `ExportCsvAsync` uses `StorageProvider.SaveFilePickerAsync` for user-chosen path; exports `_allConnections` (full list, not filtered view).
- **`AuditLogViewModel`** — subscribes to `AuditLogService.Instance.EntryAdded`; inserts new entries at index 0 (newest-first) via `Dispatcher.UIThread.Post`. `SaveToFileCommand` uses `StorageProvider` to write CSV. `ClearLogCommand` clears the collection.
- **`LoginViewModel`** — `ToggleLanguage` command switches between English and Chinese. Tracks `StatusType` enum (`None`, `PleaseSelect`, `Connecting`, `ConnectedTo`) + `_connectedServerName` so `UpdateStrings()` re-translates `StatusMessage` when language changes.

### Audit Log

`AuditLogEntry` is a `sealed record` with fields: `Timestamp`, `Category`, `Operation`, `Target`, `Success`, `Details`.

Categories used across VMs:
- `"Queue"` — Create, Delete, Purge
- `"Message"` — Put, Browse, Get, Purge, SaveLog
- `"Channel"` — Start, Stop, Reset, Refresh
- `"Connection"` — ExportCSV
- `"System"` — Connect, Disconnect, SaveLog (from AuditLogViewModel)

`AuditLogView` uses `Semi.Avalonia.DataGrid` with 6 columns. Column `Header` bindings use `{Binding #Root.((vm:AuditLogViewModel)DataContext).ColXxx}` pattern since DataGrid column headers cannot use standard compiled bindings.

The audit log is session-only (in-memory). It is NOT persisted to SQLite.

### i18n System

All UI strings live in `desktop/i18n/`:

- `Strings.axaml` — English resource dictionary (default, loaded in `App.axaml` via `ResourceInclude`)
- `Strings.zh.axaml` — Chinese resource dictionary (same key set)
- `Strings.cs` — `Strings.Get(key)` reads from `Application.Current.Resources`; `Strings.Format(key, args)` wraps `string.Format`
- `CultureHelper.cs` — singleton; call `CultureHelper.Instance.SetCulture(culture)` to switch language. Sets both `DefaultThreadCurrentCulture`/`DefaultThreadCurrentUICulture` (for new threads) and `Thread.CurrentThread.CurrentCulture`/`CurrentUICulture` (for the already-running UI thread) so that `Strings.Get` resolves the correct culture immediately.

Language switching in `App.axaml.cs` (`OnLanguageChanged`): clears `Resources.MergedDictionaries` and adds a new `ResourceInclude` pointing to the appropriate `.axaml` file. Requires `using Avalonia.Markup.Xaml.Styling` for `ResourceInclude`.

**Adding a new string:** add the key/value to both `Strings.axaml` and `Strings.zh.axaml`, then call `Strings.Get("Your_Key")` in the ViewModel's `UpdateStrings()`.

Audit log i18n keys: `Tab_AuditLog`, `AuditLog_Title`, `AuditLog_Save`, `AuditLog_Clear`, `AuditLog_ColTime`, `AuditLog_ColCategory`, `AuditLog_ColOperation`, `AuditLog_ColTarget`, `AuditLog_ColSuccess`, `AuditLog_ColDetails`.

### CLI JSON Output

CLI (`src/IbmMqCli/Serialization/JsonContexts.cs`) uses `PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase` on `DefaultJsonContext`. All desktop `[JsonPropertyName]` attributes must use camelCase (e.g., `"channelName"`, `"mcaUserId"`).

### Publishing (Trim)

Three publish profiles in `desktop/Properties/PublishProfiles/`:
- `win-x64.pubxml`, `linux-x64.pubxml`, `osx-x64.pubxml`
- All: `SelfContained=true`, `PublishTrimmed=true`, `PublishSingleFile=true`

Run: `dotnet publish desktop/IbmMqDesktop.csproj /p:PublishProfile=win-x64`

Known suppressed trim warnings (by design):
- `IL2026` on `App.axaml.cs:Initialize()` — `AvaloniaXamlLoader.Load(this)`
- `IL2026` on `App.axaml.cs:OnLanguageChanged()` — runtime `ResourceInclude` for i18n
- `IL2104` (`NoWarn`) — `Avalonia.Controls.DataGrid` upstream assembly (dependency of `Semi.Avalonia.DataGrid`)

### Running

```bash
dotnet run --project desktop
```
