using FluentAssertions;
using KanbanBoard.Core.Domain;

namespace KanbanBoard.Tests.Core.Domain;

public class BoardTests
{
    private static Ticket NewTicket(Guid id, TicketStatus status) => new(id, status);

    [Fact]
    public void CreateEmptyBoard_ReturnsBoardWithEmptyColumns()
    {
        var boardId = Guid.NewGuid();

        var board = Board.CreateEmptyBoard(boardId, "My board");

        board.Id.Should().Be(boardId);
        board.Backlog.Should().BeEmpty();
        board.Todo.Should().BeEmpty();
        board.Ongoing.Should().BeEmpty();
        board.Done.Should().BeEmpty();
    }

    [Fact]
    public void CreateTicket_AddsTicketToBacklog()
    {
        var board = new Board(Guid.NewGuid(), WipLimit.NoWipLimit(), []);
        var ticketId = Guid.NewGuid();

        board.CreateTicket(ticketId, "title", "description");

        board.Backlog.Select(x => x.Id).Should().Equal(ticketId);
    }

    [Fact]
    public void MoveTicketInTodoColumn_WhenTicketIsInBacklog_MovesIt()
    {
        var ticketId = Guid.NewGuid();
        var board = new Board(Guid.NewGuid(), WipLimit.NoWipLimit(), [NewTicket(ticketId, TicketStatus.Backlog)]);

        board.MoveTicketInTodoColumn(ticketId);

        board.Backlog.Should().BeEmpty();
        board.Todo.Select(x => x.Id).Should().Equal(ticketId);
    }

    [Fact]
    public void MoveTicketInTodoColumn_WhenTicketIsNotInBacklog_DoesNothing()
    {
        var ticketId = Guid.NewGuid();
        var board = new Board(Guid.NewGuid(), WipLimit.NoWipLimit(), [NewTicket(ticketId, TicketStatus.Done)]);

        board.MoveTicketInTodoColumn(ticketId);

        board.Todo.Should().BeEmpty();
        board.Done.Select(x => x.Id).Should().Equal(ticketId);
    }

    [Fact]
    public void MoveTicketInOngoingColumn_WhenTicketIsInTodo_MovesIt()
    {
        var ticketId = Guid.NewGuid();
        var board = new Board(Guid.NewGuid(), WipLimit.NoWipLimit(), [NewTicket(ticketId, TicketStatus.Todo)]);

        board.MoveTicketInOngoingColumn(ticketId);

        board.Todo.Should().BeEmpty();
        board.Ongoing.Select(x => x.Id).Should().Equal(ticketId);
    }

    [Fact]
    public void MoveTicketInOngoingColumn_WhenTicketIsNotInTodo_DoesNothing()
    {
        var ticketId = Guid.NewGuid();
        var board = new Board(Guid.NewGuid(), WipLimit.NoWipLimit(), [NewTicket(ticketId, TicketStatus.Backlog)]);

        board.MoveTicketInOngoingColumn(ticketId);

        board.Ongoing.Should().BeEmpty();
        board.Backlog.Select(x => x.Id).Should().Equal(ticketId);
    }

    [Fact]
    public void MoveTicketInOngoingColumn_WhenBelowWipLimit_DoesNotExceedWipLimit()
    {
        var ticketId = Guid.NewGuid();
        var board = new Board(Guid.NewGuid(), new WipLimit(3), [NewTicket(ticketId, TicketStatus.Todo)]);

        var wipLimitExceeded = board.MoveTicketInOngoingColumn(ticketId);

        wipLimitExceeded.Should().BeFalse();
    }

    [Fact]
    public void MoveTicketInOngoingColumn_WhenItFillsUpTheWipLimit_ExceedsWipLimit()
    {
        var alreadyOngoingTicket = NewTicket(Guid.NewGuid(), TicketStatus.OnGoing);
        var incomingTicketId = Guid.NewGuid();
        var incomingTicket = NewTicket(incomingTicketId, TicketStatus.Todo);
        var board = new Board(Guid.NewGuid(), new WipLimit(1), [alreadyOngoingTicket, incomingTicket]);

        var wipLimitExceeded = board.MoveTicketInOngoingColumn(incomingTicketId);

        wipLimitExceeded.Should().BeTrue();
        board.Ongoing.Should().HaveCount(2);
    }

