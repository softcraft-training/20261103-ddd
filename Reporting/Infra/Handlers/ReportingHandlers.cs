using IntegrationEvents;
using Reporting.Core.UseCases;
using Reporting.Core.UseCases.Ports;

namespace Reporting.Infra.Handlers;

public interface ISubscriber
{
    void Register<T>(Action<T> action) where T : class;
}

public class ReportingHandlers
{
    public static void RegisterHandlers(ISubscriber subscriber, IDatabase database, IEventStore eventStore)
    {
        subscriber.Register<TaskCreated>(taskCreated => {
            var metricReportService = new MetricReportService(database, eventStore);

            metricReportService.RecordTaskCreated(taskCreated.BoardId, taskCreated.TaskId, taskCreated.At);
        });

        subscriber.Register<TaskStatusChanged>(taskStatusChanged => {
            var metricReportService = new MetricReportService(database, eventStore);

            var status = taskStatusChanged.To switch {
                Status.Todo => Core.Domain.Status.Todo,
                Status.OnGoing => Core.Domain.Status.OnGoing,
                Status.Done => Core.Domain.Status.Done,
                _ => throw new ArgumentOutOfRangeException()
            };

            metricReportService.RecordTaskStatusChanged(taskStatusChanged.BoardId, taskStatusChanged.TaskId, status, taskStatusChanged.At);
        });
    }
}
