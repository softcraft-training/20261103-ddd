namespace IntegrationEvents;

public enum Status { Todo, OnGoing, Done }

public record TaskCreated(Guid BoardId, Guid TaskId, DateTime At);

public record TaskStatusChanged(Guid BoardId, Guid TaskId, Status To, DateTime At);
