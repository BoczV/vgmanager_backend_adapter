namespace VGManager.Adapter.Models.Requests;

public class PRRequest
{
    public string Organization { get; set; } = null!;
    public string PAT { get; set; } = null!;
    public string SourceRefName { get; set; } = null!;
    public string TargetRefName { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string Project { get; set; } = null!;
    public string Repository { get; set; } = null!;
}
