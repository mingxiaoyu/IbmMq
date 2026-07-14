using MqManager.CliMock.Models;

namespace MqManager.CliMock.Services;

public class MockMqAdminService : IDisposable
{
    private readonly MockDataStore _store;

    public MockMqAdminService(MqConnectionSettings settings, MockDataStore store)
    {
        _store = store;
    }

    public List<QueueInfo> ListQueues(string filter = "*", bool includeDepth = false, string? typeFilter = null, int? maxResults = null)
    {
        var query = _store.GetAllQueues(filter);

        if (!string.IsNullOrEmpty(typeFilter))
            query = query.Where(q => q.Type.Equals(typeFilter, StringComparison.OrdinalIgnoreCase)).ToList();

        var queues = query.Select(q => new QueueInfo
        {
            Name = q.Name,
            Type = q.Type,
            Depth = includeDepth ? q.Messages.Count : 0
        }).ToList();

        if (maxResults.HasValue)
            return queues.Take(maxResults.Value).ToList();

        return queues;
    }

    public int GetQueueDepth(string queueName)
    {
        var queue = _store.GetQueue(queueName);
        if (queue == null)
            throw new Exception($"Queue '{queueName}' not found.");
        return queue.Messages.Count;
    }

    public void CreateQueue(string queueName, string queueType = "local")
    {
        if (_store.GetQueue(queueName) != null)
            throw new Exception($"Queue '{queueName}' already exists.");

        string type = queueType.ToUpper() switch
        {
            "LQ" or "LOCAL" => "Local",
            "ALIAS" => "Alias",
            "MODEL" => "Model",
            _ => "Local"
        };

        var queue = _store.GetOrCreateQueue(queueName);
        queue.Type = type;
    }

    public void DeleteQueue(string queueName)
    {
        if (!_store.RemoveQueue(queueName))
            throw new Exception($"Queue '{queueName}' not found.");
    }

    public List<ChannelInfo> ListChannels(string filter = "*")
    {
        return _store.GetAllChannels(filter).Select(c => new ChannelInfo
        {
            Name = c.Name,
            Type = c.Type,
            Description = c.Description
        }).ToList();
    }

    public ChannelStatus GetChannelStatus(string channelName)
    {
        var ch = _store.GetChannel(channelName);
        if (ch == null)
            throw new Exception($"Channel '{channelName}' not found.");

        return new ChannelStatus
        {
            Name = ch.Name,
            Type = ch.Type,
            Status = ch.Status,
            ConnectionName = ch.ConnectionName,
            BytesSent = ch.BytesSent,
            BytesReceived = ch.BytesReceived,
            Messages = ch.Messages,
            BuffersSent = ch.BuffersSent,
            BuffersReceived = ch.BuffersReceived
        };
    }

    public void StartChannel(string channelName)
    {
        var ch = _store.GetChannel(channelName);
        if (ch == null)
            throw new Exception($"Channel '{channelName}' not found.");
        ch.Status = "Running";
    }

    public void StopChannel(string channelName)
    {
        var ch = _store.GetChannel(channelName);
        if (ch == null)
            throw new Exception($"Channel '{channelName}' not found.");
        ch.Status = "Stopped";
    }

    public void ResetChannel(string channelName)
    {
        var ch = _store.GetChannel(channelName);
        if (ch == null)
            throw new Exception($"Channel '{channelName}' not found.");
        ch.Status = "Stopped";
        ch.BytesSent = 0;
        ch.BytesReceived = 0;
        ch.Messages = 0;
        ch.BuffersSent = 0;
        ch.BuffersReceived = 0;
    }

    public List<ChannelConnectionInfo> GetChannelConnectionInfo(string channelName)
    {
        var ch = _store.GetChannel(channelName);
        if (ch == null)
            return new List<ChannelConnectionInfo>
            {
                new ChannelConnectionInfo { Name = channelName, Status = "Inactive" }
            };

        return new List<ChannelConnectionInfo>
        {
            new ChannelConnectionInfo
            {
                Name = ch.Name,
                Type = ch.Type,
                Status = ch.Status,
                ConnectionName = ch.ConnectionName,
                QueueManagerName = ch.QueueManagerName,
                TransmissionQueue = ch.TransmissionQueue,
                Messages = ch.Messages,
                Substate = ch.Substate,
                IndoubtStatus = ch.IndoubtStatus,
                MessageCompression = ch.MessageCompression,
                HeaderCompression = ch.HeaderCompression,
                McaUserId = ch.McaUserId
            }
        };
    }

    public List<ApplicationConnectionInfo> ListApplicationConnections()
    {
        return _store.GetAllChannels("*")
            .Where(c => c.Type == "SVRCONN")
            .Select(c => new ApplicationConnectionInfo
            {
                AppName = c.AppName,
                ChannelName = c.Name,
                ConnectionName = c.ConnectionName,
                UserId = c.McaUserId,
                Status = c.Status
            })
            .ToList();
    }

    public void Dispose()
    {
        // No-op: no real connections to close
    }
}

public class QueueInfo
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public int Depth { get; set; }
}

public class ChannelInfo
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
}

public class ChannelStatus
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public string ConnectionName { get; set; } = "";
    public long BytesSent { get; set; }
    public long BytesReceived { get; set; }
    public int Messages { get; set; }
    public int BuffersSent { get; set; }
    public int BuffersReceived { get; set; }
}

public class ChannelConnectionInfo
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public string ConnectionName { get; set; } = "";
    public string QueueManagerName { get; set; } = "";
    public string TransmissionQueue { get; set; } = "";
    public int Messages { get; set; }
    public string Substate { get; set; } = "";
    public string IndoubtStatus { get; set; } = "";
    public string MessageCompression { get; set; } = "";
    public string HeaderCompression { get; set; } = "";
    public string McaUserId { get; set; } = "";
}

public class ApplicationConnectionInfo
{
    public string AppName { get; set; } = "";
    public string ChannelName { get; set; } = "";
    public string ConnectionName { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Status { get; set; } = "";
}
