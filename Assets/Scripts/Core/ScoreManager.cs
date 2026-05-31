using UnityEngine;

public class ScoreManager : Singleton<ScoreManager>
{

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
