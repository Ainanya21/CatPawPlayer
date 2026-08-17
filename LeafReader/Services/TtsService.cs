using System.Speech.Synthesis;

namespace LeafReader.Services;

public class TtsService : IDisposable
{
    private readonly SpeechSynthesizer _synthesizer = new();
    private bool _isPaused;

    public event EventHandler<bool>? StateChanged;

    public bool IsSpeaking => _synthesizer.State != SynthesizerState.Ready;
    public bool IsPaused => _isPaused;

    public TtsService()
    {
        _synthesizer.StateChanged += Synthesizer_StateChanged;
    }

    public void Speak(string text)
    {
        Stop();
        if (string.IsNullOrWhiteSpace(text)) return;
        _isPaused = false;
        _synthesizer.SpeakAsync(text);
    }

    public void Pause()
    {
        if (_synthesizer.State == SynthesizerState.Speaking)
        {
            _synthesizer.Pause();
            _isPaused = true;
            StateChanged?.Invoke(this, false);
        }
    }

    public void Resume()
    {
        if (_isPaused)
        {
            _synthesizer.Resume();
            _isPaused = false;
            StateChanged?.Invoke(this, true);
        }
    }

    public void Stop()
    {
        if (_synthesizer.State != SynthesizerState.Ready)
        {
            _synthesizer.SpeakAsyncCancelAll();
        }
        _isPaused = false;
        StateChanged?.Invoke(this, false);
    }

    public void SetRate(int rate)
    {
        _synthesizer.Rate = Math.Clamp(rate, -10, 10);
    }

    public void SetVolume(int volume)
    {
        _synthesizer.Volume = Math.Clamp(volume, 0, 100);
    }

    private void Synthesizer_StateChanged(object? sender, StateChangedEventArgs e)
    {
        StateChanged?.Invoke(this, e.State == SynthesizerState.Speaking);
    }

    public void Dispose()
    {
        _synthesizer.Dispose();
        GC.SuppressFinalize(this);
    }
}
