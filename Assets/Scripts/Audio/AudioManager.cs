using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [Header("AudioSources (añade 2 al GameObject)")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Música")]
    public AudioClip musicTitle;
    public AudioClip musicMenu;
    public AudioClip musicGame;
    public AudioClip musicBoss;
    public AudioClip musicGameOver;
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

    [Header("SFX — UI")]
    public AudioClip sfxMenuSelect;
    public AudioClip sfxMenuConfirm;
    [Range(0f,1f)] public float sfxVolume = 0.8f;

    bool _musicMuted;
    bool _sfxMuted;

    void Start()
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.6f);
        sfxVolume   = PlayerPrefs.GetFloat("SFXVolume",   0.8f);
        if (musicSource != null) { musicSource.volume = musicVolume; musicSource.loop = true; }
    }

    public void PlayMusic(AudioClip clip, bool fade = true)
    {
        if (clip == null || musicSource == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        if (fade) StartCoroutine(FadeMusic(clip));
        else { musicSource.clip = clip; musicSource.Play(); }
    }

    public void PlayTitleMusic()     => PlayMusic(musicTitle);
    public void PlayMenuMusic()     => PlayMusic(musicMenu);
    public void PlayGameMusic()     => PlayMusic(musicGame);
    public void PlayBossMusic()     => PlayMusic(musicBoss);
    public void PlayGameOverMusic() => PlayMusic(musicGameOver, false);
    public void StopMusic()         => musicSource?.Stop();

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
        if (clip == null) { Debug.LogWarning("[AudioManager] SFX no encontrado: " + name); return; }
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
            case "menu_select":   return sfxMenuSelect;
            case "menu_confirm":  return sfxMenuConfirm;
            default:              return null;
        }
    }

    System.Collections.IEnumerator FadeMusic(AudioClip newClip)
    {
        float fadeDuration = 0.8f;
        float startVolume  = musicSource.volume;

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }
        musicSource.Stop();
        musicSource.clip   = newClip;
        musicSource.volume = 0f;
        musicSource.Play();

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, musicVolume, t / fadeDuration);
            yield return null;
        }
        musicSource.volume = musicVolume;
    }
}
