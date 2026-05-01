namespace Application;

public class OutboxMessageConsumer
{
    public Guid OutboxMessageId { get; set; }
    public string HandlerType { get; set; } = string.Empty;
    public DateTime ProcessedOnUtc { get; set; }
}
