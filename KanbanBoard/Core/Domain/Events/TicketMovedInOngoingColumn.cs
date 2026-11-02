namespace KanbanBoard.Core.Domain.Events;

public record TicketMovedInOngoingColumn(Guid AggregateId, Guid TicketId, DateTime At) : IDomainEvent;
