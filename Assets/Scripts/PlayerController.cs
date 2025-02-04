using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    #region Properties
    public float MovementSpeed = 7f, JumpForce = 7f, RotationSpeed = 700f, FallMultiplier = 3f, GroundCheckDistance = 0.5f, DoubleJumpCooldown = 5f, TrickDistance = 5f, MinYPos = 1.93f, StepInterval = 0.5f;
    public LayerMask GroundLayer;
    public LayerMask TrickObsticleLayer;

    public AudioClip[] FootstepSounds;
    public AudioClip JumpSound;
    public AudioClip TrickSound;

    public GameObject Agent;

    private Animator animator;
    private Rigidbody rb;
    private Vector3 movementDirection;
    private Quaternion rotationDirection;

    public bool readyToDoubleJump;
    public bool canDoATrick;

    private bool isGrounded;
    private float originalMovementSpeed;

    private float originalZValue;

    private BoxCollider boxColider;
    private Vector3 originalColiderSize;
    private Vector3 originalColiderCenter;

    private AudioSource audioSource;
    private bool readyToPlaySound;
    private int stepSoundNum;
    private float originalStepInterval;

    private ScoreManager scoreManager;

    #endregion

    #region CallbackContext methods
    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (isGrounded && animator.GetBool("doingTrick") == false && animator.GetBool("isCrouching") == false && animator.GetBool("IsJumping") == false)
            {
                animator.SetBool("IsJumping", true);
                animator.SetBool("FinishedJump", false);
                animator.SetTrigger("Jump");
            }
            if (!isGrounded && readyToDoubleJump && animator.GetBool("doingTrick") == false && animator.GetBool("isCrouching") == false)
            {
                readyToDoubleJump = false;
                animator.SetTrigger("Double jump");
                animator.SetBool("FinishedJump", false);
                rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
                audioSource.PlayOneShot(JumpSound);
                Invoke(nameof(ResetDoubleJumpCooldown), DoubleJumpCooldown);
            }
        }

    }
    public void Move(InputAction.CallbackContext context)
    {
        if (animator.GetBool("doingTrick") == false)
        {
            Vector2 inputVector = context.ReadValue<Vector2>();
            if (context.performed)
            {
                if (inputVector.x != 0)
                {
                    animator.SetBool("isMoving", true);
                    movementDirection = new Vector3(inputVector.x, 0f, 0f);
                }
            }
        }
        if (context.canceled)
        {
            animator.SetBool("isMoving", false);
        }
    }
    public void Crouch(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (CanCrouch())
            {
                animator.SetBool("isCrouching", true);
                MovementSpeed = MovementSpeed / 3;
                StepInterval = StepInterval * 3;
                boxColider.center = new Vector3(boxColider.center.x, boxColider.center.y / 2.5f, boxColider.center.z);
                boxColider.size = new Vector3(boxColider.size.x, boxColider.size.y / 2f, boxColider.size.z);
            }

        }
        else if (context.canceled)
        {
            animator.SetBool("isCrouching", false);
            MovementSpeed = originalMovementSpeed;
            boxColider.center = originalColiderCenter;
            boxColider.size = originalColiderSize;
            StepInterval = originalStepInterval;

        }
    }
    public void Trick(InputAction.CallbackContext context)
    {
        if (canDoATrick)
        {
            if (context.performed)
            {
                animator.SetBool("doingTrick", true);
                var rnd = new System.Random(DateTime.Now.Millisecond);
                animator.SetInteger("Trick", rnd.Next(0, 4));
            }
        }
    }
    #endregion

    #region Start & Update
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        readyToDoubleJump = true;
        readyToPlaySound = true;
        isGrounded = true;
        animator = GetComponent<Animator>();
        animator.SetBool("isMoving", false);
        originalMovementSpeed = MovementSpeed;
        rotationDirection = Quaternion.LookRotation(new Vector3(1f, 0f, 0f), Vector3.up);
        originalZValue = transform.position.z;
        boxColider = GetComponent<BoxCollider>();
        audioSource = GetComponent<AudioSource>();
        stepSoundNum = 0;
        originalStepInterval = StepInterval;

        originalColiderSize = boxColider.size;
        originalColiderCenter = boxColider.center;

        scoreManager = GameObject.Find("UIPanel").GetComponent<ScoreManager>();

    }

    private void Update()
    {
        CheckGroundStatus();
        canDoATrick = CanDoATrick();
        MoveRigidBody();
    }
    #endregion

    #region Helper methods
    public void FinishedJump()
    {
        animator.SetBool("FinishedJump", true);
    }
    public void FinishedTrick()
    {
        animator.SetBool("doingTrick", false);
        scoreManager.AddTrick();
    }

    public void JumpRigidBodyWhileDoingTrick()
    {
        rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
        audioSource.PlayOneShot(TrickSound);
    }
    private void MoveRigidBody()
    {
        if (animator.GetBool("isMoving") && movementDirection.x != 0f && animator.GetBool("doingTrick") == false)
        {
            rotationDirection = Quaternion.LookRotation(movementDirection, Vector3.up);
            transform.position = transform.position + movementDirection * Time.deltaTime * MovementSpeed;
            if (isGrounded)
                PlayFootstepSound();
        }
        if (animator.GetBool("doingTrick"))
        {
            transform.position = transform.position + movementDirection * Time.deltaTime * MovementSpeed;

        }
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationDirection, RotationSpeed * Time.deltaTime);
        float ypos = transform.position.y;
        if (ypos <= MinYPos)
        {
            ypos = MinYPos + 0.001f;
        }
        transform.position = new Vector3(transform.position.x, ypos, originalZValue);

        if (!isGrounded)
        {
            rb.linearVelocity = rb.linearVelocity + Vector3.up * Physics.gravity.y * FallMultiplier * Time.deltaTime;
        }
    }
    private void JumpRigidBody()
    {
        rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
        audioSource.PlayOneShot(JumpSound);
    }
    private void CheckGroundStatus()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, GroundCheckDistance, GroundLayer);
        if (isGrounded && animator.GetBool("FinishedJump"))
        {
            animator.SetBool("IsJumping", false);
        }
    }
    private void ResetDoubleJumpCooldown()
    {
        readyToDoubleJump = true;
    }
    private bool CanCrouch()
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Fast Run") || animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
            return true;
        return false;
    }
    private bool CanDoATrick()
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Fast Run"))
        {
            if (Physics.Raycast(transform.position, Vector3.right, TrickDistance, TrickObsticleLayer))
            {
                return true;
            }
            if (Physics.Raycast(transform.position, Vector3.left, TrickDistance, TrickObsticleLayer))
            {
                return true;
            }
        }

        return false;
    }

    private void PlayFootstepSound()
    {
        if (FootstepSounds.Length == 0)
            return;
        if (!readyToPlaySound)
            return;
        audioSource.PlayOneShot(FootstepSounds[stepSoundNum]);
        stepSoundNum++;
        if (stepSoundNum == FootstepSounds.Length)
            stepSoundNum = 0;
        readyToPlaySound = false;
        Invoke(nameof(ResetStepSoundCooldown), StepInterval);
    }
    private void ResetStepSoundCooldown()
    {
        readyToPlaySound = true;
    }
    private void ActivateAgent()
    {
        Agent.SetActive(true);
    }
    #endregion

    #region Collision methods
    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.CompareTag("ActivateAgent"))
        {
            Invoke(nameof(ActivateAgent), 2.5f);
        }
        if (collider.gameObject.CompareTag("Agent"))
        {
            Debug.Log("Agent caught you!");
        }
    }

    #endregion
}
