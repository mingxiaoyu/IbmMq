using System;
using System.Collections.Generic;
using MqManager.Core.Models;
using MqManager.Desktop.Models;

namespace MqManager.Desktop.Services;

public interface IMqService : IDisposable
{
    void Connect(ServerConfig config);
    void Disconnect();
    List<QueueInfo> ListQueues(string? filter = null);
    long GetQueueDepth(string queueName);
    void CreateQueue(string queueName, string type = "local");
    void DeleteQueue(string queueName, bool force = false);
    void PurgeQueue(string queueName, bool force = false);
    void PutMessage(string queueName, string message);
    void PutMessageFromFile(string queueName, string filePath);
    List<MessageInfo> GetMessages(string queueName, int count = 1);
    List<MessageInfo> BrowseMessages(string queueName, int count = 1);
    List<ChannelInfo> ListChannels(string? filter = null);
    ChannelStatus GetChannelStatus(string channelName);
    List<ChannelConnectionInfo> GetChannelConnInfo(string channelName);
    void StartChannel(string channelName);
    void StopChannel(string channelName, bool force = false);
    void ResetChannel(string channelName, bool force = false);
    List<ConnectionInfo> ListConnections(string? filter = null);
}
