using System.Collections;
using IBM.WMQ;
using IBM.WMQ.PCF;
using MqManager.Core.Models;

namespace MqManager.Core.Services;

public class MqAdminService : IDisposable
{
    private readonly MqConnectionSettings _settings;
    private MQQueueManager? _qm;
    private bool _disposed;

    public MqAdminService(MqConnectionSettings settings)
    {
        _settings = settings;
    }

    private void EnsureConnected()
    {
        if (_qm != null) return;

        var connectionInfo = $"{_settings.HostName}({_settings.Port})";

        if (!string.IsNullOrEmpty(_settings.Username))
        {
            var props = new Hashtable
            {
                { MQC.TRANSPORT_PROPERTY, MQC.TRANSPORT_MQSERIES_MANAGED },
                { MQC.HOST_NAME_PROPERTY, _settings.HostName },
                { MQC.PORT_PROPERTY, _settings.Port },
                { MQC.CHANNEL_PROPERTY, _settings.Channel },
                { MQC.USE_MQCSP_AUTHENTICATION_PROPERTY, "YES" },
                { MQC.USER_ID_PROPERTY, _settings.Username },
                { MQC.PASSWORD_PROPERTY, _settings.Password }
            };
            _qm = new MQQueueManager(_settings.QueueManager, props);
        }
        else
        {
            _qm = new MQQueueManager(_settings.QueueManager, _settings.Channel, connectionInfo);
        }
    }

    public List<QueueInfo> ListQueues(string filter = "*", bool includeDepth = false, string? typeFilter = null, int? maxResults = null)
    {
        EnsureConnected();

        var mqType = typeFilter?.ToUpperInvariant() switch
        {
            "LOCAL" => MQC.MQQT_LOCAL,
            "ALIAS" => MQC.MQQT_ALIAS,
            "MODEL" => MQC.MQQT_MODEL,
            _ => MQC.MQQT_ALL
        };

        List<QueueInfo> queues;
        try
        {
            queues = QueryQueues(filter, mqType, includeDepth);
        }
        catch (MQException ex) when (ex.ReasonCode == MQC.MQRC_OBJECT_DAMAGED && mqType == MQC.MQQT_ALL)
        {
            queues = new List<QueueInfo>();
            foreach (var fallbackType in new[] { MQC.MQQT_LOCAL, MQC.MQQT_ALIAS, MQC.MQQT_MODEL })
            {
                try
                {
                    queues.AddRange(QueryQueues(filter, fallbackType, includeDepth));
                }
                catch (MQException)
                {
                }
            }
        }

        if (maxResults.HasValue)
            return queues.Take(maxResults.Value).ToList();

        return queues;
    }

    private List<QueueInfo> QueryQueues(string filter, int mqType, bool includeDepth)
    {
        if (!includeDepth && mqType == MQC.MQQT_ALL)
            return QueryQueueNamesOnly(filter);

        return QueryQueuesWithDetails(filter, mqType, includeDepth);
    }

    private List<QueueInfo> QueryQueueNamesOnly(string filter)
    {
        var queues = new List<QueueInfo>();
        var pcf = new PCFMessageAgent(_qm!);
        try
        {
            var request = new PCFMessage(CMQCFC.MQCMD_INQUIRE_Q_NAMES);
            request.AddParameter(MQC.MQCA_Q_NAME, filter);

            var responses = pcf.Send(request);
            var names = (string[])responses[0].GetStringListParameterValue(CMQCFC.MQCACF_Q_NAMES);
            foreach (var name in names)
            {
                var trimmed = name.TrimEnd();
                if (!string.IsNullOrEmpty(trimmed))
                    queues.Add(new QueueInfo { Name = trimmed });
            }
        }
        finally
        {
            pcf.Disconnect();
        }

        return queues;
    }

