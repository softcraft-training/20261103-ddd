namespace Reporting.Core.Domain.Events;

public record TaskStarted(string AggregateId, Guid TaskId, DateTime At) : IEvent;
