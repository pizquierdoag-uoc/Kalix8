using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Si loopSfxSource es null o apunta al mismo AudioSource que musicSource,
        // creamos uno nuevo para evitar que el láser en bucle sobreescriba la música.
        if (loopSfxSource == null || loopSfxSource == musicSource)
        {
            loopSfxSource        = gameObject.AddComponent<AudioSource>();
            loopSfxSource.loop   = false;
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
        // Con efecto fade o no según parámetros
		if (clip == null || musicSource == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.loop = true;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        if (fade) _fadeCoroutine = StartCoroutine(FadeMusic(clip));
        else { musicSource.clip = clip; musicSource.Play(); }
    }

    // Métodos para las dirfefentes músicas del juego
	public void PlayTitleMusic()   => PlayMusic(musicTitle);			// Pantalla Título
    public void PlayMenuMusic()    => PlayMusic(musicMenu);				// Pantalla Menú
    public void PlayGameMusic()    => PlayMusic(musicGame);				// Pantalla Juego. Parea más fases tendré que hacerlo distinto
    public void PlayBossMusic()    => PlayMusic(musicBoss);				// Música Boss
    public void PlayGameOverMusic()=> PlayMusic(musicGameOver, false);	// Música Game Over. 
    public void PlayVictoryMusic()										// Música Stage Clear
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
        // Buscamos el efecto de sonido
		AudioClip clip = GetClipByName(name);
		
		// Añadido para comprobar si no he puesto el sonido
        if (clip == null) { Debug.LogWarning("[AudioManager] SFX no encontrado: " + name); return; }
		
		// Play el efecto de sonido
        PlaySFX(clip);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null|| _sfxMuted) return;
		//	{ Debug.LogWarning("[AudioManager] SFX no encontrado: " + name); return; }
			        
		// Play
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
        if (sfxShootLaser == null || sfxSource == null || _sfxMuted) return;
        sfxSource.PlayOneShot(sfxShootLaser, sfxVolume);
    }

    public void StopLaserSFX() { }

    public void SetSFXVolume(float v) { sfxVolume = Mathf.Clamp01(v); PlayerPrefs.SetFloat("SFXVolume", sfxVolume); }
    public void ToggleSFXMute()       { _sfxMuted = !_sfxMuted; }

    // Distintos sonidos para el juego
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
