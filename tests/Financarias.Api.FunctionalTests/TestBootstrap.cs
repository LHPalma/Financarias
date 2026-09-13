using System.Runtime.CompilerServices;

namespace Financarias.Api.FunctionalTests;

// AddIntegrations lê Integrations:Brapi:Token durante o registro de serviços (Program.cs),
// que roda ANTES do ConfigureAppConfiguration da WebApplicationFactory — logo uma fonte
// in-memory adicionada lá chega tarde demais. Uma variável de ambiente é lida pelo
// CreateBuilder a tempo. O token real vive no user-secrets (dev) / env var (prod);
// aqui injetamos um valor fake só para o app subir nos testes.
internal static class TestBootstrap
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        Environment.SetEnvironmentVariable("Integrations__Brapi__Token", "test-token");

        // Os funcionais sobem em "Testing", que não carrega o appsettings.Development.json, e o
        // ValidateOnStart derruba o boot sem pepper. Valor de teste, sem nenhum valor de segredo.
        Environment.SetEnvironmentVariable("PasswordHashing__CurrentPepperVersion", "1");
        Environment.SetEnvironmentVariable("PasswordHashing__Peppers__1",
            "pepper-de-teste-com-pelo-menos-32-caracteres");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "Y2hhdmUtZGUtdGVzdGUtand0LWNvbS0zMi1ieXRlcyE=");
    }
}