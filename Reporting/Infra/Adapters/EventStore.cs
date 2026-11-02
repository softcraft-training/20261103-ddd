using System.Data;
using System.Text.Json;
using Dapper;
using Reporting.Core.Domain.Events;
using Reporting.Core.UseCases.Ports;
using Reporting.Infra.Readmodels;

namespace Reporting.Infra.Adapters;

public record EventDao(string data, long event_num, string event_type);

public class EventStore : IEventStore
{
    public (IEvent[], long) LoadMetricReportEvents(IDbConnection connection, string aggregateId)
    {
        var events = connection
            .Query<EventDao>("SELECT data, event_num, event_type FROM events WHERE aggregate_name=@aggregateName AND aggregate_id=@aggregateId ORDER BY event_num", new {
                aggregateId = aggregateId,
                aggregateName = "MetricReport"
            }).ToList();


        var lastEventNum = events.LastOrDefault()?.event_num ?? 0;

        return (events.Select(e =>
            e.event_type switch {
                "TaskFinished" => (IEvent)JsonSerializer.Deserialize<TaskCompleted>(e.data)!,
                "TaskRequested" => (IEvent)JsonSerializer.Deserialize<TaskRequested>(e.data)!,
                "TaskStarted" => (IEvent)JsonSerializer.Deserialize<TaskStarted>(e.data)!,
                _ => throw new ArgumentOutOfRangeException()
            }).ToArray(), lastEventNum);

    }

    public void SaveMetricReportEvents(IDbConnection connection, string aggregateId, long lastEventNum, IEvent[] events)
    {
        var metricReportReadmodel = new MetricReportReadmodel();

        foreach (var (evt, eventNum) in events.Select((x, index) => (x, lastEventNum + index + 1) ).ToList())
        {
            connection.Execute("INSERT INTO events (aggregate_id, aggregate_name, event_num, event_type, data) VALUES (@aggregateId, @aggregateName, @eventNum, @eventType, @data)", new {
                aggregateId = aggregateId,
                aggregateName = "MetricReport",
                eventNum = eventNum,
                eventType = evt switch {
                    TaskCompleted => "TaskFinished",
                    TaskRequested => "TaskRequested",
                    TaskStarted => "TaskStarted",
                    _ => throw new ArgumentOutOfRangeException()
                },
                data = JsonSerializer.Serialize(evt, evt.GetType())
            });

            metricReportReadmodel.HandleEvent(connection, evt);
        }
    }
}
