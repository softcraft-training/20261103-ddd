using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Runner;

public class SqliteDatabase(string cnx) : KanbanBoard.Core.UseCases.Ports.IDatabase, Reporting.Core.UseCases.Ports.IDatabase
{
    public IDbConnection CreateConnection()
    {
        return new SqliteConnection(cnx);
    }

    public void Init()
    {
        using var connection = CreateConnection();

        connection.Open();

        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS boards (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                wip_limit_value INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS tickets (
                id TEXT PRIMARY KEY,
                board_id TEXT NOT NULL,
                title TEXT NOT NULL,
                description TEXT NOT NULL,
                status INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS events (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                aggregate_name TEXT NOT NULL,
                aggregate_id TEXT NOT NULL,
                event_type TEXT NOT NULL,
                event_num INTEGER NOT NULL,
                data TEXT NOT NULL,
                UNIQUE(aggregate_name, aggregate_id, event_num)
            );
            CREATE TABLE IF NOT EXISTS metric_report_readmodel (
                id TEXT PRIMARY KEY,
                leadtime_total_seconds INTEGER NOT NULL,
                cycletime_total_seconds INTEGER NOT NULL,
                inprogress_count INTEGER NOT NULL,
                completed_count INTEGER NOT NULL,
                last_update_at TEXT NOT NULL
            );"
        );
    }
}
