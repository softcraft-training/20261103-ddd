namespace KanbanBoard.Core.UseCases.Ports;

public interface IPublisher
{
    void Publish<T>(T evt) where T : class;
}
