namespace KanbanBoard.Core.Domain;

public enum TicketStatus { Backlog, Todo, OnGoing, Done }

public class Ticket(Guid id, TicketStatus status)
{
    public Guid Id { get; } = id;

    public bool IsInBacklogColumn() => status == TicketStatus.Backlog;
    public bool IsInTodoColumn() => status == TicketStatus.Todo;
    public bool IsInOngoingColumn() => status == TicketStatus.OnGoing;
    public bool IsInDoneColumn() => status == TicketStatus.Done;
}
