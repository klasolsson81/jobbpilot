namespace JobbPilot.Domain.Common;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
