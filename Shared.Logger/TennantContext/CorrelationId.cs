namespace Shared.Logger.TennantContext;

public sealed record CorrelationId {
    public Guid Value { get; }

    private CorrelationId(Guid value) {
        if (value == Guid.Empty) {
            throw new ArgumentException("Correlation ID cannot be empty.", nameof(value));
        }

        this.Value = value;
    }

    public static CorrelationId New() {
        return new CorrelationId(Guid.NewGuid());
    }

    public static CorrelationId From(Guid value) {
        return new CorrelationId(value);
    }

    public static CorrelationId Parse(string value) {
        if (!Guid.TryParse(value, out Guid parsed)) {
            throw new ArgumentException("Correlation ID must be a valid GUID.", nameof(value));
        }

        return From(parsed);
    }

    public override string ToString() {
        return this.Value.ToString("N");
    }
}