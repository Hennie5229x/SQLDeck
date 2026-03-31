using Contracts;

namespace Core;

public interface IExplorerService
{
    List<DatabaseInfo> GetDatabases(ExplorerConnectionRequest request);
    List<TableInfo> GetTables(TableListRequest request);
}
