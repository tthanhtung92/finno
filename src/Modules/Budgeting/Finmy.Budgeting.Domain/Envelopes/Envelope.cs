using Finmy.SharedKernel.Results;

namespace Finmy.Budgeting.Domain.Envelopes;

public sealed class Envelope
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Guid CategoryId { get; private set; }
    public decimal Allocated { get; private set; }
    public DateTimeOffset PeriodStartUtc { get; private set; }
    public DateTimeOffset PeriodEndUtc { get; private set; }

    public static Result<Envelope> Create(
        string name, string? description,
        Guid categoryId, decimal allocated,
        DateTimeOffset periodStart, DateTimeOffset periodEnd)
    {
        if (string.IsNullOrWhiteSpace(name))
            return EnvelopeErrors.NameEmpty;

        name = name.Trim();

        if (name.Length > 200)
            return EnvelopeErrors.NameTooLong;

        // Nếu not null nhưng Trim xong ra rỗng thì coi nó là null luôn
        if (description is not null && string.IsNullOrWhiteSpace(description.Trim()))
        {
            description = null;
        }

        if (categoryId == Guid.Empty)
            return EnvelopeErrors.CategoryRequired;

        periodStart = periodStart.ToUniversalTime();
        periodEnd = periodEnd.ToUniversalTime();

        if (periodEnd <= periodStart)
            return EnvelopeErrors.PeriodInvalid;

        if (allocated <= 0m)
            return EnvelopeErrors.AllocatedNotPositive;

        return new Envelope
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Description = description,
            CategoryId = categoryId,
            Allocated = allocated,
            PeriodStartUtc = periodStart,
            PeriodEndUtc = periodEnd
        };
    }
}
