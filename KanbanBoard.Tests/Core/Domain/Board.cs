using FluentAssertions;
using KanbanBoard.Core.Domain;
using KanbanBoard.Core.Domain.Events;

namespace KanbanBoard.Tests.Core.Domain;

public class BoardTests
{
    private static Ticket NewTicket(Guid id, TicketStatus status) => new(id, status);

    [Fact]
    public void CreateEmptyBoard_ReturnsBoardCreatedEvent()
    {
        var boardId = Guid.NewGuid();

        var events = Board.CreateEmptyBoard(boardId, "My board");

        events.Should().Equal(new BoardCreated(boardId, "My board"));
    }

    [Fact]
    public void CreateTicket_AddsTicketToBacklog_AndReturnsTicketWrittenEvent()
    {
        var board = new Board(Guid.NewGuid(), WipLimit.NoWipLimit(), []);
        var ticketId = Guid.NewGuid();

        var events = board.CreateTicket(ticketId, "title", "description", new DateTime(2026, 01, 01));

        events.Should().Equal(new TicketWritten(board.Id, ticketId, "title", "description", new DateTime(2026, 01, 01)));

        board.MoveTicketInTodoColumn(ticketId, new DateTime(2026, 01, 01)).Should().Equal(new TicketMovedInTodoColumn(board.Id, ticketId, new DateTime(2026, 01, 01)));
    }

    [Fact]
    public void MoveTicketInTodoColumn_WhenTicketIsInBacklog_MovesItAndReturnsEvent()
    {
        var ticketId = Guid.NewGuid();
        var board = new Board(Guid.NewGuid(), WipLimit.NoWipLimit(), [NewTicket(ticketId, TicketStatus.Backlog)]);

        var events = board.MoveTicketInTodoColumn(ticketId, new DateTime(2026, 01, 01));

        events.Should().Equal(new TicketMovedInTodoColumn(board.Id, ticketId, new DateTime(2026, 01, 01)));
    }

    [Fact]
    public void MoveTicketInTodoColumn_WhenTicketIsNotInBacklog_ReturnsNoEvents()
    {
        var ticketId = Guid.NewGuid();
        var board = new Board(Guid.NewGuid(), WipLimit.NoWipLimit(), [NewTicket(ticketId, TicketStatus.Done)]);

        var events = board.MoveTicketInTodoColumn(ticketId, new DateTime(2026, 01, 01));

        events.Should().BeEmpty();
    }

    [Fact]
    public void MoveTicketInOngoingColumn_WhenTicketIsInTodo_MovesItAndReturnsEvent()
    {
        var ticketId = Guid.NewGuid();
        var board = new Board(Guid.NewGuid(), WipLimit.NoWipLimit(), [NewTicket(ticketId, TicketStatus.Todo)]);

        var events = board.MoveTicketInOngoingColumn(ticketId, new DateTime(2026, 01, 01));

        events.Should().Equal(new TicketMovedInOngoingColumn(board.Id, ticketId, new DateTime(2026, 01, 01)));
    }

    [Fact]
    public void MoveTicketInOngoingColumn_WhenTicketIsNotInTodo_ReturnsNoEvents()
    {
        var ticketId = Guid.NewGuid();
        var board = new Board(Guid.NewGuid(), WipLimit.NoWipLimit(), [NewTicket(ticketId, TicketStatus.Backlog)]);

        var events = board.MoveTicketInOngoingColumn(ticketId, new DateTime(2026, 01, 01));

        events.Should().BeEmpty();
    }

    [Fact]
    public void MoveTicketInOngoingColumn_WhenBelowWipLimit_ReturnsOnlyTicketMovedEvent()
    {
        var ticketId = Guid.NewGuid();
        var board = new Board(Guid.NewGuid(), new WipLimit(3), [NewTicket(ticketId, TicketStatus.Todo)]);

        var events = board.MoveTicketInOngoingColumn(ticketId, new DateTime(2026, 01, 01));

        events.Should().Equal(new TicketMovedInOngoingColumn(board.Id, ticketId, new DateTime(2026, 01, 01)));
    }

    [Fact]
    public void MoveTicketInOngoingColumn_WhenItFillsUpTheWipLimit_AlsoReturnsWipLimitExceededEvent()
    {
        var alreadyOngoingTicket = NewTicket(Guid.NewGuid(), TicketStatus.OnGoing);
        var incomingTicketId = Guid.NewGuid();
        var incomingTicket = NewTicket(incomingTicketId, TicketStatus.Todo);
        var board = new Board(Guid.NewGuid(), new WipLimit(1), [alreadyOngoingTicket, incomingTicket]);

        var events = board.MoveTicketInOngoingColumn(incomingTicketId, new DateTime(2026, 01, 01));

        events.Should().Equal(
            new TicketMovedInOngoingColumn(board.Id, incomingTicketId, new DateTime(2026, 01, 01)),
            new WipLimitExceeded(board.Id)
        );
    }

