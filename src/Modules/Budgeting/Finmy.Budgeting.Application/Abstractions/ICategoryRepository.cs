namespace Finmy.Budgeting.Application.Abstractions;

public interface ICategoryRepository
{
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
}
