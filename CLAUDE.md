# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Structure

```
src/
├── MqManager.Core/            # Shared core library — IBM MQ SDK, models, services, config
│   ├── Config/                # ConfigLoader.cs — config resolution
│   ├── Models/                # Shared data models (MqConnectionSettings, QueueInfo, ChannelInfo, …)
│   ├── Serialization/         # JsonContexts.cs — source-gen contexts, camelCase output
│   └── Services/              # MqService (XMS), MqAdminService (MQI + PCF)
├── MqManager.Cli/             # CLI entry point (no IBM MQ packages — uses Core)
│   ├── Commands/              # CLI commands (System.CommandLine)
│   └── Program.cs             # Entry point; output exe name: mqmanager
├── MqManager.CliMock/         # Mock CLI — standalone, no real MQ dependency
│   ├── Commands/              # Same command structure, uses MockMqAdminService
│   ├── Config/                # Standalone ConfigLoader.cs (re-declared)
│   ├── Models/                # Standalone MqConnectionSettings.cs (re-declared)
│   ├── Services/              # MockMqService, MockMqAdminService, MockDataStore
│   └── Program.cs             # Entry point (skips connection validation)
└── MqManager.Desktop/         # Avalonia desktop app; output exe name: MqManagerDesktop
    ├── i18n/                  # Multilingual strings
    │   ├── Strings.axaml      # English resource dictionary (default)
    │   ├── Strings.zh.axaml   # Chinese resource dictionary
    │   ├── Strings.cs         # Static helper: Strings.Get(key), Strings.Format(key, args)
    │   └── CultureHelper.cs   # Singleton, CultureChanged event, language switching
    ├── Models/                # Desktop-only models (ConnectionInfo, ServerConfig)
    ├── Services/              # IMqService, MqService (wraps Core), AuditLogService.cs
    ├── ViewModels/            # MVVM ViewModels (CommunityToolkit.Mvvm)
    │   ├── ViewModelBase.cs   # Abstract base (ObservableObject)
    │   ├── LoginViewModel.cs, ServerSettingsViewModel.cs, MainWindowViewModel.cs
    │   ├── QueueListViewModel.cs, QueueActionsViewModel.cs, MessagePanelViewModel.cs
    │   ├── ChannelListViewModel.cs, ChannelStatusViewModel.cs
    │   ├── ConnectionListViewModel.cs, ConnectionDetailViewModel.cs
    │   └── AuditLogViewModel.cs
    ├── Views/                 # Avalonia AXAML views
    │   ├── MainWindow.axaml
    │   ├── LoginView.axaml, ServerSettingsView.axaml
    │   ├── QueueListView.axaml, MessagePanelView.axaml
    │   ├── ChannelListView.axaml, ChannelStatusView.axaml
    │   ├── ConnectionListView.axaml, ConnectionDetailView.axaml
    │   └── AuditLogView.axaml
    ├── Data/                  # Data persistence (pure ADO.NET / Microsoft.Data.Sqlite)
    │   ├── AppDbContext.cs    # SqliteConnection wrapper, creates tables on Initialize()
    │   ├── ConfigRepository.cs
    │   ├── CacheRepository.cs
    │   └── PreferencesRepository.cs
    ├── Converters/
    ├── App.axaml / App.axaml.cs
    └── Program.cs
tests/
└── MqManager.Tests/           # Test project (xUnit + FluentAssertions)
```

## Architecture

### Core Library (`MqManager.Core`)

Two service classes for IBM MQ operations:

- **`Services/MqService.cs`** — XMS API layer (`IBM.XMS`). Handles messaging: connect, put, get, browse, purge. Uses `XMSFactoryFactory` → `IConnectionFactory` → `IConnection` → `ISession` → `IProducer`/`IConsumer`/`IBrowser`. Exposes `IsConnected` bool property (`_connection != null`).
- **`Services/MqAdminService.cs`** — MQI + PCF layer (`IBM.WMQ`, `IBM.WMQ.PCF`). Handles admin ops: list queues, get depth, create/delete queues; list channels, get channel status, start/stop/reset channels; list application connections. Uses `MQQueueManager` for MQI and `PCFMessageAgent` for PCF commands.

