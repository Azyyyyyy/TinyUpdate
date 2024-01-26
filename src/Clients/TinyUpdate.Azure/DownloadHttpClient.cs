using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace TinyUpdate.Azure;

public class DownloadHttpClient(Uri baseUrl, VssCredentials credentials) : VssHttpClientBase(baseUrl, credentials)
{
    public Task<Stream> GetStream(string downloadUrl) => Client.GetStreamAsync(downloadUrl);
};