using System.Text.Json.Serialization;

namespace MqManager.Core.Models;

public class QueueInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("depth")]
    public int Depth { get; set; }
}
