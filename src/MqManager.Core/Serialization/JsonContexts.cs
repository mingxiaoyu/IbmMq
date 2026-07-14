using System.Collections.Generic;
using System.Text.Json.Serialization;
using MqManager.Core.Models;

namespace MqManager.Core.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(List<ChannelInfo>))]
[JsonSerializable(typeof(ChannelStatus))]
[JsonSerializable(typeof(ChannelConnectionInfo))]
[JsonSerializable(typeof(List<ChannelConnectionInfo>))]
[JsonSerializable(typeof(List<ApplicationConnectionInfo>))]
[JsonSerializable(typeof(List<MessageInfo>))]
public partial class DefaultJsonContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(List<QueueInfo>))]
public partial class CamelCaseJsonContext : JsonSerializerContext
{
}
