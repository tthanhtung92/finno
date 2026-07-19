using Finmy.Budgeting.Domain.Envelopes;

namespace Finmy.Budgeting.Application.Abstractions;

public interface IEnvelopeRepository
{
    Task AddAsync(Envelope envelope, CancellationToken cancellationToken);
    Task<Envelope?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
