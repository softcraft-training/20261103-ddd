namespace KanbanBoard.Core.Domain.Events;

public record WipLimitRemoved(Guid AggregateId) : IDomainEvent;
