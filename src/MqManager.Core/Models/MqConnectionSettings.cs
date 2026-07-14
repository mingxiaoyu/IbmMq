namespace MqManager.Core.Models;

public class MqConnectionSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 1414;
    public string QueueManager { get; set; } = "";
    public string Channel { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";

    public MqConnectionSettings Clone() => new()
    {
        HostName = HostName,
        Port = Port,
        QueueManager = QueueManager,
        Channel = Channel,
        Username = Username,
        Password = Password
    };
}
