namespace Reporting.Core.Domain.Events;

public record TaskCompleted(string AggregateId, Guid TaskId, DateTime At, TimeSpan Leadtime, TimeSpan Cycletime) : IEvent;
