using System.Data;

namespace KanbanBoard.Core.UseCases.Ports;

public interface IDatabase
{
    IDbConnection CreateConnection();
}
