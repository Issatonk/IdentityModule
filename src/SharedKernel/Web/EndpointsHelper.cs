using Microsoft.AspNetCore.Routing;

namespace SharedKernel.Web;

public static class EndpointsHelper
{
    public static IEndpointRouteBuilder MapEndpoint<T>(this IEndpointRouteBuilder endpointsBuilder)
       where T : IEndpoint, new()
    {
        var endpointsProvider = new T();
        endpointsProvider.MapEndpoint(endpointsBuilder);
        return endpointsBuilder;
    }
}