Both services take `MqConnectionSettings` and implement `IDisposable`.

### Desktop App (`MqManager.Desktop`)

The Desktop directly uses `MqManager.Core` — **no subprocess spawning**. `MqManager.Desktop.Services.MqService` wraps `MqManager.Core.Services.MqService` and `MqAdminService` directly. It implements `IMqService` and is instantiated in `MainWindowViewModel`.

`Connect(ServerConfig)` creates both `MqServiceCore` and `MqAdminService` instances and calls `Connect()`. `Disconnect()` disposes both. All methods delegate to the appropriate Core service.

## Mock Project

`src/MqManager.CliMock/` is a standalone .NET 9 console app that mirrors all CLI commands without requiring a real IBM MQ server. It has zero IBM MQ dependencies — `MockDataStore` holds in-memory queues and channels, and mock services operate on that data.

The Mock project re-declares its own model classes and `Config/ConfigLoader.cs` in standalone copies (does **not** reference `MqManager.Core`). Commands share the `MqManager.CliMock.Commands` namespace. Mock services live in `MqManager.CliMock.Services`.

Command factory signature: `Create(Func<MqConnectionSettings> getSettings, MockDataStore store)` — the extra `store` parameter is the shared in-memory data source.

All commands from the main project are covered: `connect`, `put`, `get`, `browse`, `list`, `queue` (depth/purge/create/delete), `channel` (list/status/conninfo/start/stop/reset), `connection list`.

Run with `dotnet run --project src/MqManager.CliMock -- <args>`.

## Configuration Priority

CLI flags (`--host`, `--port`, `--qm`, `--channel`, `--user`, `--password`) > environment variables (`MQ_HOSTNAME`, `MQ_PORT`, `MQ_QUEUEMANAGER`, `MQ_CHANNEL`, `MQ_USERNAME`, `MQ_PASSWORD`) > `appsettings.json` under `MqConnection` section.

Resolution happens in `src/MqManager.Core/Config/ConfigLoader.cs`. `ConfigLoader.Load()` reads config + env vars; `ConfigLoader.ApplyOverrides(base, ...)` applies CLI flag overrides and returns a cloned `MqConnectionSettings`. `Program.cs` uses middleware to call `ApplyOverrides` before command handlers run.

## Command Pattern

Each command is a static class in `src/MqManager.Cli/Commands/` with a `Create(Func<MqConnectionSettings> getSettings)` factory method. The `getSettings` delegate resolves after CLI parsing via the middleware in `Program.cs`.

`System.CommandLine` beta4 is used — `Option<T>`, `Argument<T>`, `SetHandler`, `AddGlobalOption`.

## Key IBM.XMS / IBM.WMQ API Notes

- XMS message properties: use IBM-specific names (`JMS_IBM_PutDate`, `JMS_IBM_PutTime`) via `GetStringProperty` for put timestamp; standard JMS properties (`JMSMessageID`, `JMSCorrelationID`) via direct property access.
- MQI constants: all prefixed with `MQC.` (e.g., `MQC.MQOO_INQUIRE`, `MQC.MQCMD_CREATE_Q`, `MQC.MQIA_CURRENT_Q_DEPTH`). Queue/queue-manager attributes use `MQIA_` prefix; channel attributes use `MQIACH_` prefix; channel char attributes use `MQCACH_` prefix.
- `MQQueueManager` constructor uses `Hashtable` for connection properties, not positional args. Auth uses `MQC.USE_MQCSP_AUTHENTICATION_PROPERTY` = `"YES"` + `MQC.USER_ID_PROPERTY` + `MQC.PASSWORD_PROPERTY`.
- `MQManagedObject.Inquire(int[] selectors, int[] intAttrs, byte[] charAttrs)` — three-param signature, not single-param.
- PCF agent must be explicitly disconnected after use.

## NuGet Packages

