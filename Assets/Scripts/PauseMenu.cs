using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject PauseMenuPanel;
    public GameObject Stats;
    public GameObject Title;
    public GameObject ResumeButton;
    public bool isPaused;

    private ScoreManager scoreManager;
    private TextMeshProUGUI statsText;

    private void Start()
    {
        isPaused = false;
        scoreManager = GameObject.Find("UIPanel").GetComponent<ScoreManager>();
        statsText = Stats.GetComponent<TextMeshProUGUI>();
        statsText.text = "\nTime: 0s\nTricks performed: 0\n\nScore: 0";
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused && !scoreManager.levelEnded)
            {
                Resume();
            }
            else if(!scoreManager.levelEnded)
            {
                Pause();
            }
        }
    }
    public void Restart()
    {
        PauseMenuPanel.SetActive(false);
        Time.timeScale = 1.0f;
        isPaused = false;
        SceneManager.LoadScene("LevelScene");
    }
    public void Resume()
    {
        PauseMenuPanel.SetActive(false);
        Time.timeScale = 1.0f;
        isPaused = false;
        EventSystem.current.SetSelectedGameObject(null);
    }
    public void MainMenu()
    {
        PauseMenuPanel.SetActive(false);
        Time.timeScale = 1.0f;
        isPaused = false;
        SceneManager.LoadScene("MainMenu");
    }

    public void Pause()
    {
        PauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        statsText.text = "\nTime: " + scoreManager.ReturnTime().ToString() + "s\nTricks performed: " + scoreManager.ReturnTricksPerformed().ToString()
            + "\n\nScore: " + scoreManager.ReturnScore().ToString();
        EventSystem.current.SetSelectedGameObject(null);
    }
    public void LevelEnd()
    {
        PauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        statsText.text = "\nTime: " + scoreManager.ReturnTime().ToString() + "s\nTricks performed: " + scoreManager.ReturnTricksPerformed().ToString()
            + "\n\nScore: " + scoreManager.ReturnScore().ToString();

        Title.GetComponent<TextMeshProUGUI>().text = "Level complete!";
        ResumeButton.SetActive(false);
        EventSystem.current.SetSelectedGameObject(null);
    }
    public void PlayerCaught()
    {
        PauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        statsText.text = "\nAgent has caught you!\n";
        statsText.text += "\nTime: " + scoreManager.ReturnTime().ToString() + "s\nTricks performed: " + scoreManager.ReturnTricksPerformed().ToString()
            + "\n\nScore: " + scoreManager.ReturnScore().ToString();

        Title.GetComponent<TextMeshProUGUI>().text = "Level failed!";
        ResumeButton.SetActive(false);
        EventSystem.current.SetSelectedGameObject(null);
    }
}
