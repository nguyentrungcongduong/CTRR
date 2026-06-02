using System.Windows.Forms;
using GraphApp.Core.Algorithms.Base;

namespace GraphApp.UI.Controls;

/// <summary>
/// Điều khiển animation từng bước thuật toán.
/// State machine: Idle → Ready → Playing ↔ Paused → Done
/// </summary>
public class AnimationEngine
{
    private List<AlgorithmStep> _steps = new();
    private int _currentIndex = -1;
    private readonly System.Windows.Forms.Timer _timer;

    public event Action<AlgorithmStep, int, int>? OnStepChanged;  // (step, index, total)

    public AnimationState State { get; private set; } = AnimationState.Idle;
    public int CurrentIndex => _currentIndex;
    public int TotalSteps  => _steps.Count;

    public AnimationEngine()
    {
        _timer = new System.Windows.Forms.Timer();
        _timer.Tick += (_, _) => Next();
    }

    /// <summary>Tải danh sách bước mới, reset về đầu.</summary>
    public void Load(List<AlgorithmStep> steps)
    {
        _steps        = steps;
        _currentIndex = -1;
        State         = AnimationState.Ready;
        _timer.Stop();
    }

    /// <summary>Bắt đầu phát animation.</summary>
    public void Play(int intervalMs = 800)
    {
        if (State is AnimationState.Idle) return;
        _timer.Interval = intervalMs;
        _timer.Start();
        State = AnimationState.Playing;
    }

    /// <summary>Tạm dừng.</summary>
    public void Pause()
    {
        _timer.Stop();
        State = AnimationState.Paused;
    }

    /// <summary>Đi đến bước tiếp theo.</summary>
    public void Next()
    {
        if (_steps.Count == 0) return;
        _currentIndex = Math.Min(_currentIndex + 1, _steps.Count - 1);
        NotifyStep();
        if (_currentIndex == _steps.Count - 1)
        {
            _timer.Stop();
            State = AnimationState.Done;
        }
    }

    /// <summary>Quay lại bước trước.</summary>
    public void Prev()
    {
        if (_steps.Count == 0) return;
        _currentIndex = Math.Max(_currentIndex - 1, 0);
        NotifyStep();
        if (State == AnimationState.Done) State = AnimationState.Paused;
    }

    /// <summary>Nhảy đến bước đầu tiên.</summary>
    public void Reset()
    {
        _timer.Stop();
        _currentIndex = -1;
        State         = _steps.Count > 0 ? AnimationState.Ready : AnimationState.Idle;
    }

    private void NotifyStep()
    {
        if (_currentIndex >= 0 && _currentIndex < _steps.Count)
            OnStepChanged?.Invoke(_steps[_currentIndex], _currentIndex + 1, _steps.Count);
    }
}

public enum AnimationState { Idle, Ready, Playing, Paused, Done }
