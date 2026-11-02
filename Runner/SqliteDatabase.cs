using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Runner;

public class SqliteDatabase(string cnx) : KanbanBoard.Core.UseCases.Ports.IDatabase
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
            );"
        );
    }
}
