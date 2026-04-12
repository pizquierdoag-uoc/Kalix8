using UnityEngine;

public static class GameSettings
{
    public enum Difficulty { Easy = 0, Normal = 1, Hard = 2 }

    const string KEY_LIVES = "opt_lives";
    const string KEY_DIFF  = "opt_difficulty";

    static int        _lives;
    static Difficulty _difficulty;
    static bool       _loaded;

    static void EnsureLoaded()
    {
        if (_loaded) return;
        _lives      = PlayerPrefs.GetInt(KEY_LIVES, 3);
        _difficulty = (Difficulty)PlayerPrefs.GetInt(KEY_DIFF, (int)Difficulty.Normal);
        _loaded     = true;
    }

    public static int        StartingLives      { get { EnsureLoaded(); return _lives;      } }
    public static Difficulty CurrentDifficulty  { get { EnsureLoaded(); return _difficulty; } }


    /// Multiplicador de vida de enemigos
    public static float EnemyHealthMult
    {
        get
        {
            switch (CurrentDifficulty)
            {
                case Difficulty.Easy: return 0.75f;
                case Difficulty.Hard: return 1.50f;
                default:              return 1.00f;
            }
        }
    }

    /// Multiplicador de velocidad de enemigos y balas enemigas
    public static float EnemySpeedMult
    {
        get
        {
            switch (CurrentDifficulty)
            {
                case Difficulty.Easy: return 0.85f;
                case Difficulty.Hard: return 1.20f;
                default:              return 1.00f;
            }
        }
    }

    /// Multiplicador de intervalo entre spawns (>1 = más lento = más fácil)
    public static float SpawnIntervalMult
    {
        get
        {
            switch (CurrentDifficulty)
            {
                case Difficulty.Easy: return 1.35f;
                case Difficulty.Hard: return 0.75f;
                default:              return 1.00f;
            }
        }
    }

    public static string DifficultyName
    {
        get
        {
            switch (CurrentDifficulty)
            {
                case Difficulty.Easy: return "FÁCIL";
                case Difficulty.Hard: return "DIFÍCIL";
                default:              return "NORMAL";
            }
        }
    }

    public static string DifficultyDescription
    {
        get
        {
            switch (CurrentDifficulty)
            {
                case Difficulty.Easy: return "Enemigos más lentos y con menos vida";
                case Difficulty.Hard: return "Enemigos rápidos, oleadas más densas";
                default:              return "Experiencia estándar";
            }
        }
    }

    public static void SetLives(int lives)
    {
        EnsureLoaded();
        _lives = Mathf.Clamp(lives, 3, 5);
        PlayerPrefs.SetInt(KEY_LIVES, _lives);
        PlayerPrefs.Save();
    }

    public static void SetDifficulty(Difficulty d)
    {
        EnsureLoaded();
        _difficulty = d;
        PlayerPrefs.SetInt(KEY_DIFF, (int)_difficulty);
        PlayerPrefs.Save();
    }
}
