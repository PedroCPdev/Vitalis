// Importa abstrações de contexto do ASP.NET Core
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
// Importa a configuração usada pelos controllers
using Microsoft.Extensions.Configuration;

namespace Vitalis.Tests.Unit.Fixtures;

// Fixture compartilhada pelos testes de aplicação: fornece a configuração com o
// Service Token e monta o HttpContext necessário para os controllers
public class ConfiguracaoFixture
{
    // Token de serviço válido usado nos cenários de integração com o backend Java
    public const string ServiceTokenValido = "token-de-teste-2026";

    // Configuração em memória contendo o Service Token esperado pelos controllers
    public IConfiguration Configuration { get; } = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ServiceToken"] = ServiceTokenValido
        })
        .Build();

    // Associa um HttpContext ao controller, opcionalmente com o header do Service Token
    public static T ComHttpContext<T>(T controller, string? serviceToken = null) where T : ControllerBase
    {
        var httpContext = new DefaultHttpContext();

        if (serviceToken is not null)
            httpContext.Request.Headers["X-Service-Token"] = serviceToken;

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }
}
