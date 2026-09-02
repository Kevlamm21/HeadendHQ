using HeadendHQ.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.WebScraping.AmazonPrime;

internal static class AmazonPrimeExtensions
{
    internal static void ConfigureAmazonPrime(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<AmazonPrimeLinkResolver>();
        builder.Services.AddSingleton<IAdbExtractor, AmazonPrimeExtractor>();
    }
}
