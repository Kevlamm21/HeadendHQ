using HeadendHQ.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.AmazonPrime;

public static class AmazonPrimeExtensions
{
    public static void ConfigureAmazonPrime(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<AmazonPrimeLinkResolver>();
        builder.Services.AddSingleton<IAdbExtractor, AmazonPrimeExtractor>();
    }
}
