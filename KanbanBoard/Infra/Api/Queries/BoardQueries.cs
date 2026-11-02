using Dapper;
using KanbanBoard.Core.Domain;
using KanbanBoard.Core.UseCases.Ports;

namespace KanbanBoard.Infra.Api.Queries;

public record BoardDao(string id, string name, long wipLimitValue);
public record TicketDao(string id, string title, string description, long status);

public record TicketDto(Guid id, string title, string description, string status);
public record BoardDto(Guid id, string name, int wipLimit, List<TicketDto> backlog, List<TicketDto> todo, List<TicketDto> ongoing, List<TicketDto> done);

public class BoardQueries(IDatabase database)
{
    public BoardDto? GetBoard(Guid boardId)
    {
        using var connection = database.CreateConnection();

        connection.Open();

        var board = connection.QuerySingleOrDefault<BoardDao>("SELECT id, name, wip_limit_value AS wipLimitValue FROM boards WHERE id=@boardId", new { boardId });

        if (board == null) return null;

        var tickets = connection.Query<TicketDao>("SELECT id, title, description, status FROM tickets WHERE board_id=@boardId", new { boardId });

        return new BoardDto(Guid.Parse(board.id), board.name, (int)board.wipLimitValue,
        tickets.Where(x => (TicketStatus)x.status == TicketStatus.Backlog).Select(x => new TicketDto(Guid.Parse(x.id), x.title, x.description, ((TicketStatus)x.status).ToString())).ToList(),
        tickets.Where(x => (TicketStatus)x.status == TicketStatus.Todo).Select(x => new TicketDto(Guid.Parse(x.id), x.title, x.description, ((TicketStatus)x.status).ToString())).ToList(),
        tickets.Where(x => (TicketStatus)x.status == TicketStatus.OnGoing).Select(x => new TicketDto(Guid.Parse(x.id), x.title, x.description, ((TicketStatus)x.status).ToString())).ToList(),
        tickets.Where(x => (TicketStatus)x.status == TicketStatus.Done).Select(x => new TicketDto(Guid.Parse(x.id), x.title, x.description, ((TicketStatus)x.status).ToString())).ToList()
        );
    }
}
