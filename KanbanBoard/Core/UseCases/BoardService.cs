using KanbanBoard.Core.Domain;
using KanbanBoard.Core.Domain.Events;
using KanbanBoard.Core.UseCases.Ports;
using IntegrationEvents;

namespace KanbanBoard.Core.UseCases;

public class UnknownBoardException : Exception;

public class BoardService(IDatabase database, IBoardRepository boardRepository, IAlertService alertService, IClock clock, IPublisher publisher)
{
    public Guid CreateEmptyBoard(string name)
    {
        using var connection = database.CreateConnection();

        connection.Open();

        var newBoardId = Guid.NewGuid();

        var events = Board.CreateEmptyBoard(newBoardId, name);

        boardRepository.Save(connection, events);

        HandleEvent(events);

        return newBoardId;
    }

    public void MoveTicketInTodoColumn(Guid boardId, Guid ticketId) {
        using var connection = database.CreateConnection();

        connection.Open();

        var board = boardRepository.Load(connection, boardId);

        if (board == null) throw new UnknownBoardException();

        var events = board.MoveTicketInTodoColumn(ticketId, clock.GetNow());

        boardRepository.Save(connection, events);

        HandleEvent(events);
    }

    public void MoveTicketInOngoingColumn(Guid boardId, Guid ticketId) {
        using var connection = database.CreateConnection();

        connection.Open();

        var board = boardRepository.Load(connection, boardId);

        if (board == null) throw new UnknownBoardException();

        var events = board.MoveTicketInOngoingColumn(ticketId, clock.GetNow());

        boardRepository.Save(connection, events);

        HandleEvent(events);
    }

    public void MoveTicketInDoneColumn(Guid boardId, Guid ticketId) {
        using var connection = database.CreateConnection();

        connection.Open();

        var board = boardRepository.Load(connection, boardId);

        if (board == null) throw new UnknownBoardException();

        var events = board.MoveTicketInDoneColumn(ticketId, clock.GetNow());

        boardRepository.Save(connection, events);

        HandleEvent(events);
    }

    public Guid CreateTicket(Guid boardId, string title, string description) {
        using var connection = database.CreateConnection();

        connection.Open();

        var board = boardRepository.Load(connection, boardId);

        if (board == null) throw new UnknownBoardException();

        var newTicketId = Guid.NewGuid();

        var events = board.CreateTicket(newTicketId, title, description, clock.GetNow());

        boardRepository.Save(connection, events);

        HandleEvent(events);

        return newTicketId;
    }

    public void DefineWipLimit(Guid boardId, WipLimit wipLimit) {
        using var connection = database.CreateConnection();

        connection.Open();

        var board = boardRepository.Load(connection, boardId);

        if (board == null) throw new UnknownBoardException();

        var events = board.DefineWipLimit(wipLimit);

        boardRepository.Save(connection, events);

        HandleEvent(events);
    }

    public void RemoveWipLimit(Guid boardId) {
        using var connection = database.CreateConnection();

        connection.Open();

        var board = boardRepository.Load(connection, boardId);

        if (board == null) throw new UnknownBoardException();

        var events = board.RemoveWipLimit();

        boardRepository.Save(connection, events);

        HandleEvent(events);
    }

    private void HandleEvent(IDomainEvent[] events)
    {
        foreach (var evt in events)
        {
            switch (evt)
            {
                case TicketWritten ticketWritten:
                    publisher.Publish(new TaskCreated(ticketWritten.AggregateId, ticketWritten.TicketId, ticketWritten.At));
                    break;
                case TicketMovedInTodoColumn ticketMovedInTodoColumn:
                    publisher.Publish(new TaskStatusChanged(ticketMovedInTodoColumn.AggregateId, ticketMovedInTodoColumn.TicketId, Status.Todo, ticketMovedInTodoColumn.At));
                    break;
                case TicketMovedInOngoingColumn ticketMovedInOngoingColumn:
                    publisher.Publish(new TaskStatusChanged(ticketMovedInOngoingColumn.AggregateId, ticketMovedInOngoingColumn.TicketId, Status.OnGoing, ticketMovedInOngoingColumn.At));
                    break;
                case TicketMovedInDoneColumn ticketMovedInDoneColumn:
                    publisher.Publish(new TaskStatusChanged(ticketMovedInDoneColumn.AggregateId, ticketMovedInDoneColumn.TicketId, Status.Done, ticketMovedInDoneColumn.At));
                    break;
                case WipLimitExceeded wipLimitExceeded:
                    alertService.SendAlert($"Wip limit exceeded",
                    $"Board {wipLimitExceeded.AggregateId} has reached WIP limit",
                    "dest"
                    );
                    break;
            }
        }
    }
}
