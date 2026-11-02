namespace KanbanBoard.Core.Domain.Events;

public record TicketWritten(Guid AggregateId, Guid TicketId, string Title, string Description, DateTime At) : IDomainEvent;
