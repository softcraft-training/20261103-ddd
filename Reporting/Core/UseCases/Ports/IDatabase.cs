using System.Data;

namespace Reporting.Core.UseCases.Ports;

public interface IDatabase
{
    IDbConnection CreateConnection();
}
