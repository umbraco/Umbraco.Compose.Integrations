using System.Net.Http.Headers;
using OpenIddict.Client;
using static OpenIddict.Client.OpenIddictClientEvents;

namespace Umbraco.Cms.Core.DependencyInjection;

internal sealed class SetUserAgentHeader<TContext> : IOpenIddictClientHandler<TContext> where TContext : BaseExternalContext
{
    public ValueTask HandleAsync(TContext context)
    {
        HttpRequestMessage request = context.Transaction.GetHttpRequestMessage()
            ?? throw new InvalidOperationException("No request message available.");

        // set the custom user agent if present in registation properties,
        // we do it this way so we only change if for the compose requests
        if (!context.Registration.Properties.TryGetValue("Umbraco-Compose-Integration-Core-Version", out object? value)
            || value is not ProductInfoHeaderValue productInfo)
        {
            return default;
        }

        request.Headers.UserAgent.Add(productInfo);

        return default;
    }
}
