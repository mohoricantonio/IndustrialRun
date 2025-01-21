using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public int InitialScore = 1000;
    public int LevelEndBonus = 50;
    public int TrickPoints = 2;
    public float LevelEndDelay = 0.5f;

    private PauseMenu pauseMenu;
    private int numOfPerformedTricks;
    private float timeTaken;
    public bool levelEnded;
    private bool levelStarted;
    private int score;
    // Start is called before the first frame update
    void Start()
    {
        numOfPerformedTricks = 0;
        timeTaken = 0f;
        levelEnded = false;
        levelStarted = false;
        score = InitialScore;

        pauseMenu = GetComponentInParent<PauseMenu>();
    }

    // Update is called once per frame
    void Update()
    {
        if (levelStarted && !levelEnded)
            timeTaken += Time.deltaTime;
    }
    public void StartLevel()
    {
        levelStarted = true;
    }
    public void EndLevel()
    {
        levelEnded = true;
        Invoke(nameof(ShowLevelEndMenu), LevelEndDelay);

    }
    public void AddTrick()
    {
        if (levelStarted && !levelEnded)
        {
            numOfPerformedTricks++;
        }
    }
    public int ReturnTricksPerformed()
    {
        return numOfPerformedTricks;
    }
    public int ReturnTime()
    {
        return Mathf.CeilToInt(timeTaken);
    }
    public int ReturnScore()
    {
        if (!levelStarted) return 0;
        if (!levelEnded) UpdateScore();
        return score;
    }

    private void UpdateScore()
    {
        score = InitialScore - Mathf.CeilToInt(timeTaken) + numOfPerformedTricks * TrickPoints;
        if (levelEnded)
        {
            score += LevelEndBonus;
        }
        if (score < 0) score = 0;
    }

    private void ShowLevelEndMenu()
    {
        UpdateScore();
        pauseMenu.LevelEnd();
    }
}
