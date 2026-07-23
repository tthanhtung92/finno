using Finmy.Budgeting.Application.Abstractions;

using Microsoft.AspNetCore.OutputCaching;

namespace Finmy.Budgeting.Api.Caching;

public sealed class OutputCacheInvalidator(IOutputCacheStore store) : IOutputCacheInvalidator
{
    public async Task EvictByTagsAsync(IReadOnlyList<string> tags, CancellationToken cancellationToken)
    {
        foreach (var tag in tags)
        {
            await store.EvictByTagAsync(tag, cancellationToken);
        }
    }
}
