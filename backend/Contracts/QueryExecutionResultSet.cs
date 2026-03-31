namespace Contracts;

public class QueryExecutionResultSet
{
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int RowsAffected { get; set; }
    public string Message { get; set; } = string.Empty;
}