    private List<QueueInfo> QueryQueuesWithDetails(string filter, int mqType, bool includeDepth)
    {
        var queues = new List<QueueInfo>();
        var pcf = new PCFMessageAgent(_qm!);
        try
        {
            var request = new PCFMessage(MQC.MQCMD_INQUIRE_Q);
            request.AddParameter(MQC.MQCA_Q_NAME, filter);
            request.AddParameter(MQC.MQIA_Q_TYPE, mqType);

            var attrs = includeDepth
                ? new int[] { MQC.MQCA_Q_NAME, MQC.MQIA_Q_TYPE, MQC.MQIA_CURRENT_Q_DEPTH }
                : new int[] { MQC.MQCA_Q_NAME, MQC.MQIA_Q_TYPE };
            request.AddParameter(CMQCFC.MQIACF_Q_ATTRS, attrs);

            var responses = pcf.Send(request);
            foreach (var response in responses)
            {
                var name = response.GetStringParameterValue(MQC.MQCA_Q_NAME).TrimEnd();
                var qType = response.GetIntParameterValue(MQC.MQIA_Q_TYPE);
                var typeStr = qType switch
                {
                    MQC.MQQT_LOCAL => "Local",
                    MQC.MQQT_ALIAS => "Alias",
                    MQC.MQQT_MODEL => "Model",
                    _ => "Other"
                };

                queues.Add(new QueueInfo
                {
                    Name = name,
                    Type = typeStr,
                    Depth = includeDepth ? response.GetIntParameterValue(MQC.MQIA_CURRENT_Q_DEPTH) : 0
                });
            }
        }
        finally
        {
            pcf.Disconnect();
        }

        return queues;
    }

    public int GetQueueDepth(string queueName)
    {
        EnsureConnected();
        return GetQueueAttributes(queueName).Depth;
    }

    public void CreateQueue(string queueName, string queueType = "Q")
    {
        EnsureConnected();
        var pcf = new PCFMessageAgent(_qm!);
        try
        {
            int mqType = queueType.ToUpper() switch
            {
                "LQ" or "LOCAL" => MQC.MQQT_LOCAL,
                "ALIAS" => MQC.MQQT_ALIAS,
                "MODEL" => MQC.MQQT_MODEL,
                _ => MQC.MQQT_LOCAL
            };

            var request = new PCFMessage(MQC.MQCMD_CREATE_Q);
            request.AddParameter(MQC.MQCA_Q_NAME, queueName);
            request.AddParameter(MQC.MQIA_Q_TYPE, mqType);

            pcf.Send(request);
        }
        finally
        {
            pcf.Disconnect();
        }
    }

    public void DeleteQueue(string queueName)
    {
        EnsureConnected();
        var pcf = new PCFMessageAgent(_qm!);
        try
        {
            var request = new PCFMessage(MQC.MQCMD_DELETE_Q);
            request.AddParameter(MQC.MQCA_Q_NAME, queueName);

            pcf.Send(request);
        }
        finally
        {
            pcf.Disconnect();
        }
    }

    private (int Depth, string Type) GetQueueAttributes(string queueName)
    {
        var pcf = new PCFMessageAgent(_qm!);
        try
        {
            var request = new PCFMessage(MQC.MQCMD_INQUIRE_Q);
            request.AddParameter(MQC.MQCA_Q_NAME, queueName);

            var responses = pcf.Send(request);
            var response = responses[0];

            var depth = response.GetIntParameterValue(MQC.MQIA_CURRENT_Q_DEPTH);
            var qType = response.GetIntParameterValue(MQC.MQIA_Q_TYPE);
            var typeStr = qType switch
            {
                MQC.MQQT_LOCAL => "Local",
                MQC.MQQT_ALIAS => "Alias",
                MQC.MQQT_MODEL => "Model",
                _ => "Other"
            };

            return (depth, typeStr);
        }
        finally
        {
            pcf.Disconnect();
        }
    }

