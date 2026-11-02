using KanbanBoard.Core.Domain.Events;

namespace KanbanBoard.Core.Domain;

public class Board(Guid id, WipLimit wipLimit, List<Ticket> tickets)
{
    public Guid Id { get; } = id;
    private readonly Column _backlog = new(WipLimit.NoWipLimit(), tickets.Where(x => x.IsInBacklogColumn()).ToList());
    private readonly Column _todo = new(WipLimit.NoWipLimit(), tickets.Where(x => x.IsInTodoColumn()).ToList());
    private readonly Column _ongoing = new(wipLimit, tickets.Where(x => x.IsInOngoingColumn()).ToList());
    private readonly Column _done = new(WipLimit.NoWipLimit(), tickets.Where(x => x.IsInDoneColumn()).ToList());

    public static IDomainEvent[] CreateEmptyBoard(Guid boardId, string name)
    {
        return [ new BoardCreated(boardId, name) ];
    }

    public IDomainEvent[] MoveTicketInTodoColumn(Guid ticketId, DateTime now)
    {
        var ticket = _backlog.RemoveTicket(ticketId);

        if (ticket == null) return [];

        _todo.AddTicket(ticket);

        return [ new TicketMovedInTodoColumn(Id, ticketId, now) ];
    }

    public IDomainEvent[] MoveTicketInOngoingColumn(Guid ticketId, DateTime now)
    {
        var ticket = _todo.RemoveTicket(ticketId);

        if (ticket == null) return [];

        _ongoing.AddTicket(ticket);

        if (_ongoing.IsWipLimitExceeded())
            return [
                new TicketMovedInOngoingColumn(Id, ticketId, now),
                new WipLimitExceeded(Id)
            ];

        return [ new TicketMovedInOngoingColumn(Id, ticketId, now) ];
    }

    public IDomainEvent[] MoveTicketInDoneColumn(Guid ticketId, DateTime now)
    {
        var ticket = _ongoing.RemoveTicket(ticketId);

        if (ticket == null) return [];

        _done.AddTicket(ticket);

        return [ new TicketMovedInDoneColumn(Id, ticketId, now) ];
    }

    public IDomainEvent[] CreateTicket(Guid ticketId, string title, string description, DateTime now)
    {
        _backlog.AddTicket(new Ticket(ticketId, TicketStatus.Backlog));

        return [ new TicketWritten(Id, ticketId, title, description, now) ];
    }

    public IDomainEvent[] DefineWipLimit(WipLimit wipLimit)
    {
        _ongoing.DefineWipLimit(wipLimit);

        return [ new WipLimitDefined(Id, wipLimit.Value) ];
    }

    public IDomainEvent[] RemoveWipLimit()
    {
        _ongoing.RemoveWipLimit();

        return [ new WipLimitRemoved(Id) ];
    }
}


