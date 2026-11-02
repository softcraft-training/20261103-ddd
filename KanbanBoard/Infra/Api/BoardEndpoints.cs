using KanbanBoard.Core.Domain;
using KanbanBoard.Core.UseCases;
using KanbanBoard.Infra.Api.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace KanbanBoard.Infra.Api;

public record CreateBoardDto(string name);

public record CreateTicketDto(string title, string description);

public record DefineWipLimitDto(int value);

public class BoardEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/boards");

        group.MapGet("{boardId:guid}", (Guid boardId, [FromServices] BoardQueries boardQueries) =>
            Execute(() =>
            {
                var board = boardQueries.GetBoard(boardId);

                if (board == null) return Results.NotFound();

                return Results.Ok(board);
            })
        );

        group.MapPost("", ([FromServices] BoardService boardService, [FromBody] CreateBoardDto cmd) =>
            Execute(() =>
            {
                if (String.IsNullOrWhiteSpace(cmd.name)) return Results.BadRequest("name has to be defined");

                var boardId = boardService.CreateEmptyBoard(cmd.name);

                return Results.Created("", new {boardId});
            })
        );

        group.MapPost("{boardId:guid}/tickets", (Guid boardId, [FromServices] BoardService boardService, [FromBody] CreateTicketDto cmd) =>
            Execute(() =>
            {
                if (String.IsNullOrWhiteSpace(cmd.title)) return Results.BadRequest("title has to be defined");
                if (String.IsNullOrWhiteSpace(cmd.description)) return Results.BadRequest("description has to be defined");

                var ticketId = boardService.CreateTicket(boardId, cmd.title, cmd.description);

                return Results.Created("", new {ticketId});
            })
        );

        group.MapPost("{boardId:guid}/define-wip-limit", (Guid boardId, [FromServices] BoardService boardService, [FromBody] DefineWipLimitDto cmd) =>
            Execute(() =>
            {
                if (cmd.value < 0) return Results.BadRequest("WIP Limit has to be a positive number");

                boardService.DefineWipLimit(boardId, new WipLimit(cmd.value));

                return Results.Ok();
            })
        );

        group.MapPut("{boardId:guid}/tickets/{ticketId:guid}/move-in-todo-column", (Guid boardId, Guid ticketId, [FromServices] BoardService boardService) =>
            Execute(() =>
            {
                boardService.MoveTicketInTodoColumn(boardId, ticketId);

                return Results.Ok();
            })
        );

        group.MapPut("{boardId:guid}/tickets/{ticketId:guid}/move-in-ongoing-column", (Guid boardId, Guid ticketId, [FromServices] BoardService boardService) =>
            Execute(() =>
            {
                boardService.MoveTicketInOngoingColumn(boardId, ticketId);

                return Results.Ok();
            })
        );

        group.MapPut("{boardId:guid}/tickets/{ticketId:guid}/move-in-done-column", (Guid boardId, Guid ticketId, [FromServices] BoardService boardService) =>
            Execute(() =>
            {
                boardService.MoveTicketInDoneColumn(boardId, ticketId);

                return Results.Ok();
            })
        );

        group.MapDelete("{boardId:guid}/remove-wip-limit", (Guid boardId, [FromServices] BoardService boardService) =>
            Execute(() =>
            {
                boardService.RemoveWipLimit(boardId);

                return Results.Ok();
            })
        );
    }

    private static IResult Execute(Func<IResult> action)
    {
        try
        {
            return action();
        }
        catch (UnknownBoardException)
        {
            return Results.NotFound();
        }
        catch (Exception)
        {
            return Results.InternalServerError();
        }
    }
}
