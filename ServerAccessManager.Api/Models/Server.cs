namespace ServerAccessManager.Api.Models;

public class Server
{
    public int Id { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OsType { get; set; } = string.Empty;
}