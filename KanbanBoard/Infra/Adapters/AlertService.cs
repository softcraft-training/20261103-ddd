using KanbanBoard.Core.UseCases.Ports;

namespace KanbanBoard.Infra.Adapters;

public class AlertService : IAlertService
{
    public void SendAlert(string title, string message, string recipient)
    {
        Console.WriteLine("Alert sent");
    }
}
