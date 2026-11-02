using System.Data;
using Reporting.Core.Domain.Events;

namespace Reporting.Core.UseCases.Ports;

public interface IEventStore
{
    (IEvent[], long) LoadMetricReportEvents(IDbConnection connection, string aggregateId);
    void SaveMetricReportEvents(IDbConnection connection, string aggregateId, long lastEventNum, IEvent[] events);
}
