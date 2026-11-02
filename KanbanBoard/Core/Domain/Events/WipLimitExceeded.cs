namespace KanbanBoard.Core.Domain.Events;

public record WipLimitExceeded(Guid AggregateId) : IDomainEvent;
