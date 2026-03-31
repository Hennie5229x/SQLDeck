namespace Contracts;

public class QueryExecutionRequest
{
    public Guid? ConnectionId { get; set; }
    public string Server { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Sql { get; set; } = string.Empty;
}
