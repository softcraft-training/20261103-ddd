namespace KanbanBoard.Core.Domain;

public record WipLimit(int Value)
{
    public bool IsAWipLimit() => Value > 0;

    public static WipLimit NoWipLimit()
    {
        return new WipLimit(0);
    }
}
