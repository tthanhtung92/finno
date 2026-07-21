using Finmy.Budgeting.Application.Abstractions;
using Finmy.Budgeting.Domain.Envelopes;

using Microsoft.EntityFrameworkCore;

namespace Finmy.Budgeting.Infrastructure.Persistence;

internal sealed class EnvelopeRepository(BudgetingDbContext dbContext) : IEnvelopeRepository
{
    public void Add(Envelope envelope)
    {
        dbContext.Envelopes.Add(envelope);
    }

    public void Remove(Envelope envelope)
    {
        dbContext.Envelopes.Remove(envelope);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Envelope?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Envelopes.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Envelope> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Envelopes
            .OrderBy(e => e.PeriodStartUtc)
            .ThenBy(e => e.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
