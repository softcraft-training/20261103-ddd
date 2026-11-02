namespace KanbanBoard.Core.Domain.Events;

public record BoardCreated(Guid AggregateId, string Name) : IDomainEvent;