    public List<ChannelInfo> ListChannels(string filter = "*")
    {
        EnsureConnected();
        var channels = new List<ChannelInfo>();

        var pcf = new PCFMessageAgent(_qm!);
        try
        {
            var request = new PCFMessage(MQC.MQCMD_INQUIRE_CHANNEL);
            request.AddParameter(MQC.MQCACH_CHANNEL_NAME, filter);

            var responses = pcf.Send(request);
            foreach (var response in responses)
            {
                var name = response.GetStringParameterValue(MQC.MQCACH_CHANNEL_NAME).TrimEnd();
                var type = response.GetIntParameterValue(MQC.MQIACH_CHANNEL_TYPE);
                var desc = "";
                try { desc = response.GetStringParameterValue(MQC.MQCACH_DESC).TrimEnd(); }
                catch { /* description may not be available */ }

                channels.Add(new ChannelInfo
                {
                    Name = name,
                    Type = ChannelTypeToString(type),
                    Description = desc
                });
            }
        }
        finally
        {
            pcf.Disconnect();
        }

        return channels;
    }

    public ChannelStatus GetChannelStatus(string channelName)
    {
        EnsureConnected();

        var pcf = new PCFMessageAgent(_qm!);
        try
        {
            var request = new PCFMessage(MQC.MQCMD_INQUIRE_CHANNEL_STATUS);
            request.AddParameter(MQC.MQCACH_CHANNEL_NAME, channelName);

            var responses = pcf.Send(request);
            var response = responses[0];

            return new ChannelStatus
            {
                Name = response.GetStringParameterValue(MQC.MQCACH_CHANNEL_NAME).TrimEnd(),
                Type = ChannelTypeToString(response.GetIntParameterValue(MQC.MQIACH_CHANNEL_TYPE)),
                Status = ChannelStatusToString(response.GetIntParameterValue(MQC.MQIACH_CHANNEL_STATUS)),
                ConnectionName = response.GetStringParameterValue(MQC.MQCACH_CONNECTION_NAME).TrimEnd(),
                BytesSent = response.GetIntParameterValue(MQC.MQIACH_BYTES_SENT),
                BytesReceived = response.GetIntParameterValue(MQC.MQIACH_BYTES_RECEIVED),
                Messages = response.GetIntParameterValue(MQC.MQIACH_MSGS),
                BuffersSent = response.GetIntParameterValue(MQC.MQIACH_BUFFERS_SENT),
                BuffersReceived = response.GetIntParameterValue(MQC.MQIACH_BUFFERS_RECEIVED)
            };
        }
        catch (MQException ex) when (ex.ReasonCode == 3065) // MQRCCF_CHL_STATUS_NOT_FOUND
        {
            return new ChannelStatus
            {
                Name = channelName,
                Status = "Inactive"
            };
        }
        finally
        {
            pcf.Disconnect();
        }
    }

