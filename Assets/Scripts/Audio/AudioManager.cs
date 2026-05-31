using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;
        // loopSfxSource separado para que el láser en bucle no sobreescriba la música
        if (loopSfxSource == null || loopSfxSource == musicSource)
        {
            loopSfxSource             = gameObject.AddComponent<AudioSource>();
            loopSfxSource.loop        = false;
            loopSfxSource.playOnAwake = false;
        }
    }

    [Header("AudioSources (GameObject)")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource loopSfxSource;

    [Header("Música")]
    public AudioClip musicTitle;
    public AudioClip musicMenu;
    public AudioClip musicGame;
    public AudioClip musicBoss;
    public AudioClip musicGameOver;
    public AudioClip musicVictory;
    [Range(0f,1f)] public float musicVolume = 0.6f;

    [Header("SFX — Jugador")]
    public AudioClip sfxShootNormal;
    public AudioClip sfxShootSpread;
    public AudioClip sfxShootLaser;
    public AudioClip sfxShootHoming;
    public AudioClip sfxPlayerHit;
    public AudioClip sfxPlayerDeath;
    public AudioClip sfxPowerUp;

    [Header("SFX — Enemigos")]
    public AudioClip sfxEnemyHit;
    public AudioClip sfxEnemyDeath;
    public AudioClip sfxBossDeath;
    public AudioClip sfxWarning;
    public AudioClip sfxBombExplosion;

    [Header("SFX — UI")]
    public AudioClip sfxMenuSelect;
    public AudioClip sfxMenuConfirm;
    public AudioClip sfxStageClearScore;
    [Range(0f,1f)] public float sfxVolume = 0.8f;

    [Header("SFX — Cuenta atrás Game Over (voz)")]
    [Tooltip("11 clips: índice 0 = zero.wav, 1 = one.wav, … 10 = ten.wav")]
    public AudioClip[] countdownClips = new AudioClip[11];

    bool _musicMuted;
    bool _sfxMuted;
    Coroutine _fadeCoroutine;

    void Start()
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.6f);
        sfxVolume   = PlayerPrefs.GetFloat("SFXVolume",   0.8f);
        if (musicSource   != null) { musicSource.volume = musicVolume; musicSource.loop = true; musicSource.priority = 0; }
        if (loopSfxSource != null) loopSfxSource.priority = 64;
        if (sfxSource     != null) sfxSource.priority     = 128;
    }

    public void PlayMusic(AudioClip clip, bool fade = true)
    {
        if (clip == null || musicSource == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.loop = true;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        if (fade) _fadeCoroutine = StartCoroutine(FadeMusic(clip));
        else { musicSource.clip = clip; musicSource.Play(); }
    }

    public void PlayTitleMusic()    => PlayMusic(musicTitle);
    public void PlayMenuMusic()     => PlayMusic(musicMenu);
    public void PlayGameMusic()     => PlayMusic(musicGame);
    public void PlayBossMusic()     => PlayMusic(musicBoss);
    public void PlayGameOverMusic() => PlayMusic(musicGameOver, false);
    public void PlayVictoryMusic()
    {
        AudioClip clip = musicVictory != null ? musicVictory : musicMenu;
        if (clip == null || musicSource == null) return;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        musicSource.loop = false;
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }
    public void StopMusic()        => musicSource?.Stop();
    public void PauseMusic()       => musicSource?.Pause();
    public void ResumeMusic()      => musicSource?.UnPause();

    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);
        if (musicSource != null) musicSource.volume = _musicMuted ? 0f : musicVolume;
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
    }

    public void ToggleMusicMute()
    {
        _musicMuted = !_musicMuted;
        if (musicSource != null) musicSource.volume = _musicMuted ? 0f : musicVolume;
    }

    public void PlaySFX(string name)
    {
        AudioClip clip = GetClipByName(name);
        if (clip == null) { Debug.LogWarning("SFX no encontrado: " + name); return; }
        PlaySFX(clip);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null || _sfxMuted) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlaySFXAtPosition(AudioClip clip, Vector3 position)
    {
        if (clip == null || _sfxMuted) return;
        AudioSource.PlayClipAtPoint(clip, position, sfxVolume);
    }

    public void PlayBombExplosionSFX()
    {
        if (sfxBombExplosion != null) PlaySFX(sfxBombExplosion);
    }

    /// <summary>
    /// Reproduce un SFX con variación de pitch y volumen sin afectar a otros SFX simultáneos.
    /// Crea una AudioSource temporal que se autodestruye al terminar el clip.
    /// Útil para ráfagas de explosiones donde la repetición exacta del mismo clip suena artificial.
    /// </summary>
    public void PlaySFXVaried(AudioClip clip, float minPitch = 0.9f, float maxPitch = 1.1f, float volumeScale = 1f)
    {
        if (clip == null || _sfxMuted) return;
        var go  = new GameObject("SFX_" + clip.name);
        var src = go.AddComponent<AudioSource>();
        src.clip         = clip;
        src.volume       = sfxVolume * Mathf.Clamp01(volumeScale);
        src.pitch        = UnityEngine.Random.Range(minPitch, maxPitch);
        src.spatialBlend = 0f;
        src.priority     = 128;
        src.Play();
        // Duración real = length / pitch (clip más rápido si pitch > 1)
        Destroy(go, clip.length / Mathf.Max(src.pitch, 0.01f) + 0.1f);
    }

    /// <summary>
    /// Reproduce la locución del número indicado (0–10) sobre la cuenta atrás de Game Over.
    /// El índice del array equivale al número: countdownClips[5] reproduce "five".
    /// </summary>
    public void PlayCountdownNumber(int n)
    {
        if (n < 0 || n > 10) return;
        if (countdownClips == null || countdownClips.Length <= n) return;
        var clip = countdownClips[n];
        if (clip != null) PlaySFX(clip);
    }

    public float WarningSFXLength => sfxWarning != null ? sfxWarning.length : 3f;

    public void PlayWarningSFXOneShot()
    {
        if (sfxWarning != null) PlaySFX(sfxWarning);
    }

    public void PlayWarningSFX()
    {
        if (sfxWarning == null || loopSfxSource == null || _sfxMuted) return;
        loopSfxSource.clip   = sfxWarning;
        loopSfxSource.loop   = true;
        loopSfxSource.volume = sfxVolume;
        loopSfxSource.Play();
    }

    public void StopWarningSFX()
    {
        if (loopSfxSource != null) loopSfxSource.Stop();
    }

    public void PlayStageClearScore()
    {
        if (sfxStageClearScore == null || loopSfxSource == null || _sfxMuted) return;
        loopSfxSource.clip   = sfxStageClearScore;
        loopSfxSource.loop   = true;
        loopSfxSource.volume = sfxVolume;
        loopSfxSource.Play();
    }

    public void StopStageClearScore()
    {
        if (loopSfxSource != null) loopSfxSource.Stop();
    }

    public void PlayLaserSFX()
    {
        if (sfxShootLaser == null || loopSfxSource == null || _sfxMuted) return;
        if (loopSfxSource.isPlaying && loopSfxSource.clip == sfxShootLaser) return;
        loopSfxSource.clip   = sfxShootLaser;
        loopSfxSource.loop   = true;
        loopSfxSource.volume = sfxVolume;
        loopSfxSource.Play();
    }

    public void StopLaserSFX()
    {
        if (loopSfxSource != null && loopSfxSource.clip == sfxShootLaser)
            loopSfxSource.Stop();
    }

    public void SetSFXVolume(float v) { sfxVolume = Mathf.Clamp01(v); PlayerPrefs.SetFloat("SFXVolume", sfxVolume); }
    public void ToggleSFXMute()       { _sfxMuted = !_sfxMuted; }

    AudioClip GetClipByName(string name)
    {
        switch (name.ToLower())
        {
            case "shoot_normal":  return sfxShootNormal;
            case "shoot_spread":  return sfxShootSpread;
            case "shoot_laser":   return sfxShootLaser;
            case "shoot_homing":  return sfxShootHoming;
            case "player_hit":    return sfxPlayerHit;
            case "player_death":  return sfxPlayerDeath;
            case "powerup":       return sfxPowerUp;
            case "enemy_hit":     return sfxEnemyHit;
            case "enemy_death":   return sfxEnemyDeath;
            case "boss_death":    return sfxBossDeath;
            case "warning":       return sfxWarning;
            case "menu_select":   return sfxMenuSelect;
            case "menu_confirm":  return sfxMenuConfirm;
            default:              return null;
        }
    }

    System.Collections.IEnumerator FadeMusic(AudioClip newClip)
    {
        float fadeDuration = 0.8f;
        float startVolume  = musicSource.volume;

        for (float t = 0; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }
        musicSource.Stop();
        musicSource.clip   = newClip;
        musicSource.volume = 0f;
        musicSource.Play();

        for (float t = 0; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, musicVolume, t / fadeDuration);
            yield return null;
        }
        musicSource.volume = musicVolume;
    }
}
