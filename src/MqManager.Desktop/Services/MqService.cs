using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MqManager.Core.Models;
using MqManager.Core.Services;
using MqManager.Desktop.Models;
using MqServiceCore = MqManager.Core.Services.MqService;

namespace MqManager.Desktop.Services;

public class MqService : IMqService
{
    private MqServiceCore? _mqService;
    private MqAdminService? _adminService;
    private bool _disposed;

    public bool IsConnected => _mqService?.IsConnected == true;

    public void Connect(ServerConfig config)
    {
        var settings = new MqConnectionSettings
        {
            HostName = config.Host,
            Port = config.Port,
            QueueManager = config.QueueManager,
            Channel = config.Channel,
            Username = config.Username,
            Password = config.Password
        };
        _mqService = new MqServiceCore(settings);
        _mqService.Connect();
        _adminService = new MqAdminService(settings);
    }

    public void Disconnect()
    {
        _mqService?.Dispose();
        _mqService = null;
        _adminService?.Dispose();
        _adminService = null;
    }

    public List<QueueInfo> ListQueues(string? filter = null)
    {
        EnsureConnected();
        return _adminService!.ListQueues(filter ?? "*");
    }

    public long GetQueueDepth(string queueName)
    {
        EnsureConnected();
        return _adminService!.GetQueueDepth(queueName);
    }

    public void CreateQueue(string queueName, string type = "local")
    {
        EnsureConnected();
        _adminService!.CreateQueue(queueName, type);
    }

    public void DeleteQueue(string queueName, bool force = false)
    {
        EnsureConnected();
        _adminService!.DeleteQueue(queueName);
    }

    public void PurgeQueue(string queueName, bool force = false)
    {
        EnsureConnected();
        _mqService!.PurgeQueue(queueName);
    }

    public void PutMessage(string queueName, string message)
    {
        EnsureConnected();
        _mqService!.PutMessage(queueName, message);
    }

    public void PutMessageFromFile(string queueName, string filePath)
    {
        EnsureConnected();
        var text = File.ReadAllText(filePath);
        _mqService!.PutMessage(queueName, text);
    }

    public List<MessageInfo> GetMessages(string queueName, int count = 1)
    {
        EnsureConnected();
        return _mqService!.GetMessages(queueName, count);
    }

    public List<MessageInfo> BrowseMessages(string queueName, int count = 1)
    {
        EnsureConnected();
        return _mqService!.BrowseMessages(queueName, count);
    }

    public List<ChannelInfo> ListChannels(string? filter = null)
    {
        EnsureConnected();
        return _adminService!.ListChannels(filter ?? "*");
    }

    public ChannelStatus GetChannelStatus(string channelName)
    {
        EnsureConnected();
        return _adminService!.GetChannelStatus(channelName);
    }

    public List<ChannelConnectionInfo> GetChannelConnInfo(string channelName)
    {
        EnsureConnected();
        return _adminService!.GetChannelConnectionInfo(channelName);
    }

    public void StartChannel(string channelName)
    {
        EnsureConnected();
        _adminService!.StartChannel(channelName);
    }

    public void StopChannel(string channelName, bool force = false)
    {
        EnsureConnected();
        _adminService!.StopChannel(channelName);
    }

    public void ResetChannel(string channelName, bool force = false)
    {
        EnsureConnected();
        _adminService!.ResetChannel(channelName);
    }

    public List<ConnectionInfo> ListConnections(string? filter = null)
    {
        EnsureConnected();
        var appConns = _adminService!.ListApplicationConnections();
        return appConns.Select(c => new ConnectionInfo
        {
            ChannelName = c.ChannelName,
            ConnectionName = c.ConnectionName,
            AppName = c.AppName,
            UserId = c.UserId,
            Status = c.Status
        }).ToList();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Disconnect();
    }

    private void EnsureConnected()
    {
        if (!IsConnected)
            throw new InvalidOperationException("Not connected. Call Connect() first.");
    }
}
