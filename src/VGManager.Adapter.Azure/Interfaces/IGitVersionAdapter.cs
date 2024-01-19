using VGManager.Adapter.Azure.Entities;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Interfaces;

public interface IGitVersionAdapter
{
    Task<(AdapterStatus, IEnumerable<string>)> GetBranchesAsync(
        string organization,
        string pat,
        string repositoryId,
        CancellationToken cancellationToken = default
        );
    Task<(AdapterStatus, IEnumerable<string>)> GetTagsAsync(
        string organization,
        string pat,
        Guid repositoryId,
        CancellationToken cancellationToken = default
        );
    Task<(AdapterStatus, string)> CreateTagAsync(
        CreateTagEntity tagEntity,
        string defaultBranch,
        string sprint,
        CancellationToken cancellationToken = default
        );
}