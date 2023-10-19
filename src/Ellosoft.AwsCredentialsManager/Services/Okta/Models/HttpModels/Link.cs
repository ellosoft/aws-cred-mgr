namespace Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

public class Link
{
    public required string Href { get; set; }
    public string? Type { get; set; }
    public LinkHints? Hints { get; set; }

    public class LinkHints
    {
        public string[]? Allow { get; set; }
    }
}