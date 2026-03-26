namespace Domain.SRS.ValueObjects;

public record ReviewResult
{
    public Enums.ReviewResult Value { get; }

    public ReviewResult(Enums.ReviewResult value)
    {
        Value = value;
    }
}
