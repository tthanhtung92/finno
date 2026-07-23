namespace Finmy.Budgeting.Application.Abstractions;

public interface IOutputCacheInvalidator
{
    Task EvictByTagsAsync(IReadOnlyList<string> tags, CancellationToken cancellationToken);
}
