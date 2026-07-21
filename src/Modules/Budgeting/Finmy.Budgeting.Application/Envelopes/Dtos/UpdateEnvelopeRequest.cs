namespace Finmy.Budgeting.Application.Envelopes.Dtos;

public sealed record UpdateEnvelopeRequest(string Name, string? Description, Guid CategoryId, decimal Allocated, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd);
