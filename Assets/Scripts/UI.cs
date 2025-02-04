using UnityEngine;

public class UI : MonoBehaviour
{
    public GameObject PlayerObject;
    public GameObject DoubleJump;
    public GameObject Trick;

    private PlayerController playerMovementScript;
    private PauseMenu pauseMenuScript;
    private void Start()
    {
        playerMovementScript = PlayerObject.GetComponent<PlayerController>();
        pauseMenuScript = GetComponentInParent<PauseMenu>();
    }


    private void Update()
    {
        if (!pauseMenuScript.isPaused)
        {
            if (playerMovementScript.readyToDoubleJump)
            {
                DoubleJump.SetActive(true);
            }
            else
            {
                DoubleJump.SetActive(false);
            }
            if (playerMovementScript.canDoATrick)
            {
                Trick.SetActive(true);
            }
            else
            {
                Trick.SetActive(false);
            }
        }
        else
        {
            DoubleJump.SetActive(false);
            Trick.SetActive(false);
        }
    }
}
