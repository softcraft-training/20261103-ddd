namespace KanbanBoard.Core.Domain;

public class Column(WipLimit wipLimit, List<Ticket> tickets)
{
    private WipLimit _wipLimit = wipLimit;

    public void AddTicket(Ticket ticket)
    {
        tickets.Add(ticket);
    }

    public void DefineWipLimit(WipLimit wipLimit)
    {
        _wipLimit = wipLimit;
    }

    public void RemoveWipLimit()
    {
        _wipLimit = WipLimit.NoWipLimit();
    }

    public Ticket? RemoveTicket(Guid ticketId)
    {
        var ticket = tickets.SingleOrDefault(x => x.Id == ticketId);

        if (ticket == null) return null;

        tickets.Remove(ticket);

        return ticket;
    }

    public bool IsWipLimitExceeded()
    {
        return _wipLimit.IsAWipLimit() && tickets.Count > _wipLimit.Value;
    }
}
