using MqManager.Core.Models;
using Microsoft.Extensions.Configuration;

namespace MqManager.Core.Config;

public static class ConfigLoader
{
    public static MqConnectionSettings Load()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables(prefix: "MQ_")
            .Build();

        var settings = LoadFromConfiguration(configuration.GetSection("MqConnection"));

        // Environment variables with MQ_ prefix override config file values
        // e.g., MQ_HOSTNAME, MQ_PORT, MQ_QUEUEMANAGER, MQ_CHANNEL, MQ_USERNAME, MQ_PASSWORD
        OverrideFromEnv(settings);

        return settings;
    }

    private static void OverrideFromEnv(MqConnectionSettings settings)
    {
        var host = Environment.GetEnvironmentVariable("MQ_HOSTNAME");
        if (!string.IsNullOrEmpty(host)) settings.HostName = host;

        var port = Environment.GetEnvironmentVariable("MQ_PORT");
        if (int.TryParse(port, out var portVal)) settings.Port = portVal;

        var qm = Environment.GetEnvironmentVariable("MQ_QUEUEMANAGER");
        if (!string.IsNullOrEmpty(qm)) settings.QueueManager = qm;

        var channel = Environment.GetEnvironmentVariable("MQ_CHANNEL");
        if (!string.IsNullOrEmpty(channel)) settings.Channel = channel;

        var user = Environment.GetEnvironmentVariable("MQ_USERNAME");
        if (!string.IsNullOrEmpty(user)) settings.Username = user;

        var pass = Environment.GetEnvironmentVariable("MQ_PASSWORD");
        if (!string.IsNullOrEmpty(pass)) settings.Password = pass;
    }

    private static MqConnectionSettings LoadFromConfiguration(IConfigurationSection section)
    {
        var settings = new MqConnectionSettings();

        var hostName = section["HostName"];
        if (!string.IsNullOrWhiteSpace(hostName)) settings.HostName = hostName;

        var portValue = section["Port"];
        if (int.TryParse(portValue, out var port)) settings.Port = port;

        var queueManager = section["QueueManager"];
        if (!string.IsNullOrWhiteSpace(queueManager)) settings.QueueManager = queueManager;

        var channel = section["Channel"];
        if (!string.IsNullOrWhiteSpace(channel)) settings.Channel = channel;

        var username = section["Username"];
        if (!string.IsNullOrWhiteSpace(username)) settings.Username = username;

        var password = section["Password"];
        if (!string.IsNullOrWhiteSpace(password)) settings.Password = password;

        return settings;
    }

    public static MqConnectionSettings ApplyOverrides(
        MqConnectionSettings settings,
        string? host, int? port, string? qm, string? channel, string? user, string? password)
    {
        var result = settings.Clone();
        if (!string.IsNullOrEmpty(host)) result.HostName = host;
        if (port.HasValue) result.Port = port.Value;
        if (!string.IsNullOrEmpty(qm)) result.QueueManager = qm;
        if (!string.IsNullOrEmpty(channel)) result.Channel = channel;
        if (!string.IsNullOrEmpty(user)) result.Username = user;
        if (!string.IsNullOrEmpty(password)) result.Password = password;
        return result;
    }
}
