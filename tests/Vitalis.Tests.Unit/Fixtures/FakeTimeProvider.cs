namespace Vitalis.Tests.Unit;

/// <summary>
/// <see cref="TimeProvider"/> controlado manualmente, permitindo testar comportamentos
/// dependentes de tempo sem introduzir esperas reais nos testes.
/// </summary>
public sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _agora;

    public FakeTimeProvider(DateTimeOffset inicio) => _agora = inicio;

    public override DateTimeOffset GetUtcNow() => _agora;

    public void Advance(TimeSpan intervalo) => _agora = _agora.Add(intervalo);
}
