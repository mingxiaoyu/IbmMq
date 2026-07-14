using MqManager.CliMock.Models;
using Microsoft.Extensions.Configuration;

namespace MqManager.CliMock.Config;

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

        var settings = new MqConnectionSettings();
        configuration.GetSection("MqConnection").Bind(settings);

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
