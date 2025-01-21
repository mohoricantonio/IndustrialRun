using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject PlayButton;
    public GameObject HowToButton;
    public GameObject QuitButton;
    public GameObject BackButton;
    public GameObject InstructionsText;
    public void Play()
    {
        SceneManager.LoadScene("LevelScene");
    }
    public void HowToPlay()
    {
        PlayButton.SetActive(false);
        HowToButton.SetActive(false);
        QuitButton.SetActive(false);
        BackButton.SetActive(true);
        InstructionsText.SetActive(true);
    }
    public void Quit()
    {
        Application.Quit();
    }
    public void Back()
    {
        PlayButton.SetActive(true);
        HowToButton.SetActive(true);
        QuitButton.SetActive(true);
        BackButton.SetActive(false);
        InstructionsText.SetActive(false);
    }
}