**src/MqManager.Core:**
- `IBMXMSDotnetClient` 9.4.5.1 — bundles both `amqmxmsstd.dll` (XMS) and `amqmdnetstd.dll` (MQI+PCF)
- `Microsoft.Extensions.Configuration.Json` + `.EnvironmentVariables` 9.0.0
- `TrimmerRootAssemblies`: `amqmdnetstd`, `amqmxmsstd` (prevents trim from removing IBM reflection-heavy assemblies)

**src/MqManager.Cli:**
- `System.CommandLine` 2.0.0-beta4.22272.1
- `Microsoft.Extensions.Configuration.Json` + `.EnvironmentVariables` 9.0.0
- References `MqManager.Core`; output assembly name: `mqmanager`

**src/MqManager.Desktop:**
- `Avalonia` 11.3.7, `Avalonia.Desktop` 11.3.7, `Avalonia.Fonts.Inter` 11.3.7
- `Semi.Avalonia` 11.3.7 — replaces `Avalonia.Themes.Fluent`; provides `<semi:SemiTheme>` + light/dark theme variants
- `Semi.Avalonia.DataGrid` 11.3.7 — replaces `Avalonia.Controls.DataGrid`; styled DataGrid via `avares://Semi.Avalonia.DataGrid/Index.axaml`
- `Irihi.Ursa` 1.15.1 — Toast, Dialog overlays (1.x family for Avalonia 11.x)
- `Irihi.Ursa.Themes.Semi` 1.15.1 — Semi theme for Ursa; loaded via `StyleInclude` in `App.axaml.cs`
- `CommunityToolkit.Mvvm` 8.4.0
- `Microsoft.Data.Sqlite` 9.0.0 — raw ADO.NET only, no EF Core
- References `MqManager.Core`; output assembly name: `MqManagerDesktop`
- Trim settings: `PublishTrimmed=true`, `TrimMode=partial`, `EnableTrimAnalyzer=true`
- `NoWarn`: `IL2104` (upstream `Avalonia.Controls.DataGrid` assembly)
- `TrimmerRootAssemblies`: `Avalonia`, `Avalonia.Desktop`, `Semi.Avalonia`, `Semi.Avalonia.DataGrid`, `Ursa`, `Ursa.Themes.Semi`
- `RuntimeIdentifiers`: `win-x64;linux-x64;osx-x64`
- `AvaloniaUseCompiledBindingsByDefault=true`

**Version matrix (must match):** Semi.Avalonia 11.x → Avalonia 11.x; Semi.Avalonia 12.x → Avalonia 12.x. Ursa 1.x → Avalonia 11.x; Ursa 2.x → Avalonia 12.x.

## Testing

Tests are in `tests/MqManager.Tests/` using xUnit and FluentAssertions. Run with:

```bash
dotnet test tests/MqManager.Tests
```

## Desktop App (Avalonia)

`src/MqManager.Desktop/` is a standalone .NET 9 Avalonia app. It references `MqManager.Core` directly — no CLI subprocess dependency. Focus of active development; CLI is stable.

### Architecture

- **`Services/MqService.cs`** — implements `IMqService`. Wraps `MqManager.Core.Services.MqService` (XMS, messaging) and `MqAdminService` (MQI+PCF, admin). `Connect(ServerConfig)` instantiates both Core services and calls `Connect()`. `Disconnect()` disposes both. `IsConnected` delegates to the Core `MqService.IsConnected`.
- **`Services/AuditLogService.cs`** — singleton (`AuditLogService.Instance`). Holds `AuditLogEntry` records in-memory (session-only). All ViewModels call `AuditLogService.Instance.Log(category, operation, target, success, details)`. Fires `EntryAdded` event for UI updates.
- **`Models/ConnectionInfo.cs`** — Desktop-only view model for connection rows. All Core models (`QueueInfo`, `ChannelInfo`, `ChannelStatus`, `ChannelConnectionInfo`, `ApplicationConnectionInfo`, `MessageInfo`, `MqConnectionSettings`) live in `MqManager.Core.Models`.
- **`Models/ServerConfig.cs`** — SQLite-persisted server configuration.
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

