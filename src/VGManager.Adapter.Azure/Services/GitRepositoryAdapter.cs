using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.Text;
using System.Text.Json;
using VGManager.Adapter.Azure.Services.Helper;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Azure.Settings;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Requests;
using VGManager.Adapter.Models.Response;
using YamlDotNet.RepresentationModel;

namespace VGManager.Adapter.Azure.Services;

public class GitRepositoryAdapter(
    IHttpClientProvider clientProvider,
    IOptions<GitRepositoryAdapterSettings> options,
    IOptions<ExtensionSettings> options2,
    ILogger<GitRepositoryAdapter> logger
    ) : IGitRepositoryAdapter
{
    private readonly GitRepositoryAdapterSettings Settings = options.Value;
    private readonly ExtensionSettings ExtensionSettings = options2.Value;

    public async Task<BaseResponse<IEnumerable<GitRepository>>> GetAllAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        var payload = PayloadProvider<ExtendedBaseRequest>.GetPayload(command.Payload);
        if (payload is null)
        {
            return ResponseProvider.GetResponse(Enumerable.Empty<GitRepository>());
        }

        logger.LogInformation("Request git repositories from {project} azure project.", payload.Project);
        clientProvider.Setup(payload.Organization, payload.PAT);
        using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken);
        var repositories = await client.GetRepositoriesAsync(cancellationToken: cancellationToken);
        var result = repositories.Where(repo => (!repo.IsDisabled ?? false) && repo.ProjectReference.Name == payload.Project).ToList();
        return ResponseProvider.GetResponse(result);
    }

    public async Task<BaseResponse<List<string>>> GetVariablesFromConfigAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        var payload = PayloadProvider<GitRepositoryRequest<string>>.GetPayload(command.Payload);
        if (payload is null)
        {
            return ResponseProvider.GetResponse(Enumerable.Empty<string>().ToList());
        }

        var project = payload.Project;
        var repositoryId = payload.RepositoryId;

        logger.LogInformation(
            "Requesting configurations from {project} azure project, {repositoryId} git repository.",
            project,
            repositoryId
            );
        clientProvider.Setup(payload.Organization, payload.PAT);
        using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken);
        var gitVersionDescriptor = new GitVersionDescriptor
        {
            VersionType = GitVersionType.Branch,
            Version = payload.Branch
        };

        var item = await client.GetItemTextAsync(
            project: project,
            repositoryId: repositoryId,
            path: payload.FilePath,
            versionDescriptor: gitVersionDescriptor,
            cancellationToken: cancellationToken
            );

        if (payload.FilePath.EndsWith(ExtensionSettings.JsonExtension))
        {
            var json = await GetJsonObjectAsync(item, cancellationToken);
            var result = GetKeysFromJson(json, payload.Exceptions ?? Enumerable.Empty<string>(), payload.Delimiter);
            return ResponseProvider.GetResponse(result);
        }
        else if (payload.FilePath.EndsWith(ExtensionSettings.YamlExtension))
        {
            return ResponseProvider.GetResponse(GetKeysFromYaml(item));
        }
        else
        {
            return ResponseProvider.GetResponse(Enumerable.Empty<string>().ToList());
        }
    }

    private static List<string> GetKeysFromJson(JsonElement jsonObject, IEnumerable<string> exceptions, string delimiter)
    {
        var keys = new List<string>();
        GetKeysFromJsonHelper(jsonObject, delimiter, string.Empty, exceptions, keys);
        return keys;
    }

    private static void GetKeysFromJsonHelper(
        JsonElement jsonObject,
        string delimiter,
        string prefix,
        IEnumerable<string> exceptions,
        List<string> keys
        )
    {
        if (jsonObject.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in jsonObject.EnumerateObject())
            {
                var name = property.Name;
                var key = prefix + name + delimiter;

                if (property.Value.ValueKind != JsonValueKind.Object || exceptions.Contains(property.Name))
                {
                    keys.Add(key.Remove(key.Length - delimiter.Length));
                }

                if (!exceptions.Contains(property.Name))
                {
                    GetKeysFromJsonHelper(property.Value, delimiter, key, exceptions, keys);
                }
            }
        }
    }

    private static async Task<JsonElement> GetJsonObjectAsync(Stream item, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        var buffer = new byte[2048];
        int bytesRead;
        while ((bytesRead = await item.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await stream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }
        var result = stream.ToArray();
        var itemText = Encoding.UTF8.GetString(result);
        return JsonSerializer.Deserialize<JsonElement>(itemText);
    }

    private List<string> GetKeysFromYaml(Stream item)
    {
        var yamls = GetYamlDocuments(item);
        var result = new List<string>();
        var counter = 0;
        foreach (var yaml in yamls)
        {
            var subResult = CollectKeysFromYaml(yaml, counter == 0 ? Settings.VariableYamlElement : Settings.SecretYamlElement);
            result.AddRange(subResult);
            counter++;
            if (counter == 2)
            {
                break;
            }
        }
        return result;
    }

    private List<YamlDocument> GetYamlDocuments(Stream item)
    {
        var reader = new StreamReader(item);
        var yaml = new YamlStream();
        yaml.Load(reader);
        return yaml.Documents.Where(
            document => document.AllNodes.Contains(Settings.SecretYamlKind) || document.AllNodes.Contains(Settings.VariableYamlKind)
            ).ToList();
    }

    private List<string> CollectKeysFromYaml(YamlDocument yaml, string nodeKey)
    {
        var data = yaml.AllNodes.FirstOrDefault(node => node.ToString().Contains(nodeKey));
        var strNode = data?.ToString() ?? string.Empty;
        var listNode = strNode.Split($" {nodeKey}").ToList();
        var rawVariables = listNode[1].Split(",");
        return CollectKeysFromYaml(rawVariables);
    }

    private List<string> CollectKeysFromYaml(string[] rawVariables)
    {
        var result = new List<string>();
        foreach (var rawVariable in rawVariables)
        {
            var strBuilder = new StringBuilder();
            var startCollecting = false;
            foreach (var character in rawVariable)
            {
                if (character == Settings.StartingChar)
                {
                    startCollecting = true;
                }
                if (startCollecting &&
                    !Settings.NotAllowedCharacters.Contains(character)
                    )
                {
                    strBuilder.Append(character);
                }
                else if (character == Settings.EndingChar)
                {
                    startCollecting = false;
                }
            }
            var strResult = strBuilder.ToString();
            if (!string.IsNullOrEmpty(strResult))
            {
                result.Add(strResult);
            }
        }
        return result;
    }
}
