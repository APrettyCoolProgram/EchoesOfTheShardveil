using Microsoft.Xna.Framework.Audio;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Plays the theme music and holds the sound preference shared by every screen.</summary>
internal sealed class GameAudio : IDisposable
{
    private SoundEffect? soundtrack;
    private SoundEffectInstance? music;
    private bool isStarted;

    /// <summary>Gets whether the player wants sound.</summary>
    public bool Enabled { get; private set; } = true;

    /// <summary>Begins playback, if sound is enabled and audio hardware is available.</summary>
    public void Start()
    {
        isStarted = true;
        ApplyPreference();
    }

    /// <summary>Turns sound on or off.</summary>
    public void Toggle()
    {
        Enabled = !Enabled;
        ApplyPreference();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        isStarted = false;
        ReleasePlayback();
    }

    /// <summary>Plays or pauses the music to match the preference, loading it when first needed.</summary>
    private void ApplyPreference()
    {
        if (!isStarted)
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
                using var stream = EmbeddedAssets.Open("MainUI-Theme.wav");
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

    /// <summary>Disposes the music and its sound data.</summary>
    private void ReleasePlayback()
    {
        music?.Dispose();
        music = null;
        soundtrack?.Dispose();
        soundtrack = null;
    }
}
