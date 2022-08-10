namespace Girvs.Driven.Events;

public record Event(DateTime Timestamp) : Message, INotification
{
    public Event() : this(DateTime.Now)
    {
    }
}