    public List<ChannelConnectionInfo> GetChannelConnectionInfo(string channelName)
    {
        EnsureConnected();

        var pcf = new PCFMessageAgent(_qm!);
        try
        {
            var request = new PCFMessage(MQC.MQCMD_INQUIRE_CHANNEL_STATUS);
            request.AddParameter(MQC.MQCACH_CHANNEL_NAME, channelName);

            var responses = pcf.Send(request);
            var results = new List<ChannelConnectionInfo>();

            foreach (var response in responses)
            {
                var connName = "";
                try { connName = response.GetStringParameterValue(MQC.MQCACH_CONNECTION_NAME).TrimEnd(); }
                catch { /* may not be available */ }

                var qmName = "";
                try { qmName = response.GetStringParameterValue(3596).TrimEnd(); } // MQCACH_REMOTE_Q_MGR_NAME
                catch { /* may not be available */ }

                var xmitQ = "";
                try { xmitQ = response.GetStringParameterValue(MQC.MQCACH_XMIT_Q_NAME).TrimEnd(); }
                catch { /* may not be available */ }

                var mcaUserId = "";
                try { mcaUserId = response.GetStringParameterValue(MQC.MQCACH_MCA_USER_ID).TrimEnd(); }
                catch { /* may not be available */ }

                var messages = 0;
                try { messages = response.GetIntParameterValue(MQC.MQIACH_MSGS); }
                catch { /* may not be available */ }

                var substate = 0;
                try { substate = response.GetIntParameterValue(MQC.MQIACH_CHANNEL_SUBSTATE); }
                catch { /* may not be available */ }

                var indoubt = 0;
                try { indoubt = response.GetIntParameterValue(MQC.MQIACH_INDOUBT_STATUS); }
                catch { /* may not be available */ }

                var msgCompression = 0;
                try { msgCompression = response.GetIntParameterValue(MQC.MQIACH_MSG_COMPRESSION); }
                catch { /* may not be available */ }

                var hdrCompression = 0;
                try { hdrCompression = response.GetIntParameterValue(MQC.MQIACH_HDR_COMPRESSION); }
                catch { /* may not be available */ }

                results.Add(new ChannelConnectionInfo
                {
                    Name = response.GetStringParameterValue(MQC.MQCACH_CHANNEL_NAME).TrimEnd(),
                    Type = ChannelTypeToString(response.GetIntParameterValue(MQC.MQIACH_CHANNEL_TYPE)),
                    Status = ChannelStatusToString(response.GetIntParameterValue(MQC.MQIACH_CHANNEL_STATUS)),
                    ConnectionName = connName,
                    QueueManagerName = qmName,
                    TransmissionQueue = xmitQ,
                    Messages = messages,
                    Substate = ChannelSubstateToString(substate),
                    IndoubtStatus = IndoubtStatusToString(indoubt),
                    MessageCompression = CompressionToString(msgCompression),
                    HeaderCompression = CompressionToString(hdrCompression),
                    McaUserId = mcaUserId
                });
            }

            return results;
        }
        catch (MQException ex) when (ex.ReasonCode == 3065) // MQRCCF_CHL_STATUS_NOT_FOUND
        {
            return new List<ChannelConnectionInfo>
            {
                new ChannelConnectionInfo
                {
                    Name = channelName,
                    Status = "Inactive"
                }
            };
        }
        finally
        {
            pcf.Disconnect();
        }
    }

    public void StartChannel(string channelName)
    {
        EnsureConnected();
        var pcf = new PCFMessageAgent(_qm!);
        try
        {
            var request = new PCFMessage(MQC.MQCMD_START_CHANNEL);
            request.AddParameter(MQC.MQCACH_CHANNEL_NAME, channelName);
            pcf.Send(request);
        }
        finally
        {
            pcf.Disconnect();
        }
    }

    public void StopChannel(string channelName)
    {
        EnsureConnected();
        var pcf = new PCFMessageAgent(_qm!);
        try
        {
            var request = new PCFMessage(MQC.MQCMD_STOP_CHANNEL);
            request.AddParameter(MQC.MQCACH_CHANNEL_NAME, channelName);
            pcf.Send(request);
        }
        finally
        {
            pcf.Disconnect();
        }
    }

    public void ResetChannel(string channelName)
    {
        EnsureConnected();
        var pcf = new PCFMessageAgent(_qm!);
        try
        {
            var request = new PCFMessage(MQC.MQCMD_RESET_CHANNEL);
            request.AddParameter(MQC.MQCACH_CHANNEL_NAME, channelName);
            pcf.Send(request);
        }
        finally
        {
            pcf.Disconnect();
        }
    }

    private static string ChannelTypeToString(int type) => type switch
    {
        MQC.MQCHT_SERVER => "Server",
        MQC.MQCHT_SENDER => "Sender",
        MQC.MQCHT_SVRCONN => "SVRCONN",
        MQC.MQCHT_REQUESTER => "Requester",
        MQC.MQCHT_RECEIVER => "Receiver",
        MQC.MQCHT_CLUSSDR => "ClusterSender",
        MQC.MQCHT_CLUSRCVR => "ClusterReceiver",
        MQC.MQCHT_MQTT => "MQTT",
        _ => $"Unknown({type})"
    };