    [Fact]
    public void MoveTicketInOngoingColumn_WhenNoWipLimitIsDefined_NeverExceedsWipLimit()
    {
        var alreadyOngoingTicket = NewTicket(Guid.NewGuid(), TicketStatus.OnGoing);
        var incomingTicketId = Guid.NewGuid();
        var incomingTicket = NewTicket(incomingTicketId, TicketStatus.Todo);
        var board = new Board(Guid.NewGuid(), WipLimit.NoWipLimit(), [alreadyOngoingTicket, incomingTicket]);

        var wipLimitExceeded = board.MoveTicketInOngoingColumn(incomingTicketId);

        wipLimitExceeded.Should().BeFalse();
    }

    [Fact]
    public void MoveTicketInDoneColumn_WhenTicketIsInOngoing_MovesIt()
    {
        var ticketId = Guid.NewGuid();
        var board = new Board(Guid.NewGuid(), WipLimit.NoWipLimit(), [NewTicket(ticketId, TicketStatus.OnGoing)]);

        board.MoveTicketInDoneColumn(ticketId);

        board.Ongoing.Should().BeEmpty();
        board.Done.Select(x => x.Id).Should().Equal(ticketId);
    }

    [Fact]
    public void MoveTicketInDoneColumn_WhenTicketIsNotInOngoing_DoesNothing()
    {
        var ticketId = Guid.NewGuid();
        var board = new Board(Guid.NewGuid(), WipLimit.NoWipLimit(), [NewTicket(ticketId, TicketStatus.Todo)]);

        board.MoveTicketInDoneColumn(ticketId);

        board.Done.Should().BeEmpty();
        board.Todo.Select(x => x.Id).Should().Equal(ticketId);
    }

    [Fact]
    public void DefineWipLimit_AppliesImmediatelyToTheOngoingColumn()
    {
        var alreadyOngoingTicket = NewTicket(Guid.NewGuid(), TicketStatus.OnGoing);
        var incomingTicketId = Guid.NewGuid();
        var incomingTicket = NewTicket(incomingTicketId, TicketStatus.Todo);
        var board = new Board(Guid.NewGuid(), WipLimit.NoWipLimit(), [alreadyOngoingTicket, incomingTicket]);

        board.DefineWipLimit(new WipLimit(1));
        var wipLimitExceeded = board.MoveTicketInOngoingColumn(incomingTicketId);

        wipLimitExceeded.Should().BeTrue();
    }

    [Fact]
    public void RemoveWipLimit_AllowsMovingIntoOngoingWithoutExceedingWipLimit()
    {
        var alreadyOngoingTicket = NewTicket(Guid.NewGuid(), TicketStatus.OnGoing);
        var incomingTicketId = Guid.NewGuid();
        var incomingTicket = NewTicket(incomingTicketId, TicketStatus.Todo);
        var board = new Board(Guid.NewGuid(), new WipLimit(1), [alreadyOngoingTicket, incomingTicket]);

        board.RemoveWipLimit();
        var wipLimitExceeded = board.MoveTicketInOngoingColumn(incomingTicketId);

        wipLimitExceeded.Should().BeFalse();
    }

    [Fact]
    public void Ticket_CanFlowThroughEveryColumnInOrder()
    {
        var board = new Board(Guid.NewGuid(), WipLimit.NoWipLimit(), []);
        var ticketId = Guid.NewGuid();
        board.CreateTicket(ticketId, "title", "description");

        board.MoveTicketInTodoColumn(ticketId);
        board.Todo.Select(x => x.Id).Should().Equal(ticketId);

        board.MoveTicketInOngoingColumn(ticketId);
        board.Ongoing.Select(x => x.Id).Should().Equal(ticketId);

        board.MoveTicketInDoneColumn(ticketId);
        board.Done.Select(x => x.Id).Should().Equal(ticketId);

        board.MoveTicketInTodoColumn(ticketId);
        board.MoveTicketInOngoingColumn(ticketId);
        board.Backlog.Should().BeEmpty();
        board.Todo.Should().BeEmpty();
        board.Ongoing.Should().BeEmpty();
        board.Done.Select(x => x.Id).Should().Equal(ticketId);
    }
}
