using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class AIAgent : Agent
{

    #region Properties
    public float MovementSpeed = 8f, JumpForce = 10f, RotationSpeed = 800f, FallMultiplier = 3f, GroundCheckDistance = 0.5f, DoubleJumpCooldown = 2.5f, TrickDistance = 7.5f, MinYPos = 1.933181f;
    public LayerMask GroundLayer;
    public LayerMask TrickObsticleLayer;

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


    //AI parameters
    public Transform TargetTransform;
    public Transform Box;
    private Vector3 originalPosition;
    private bool isCrouching = false;
    private float distanceToGoal;
    #endregion

    #region Methods
    public override void OnActionReceived(ActionBuffers actions)
    {
        int move = actions.DiscreteActions[0];
        Debug.Log("move " + move);

        switch (move)
        {
            //don't move
            case 0:
                animator.SetBool("isMoving", false);
                break;
            //move right
            case 1:
                animator.SetBool("isMoving", true);
                movementDirection = new Vector3(1f, 0f, 0f);
                break;
            //move left
            case 2:
                animator.SetBool("isMoving", true);
                movementDirection = new Vector3(-1f, 0f, 0f);
                break;
            
        }

        // Penalty given each step to encourage agent to finish task quickly.
        AddReward(-1f / MaxStep);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition.x - TargetTransform.localPosition.x);
        //sensor.AddObservation(TargetTransform.localPosition.x);
    }

    public override void OnEpisodeBegin()
    {
        System.Random random = new System.Random();
        int randomValue = random.Next(0, 2);
        if (randomValue == 0)
        {
            TargetTransform.localPosition = new Vector3(-80, TargetTransform.localPosition.y, TargetTransform.localPosition.z);
            Box.localPosition = new Vector3(80, Box.localPosition.y, Box.localPosition.z);
        }
        else
        {
            TargetTransform.localPosition = new Vector3(-40, TargetTransform.localPosition.y, TargetTransform.localPosition.z);
            Box.localPosition = new Vector3(45, Box.localPosition.y, Box.localPosition.z);
        }
        
        Time.timeScale = 8f;
        transform.localPosition = originalPosition;
        isCrouching = false;
        SetAnimParameters();
    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.DiscreteActions;
        var move = Input.GetAxisRaw("Horizontal");
        switch (move)
        {
            case 0:
                actions[0] = 0;
                break;
            case 1:
                actions[0] = 1;
                break;
            case -1:
                actions[0] = 2;
                break;
        }

    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("LevelStart"))
        {
            AddReward(5f);
            EndEpisode();
        }
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            AddReward(-10f);
            EndEpisode();
        }
    }
    private void Jump()
    {
        animator.ResetTrigger("Jump");
        if (isGrounded && animator.GetBool("doingTrick") == false)
        {
            animator.SetBool("IsJumping", true);
            animator.SetBool("FinishedJump", false);
            animator.SetTrigger("Jump");
        }
        if (!isGrounded && readyToDoubleJump && animator.GetBool("doingTrick") == false)
        {
            readyToDoubleJump = false;
            animator.SetTrigger("Double jump");
            animator.SetBool("FinishedJump", false);
            rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
            Invoke(nameof(ResetDoubleJumpCooldown), DoubleJumpCooldown);
        }
    }
    private void Crouch()
    {
        if (CanCrouch() && !isCrouching)
        {
            animator.SetBool("isCrouching", true);
            MovementSpeed = MovementSpeed / 3;
            boxColider.center = new Vector3(boxColider.center.x, boxColider.center.y / 2.5f, boxColider.center.z);
            boxColider.size = new Vector3(boxColider.size.x, boxColider.size.y / 2f, boxColider.size.z);
            isCrouching = true;
        }
    }
    private void GetUp()
    {
        if (isCrouching)
        {
            animator.SetBool("isCrouching", false);
            MovementSpeed = originalMovementSpeed;
            boxColider.center = originalColiderCenter;
            boxColider.size = originalColiderSize;
            isCrouching = false;
        }
    }
    private void Trick()
    {
        if (canDoATrick)
        {
            animator.SetBool("doingTrick", true);
            var rnd = new System.Random(DateTime.Now.Millisecond);
            animator.SetInteger("Trick", rnd.Next(0, 4));
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        readyToDoubleJump = true;
        isGrounded = true;
        animator = GetComponent<Animator>();
        originalMovementSpeed = MovementSpeed;
        rotationDirection = Quaternion.LookRotation(new Vector3(1f, 0f, 0f), Vector3.up);
        originalZValue = transform.position.z;
        boxColider = GetComponent<BoxCollider>();
        isCrouching = false;

        originalPosition = transform.localPosition;
        originalColiderSize = boxColider.size;
        originalColiderCenter = boxColider.center;
        distanceToGoal = TargetTransform.position.x - originalPosition.x;
        SetAnimParameters();
    }
    private void Update()
    {
        CheckGroundStatus();
        canDoATrick = CanDoATrick();
        MoveRigidBody();
    }

    private void CheckGroundStatus()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, GroundCheckDistance, GroundLayer);
        if (isGrounded && animator.GetBool("FinishedJump"))
        {
            animator.SetBool("IsJumping", false);
        }
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
    private void MoveRigidBody()
    {
        if (animator.GetBool("isMoving") && movementDirection.x != 0f && animator.GetBool("doingTrick") == false)
        {
            rotationDirection = Quaternion.LookRotation(movementDirection, Vector3.up);
            transform.position = transform.position + movementDirection * Time.deltaTime * MovementSpeed;
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

    private void SetAnimParameters()
    {
        animator.SetBool("isMoving", false);
        animator.ResetTrigger("Double jump");
        animator.ResetTrigger("Jump");
        animator.SetBool("IsJumping", false);
        animator.SetBool("FinishedJump", false);
        animator.SetBool("isCrouching", false);
        animator.SetBool("doingTrick", false);
        animator.SetInteger("Trick", 0);
    }
    private void ResetDoubleJumpCooldown()
    {
        readyToDoubleJump = true;
    }

    public void FinishedJump()
    {
        animator.SetBool("FinishedJump", true);
    }
    public void FinishedTrick()
    {
        animator.SetBool("doingTrick", false);
    }

    public void JumpRigidBody()
    {
        rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
    }
    #endregion
}
