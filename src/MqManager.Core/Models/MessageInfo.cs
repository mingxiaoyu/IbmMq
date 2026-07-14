using System.Text.Json.Serialization;

namespace MqManager.Core.Models;

public class MessageInfo
{
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = "";

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = "";

    [JsonPropertyName("putDateTime")]
    public string PutDateTime { get; set; } = "";

    [JsonPropertyName("body")]
    public string Body { get; set; } = "";
}
