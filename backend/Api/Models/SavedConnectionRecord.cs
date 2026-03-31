namespace Api.Models;

public class SavedConnectionRecord
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; } = 1433;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool TrustServerCertificate { get; set; }
}
