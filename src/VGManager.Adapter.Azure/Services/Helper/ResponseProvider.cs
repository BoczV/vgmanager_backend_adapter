using Azure.Security.KeyVault.Secrets;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Profile;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Requests;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;
using VariableGroup = Microsoft.TeamFoundation.DistributedTask.WebApi.VariableGroup;

namespace VGManager.Adapter.Azure.Services.Helper;

public static class ResponseProvider
{
    public static BaseResponse<AdapterStatus> GetResponse(AdapterStatus adapterStatus)
    {
        return new()
        {
            Data = adapterStatus
        };
    }

    public static BaseResponse<BuildDefinitionReference> GetResponse(BuildDefinitionReference result)
    => new()
    {
        Data = result
    };

    public static BaseResponse<IEnumerable<BuildDefinitionReference>> GetResponse(IEnumerable<BuildDefinitionReference> result)
    => new()
    {
        Data = result
    };

    public static BaseResponse<Dictionary<string, object>> GetResponse((AdapterStatus, IEnumerable<string>) result)
    => new()
    {
        Data = new Dictionary<string, object>()
        {
            ["Status"] = result.Item1,
            ["Data"] = result.Item2
        }
    };

    public static BaseResponse<List<string>> GetResponse(List<string> result)
        => new()
        {
            Data = result
        };

    public static BaseResponse<IEnumerable<GitRepository>> GetResponse(IEnumerable<GitRepository> result)
        => new()
        {
            Data = result
        };

    public static BaseResponse<Dictionary<string, object>> GetResponse((AdapterStatus, string) result)
        => new()
        {
            Data = new Dictionary<string, object>()
            {
                ["Status"] = result.Item1,
                ["Data"] = result.Item2
            }
        };

    public static BaseResponse<Profile?> GetResponse(Profile? result)
        => new()
        {
            Data = result
        };

    public static BaseResponse<AdapterResponseModel<IEnumerable<ProjectRequest>>> GetResponse(
        AdapterResponseModel<IEnumerable<ProjectRequest>> result
        )
        => new()
        {
            Data = result
        };

    public static BaseResponse<AdapterResponseModel<IEnumerable<VariableGroup>>> GetResponse(
        AdapterResponseModel<IEnumerable<VariableGroup>> result
        )
        => new()
        {
            Data = result
        };

    public static BaseResponse<Dictionary<string, object>> GetResponse(
        (AdapterStatus, IEnumerable<(string, string)>) result
        )
    {
        var res = new List<Dictionary<string, string>>();
        foreach (var item in result.Item2)
        {
            res.Add(new()
            {
                ["Name"] = item.Item1,
                ["Type"] = item.Item2
            });
        }
        return new()
        {
            Data = new Dictionary<string, object>()
            {
                ["Status"] = result.Item1,
                ["Data"] = res
            }
        };
    }

    public static BaseResponse<Dictionary<string, object>> GetResponse(
        (string?, IEnumerable<string>) result
        )
    {
        var res = new Dictionary<string, object>
        {
            { "subscription", result.Item1 ?? string.Empty },
            { "keyVaults", result.Item2 }
        };
        return new()
        {
            Data = new Dictionary<string, object>()
            {
                ["Status"] = AdapterStatus.Success,
                ["Data"] = res
            }
        };
    }

    public static BaseResponse<AdapterResponseModel<KeyVaultSecret?>> GetResponse(AdapterResponseModel<KeyVaultSecret?> result)
    {
        return new()
        {
            Data = result
        };
    }

    public static BaseResponse<AdapterResponseModel<IEnumerable<AdapterResponseModel<KeyVaultSecret?>>>> GetResponse(
        AdapterResponseModel<IEnumerable<AdapterResponseModel<KeyVaultSecret?>>> result
        )
    {
        return new()
        {
            Data = result
        };
    }

    public static BaseResponse<AdapterResponseModel<IEnumerable<Dictionary<string, object>>>> GetResponse(
        AdapterResponseModel<IEnumerable<Dictionary<string, object>>> result
        )
    {
        return new()
        {
            Data = result
        };
    }

    public static BaseResponse<AdapterResponseModel<IEnumerable<KeyVaultSecret>>> GetResponse(
        AdapterResponseModel<IEnumerable<KeyVaultSecret>> result
        )
    {
        return new()
        {
            Data = result
        };
    }

    public static BaseResponse<AdapterResponseModel<IEnumerable<AdapterResponseModel<SimplifiedSecretResponse?>>>> GetResponse(
        AdapterResponseModel<IEnumerable<AdapterResponseModel<SimplifiedSecretResponse?>>> result
        )
    {
        return new()
        {
            Data = result
        };
    }

    public static BaseResponse<AdapterResponseModel<IEnumerable<SimplifiedVGResponse<T>>>> GetResponse<T>(
        AdapterResponseModel<IEnumerable<SimplifiedVGResponse<T>>> result
        )
    {
        return new()
        {
            Data = result
        };
    }

    public static BaseResponse<AdapterResponseModel<int>> GetResponse(
        AdapterResponseModel<int> result
        )
    {
        return new()
        {
            Data = result
        };
    }

    public static BaseResponse<AdapterResponseModel<bool>> GetResponse(
        AdapterResponseModel<bool> result
               )
    {
        return new()
        {
            Data = result
        };
    }
}

