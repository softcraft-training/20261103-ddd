using Reporting.Core.Domain;
using Reporting.Core.UseCases.Ports;

namespace Reporting.Core.UseCases;

public class MetricReportService(IDatabase database, IEventStore eventStore)
{
    public void RecordTaskCreated(Guid boardId, Guid taskId, DateTime at) {
        using var connection = database.CreateConnection();

        connection.Open();

        var metricReportId = MetricReport.BuildIdFromBoardId(boardId);

        var (history, lastEventNum) = eventStore.LoadMetricReportEvents(connection, metricReportId);

        var metricReport = new MetricReport(metricReportId, history);

        var events = metricReport.Handle(new RecordTaskCreated(taskId, at));

        eventStore.SaveMetricReportEvents(connection, metricReportId, lastEventNum, events);
    }

    public void RecordTaskStatusChanged(Guid boardId, Guid taskId, Status to, DateTime at) {
        using var connection = database.CreateConnection();

        connection.Open();

        var metricReportId = MetricReport.BuildIdFromBoardId(boardId);

        var (history, lastEventNum) = eventStore.LoadMetricReportEvents(connection, metricReportId);

        var metricReport = new MetricReport(metricReportId, history);

        var events = metricReport.Handle(new RecordTaskStatusChanged(taskId, to, at));

        eventStore.SaveMetricReportEvents(connection, metricReportId, lastEventNum, events);
    }
}
