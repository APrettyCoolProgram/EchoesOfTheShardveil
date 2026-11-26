using Microsoft.Xna.Framework.Audio;

namespace TheCards.Desktop;

/// <summary>Shares the sound preference while keeping playback tied to the active window.</summary>
internal sealed class GameAudio : IDisposable
{
    private SoundEffect? soundtrack;
    private SoundEffectInstance? music;
    private bool active;

    public bool Enabled { get; private set; } = true;

    public void Start()
    {
        active = true;
        Apply();
    }

    public void Toggle()
    {
        Enabled = !Enabled;
        Apply();
    }

    public void Stop()
    {
        active = false;
        ReleasePlayback();
    }

    private void Apply()
    {
        if (!active)
        {
            return;
        }

        if (!Enabled)
        {
            if (music is { IsDisposed: false, State: SoundState.Playing })
            {
                music.Pause();
            }
            return;
        }

        if (music is not { IsDisposed: false })
        {
            ReleasePlayback();
            try
            {
                using var stream = CardGame.OpenEmbeddedAsset("MainUI-Theme.wav");
                soundtrack = SoundEffect.FromStream(stream);
                music = soundtrack.CreateInstance();
                music.IsLooped = true;
                music.Volume = 0.35f;
            }
            catch (NoAudioHardwareException)
            {
                ReleasePlayback();
                return;
            }
        }

        if (music.State == SoundState.Paused)
        {
            music.Resume();
        }
        else if (music.State == SoundState.Stopped)
        {
            music.Play();
        }
    }

    private void ReleasePlayback()
    {
        music?.Dispose();
        music = null;
        soundtrack?.Dispose();
        soundtrack = null;
    }

    public void Dispose() => Stop();
}
