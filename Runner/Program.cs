using KanbanBoard.Infra.Adapters;
using KanbanBoard.Infra.Api;
using Reporting.Infra.Adapters;
using Reporting.Infra.Api;
using Reporting.Infra.Handlers;
using Runner;

var builder = WebApplication.CreateBuilder(args);

var database = new SqliteDatabase("Data Source=kanbanboard.db");
var eventStore = new EventStore();
var pubSub = new InMemoryPubSub();

database.Init();

builder.Services.AddSingleton<KanbanBoard.Core.UseCases.Ports.IDatabase>(database);
builder.Services.AddSingleton<KanbanBoard.Core.UseCases.Ports.IPublisher>(pubSub);
builder.Services.AddScoped<KanbanBoard.Core.UseCases.Ports.IBoardRepository, BoardRepository>();
builder.Services.AddScoped<KanbanBoard.Core.UseCases.Ports.IAlertService, AlertService>();
builder.Services.AddScoped<KanbanBoard.Core.UseCases.Ports.IClock, Clock>();
builder.Services.AddScoped<KanbanBoard.Core.UseCases.BoardService>();
builder.Services.AddScoped<KanbanBoard.Infra.Api.Queries.BoardQueries>();

builder.Services.AddSingleton<Reporting.Core.UseCases.Ports.IDatabase>(database);
builder.Services.AddScoped<Reporting.Infra.Readmodels.MetricReportReadmodel>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

BoardEndpoints.MapEndpoints(app);
ReportingEndpoints.MapEndpoints(app);
ReportingHandlers.RegisterHandlers(pubSub, database, eventStore);

app.Run();
