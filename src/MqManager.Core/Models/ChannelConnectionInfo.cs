using System.Text.Json.Serialization;

namespace MqManager.Core.Models;

public class ChannelConnectionInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("connectionName")]
    public string ConnectionName { get; set; } = "";

    [JsonPropertyName("queueManagerName")]
    public string QueueManagerName { get; set; } = "";

    [JsonPropertyName("transmissionQueue")]
    public string TransmissionQueue { get; set; } = "";

    [JsonPropertyName("messages")]
    public int Messages { get; set; }

    [JsonPropertyName("substate")]
    public string Substate { get; set; } = "";

    [JsonPropertyName("indoubtStatus")]
    public string IndoubtStatus { get; set; } = "";

    [JsonPropertyName("messageCompression")]
    public string MessageCompression { get; set; } = "";

    [JsonPropertyName("headerCompression")]
    public string HeaderCompression { get; set; } = "";

    [JsonPropertyName("mcaUserId")]
    public string McaUserId { get; set; } = "";
}
