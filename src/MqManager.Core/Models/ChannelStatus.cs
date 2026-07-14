using System.Text.Json.Serialization;

namespace MqManager.Core.Models;

public class ChannelStatus
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("connectionName")]
    public string ConnectionName { get; set; } = "";

    [JsonPropertyName("bytesSent")]
    public long BytesSent { get; set; }

    [JsonPropertyName("bytesReceived")]
    public long BytesReceived { get; set; }

    [JsonPropertyName("messages")]
    public int Messages { get; set; }

    [JsonPropertyName("buffersSent")]
    public int BuffersSent { get; set; }

    [JsonPropertyName("buffersReceived")]
    public int BuffersReceived { get; set; }
}
