using System;
using System.Net.Http;
using System.Threading.Tasks;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Http.Extensions
{
    public static class HttpClientExt
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger(nameof(HttpClientExt));
        
        public static async Task<HttpResponseMessage?> GetResponseMessage(this HttpClient httpClient, HttpRequestMessage requestMessage)
        {
            HttpResponseMessage? response;
            try
            {
                response = await httpClient.SendAsync(requestMessage);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
                    
            //Check that we got something from it
            return response.IsSuccessStatusCode ? response : null;
        }
    }
}