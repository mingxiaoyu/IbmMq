namespace MqManager.Desktop.Models;

public class ServerConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 1414;
    public string Channel { get; set; } = string.Empty;
    public string QueueManager { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
