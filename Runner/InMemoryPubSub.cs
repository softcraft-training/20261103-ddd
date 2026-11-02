using KanbanBoard.Core.UseCases.Ports;
using Reporting.Infra.Handlers;

namespace Runner;

public class InMemoryPubSub : IPublisher, ISubscriber
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public void Publish<T>(T evt) where T : class
    {
        var handlerType = typeof(T);

        if (_handlers.ContainsKey(handlerType) == false) return;

        _handlers[handlerType].ForEach(handler => {
            try
            {
                ((Action<T>)handler)(evt);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
            }
        });
    }

    public void Register<T>(Action<T> action) where T : class
    {
        var handlerType = typeof(T);

        if (_handlers.ContainsKey(handlerType) == false)
            _handlers[handlerType] = new List<Delegate>();

        _handlers[handlerType].Add(action);
    }
}