### IMqService Methods

All methods are synchronous direct calls to Core services:

| Method | Core service | Notes |
|---|---|---|
| `Connect(ServerConfig)` | creates `MqServiceCore` + `MqAdminService`, calls `Connect()` | |
| `Disconnect()` | disposes both services | |
| `ListQueues(filter?)` | `MqAdminService.ListQueues(filter ?? "*")` | |
| `GetQueueDepth(name)` | `MqAdminService.GetQueueDepth(name)` | |
| `CreateQueue(name, type)` | `MqAdminService.CreateQueue(name, type)` | |
| `DeleteQueue(name, force)` | `MqAdminService.DeleteQueue(name)` | force param ignored |
| `PurgeQueue(name, force)` | `MqServiceCore.PurgeQueue(name)` | force param ignored |
| `PutMessage(queue, text)` | `MqServiceCore.PutMessage(queue, text)` | |
| `PutMessageFromFile(queue, path)` | reads file, calls `PutMessage` | |
| `GetMessages(queue, count)` | `MqServiceCore.GetMessages(queue, count)` | |
| `BrowseMessages(queue, count)` | `MqServiceCore.BrowseMessages(queue, count)` | |
| `ListChannels(filter?)` | `MqAdminService.ListChannels(filter ?? "*")` | |
| `GetChannelStatus(name)` | `MqAdminService.GetChannelStatus(name)` | |
| `GetChannelConnInfo(name)` | `MqAdminService.GetChannelConnectionInfo(name)` | |
| `StartChannel(name)` | `MqAdminService.StartChannel(name)` | no force parameter |
| `StopChannel(name, force)` | `MqAdminService.StopChannel(name)` | force param ignored |
| `ResetChannel(name, force)` | `MqAdminService.ResetChannel(name)` | force param ignored |
| `ListConnections(filter?)` | `MqAdminService.ListApplicationConnections()` → maps to `ConnectionInfo` | |

Note: `StartChannel` has no force parameter — IBM MQ `START CHANNEL` does not support force.

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

**`OnConnected`**: calls `Array.Clear` on `_tabContent` and `_tabInitialized`, resets `_previousTabIndex = -1`, then bootstraps tab 0 directly — sets `ActiveTabIndex = 0`, marks `_tabInitialized[0] = true`, wires `SidebarContent = _queueListView ??= ...`, sets `ContentPanel = _messagePanelView ??= ...`, seeds `_tabContent[0] = _messagePanelView`, calls `QueueListVm.LoadQueuesCommand.Execute(null)`. Bypasses `OnActiveTabIndexChanged` to avoid double-load on connect.

**`Disconnect`**: calls `Array.Clear` on `_tabContent` and `_tabInitialized`, resets `_previousTabIndex = -1`, sets `ActiveTabIndex = 0`, clears `SidebarContent` and `ContentPanel`.

Key behavioral notes:
- **`MainWindowViewModel`** — 4 tabs: Queues (0), Channels (1), Connections (2), Log (3). Tab 3 sets `SidebarContent = null` (full-width audit log). Dark mode toggle via `ToggleThemeCommand`. Language toggle via `ToggleLanguageCommand`. Reads saved theme from `PreferencesRepository` on startup.
- **`QueueListViewModel`** — client-side glob filter wraps bare text with `*...*` for fuzzy matching. Uses `CacheRepository` to show cached queue names immediately, then refreshes from Core service. `SetServer(ServerConfig)` updates the current server for cache keying.
- **`QueueActionsViewModel`** — `StatusMessage` observable property shows create/delete error feedback. `SetRefreshAction(Action)` wires the refresh callback.
- **`MessagePanelViewModel`** — three send modes: text (0), file (1), folder (2). Folder mode iterates all files and puts each as a message. `ConsumeAllAsync` drains in 100-message batches until queue is empty.
- **`ChannelStatusViewModel`** — two-tab panel (Status + ConnInfo); ConnInfo loaded lazily on tab switch. `IsForceStop` and `IsForceReset` toggle force flags; no `IsForceStart` (StartChannel has no force).
- **`ConnectionListViewModel`** — `ExportCsvAsync` uses `StorageProvider.SaveFilePickerAsync` for user-chosen path; exports `_allConnections` (full list, not filtered view).
- **`AuditLogViewModel`** — subscribes to `AuditLogService.Instance.EntryAdded`; inserts new entries at index 0 (newest-first) via `Dispatcher.UIThread.Post`. `SaveToFileCommand` uses `StorageProvider` to write CSV. `ClearLogCommand` clears the collection.
- **`LoginViewModel`** — `ToggleLanguage` command switches between English and Chinese. Tracks `StatusType` enum (`None`, `PleaseSelect`, `Connecting`, `ConnectedTo`) + `_connectedServerName` so `UpdateStrings()` re-translates `StatusMessage` when language changes. `ConnectAsync` calls `_mqService.Connect(SelectedServer)` on a background thread.

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

