using FluentAssertions;
using Reporting.Core.Domain;
using Reporting.Core.Domain.Events;

namespace Reporting.Tests.Core.Domain;

public class MetricReportTests
{
    private static readonly DateTime RequestedAt = new(2026, 01, 01, 9, 0, 0);
    private static readonly DateTime StartedAt = new(2026, 01, 02, 9, 0, 0);
    private static readonly DateTime CompletedAt = new(2026, 01, 04, 18, 0, 0);

    private static string NewMetricReportId() => MetricReport.BuildIdFromBoardId(Guid.NewGuid());

    [Fact]
    public void BuildIdFromBoardId_SuffixesBoardIdWithMetricReport()
    {
        var boardId = Guid.NewGuid();

        MetricReport.BuildIdFromBoardId(boardId).Should().Be($"{boardId}-metricReport");
    }

    [Fact]
    public void RecordTaskCreated_WhenTaskIsUnknown_ReturnsTaskRequestedEvent()
    {
        var metricReport = new MetricReport(NewMetricReportId(), []);
        var taskId = Guid.NewGuid();

        var events = metricReport.Handle(new RecordTaskCreated(taskId, RequestedAt));

        events.Should().Equal(new TaskRequested(metricReport.Id, taskId, RequestedAt));
    }

    [Fact]
    public void RecordTaskCreated_WhenTaskIsAlreadyKnown_ReturnsNoEvents()
    {
        var id = NewMetricReportId();
        var taskId = Guid.NewGuid();
        var metricReport = new MetricReport(id, [new TaskRequested(id, taskId, RequestedAt)]);

        var events = metricReport.Handle(new RecordTaskCreated(taskId, StartedAt));

        events.Should().BeEmpty();
    }

    [Theory]
    [InlineData(Status.Todo)]
    [InlineData(Status.OnGoing)]
    [InlineData(Status.Done)]
    public void RecordTaskStatusChanged_WhenTaskIsUnknown_ReturnsNoEvents(Status to)
    {
        var metricReport = new MetricReport(NewMetricReportId(), []);

        var events = metricReport.Handle(new RecordTaskStatusChanged(Guid.NewGuid(), to, StartedAt));

        events.Should().BeEmpty();
    }

    [Fact]
    public void RecordTaskStatusChanged_ToTodo_ReturnsNoEvents()
    {
        var id = NewMetricReportId();
        var taskId = Guid.NewGuid();
        var metricReport = new MetricReport(id, [new TaskRequested(id, taskId, RequestedAt)]);

        var events = metricReport.Handle(new RecordTaskStatusChanged(taskId, Status.Todo, StartedAt));

        events.Should().BeEmpty();
    }

    [Fact]
    public void RecordTaskStatusChanged_ToOnGoing_WhenTaskIsRequested_ReturnsTaskStartedEvent()
    {
        var id = NewMetricReportId();
        var taskId = Guid.NewGuid();
        var metricReport = new MetricReport(id, [new TaskRequested(id, taskId, RequestedAt)]);

        var events = metricReport.Handle(new RecordTaskStatusChanged(taskId, Status.OnGoing, StartedAt));

        events.Should().Equal(new TaskStarted(id, taskId, StartedAt));
    }

    [Fact]
    public void RecordTaskStatusChanged_ToOnGoing_WhenTaskIsAlreadyStarted_ReturnsNoEvents()
    {
        var id = NewMetricReportId();
        var taskId = Guid.NewGuid();
        var metricReport = new MetricReport(id, [
            new TaskRequested(id, taskId, RequestedAt),
            new TaskStarted(id, taskId, StartedAt)
        ]);

        var events = metricReport.Handle(new RecordTaskStatusChanged(taskId, Status.OnGoing, CompletedAt));

        events.Should().BeEmpty();
    }

    [Fact]
    public void RecordTaskStatusChanged_ToOnGoing_WhenTaskIsCompleted_ReturnsNoEvents()
    {
        var id = NewMetricReportId();
        var taskId = Guid.NewGuid();
        var metricReport = new MetricReport(id, [
            new TaskRequested(id, taskId, RequestedAt),
            new TaskStarted(id, taskId, StartedAt),
            new TaskCompleted(id, taskId, CompletedAt, CompletedAt - RequestedAt, CompletedAt - StartedAt)
        ]);

        var events = metricReport.Handle(new RecordTaskStatusChanged(taskId, Status.OnGoing, CompletedAt.AddDays(1)));

        events.Should().BeEmpty();
    }

    [Fact]
    public void RecordTaskStatusChanged_ToDone_WhenTaskIsStarted_ReturnsTaskCompletedEventWithLeadtimeAndCycletime()
    {
        var id = NewMetricReportId();
        var taskId = Guid.NewGuid();
        var metricReport = new MetricReport(id, [
            new TaskRequested(id, taskId, RequestedAt),
            new TaskStarted(id, taskId, StartedAt)
        ]);

        var events = metricReport.Handle(new RecordTaskStatusChanged(taskId, Status.Done, CompletedAt));

        events.Should().Equal(new TaskCompleted(id, taskId, CompletedAt, TimeSpan.FromHours(81), TimeSpan.FromHours(57)));
    }

