using System.Data;
using Dapper;
using Reporting.Core.Domain;
using Reporting.Core.Domain.Events;

namespace Reporting.Infra.Readmodels;

public record MetricReportDto(TimeSpan leadtime, TimeSpan cycletime, int inProgressTasks, int completedTasks, DateTime lastUpdateAt);

public record MetricReportDao(long leadtimeTotalSeconds, long cycletimeTotalSeconds, long inProgressCount, long completedCount, string lastUpdateAt);

public class MetricReportReadmodel
{
    public MetricReportDto? Get(IDbConnection connection, Guid boardId)
    {
        var metricReport = connection.QueryFirstOrDefault<MetricReportDao>("SELECT leadtime_total_seconds AS leadtimeTotalSeconds, cycletime_total_seconds AS cycletimeTotalSeconds, inprogress_count AS inProgressCount, completed_count AS completedCount, last_update_at AS lastUpdateAt FROM metric_report_readmodel WHERE id=@id", new { id = MetricReport.BuildIdFromBoardId(boardId) });

        if (metricReport == null) return null;

        return new MetricReportDto(
            metricReport.completedCount > 0 ? TimeSpan.FromSeconds((double)metricReport.leadtimeTotalSeconds / metricReport.completedCount) : TimeSpan.Zero,
            metricReport.completedCount > 0 ? TimeSpan.FromSeconds((double)metricReport.cycletimeTotalSeconds / metricReport.completedCount) : TimeSpan.Zero,
            (int)metricReport.inProgressCount,
            (int)metricReport.completedCount,
            DateTime.Parse(metricReport.lastUpdateAt, null, System.Globalization.DateTimeStyles.RoundtripKind));
    }

    public void HandleEvent(IDbConnection connection, IEvent evt)
    {
        switch (evt)
        {
            case TaskRequested taskRequested:
                connection.Execute(@"
                    INSERT INTO metric_report_readmodel (id, leadtime_total_seconds, cycletime_total_seconds, inprogress_count, completed_count, last_update_at) VALUES (@id, @leadtimeSeconds, @cycletimeSeconds, 0, 0, @lastUpdateAt)
                    ON CONFLICT(id) DO NOTHING;
                ", new {
                    id = taskRequested.AggregateId,
                    leadtimeSeconds = 0,
                    cycletimeSeconds = 0,
                    lastUpdateAt = taskRequested.At.ToString("O")
                });
                break;

            case TaskStarted taskStarted:
                connection.Execute(@"UPDATE metric_report_readmodel SET
                    inprogress_count = inprogress_count + 1,
                    last_update_at = @lastUpdateAt
                    WHERE id=@id;
                ", new {
                    id = taskStarted.AggregateId,
                    lastUpdateAt = taskStarted.At.ToString("O")
                });
                break;

            case TaskCompleted taskCompleted:
                connection.Execute(@"UPDATE metric_report_readmodel SET
                    leadtime_total_seconds = leadtime_total_seconds + @leadtimeSeconds,
                    cycletime_total_seconds = cycletime_total_seconds + @cycletimeSeconds,
                    inprogress_count = inprogress_count - 1,
                    completed_count = completed_count + 1,
                    last_update_at = @lastUpdateAt
                    WHERE id=@id;
                ", new {
                    id = taskCompleted.AggregateId,
                    leadtimeSeconds = (long)taskCompleted.Leadtime.TotalSeconds,
                    cycletimeSeconds = (long)taskCompleted.Cycletime.TotalSeconds,
                    lastUpdateAt = taskCompleted.At.ToString("O")
                });
                break;
        }
    }
}
