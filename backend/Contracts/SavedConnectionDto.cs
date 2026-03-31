namespace Contracts;

public class SavedConnectionDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool TrustServerCertificate { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsConnected { get; set; } = true;
}
