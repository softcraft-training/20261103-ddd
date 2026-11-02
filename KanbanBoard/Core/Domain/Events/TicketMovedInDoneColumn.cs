namespace KanbanBoard.Core.Domain.Events;

public record TicketMovedInDoneColumn(Guid AggregateId, Guid TicketId, DateTime At) : IDomainEvent;
