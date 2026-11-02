namespace Reporting.Core.Domain.Events;

public record TaskRequested(string AggregateId, Guid TaskId, DateTime At) : IEvent;
