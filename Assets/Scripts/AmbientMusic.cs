using UnityEngine;

public class AmbientMusic : MonoBehaviour
{


    public AudioClip LevelStartSound;
    public AudioClip LevelEndSound;
    public AudioClip BackgroundMusic;


    private bool levelStarted;
    private bool levelEnd;
    private AudioSource audioSource;
    private ScoreManager scoreManager;

    // Start is called before the first frame update
    void Start()
    {
        levelEnd = false;
        levelStarted = false;
        audioSource = GetComponent<AudioSource>();

        audioSource.clip = BackgroundMusic;
        audioSource.loop = true;
        audioSource.volume = 0.15f;
        audioSource.Play();
        scoreManager = GameObject.Find("UIPanel").GetComponent<ScoreManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (levelStarted && audioSource.volume == 0.15f)
        {
            audioSource.volume = 0.6f;
        }
    }

    private void OnTriggerExit(Collider collider)
    {
        if (collider.gameObject.layer == LayerMask.NameToLayer("LevelStart"))
        {
            if (levelStarted == false)
            {
                levelStarted = true;
                audioSource.PlayOneShot(LevelStartSound, 0.4f);
                scoreManager.StartLevel();
            }
        }
        else if (collider.gameObject.layer == LayerMask.NameToLayer("LevelEnd"))
        {
            if (levelStarted == true && levelEnd == false)
            {
                levelEnd = true;
                audioSource.Stop();
                audioSource.PlayOneShot(LevelEndSound, 1f);
                scoreManager.EndLevel();
            }
        }
    }
}
