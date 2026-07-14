using System.Text.Json.Serialization;

namespace MqManager.Core.Models;

public class ApplicationConnectionInfo
{
    [JsonPropertyName("appName")]
    public string AppName { get; set; } = "";

    [JsonPropertyName("channelName")]
    public string ChannelName { get; set; } = "";

    [JsonPropertyName("connectionName")]
    public string ConnectionName { get; set; } = "";

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";
}
