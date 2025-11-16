using Microsoft.AspNetCore.Routing;

namespace SharedKernel.Web;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder endpointsBuilder);
}