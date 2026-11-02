namespace KanbanBoard.Core.Domain;

public class Board(Guid id, WipLimit wipLimit, List<Ticket> tickets)
{
    public Guid Id { get; } = id;
    private readonly Column _backlog = new(WipLimit.NoWipLimit(), tickets.Where(x => x.IsInBacklogColumn()).ToList());
    private readonly Column _todo = new(WipLimit.NoWipLimit(), tickets.Where(x => x.IsInTodoColumn()).ToList());
    private readonly Column _ongoing = new(wipLimit, tickets.Where(x => x.IsInOngoingColumn()).ToList());
    private readonly Column _done = new(WipLimit.NoWipLimit(), tickets.Where(x => x.IsInDoneColumn()).ToList());

    public IReadOnlyList<Ticket> Backlog => _backlog.Tickets;
    public IReadOnlyList<Ticket> Todo => _todo.Tickets;
    public IReadOnlyList<Ticket> Ongoing => _ongoing.Tickets;
    public IReadOnlyList<Ticket> Done => _done.Tickets;

    public static Board CreateEmptyBoard(Guid boardId, string name)
    {
        return new Board(boardId, WipLimit.NoWipLimit(), []);
    }

    public void MoveTicketInTodoColumn(Guid ticketId)
    {
        var ticket = _backlog.RemoveTicket(ticketId);

        if (ticket == null) return;

        _todo.AddTicket(ticket);
    }

    public bool MoveTicketInOngoingColumn(Guid ticketId)
    {
        var ticket = _todo.RemoveTicket(ticketId);

        if (ticket == null) return false;

        _ongoing.AddTicket(ticket);

        return _ongoing.IsWipLimitExceeded();
    }

    public void MoveTicketInDoneColumn(Guid ticketId)
    {
        var ticket = _ongoing.RemoveTicket(ticketId);

        if (ticket == null) return;

        _done.AddTicket(ticket);
    }

    public void CreateTicket(Guid ticketId, string title, string description)
    {
        _backlog.AddTicket(new Ticket(ticketId, TicketStatus.Backlog));
    }

    public void DefineWipLimit(WipLimit wipLimit)
    {
        _ongoing.DefineWipLimit(wipLimit);
    }

    public void RemoveWipLimit()
    {
        _ongoing.RemoveWipLimit();
    }
}
