using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace RemoteMaster.Server.Core.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder ConfigureCoreUrls(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(5076);
        });

        return builder;
    }
}