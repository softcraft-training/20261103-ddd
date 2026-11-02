namespace KanbanBoard.Core.Domain.Events;

public interface IDomainEvent
{
    Guid AggregateId { get; }
}