    [Fact]
    public void RecordTaskStatusChanged_ToDone_WhenTaskIsNotStarted_ReturnsNoEvents()
    {
        var id = NewMetricReportId();
        var taskId = Guid.NewGuid();
        var metricReport = new MetricReport(id, [new TaskRequested(id, taskId, RequestedAt)]);

        var events = metricReport.Handle(new RecordTaskStatusChanged(taskId, Status.Done, CompletedAt));

        events.Should().BeEmpty();
    }

    [Fact]
    public void RecordTaskStatusChanged_ToDone_WhenTaskIsAlreadyCompleted_ReturnsNoEvents()
    {
        var id = NewMetricReportId();
        var taskId = Guid.NewGuid();
        var metricReport = new MetricReport(id, [
            new TaskRequested(id, taskId, RequestedAt),
            new TaskStarted(id, taskId, StartedAt),
            new TaskCompleted(id, taskId, CompletedAt, CompletedAt - RequestedAt, CompletedAt - StartedAt)
        ]);

        var events = metricReport.Handle(new RecordTaskStatusChanged(taskId, Status.Done, CompletedAt.AddDays(1)));

        events.Should().BeEmpty();
    }

    [Fact]
    public void RecordTaskStatusChanged_KeepsFirstStartDate_WhenTaskGoesBackToTodoAndIsRestarted()
    {
        var id = NewMetricReportId();
        var taskId = Guid.NewGuid();
        var metricReport = new MetricReport(id, [new TaskRequested(id, taskId, RequestedAt)]);

        metricReport.Handle(new RecordTaskStatusChanged(taskId, Status.OnGoing, StartedAt));
        metricReport.Handle(new RecordTaskStatusChanged(taskId, Status.Todo, StartedAt.AddHours(1))).Should().BeEmpty();
        metricReport.Handle(new RecordTaskStatusChanged(taskId, Status.OnGoing, StartedAt.AddHours(2))).Should().BeEmpty();

        var events = metricReport.Handle(new RecordTaskStatusChanged(taskId, Status.Done, CompletedAt));

        events.Should().Equal(new TaskCompleted(id, taskId, CompletedAt, TimeSpan.FromHours(81), TimeSpan.FromHours(57)));
    }

    [Fact]
    public void RecordTaskStatusChanged_TracksEachTaskIndependently()
    {
        var id = NewMetricReportId();
        var startedTaskId = Guid.NewGuid();
        var requestedTaskId = Guid.NewGuid();
        var metricReport = new MetricReport(id, [
            new TaskRequested(id, startedTaskId, RequestedAt),
            new TaskStarted(id, startedTaskId, StartedAt),
            new TaskRequested(id, requestedTaskId, RequestedAt)
        ]);

        metricReport.Handle(new RecordTaskStatusChanged(requestedTaskId, Status.Done, CompletedAt)).Should().BeEmpty();
        metricReport.Handle(new RecordTaskStatusChanged(startedTaskId, Status.Done, CompletedAt)).Should().Equal(
            new TaskCompleted(id, startedTaskId, CompletedAt, TimeSpan.FromHours(81), TimeSpan.FromHours(57)));
    }

    [Fact]
    public void Task_CanFlowThroughEveryStatusInOrder()
    {
        var metricReport = new MetricReport(NewMetricReportId(), []);
        var taskId = Guid.NewGuid();

        metricReport.Handle(new RecordTaskCreated(taskId, RequestedAt)).Should().Equal(new TaskRequested(metricReport.Id, taskId, RequestedAt));
        metricReport.Handle(new RecordTaskStatusChanged(taskId, Status.Todo, RequestedAt)).Should().BeEmpty();
        metricReport.Handle(new RecordTaskStatusChanged(taskId, Status.OnGoing, StartedAt)).Should().Equal(new TaskStarted(metricReport.Id, taskId, StartedAt));
        metricReport.Handle(new RecordTaskStatusChanged(taskId, Status.Done, CompletedAt)).Should().Equal(
            new TaskCompleted(metricReport.Id, taskId, CompletedAt, TimeSpan.FromHours(81), TimeSpan.FromHours(57)));

        metricReport.Handle(new RecordTaskCreated(taskId, CompletedAt)).Should().BeEmpty();
        metricReport.Handle(new RecordTaskStatusChanged(taskId, Status.OnGoing, CompletedAt)).Should().BeEmpty();
        metricReport.Handle(new RecordTaskStatusChanged(taskId, Status.Done, CompletedAt)).Should().BeEmpty();
    }
}
