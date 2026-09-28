namespace Girvs.Infrastructure;

/// <summary>
/// 基于 HTTP 上下文和异步本地状态的身份访问器。
/// </summary>
public sealed class GirvsPrincipalAccessor(IHttpContextAccessor httpContextAccessor)
    : IGirvsPrincipalAccessor
{
    private readonly AsyncLocal<ClaimsPrincipal> _currentPrincipal = new();

    public ClaimsPrincipal Principal =>
        _currentPrincipal.Value ?? httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

    public IDisposable Change(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var previous = _currentPrincipal.Value;
        _currentPrincipal.Value = principal;
        return new PrincipalScope(this, previous);
    }

    private sealed class PrincipalScope(
        GirvsPrincipalAccessor accessor,
        ClaimsPrincipal previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            accessor._currentPrincipal.Value = previous;
        }
    }
}
