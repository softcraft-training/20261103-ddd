using System.Collections.Immutable;
using Reporting.Core.Domain.Events;

namespace Reporting.Core.Domain;

public enum Status { Todo, OnGoing, Done }

public record RecordTaskCreated(Guid TaskId, DateTime At);
public record RecordTaskStatusChanged(Guid TaskId, Status To, DateTime At);

public record TaskTimeline(DateTime RequestedAt, DateTime? StartedAt, DateTime? CompletedAt);

public record MetricReportState(ImmutableDictionary<Guid, TaskTimeline> TaskTimelines)
{
    public static MetricReportState Empty() => new(ImmutableDictionary<Guid, TaskTimeline>.Empty);
}

public class MetricReport
{
    public static string BuildIdFromBoardId(Guid boardId) => $"{boardId}-metricReport";

    public string Id { get; }
    private MetricReportState _state;

    public MetricReport(string id, IEvent[] history)
    {
        Id = id;
        _state = ApplyAll(MetricReportState.Empty(), history);
    }

    public IEvent[] Handle(RecordTaskCreated cmd)
    {
        if (_state.TaskTimelines.ContainsKey(cmd.TaskId)) return [];

        var events = new IEvent[] { new TaskRequested(Id, cmd.TaskId, cmd.At) };

        _state = ApplyAll(_state, events);

        return events;
    }

    public IEvent[] Handle(RecordTaskStatusChanged cmd)
    {
        var taskTimeline = _state.TaskTimelines.GetValueOrDefault(cmd.TaskId);

        if (taskTimeline == null) return [];

        var events = cmd.To switch
        {
            Status.OnGoing
                when taskTimeline is {StartedAt: null, CompletedAt: null} => new IEvent[] {
                    new TaskStarted(Id, cmd.TaskId, cmd.At)
                },
            Status.Done
                when taskTimeline is {StartedAt: not null, CompletedAt: null} => new IEvent[] {
                    new TaskCompleted(Id, cmd.TaskId, cmd.At, cmd.At - taskTimeline.RequestedAt, cmd.At - taskTimeline.StartedAt.Value)
                },
            _ => []
        };

        _state = ApplyAll(_state, events);

        return events;
    }

    private MetricReportState ApplyAll(MetricReportState state, IEvent[] events)
    {
        return events.Aggregate(state, Apply);
    }

    private MetricReportState Apply(MetricReportState state, IEvent evt)
    {
        return evt switch
        {
            TaskRequested e => state with
            {
                TaskTimelines = state.TaskTimelines.SetItem(e.TaskId, new TaskTimeline(e.At, null, null))
            },
            TaskStarted e => state with
            {
                TaskTimelines = state.TaskTimelines.SetItem(e.TaskId, state.TaskTimelines[e.TaskId] with { StartedAt = e.At })
            },
            TaskCompleted e => state with
            {
                TaskTimelines = state.TaskTimelines.SetItem(e.TaskId, state.TaskTimelines[e.TaskId] with { CompletedAt = e.At })
            },
            _ => state
        };
    }
}
