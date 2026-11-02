using System.Data;
using KanbanBoard.Core.Domain;
using KanbanBoard.Core.Domain.Events;

namespace KanbanBoard.Core.UseCases.Ports;

public interface IBoardRepository
{
    Board? Load(IDbConnection connection, Guid boardId);
    void Save(IDbConnection connection, IDomainEvent[] events);
}
