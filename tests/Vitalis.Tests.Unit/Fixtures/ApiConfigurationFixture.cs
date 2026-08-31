using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Vitalis.Tests.Unit.Fixtures;

/// <summary>
/// Class fixture com o contexto compartilhado pelos testes de controller: a configuração
/// contendo o <c>ServiceToken</c> e utilitários para montar o <see cref="HttpContext"/>.
/// </summary>
public sealed class ApiConfigurationFixture
{
    public const string ServiceTokenValido = "token-de-teste-2026";

    public IConfiguration Configuration { get; } = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ServiceToken"] = ServiceTokenValido
        })
        .Build();

    /// <summary>Associa um <see cref="HttpContext"/> ao controller, opcionalmente com o token de serviço.</summary>
    public static T ComHttpContext<T>(T controller, string? serviceToken = null) where T : ControllerBase
    {
        var httpContext = new DefaultHttpContext();

        if (serviceToken is not null)
            httpContext.Request.Headers["X-Service-Token"] = serviceToken;

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }
}
