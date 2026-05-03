using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [Header("Puntos base por enemigo destruido")]
    public int pointsSmallEnemy  = 100;
    public int pointsMediumEnemy = 300;
    public int pointsBoss        = 5000;

    public int CurrentScore { get; private set; }
    public int HiScore      { get; private set; }

    void Start()
    {
        CurrentScore = 0;
        HiScore      = PlayerPrefs.GetInt("HiScore", 0);
        HUDController.Instance?.UpdateHiScore(HiScore);
    }

    public void AddScore(int basePoints)
    {
        CurrentScore += basePoints;

        HUDController.Instance?.UpdateScore(CurrentScore, basePoints);

        if (CurrentScore > HiScore)
        {
            HiScore = CurrentScore;
            HUDController.Instance?.UpdateHiScore(HiScore);
        }
    }

    public void AddScoreSmallEnemy()  => AddScore(pointsSmallEnemy);
    public void AddScoreMediumEnemy() => AddScore(pointsMediumEnemy);
    public void AddScoreBoss()        => AddScore(pointsBoss);

    public void ResetScore()
    {
        CurrentScore = 0;
        HUDController.Instance?.UpdateScore(0, 0);
    }

    public void SaveHiScore()
    {
        if (CurrentScore > PlayerPrefs.GetInt("HiScore", 0))
        {
            PlayerPrefs.SetInt("HiScore", CurrentScore);
            PlayerPrefs.Save();
        }
    }
}
