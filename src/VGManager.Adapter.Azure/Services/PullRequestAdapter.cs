using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using VGManager.Adapter.Azure.Services.Helper;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Requests;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services;

public class PullRequestAdapter(IHttpClientProvider clientProvider, ILogger<PullRequestAdapter> logger) :
    IPullRequestAdapter
{
    public async Task<BaseResponse<AdapterResponseModel<bool>>> CreatePRAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken
        )
    {
        var payload = PayloadProvider<PRRequest>.GetPayload(command.Payload);
        try
        {
            if (payload is null)
            {
                return ResponseProvider.GetResponse(
                    GetFailResponse()
                );
            }

            var prRequest = new GitPullRequest
            {
                SourceRefName = payload.SourceRefName,
                TargetRefName = payload.TargetRefName,
                Title = payload.Title,
                Description = payload.Description,
                Status = PullRequestStatus.Completed
            };

            clientProvider.Setup(payload.Organization, payload.PAT);
            using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken: cancellationToken);

            var pr = await client.CreatePullRequestAsync(
                prRequest,
                payload.Project,
                payload.Repository,
                cancellationToken: cancellationToken
            );

            var success = pr is not null;

            return ResponseProvider.GetResponse(
                new AdapterResponseModel<bool>()
                {
                    Data = success,
                    Status = success ? AdapterStatus.Success : AdapterStatus.Unknown
                }
            );

        } 
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occured while creating pull request.");
            return ResponseProvider.GetResponse(
                GetFailResponse()
            );
        }
    }

    private static AdapterResponseModel<bool> GetFailResponse()
    {
        return new AdapterResponseModel<bool>()
        {
            Data = false,
            Status = AdapterStatus.Unknown
        };
    }
}
