using System.Data;
using Dapper;
using KanbanBoard.Core.Domain;
using KanbanBoard.Core.Domain.Events;
using KanbanBoard.Core.UseCases.Ports;

namespace KanbanBoard.Infra.Adapters;

public record BoardDao(string id, long wipLimitValue);
public record TicketDao(string id, long status);

public class BoardRepository : IBoardRepository
{
    public Board? Load(IDbConnection connection, Guid boardId)
    {
        var board = connection.QuerySingleOrDefault<BoardDao>("SELECT id, wip_limit_value AS wipLimitValue FROM boards WHERE id=@boardId", new { boardId });

        if (board == null) return null;

        var tickets = connection.Query<TicketDao>("SELECT id, status FROM tickets WHERE board_id=@boardId", new { boardId });

        return new Board(Guid.Parse(board.id), new WipLimit((int)board.wipLimitValue), tickets.Select(x => new Ticket(Guid.Parse(x.id), (TicketStatus)x.status)).ToList());
    }

    public void Save(IDbConnection connection, IDomainEvent[] events)
    {
        foreach (var evt in events)
        {
            switch (evt)
            {
                case BoardCreated boardCreated:
                    connection.Execute("INSERT INTO boards (id, name, wip_limit_value) VALUES (@id, @name, 0)", new {
                        id = boardCreated.AggregateId,
                        name = boardCreated.Name
                    });
                    break;
                case WipLimitDefined wipLimitDefined:
                    connection.Execute("UPDATE boards SET wip_limit_value=@wipLimitStatus WHERE id=@boardId", new {
                        boardId = wipLimitDefined.AggregateId,
                        wipLimitStatus = wipLimitDefined.Value
                    });
                    break;
                case WipLimitRemoved wipLimitRemoved:
                    connection.Execute("UPDATE boards SET wip_limit_value=@wipLimitStatus WHERE id=@boardId", new {
                        boardId = wipLimitRemoved.AggregateId,
                        wipLimitStatus = 0
                    });
                    break;
                case TicketWritten ticketWritten:
                    connection.Execute("INSERT INTO tickets (id, board_id, title, description, status) VALUES (@id, @boardId, @title, @description, @status)", new {
                        boardId = ticketWritten.AggregateId,
                        id = ticketWritten.TicketId,
                        title=ticketWritten.Title,
                        description=ticketWritten.Description,
                        status = TicketStatus.Backlog
                    });
                    break;
                case TicketMovedInTodoColumn ticketMovedInTodoColumn:
                    connection.Execute("UPDATE tickets SET status=@status WHERE board_id=@boardId AND id=@ticketId", new {
                        boardId = ticketMovedInTodoColumn.AggregateId,
                        ticketId = ticketMovedInTodoColumn.TicketId,
                        status = TicketStatus.Todo
                    });
                    break;
                case TicketMovedInOngoingColumn ticketMovedInOngoingColumn:
                    connection.Execute("UPDATE tickets SET status=@status WHERE board_id=@boardId AND id=@ticketId", new {
                        boardId = ticketMovedInOngoingColumn.AggregateId,
                        ticketId = ticketMovedInOngoingColumn.TicketId,
                        status = TicketStatus.OnGoing
                    });
                    break;
                case TicketMovedInDoneColumn ticketMovedInDoneColumn:
                    connection.Execute("UPDATE tickets SET status=@status WHERE board_id=@boardId AND id=@ticketId", new {
                        boardId = ticketMovedInDoneColumn.AggregateId,
                        ticketId = ticketMovedInDoneColumn.TicketId,
                        status = TicketStatus.Done
                    });
                    break;
                default: break;
            }
        }
    }
}
