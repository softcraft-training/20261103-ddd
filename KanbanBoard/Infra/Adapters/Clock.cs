using KanbanBoard.Core.UseCases.Ports;

namespace KanbanBoard.Infra.Adapters;

public class Clock : IClock
{
    public DateTime GetNow()
    {
        return DateTime.UtcNow;
    }
}