All UI strings live in `src/MqManager.Desktop/i18n/`:

- `Strings.axaml` — English resource dictionary (default, loaded in `App.axaml` via `ResourceInclude`)
- `Strings.zh.axaml` — Chinese resource dictionary (same key set)
- `Strings.cs` — `Strings.Get(key)` reads from `Application.Current.Resources`; `Strings.Format(key, args)` wraps `string.Format`
- `CultureHelper.cs` — singleton; call `CultureHelper.Instance.ToggleLanguage()` to switch. Sets both `DefaultThreadCurrentCulture`/`DefaultThreadCurrentUICulture` and `Thread.CurrentThread.CurrentCulture`/`CurrentUICulture`.

Language switching in `App.axaml.cs` (`OnLanguageChanged`): clears `Resources.MergedDictionaries` and adds a new `ResourceInclude` pointing to the appropriate `.axaml` file (using `avares://MqManagerDesktop/i18n/...`).

**Adding a new string:** add the key/value to both `Strings.axaml` and `Strings.zh.axaml`, then call `Strings.Get("Your_Key")` in the ViewModel's `UpdateStrings()`.

Audit log i18n keys: `Tab_AuditLog`, `AuditLog_Title`, `AuditLog_Save`, `AuditLog_Clear`, `AuditLog_ColTime`, `AuditLog_ColCategory`, `AuditLog_ColOperation`, `AuditLog_ColTarget`, `AuditLog_ColSuccess`, `AuditLog_ColDetails`.

### CLI JSON Output

`MqManager.Core/Serialization/JsonContexts.cs` uses `PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase`. Two contexts: `DefaultJsonContext` (channels, status, connections, messages) and `CamelCaseJsonContext` (queues). All desktop `[JsonPropertyName]` attributes must use camelCase (e.g., `"channelName"`, `"mcaUserId"`).

### Publishing (Trim)

Three publish profiles in `src/MqManager.Desktop/Properties/PublishProfiles/`:
- `win-x64.pubxml`, `linux-x64.pubxml`, `osx-x64.pubxml`
- All: `SelfContained=true`, `PublishTrimmed=true`, `PublishSingleFile=true`, `TrimMode=partial`

**Using the build script (recommended):**

```powershell
.\build-desktop.ps1                              # build all three platforms
.\build-desktop.ps1 -Runtimes "win-x64"         # single platform
.\build-desktop.ps1 -Runtimes "win-x64,osx-arm64"
```

Output goes to `publish-desktop/<rid>/MqManagerDesktop[.exe]`.

**Manual publish:**

```bash
dotnet publish src/MqManager.Desktop/MqManager.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:TrimMode=partial
```

Known suppressed trim warnings (by design):
- `IL2026` on `App.axaml.cs:Initialize()` — `AvaloniaXamlLoader.Load(this)`
- `IL2026` on `App.axaml.cs:OnLanguageChanged()` — runtime `ResourceInclude` for i18n
- `IL2104` (`NoWarn`) — `Avalonia.Controls.DataGrid` upstream assembly

### Running

```bash
dotnet run --project src/MqManager.Desktop
```
