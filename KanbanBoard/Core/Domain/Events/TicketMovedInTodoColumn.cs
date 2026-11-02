namespace KanbanBoard.Core.Domain.Events;

public record TicketMovedInTodoColumn(Guid AggregateId, Guid TicketId, DateTime At) : IDomainEvent;
