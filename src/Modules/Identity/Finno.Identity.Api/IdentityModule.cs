using Finno.Identity.Api.Endpoints;
using Finno.Identity.Application.Authentication.Dto;
using Finno.Identity.Infrastructure;
using Finno.Modularity.Interfaces;

using FluentValidation;

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finno.Identity.Api;

public sealed class IdentityModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructure(configuration);

        // Validator
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
        services.AddValidatorsFromAssemblyContaining<RefreshRequestValidator>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        IdentityCoreEndpoints.MapEndpoints(endpoints);
        IdentityDemoEndpoints.MapEndpoints(endpoints);
    }
}
