using KanbanBoard.Infra.Adapters;
using KanbanBoard.Infra.Api;
using Runner;

var builder = WebApplication.CreateBuilder(args);

var database = new SqliteDatabase("Data Source=kanbanboard.db");

database.Init();

builder.Services.AddSingleton<KanbanBoard.Core.UseCases.Ports.IDatabase>(database);
builder.Services.AddScoped<KanbanBoard.Core.UseCases.Ports.IBoardRepository, BoardRepository>();
builder.Services.AddScoped<KanbanBoard.Core.UseCases.Ports.IAlertService, AlertService>();
builder.Services.AddScoped<KanbanBoard.Core.UseCases.Ports.IClock, Clock>();
builder.Services.AddScoped<KanbanBoard.Core.UseCases.BoardService>();
builder.Services.AddScoped<KanbanBoard.Infra.Api.Queries.BoardQueries>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

BoardEndpoints.MapEndpoints(app);

app.Run();
