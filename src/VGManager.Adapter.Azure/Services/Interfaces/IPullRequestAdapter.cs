using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Response;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IPullRequestAdapter
{
    Task<BaseResponse<AdapterResponseModel<bool>>> CreatePRAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken
        );
}
