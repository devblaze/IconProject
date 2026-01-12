using Microsoft.Extensions.Configuration;
using IconProject.Common.Dtos;

namespace IconProject.Common;

public static class Configuration
{
    public static IconApiConfig IconApiConfig(this IConfiguration configuration) =>
        configuration.GetSection("IconApiConfig").Get<IconApiConfig>() ?? new IconApiConfig();
}