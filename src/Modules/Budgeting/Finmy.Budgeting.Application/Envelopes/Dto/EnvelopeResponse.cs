namespace Finmy.Budgeting.Application.Envelopes.Dto;

public sealed record EnvelopeResponse(Guid Id, string Name, string? Description, Guid CategoryId, decimal Allocated, DateTimeOffset PeriodStartUtc, DateTimeOffset PeriodEndUtc);