    [Fact]
    public void MoveTicketInOngoingColumn_WhenNoWipLimitIsDefined_NeverReturnsWipLimitExceededEvent()
    {
        var alreadyOngoingTicket = NewTicket(Guid.NewGuid(), TicketStatus.OnGoing);
        var incomingTicketId = Guid.NewGuid();
        var incomingTicket = NewTicket(incomingTicketId, TicketStatus.Todo);
        var board = new Board(Guid.NewGuid(), WipLimit.NoWipLimit(), [alreadyOngoingTicket, incomingTicket]);

        var events = board.MoveTicketInOngoingColumn(incomingTicketId, new DateTime(2026, 01, 01));

        events.Should().Equal(new TicketMovedInOngoingColumn(board.Id, incomingTicketId, new DateTime(2026, 01, 01)));
    }

    [Fact]
    public void MoveTicketInDoneColumn_WhenTicketIsInOngoing_MovesItAndReturnsEvent()
    {
        var ticketId = Guid.NewGuid();
        var board = new Board(Guid.NewGuid(), WipLimit.NoWipLimit(), [NewTicket(ticketId, TicketStatus.OnGoing)]);

        var events = board.MoveTicketInDoneColumn(ticketId, new DateTime(2026, 01, 01));

        events.Should().Equal(new TicketMovedInDoneColumn(board.Id, ticketId, new DateTime(2026, 01, 01)));
    }

    [Fact]
    public void MoveTicketInDoneColumn_WhenTicketIsNotInOngoing_ReturnsNoEvents()
    {
        var ticketId = Guid.NewGuid();
        var board = new Board(Guid.NewGuid(), WipLimit.NoWipLimit(), [NewTicket(ticketId, TicketStatus.Todo)]);

        var events = board.MoveTicketInDoneColumn(ticketId, new DateTime(2026, 01, 01));

        events.Should().BeEmpty();
    }

    [Fact]
    public void DefineWipLimit_ReturnsWipLimitDefinedEvent()
    {
        var board = new Board(Guid.NewGuid(), WipLimit.NoWipLimit(), []);

        var events = board.DefineWipLimit(new WipLimit(5));

        events.Should().Equal(new WipLimitDefined(board.Id, 5));
    }

    [Fact]
    public void DefineWipLimit_AppliesImmediatelyToTheOngoingColumn()
    {
        var alreadyOngoingTicket = NewTicket(Guid.NewGuid(), TicketStatus.OnGoing);
        var incomingTicketId = Guid.NewGuid();
        var incomingTicket = NewTicket(incomingTicketId, TicketStatus.Todo);
        var board = new Board(Guid.NewGuid(), WipLimit.NoWipLimit(), [alreadyOngoingTicket, incomingTicket]);

        board.DefineWipLimit(new WipLimit(1));
        var events = board.MoveTicketInOngoingColumn(incomingTicketId, new DateTime(2026, 01, 01));

        events.Should().Equal(
            new TicketMovedInOngoingColumn(board.Id, incomingTicketId, new DateTime(2026, 01, 01)),
            new WipLimitExceeded(board.Id)
        );
    }

    [Fact]
    public void RemoveWipLimit_ReturnsWipLimitRemovedEvent()
    {
        var board = new Board(Guid.NewGuid(), new WipLimit(1), []);

        var events = board.RemoveWipLimit();

        events.Should().Equal(new WipLimitRemoved(board.Id));
    }

    [Fact]
    public void RemoveWipLimit_AllowsMovingIntoOngoingWithoutWipLimitExceededEvent()
    {
        var alreadyOngoingTicket = NewTicket(Guid.NewGuid(), TicketStatus.OnGoing);
        var incomingTicketId = Guid.NewGuid();
        var incomingTicket = NewTicket(incomingTicketId, TicketStatus.Todo);
        var board = new Board(Guid.NewGuid(), new WipLimit(1), [alreadyOngoingTicket, incomingTicket]);

        board.RemoveWipLimit();
        var events = board.MoveTicketInOngoingColumn(incomingTicketId, new DateTime(2026, 01, 01));

        events.Should().Equal(new TicketMovedInOngoingColumn(board.Id, incomingTicketId, new DateTime(2026, 01, 01)));
    }

    [Fact]
    public void Ticket_CanFlowThroughEveryColumnInOrder()
    {
        var board = new Board(Guid.NewGuid(), WipLimit.NoWipLimit(), []);
        var ticketId = Guid.NewGuid();
        board.CreateTicket(ticketId, "title", "description", new DateTime(2026, 01, 01));

        board.MoveTicketInTodoColumn(ticketId, new DateTime(2026, 01, 01)).Should().Equal(new TicketMovedInTodoColumn(board.Id, ticketId, new DateTime(2026, 01, 01)));
        board.MoveTicketInOngoingColumn(ticketId, new DateTime(2026, 01, 01)).Should().Equal(new TicketMovedInOngoingColumn(board.Id, ticketId, new DateTime(2026, 01, 01)));
        board.MoveTicketInDoneColumn(ticketId, new DateTime(2026, 01, 01)).Should().Equal(new TicketMovedInDoneColumn(board.Id, ticketId, new DateTime(2026, 01, 01)));

        board.MoveTicketInTodoColumn(ticketId, new DateTime(2026, 01, 01)).Should().BeEmpty();
        board.MoveTicketInOngoingColumn(ticketId, new DateTime(2026, 01, 01)).Should().BeEmpty();
        board.MoveTicketInDoneColumn(ticketId, new DateTime(2026, 01, 01)).Should().BeEmpty();
    }
}
