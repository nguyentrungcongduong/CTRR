using System.Windows.Forms;
using GraphApp.Core.Algorithms.Base;

namespace GraphApp.UI.Controls;

/// <summary>
/// Điều phối animation từng bước của thuật toán đồ thị.
/// Dùng System.Windows.Forms.Timer để tự động phát.
/// </summary>
public class AnimationEngine
{
    // ─── State ─────────────────────────────────────────────────────────
    private List<AlgorithmStep> _steps       = new();
    private int                  _currentIndex = -1;
    private readonly Timer       _timer;

    // ─── Events ────────────────────────────────────────────────────────
    /// <summary>
    /// Kích hoạt mỗi khi bước hiện tại thay đổi.
    /// Tham số: (step hiện tại, index 1-based, tổng số bước)
    /// </summary>
    public event Action<AlgorithmStep, int, int>? OnStepChanged;

    /// <summary>Kích hoạt khi animation kết thúc (đã đến step cuối).</summary>
    public event Action? OnFinished;

    // ─── Properties ────────────────────────────────────────────────────
    public bool           IsPlaying    => _timer.Enabled;
    public int            CurrentIndex => _currentIndex;          // 0-based
    public int            Total        => _steps.Count;
    public bool           HasSteps     => _steps.Count > 0;
    public bool           IsAtStart    => _currentIndex <= 0;
    public bool           IsAtEnd      => _currentIndex >= _steps.Count - 1;
    public AlgorithmStep? CurrentStep  =>
        _currentIndex >= 0 && _currentIndex < _steps.Count ? _steps[_currentIndex] : null;

    // ─── Constructor ───────────────────────────────────────────────────
    public AnimationEngine()
    {
        _timer = new Timer { Interval = 800 };
        _timer.Tick += OnTimerTick;
    }

    // ─── Public API ────────────────────────────────────────────────────

    /// <summary>Load danh sách bước mới, reset về step đầu tiên.</summary>
    public void Load(List<AlgorithmStep> steps)
    {
        Pause();
        _steps        = steps;
        _currentIndex = steps.Count > 0 ? 0 : -1;
        Notify();
    }

    /// <summary>Tiến một bước. Trả về false nếu đã ở cuối.</summary>
    public bool Next()
    {
        if (_currentIndex >= _steps.Count - 1)
        {
            Pause();
            OnFinished?.Invoke();
            return false;
        }
        _currentIndex++;
        Notify();
        return true;
    }

    /// <summary>Lùi một bước. Trả về false nếu đã ở đầu.</summary>
    public bool Prev()
    {
        if (_currentIndex <= 0) return false;
        _currentIndex--;
        Notify();
        return true;
    }

    /// <summary>Nhảy đến bước đầu tiên.</summary>
    public void GoToStart()
    {
        if (_steps.Count == 0) return;
        _currentIndex = 0;
        Notify();
    }

    /// <summary>Nhảy đến bước cuối cùng.</summary>
    public void GoToEnd()
    {
        if (_steps.Count == 0) return;
        _currentIndex = _steps.Count - 1;
        Notify();
    }

    /// <summary>Nhảy đến bước theo index (0-based).</summary>
    public void GoTo(int index)
    {
        if (_steps.Count == 0) return;
        _currentIndex = Math.Clamp(index, 0, _steps.Count - 1);
        Notify();
    }

    /// <summary>Bắt đầu tự động phát với tốc độ <paramref name="speedMs"/> ms/step.</summary>
    public void Play(int speedMs = 800)
    {
        if (_steps.Count == 0) return;
        // Nếu đang ở cuối → restart từ đầu
        if (_currentIndex >= _steps.Count - 1) GoToStart();
        _timer.Interval = Math.Max(100, speedMs);
        _timer.Start();
    }

    /// <summary>Tạm dừng tự động phát.</summary>
    public void Pause() => _timer.Stop();

    /// <summary>Thay đổi tốc độ phát (ms/step) trong khi đang chạy.</summary>
    public void SetSpeed(int ms)
    {
        bool wasPlaying = IsPlaying;
        _timer.Stop();
        _timer.Interval = Math.Max(100, ms);
        if (wasPlaying) _timer.Start();
    }

    // ─── Private ───────────────────────────────────────────────────────

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (!Next()) _timer.Stop();
    }

    private void Notify()
    {
        if (_currentIndex < 0 || _currentIndex >= _steps.Count) return;
        OnStepChanged?.Invoke(_steps[_currentIndex], _currentIndex + 1, _steps.Count);
    }
}
