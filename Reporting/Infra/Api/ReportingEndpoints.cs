using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Reporting.Core.UseCases.Ports;
using Reporting.Infra.Readmodels;

namespace Reporting.Infra.Api;

public class ReportingEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reporting/");

        group.MapGet("{boardId:guid}", (Guid boardId, [FromServices] IDatabase database, [FromServices] MetricReportReadmodel metricReportReadmodel) =>
            Execute(() =>
            {
                using var connection = database.CreateConnection();

                connection.Open();

                var metricReport = metricReportReadmodel.Get(connection, boardId);

                if (metricReport == null) return Results.NotFound();

                return Results.Ok(metricReport);
            })
        );
    }

    private static IResult Execute(Func<IResult> action)
    {
        try
        {
            return action();
        }
        catch (Exception)
        {
            return Results.InternalServerError();
        }
    }
}
