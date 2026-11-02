namespace KanbanBoard.Core.UseCases.Ports;

public interface IAlertService
{
    void SendAlert(string title, string message, string recipient);
}
