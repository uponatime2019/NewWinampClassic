using System;
using System.Threading;

namespace NewWinampClassic.Helpers;

public sealed class Debouncer : IDisposable
{
    private readonly TimeSpan _delay;
    private Timer? _timer;
    private Action? _pendingAction;

    public Debouncer(TimeSpan? delay = null)
    {
        _delay = delay ?? TimeSpan.FromMilliseconds(300);
    }

    public void Debounce(Action action)
    {
        _timer?.Dispose();
        _pendingAction = action;
        _timer = new Timer(OnTimerElapsed, null, _delay, Timeout.InfiniteTimeSpan);
    }

    private void OnTimerElapsed(object? state)
    {
        _timer?.Dispose();
        _timer = null;
        _pendingAction?.Invoke();
        _pendingAction = null;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
        _pendingAction = null;
    }
}
