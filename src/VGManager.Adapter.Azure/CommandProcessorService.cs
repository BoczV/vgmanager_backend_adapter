using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using VGManager.Adapter.Interfaces;
using VGManager.Adapter.Models.Kafka;
using VGManager.Communication.Kafka.Interfaces;
using VGManager.Communication.Models;

namespace VGManager.Adapter.Azure;

public class CommandProcessorService(
    IKafkaProducerService<VGManagerAdapterCommandResponse> kafkaProducerService,
    ProviderDto providerDto,
    GitProviderDto gitProviderDto,
    IMapper mapper,
    ILogger<CommandProcessorService> logger
    ) : ICommandProcessorService
{
    public async Task ProcessCommandAsync(CommandMessageBase commandMessage, CancellationToken cancellationToken = default)
    {
        VGManagerAdapterCommandResponse message;
        string? destination = null;

        try
        {
            object? result = null;
            message = mapper.Map<VGManagerAdapterCommandResponse>(commandMessage);
            var vgManagerAdapterCommandMessage = (VGManagerAdapterCommand)commandMessage;

            destination = vgManagerAdapterCommandMessage.Destination;

            result = commandMessage.CommandType switch
            {
                CommandTypes.CreateTagRequest => await gitProviderDto.GitVersionAdapter.CreateTagAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetBuildPipelinesRequest => await providerDto.BuildPipelineAdapter.GetBuildPipelinesAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.RunBuildPipelinesRequest => await providerDto.BuildPipelineAdapter.RunBuildPipelinesAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetBuildPipelineRequest => await providerDto.BuildPipelineAdapter.GetBuildPipelineAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.RunBuildPipelineRequest => await providerDto.BuildPipelineAdapter.RunBuildPipelineAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetFilePathRequest => await gitProviderDto.GitFileAdapter.GetFilePathAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetConfigFilesRequest => await gitProviderDto.GitFileAdapter.GetConfigFilesAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetAllRepositoriesRequest => await gitProviderDto.GitRepositoryAdapter.GetAllAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetVariablesFromConfigRequest => await gitProviderDto.GitRepositoryAdapter.GetVariablesFromConfigAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetBranchesRequest => await gitProviderDto.GitVersionAdapter.GetBranchesAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetTagsRequest => await gitProviderDto.GitVersionAdapter.GetTagsAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetProfileRequest => await providerDto.ProfileAdapter.GetProfileAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetEnvironmentsRequest => await providerDto.ReleasePipelineAdapter.GetEnvironmentsAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetVariableGroupsRequest => await providerDto.ReleasePipelineAdapter.GetVariableGroupsAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetProjectsRequest => await providerDto.ProjectAdapter.GetProjectsAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetAllVGRequest => await providerDto.VariableGroupService.GetAllAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetNumberOfFoundVGsRequest => await providerDto.VariableGroupService.GetNumberOfFoundVGsAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.UpdateVGRequest => await providerDto.VariableGroupService.UpdateAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.AddVGRequest => await providerDto.VariableGroupService.AddVariablesAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                ),
                CommandTypes.DeleteVGRequest => await providerDto.VariableGroupService.DeleteVariablesAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                ),
                CommandTypes.GetKeyVaultsRequest => await providerDto.KeyVaultAdapter.GetKeyVaultsAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                ),
                CommandTypes.GetSecretsRequest => await providerDto.KeyVaultAdapter.GetSecretsAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                ),
                CommandTypes.DeleteSecretRequest => await providerDto.KeyVaultAdapter.DeleteSecretAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                ),
                CommandTypes.AddKeyVaultSecretRequest => await providerDto.KeyVaultAdapter.AddKeyVaultSecretAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                ),
                CommandTypes.RecoverSecretRequest => await providerDto.KeyVaultAdapter.RecoverSecretAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                ),
                CommandTypes.GetDeletedSecretsRequest => providerDto.KeyVaultAdapter.GetDeletedSecrets(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                ),
                CommandTypes.GetAllSecretsRequest => await providerDto.KeyVaultAdapter.GetAllAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                ),
                CommandTypes.CreatePullRequest => await gitProviderDto.PullRequestAdapter.CreatePRAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                _ => throw new InvalidOperationException($"Invalid command type: {commandMessage.CommandType}"),
            };

            if (result is not null)
            {
                message.Payload = JsonSerializer.Serialize(result);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not process command for {CommandType}", commandMessage.CommandType);

            message = new VGManagerAdapterCommandResponse
            {
                IsSuccess = false,
                CommandInstanceId = commandMessage.InstanceId
            };
        }

        await SendCommandResponseAsync(message, destination, cancellationToken);
    }

    private async Task SendCommandResponseAsync(
        CommandResponseMessage commandResponseMessage,
        string? topic = default,
        CancellationToken cancellationToken = default
    )
    {
        if (topic is not null)
        {
            await kafkaProducerService.ProduceAsync((VGManagerAdapterCommandResponse)commandResponseMessage, topic, cancellationToken);
        }
        else
        {
            logger.LogError("Destination topic is null");
        }
    }
}
