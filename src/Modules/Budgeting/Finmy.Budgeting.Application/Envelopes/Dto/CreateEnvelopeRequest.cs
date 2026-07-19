namespace Finmy.Budgeting.Application.Envelopes.Dto;

public sealed record CreateEnvelopeRequest(string Name, string? Description, Guid CategoryId, decimal Allocated, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd);