using Microsoft.VisualStudio.Services.Common;

namespace TinyUpdate.Azure;

public class AzureClientSettings
{
    public Uri OrganisationUri { get; set; }

    public VssCredentials Credentials { get; set; }

    public Guid ProjectGuid { get; set; }
}