using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;

namespace HoverTranslate.Core.Http;

public static class TranslationHttpClient
{
    private static readonly Lazy<HttpClient> Client = new(CreateClient);

    public static HttpClient Instance => Client.Value;

    private static HttpClient CreateClient()
    {
        var handler = new SocketsHttpHandler
        {
            UseProxy = true,
            Proxy = WebRequest.DefaultWebProxy,
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectTimeout = TimeSpan.FromSeconds(30),
            SslOptions = new SslClientAuthenticationOptions
            {
                // Win10 上仅 Tls12 兼容性更好
                EnabledSslProtocols = SslProtocols.Tls12
            }
        };

        if (handler.Proxy is not null)
            handler.Proxy.Credentials = CredentialCache.DefaultCredentials;

        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(90)
        };
    }

    public static string FormatException(Exception ex)
    {
        var msg = ex.Message;
        var inner = ex.InnerException;
        while (inner is not null)
        {
            msg += " → " + inner.Message;
            inner = inner.InnerException;
        }
        return msg;
    }
}
