# MqManager

[English](README.en.md)

IBM MQ 命令行工具和桌面 GUI，基于 XMS .NET Client，用于日常 Queue Manager 维护操作。

## 项目结构

```
MqManager.sln
├── src/MqManager.Core/       # 共享 MQ 服务层（XMS + MQI/PCF）
├── src/MqManager.Cli/        # CLI 命令行工具 → mqmanager
├── src/MqManager.CliMock/    # Mock CLI（无需真实 MQ 服务器）
├── src/MqManager.Desktop/    # Avalonia 桌面 GUI → MqManagerDesktop
└── tests/MqManager.Tests/    # 集成测试
```

## CLI 命令

| 命令 | 说明 |
|------|------|
| `connect` | 测试连接 Queue Manager |
| `put <queue> <message>` | 发送消息（`-f <file>` 从文件读取，`--raw` 从 stdin 读取） |
| `get <queue> [--count N]` | 接收并消费消息 |
| `browse <queue> [--count N]` | 浏览消息（不删除） |
| `list [--filter "PAT*"] [--depth] [--type Local\|Alias\|Model] [--max N]` | 列出队列（JSON 输出） |
| `queue depth <queue>` | 查看队列深度 |
| `queue create <queue> [--type local]` | 创建队列 |
| `queue delete <queue> [--force]` | 删除队列 |
| `queue purge <queue> [--force]` | 清空队列 |
| `channel list [--filter "PAT*"]` | 列出通道 |
| `channel status <channel>` | 查看通道状态 |
| `channel start <channel>` | 启动通道 |
| `channel stop <channel> [--force]` | 停止通道 |
| `channel reset <channel> [--force]` | 重置通道 |
| `channel conninfo <channel>` | 查看通道连接详情 |
| `connection list [--filter "PAT*"]` | 查看应用连接（SVRCONN） |

## 快速开始

需要 .NET 9 SDK。

```bash
dotnet build
```

### 运行 CLI

```bash
dotnet run --project src/MqManager.Cli -- connect
dotnet run --project src/MqManager.Cli -- put TEST.QUEUE "hello world"
dotnet run --project src/MqManager.Cli -- list --filter "APP.*"
```

### 运行桌面 GUI

```bash
dotnet run --project src/MqManager.Desktop
```

## 配置

连接信息通过 `appsettings.json` 配置：

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

也可通过环境变量覆盖：`MQ_HOSTNAME`、`MQ_PORT`、`MQ_QUEUEMANAGER`、`MQ_CHANNEL`、`MQ_USERNAME`、`MQ_PASSWORD`。

或在命令行临时指定：`--host`、`--port`、`--qm`、`--channel`、`--user`、`--password`。

优先级：命令行参数 > 环境变量 > appsettings.json

## 发送 XML/JSON 消息

消息包含特殊字符时，使用 `--raw` 从 stdin 读取：

```bash
# 从文件读取
dotnet run --project src/MqManager.Cli -- put TEST.QUEUE -f message.xml

# 通过管道传入
echo "<root>hello</root>" | dotnet run --project src/MqManager.Cli -- put TEST.QUEUE --raw
```

## Mock 模式

无需真实 IBM MQ 服务器即可体验全部命令，零外部依赖。

```bash
dotnet run --project src/MqManager.CliMock -- connect
dotnet run --project src/MqManager.CliMock -- list
dotnet run --project src/MqManager.CliMock -- channel conninfo APP.SVRCONN
```

## 多平台构建

```bash
# CLI — 全平台
.\build-cli.ps1

# Desktop — 全平台
.\build-desktop.ps1

# 指定平台
.\build-cli.ps1 -Runtimes "win-x64,linux-x64"
.\build-desktop.ps1 -Runtimes "win-x64"
```

输出目录：`publish/`（CLI）、`publish-desktop/`（Desktop）。

## 性能说明

`list` 命令采用两级 PCF 查询策略：

| 场景 | PCF 命令 | 返回内容 |
|------|----------|----------|
| 默认（无 `--depth`、无 `--type`） | `MQCMD_INQUIRE_Q_NAMES` | 仅队列名称 |
| 指定 `--depth` 或 `--type` | `MQCMD_INQUIRE_Q` + `MQIACF_Q_ATTRS` | 名称 + 类型 + 深度 |

## 依赖

- [IBMXMSDotnetClient](https://www.nuget.org/packages/IBMXMSDotnetClient) — IBM MQ XMS .NET 客户端
- [System.CommandLine](https://www.nuget.org/packages/System.CommandLine) — CLI 框架
- Microsoft.Extensions.Configuration — 配置体系
- [Avalonia](https://github.com/AvaloniaUI/Avalonia) — 跨平台桌面 GUI
