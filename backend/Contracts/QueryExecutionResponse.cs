namespace Contracts;

public class QueryExecutionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<QueryExecutionResultSet> ResultSets { get; set; } = new();

    public string ExecutedSql { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
}
