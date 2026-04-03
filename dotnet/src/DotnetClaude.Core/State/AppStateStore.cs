using System;

namespace DotnetClaude.Core.State;

public class AppStateStore
{
    private AppState _state = new();
    private readonly object _lock = new();
    public event Action<AppState>? StateChanged;

    public AppState Current => _state;

    public void Dispatch(Func<AppState, AppState> reducer)
    {
        lock (_lock)
        {
            _state = reducer(_state);
            StateChanged?.Invoke(_state);
        }
    }
}