    private static string ChannelStatusToString(int status) => status switch
    {
        MQC.MQCHS_STOPPED => "Stopped",
        MQC.MQCHS_RUNNING => "Running",
        MQC.MQCHS_STARTING => "Starting",
        MQC.MQCHS_STOPPING => "Stopping",
        MQC.MQCHS_RETRYING => "Retrying",
        MQC.MQCHS_PAUSED => "Paused",
        MQC.MQCHS_INITIALIZING => "Initializing",
        MQC.MQCHS_REQUESTING => "Requesting",
        _ => $"Unknown({status})"
    };

    private static string CompressionToString(int compression) => compression switch
    {
        MQC.MQCOMPRESS_NONE => "None",
        MQC.MQCOMPRESS_RLE => "RLE",
        MQC.MQCOMPRESS_ZLIBFAST => "ZLIBFast",
        MQC.MQCOMPRESS_ZLIBHIGH => "ZLIBHigh",
        MQC.MQCOMPRESS_SYSTEM => "System",
        MQC.MQCOMPRESS_NOT_AVAILABLE => "NotAvailable",
        _ => $"Unknown({compression})"
    };

    private static string IndoubtStatusToString(int status) => status switch
    {
        MQC.MQCHIDS_NOT_INDOUBT => "NotIndoubt",
        MQC.MQCHIDS_INDOUBT => "Indoubt",
        _ => $"Unknown({status})"
    };

    private static string ChannelSubstateToString(int substate) => substate switch
    {
        0 => "Inactive",      // MQCHSSTATE_INACTIVE
        1 => "Serializing",   // MQCHSSTATE_SERIALIZING
        2 => "Syncing",       // MQCHSSTATE_SYNCING
        3 => "Heartbeating",  // MQCHSSTATE_HEARTBEATING
        4 => "Receiving",     // MQCHSSTATE_RECEIVING
        5 => "Sending",       // MQCHSSTATE_SENDING
        _ => $"Unknown({substate})"
    };

    public List<ApplicationConnectionInfo> ListApplicationConnections()
    {
        EnsureConnected();
        var results = new List<ApplicationConnectionInfo>();

        var pcf = new PCFMessageAgent(_qm!);
        try
        {
            var request = new PCFMessage(MQC.MQCMD_INQUIRE_CHANNEL_STATUS);
            request.AddParameter(MQC.MQCACH_CHANNEL_NAME, "*");


            var responses = pcf.Send(request);

            foreach (var response in responses)
            {
                
                var channelType = 0;
                try { channelType = response.GetIntParameterValue(MQC.MQIACH_CHANNEL_TYPE); }
                catch { /* may not be available */ }

                var appName = "";
                try { appName = response.GetStringParameterValue(MQC.MQCACH_REMOTE_APPL_TAG).TrimEnd(); }
                catch { /* may not be available */ }

                // Only include SVRCONN channels (application connections)
                if (channelType != MQC.MQCHT_SVRCONN) continue;

                var channelName = "";
                try { channelName = response.GetStringParameterValue(MQC.MQCACH_CHANNEL_NAME).TrimEnd(); }
                catch { /* may not be available */ }

                var connectionName = "";
                try { connectionName = response.GetStringParameterValue(MQC.MQCACH_CONNECTION_NAME).TrimEnd(); }
                catch { /* may not be available */ }

                var mcaUserId = "";
                try { mcaUserId = response.GetStringParameterValue(MQC.MQCACH_MCA_USER_ID).TrimEnd(); }
                catch { /* may not be available */ }

                var channelStatus = 0;
                try { channelStatus = response.GetIntParameterValue(MQC.MQIACH_CHANNEL_STATUS); }
                catch { /* may not be available */ }

                results.Add(new ApplicationConnectionInfo
                {
                    AppName = appName,
                    ChannelName = channelName,
                    ConnectionName = connectionName,
                    UserId = mcaUserId,
                    Status = ChannelStatusToString(channelStatus)
                });
            }
        }
        catch (MQException ex) when (ex.ReasonCode == 3065) // MQRCCF_CHL_STATUS_NOT_FOUND
        {
            // No active connections
        }
        finally
        {
            pcf.Disconnect();
        }

        return results;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _qm?.Disconnect();
        _qm = null;
    }

}
