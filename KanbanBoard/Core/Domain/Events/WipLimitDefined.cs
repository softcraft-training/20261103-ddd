namespace KanbanBoard.Core.Domain.Events;

public record WipLimitDefined(Guid AggregateId, int Value) : IDomainEvent;
