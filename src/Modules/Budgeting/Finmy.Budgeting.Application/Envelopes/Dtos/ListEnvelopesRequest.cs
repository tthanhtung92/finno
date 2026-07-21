namespace Finmy.Budgeting.Application.Envelopes.Dtos;

public sealed record ListEnvelopesRequest(int Page = 1, int PageSize = 20);
