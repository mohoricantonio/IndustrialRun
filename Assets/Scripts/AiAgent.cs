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
    private Vector3 originalPosition;
    private float timeSinceLastPunishment = 0f;
    private bool isCrouching = false;
    private int action = 0;
    private float distanceToGoal;
    #endregion

    #region Methods
    public override void OnActionReceived(ActionBuffers actions)
    {
        int move = actions.DiscreteActions[0];
        int action = actions.DiscreteActions[1];
        Debug.Log("move " + move);
        Debug.Log("action " + action);

        switch (move)
        {
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
            //don't move
            default:
                animator.SetBool("isMoving", false);
                break;
        }
        switch (action)
        {
            case 1:
                Jump();
                break;
            case 2:
                Crouch();
                break;
            case 3:
                GetUp();
                break;
            case 4:
                Trick();
                break;
            case 0:
                break;
            default:
                break;
        }
        timeSinceLastPunishment += Time.deltaTime;

        // Check if 1 second has passed since the last reward was added
        if (timeSinceLastPunishment >= Time.timeScale)
        {
            AddReward(-1f); // Penalize the agent for taking too long
            timeSinceLastPunishment = 0f; // Reset the timer after adding the penalty
        }

        if(distanceToGoal > Mathf.Abs(TargetTransform.position.x - transform.position.x))
        {
            AddReward(5f);
        }
        else
        {
            AddReward(-3f);
        }
        distanceToGoal = TargetTransform.position.x - transform.position.x;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position.x);
        sensor.AddObservation(TargetTransform.position.x);
        sensor.AddObservation(canDoATrick);
        sensor.AddObservation(readyToDoubleJump);
    }

    public override void OnEpisodeBegin()
    {
        Time.timeScale = 4f;
        timeSinceLastPunishment = 0f;
        transform.position = originalPosition;
        isCrouching = false;
        action = 0;
        distanceToGoal = Mathf.Abs(TargetTransform.position.x - originalPosition.x);
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
        actions[1] = this.action;

    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("LevelStart"))
        {
            AddReward(50f);
            EndEpisode();
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Vector3 collisionDirection = collision.contacts[0].point - transform.position;

            // Normalize the direction vector
            collisionDirection = collisionDirection.normalized;

            // Check if the collision is from the sides (left or right)
            if (Mathf.Abs(collisionDirection.y) > Mathf.Abs(collisionDirection.x))
            {
                AddReward(-3f);
                EndEpisode();
            }
        }
    }
    private void Jump()
    {
        this.action = 0;
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
        this.action = 0;

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
        this.action = 0;
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
        this.action = 0;
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
        animator.SetBool("isMoving", false);
        originalMovementSpeed = MovementSpeed;
        rotationDirection = Quaternion.LookRotation(new Vector3(1f, 0f, 0f), Vector3.up);
        originalZValue = transform.position.z;
        boxColider = GetComponent<BoxCollider>();
        isCrouching = false;

        originalPosition = transform.position;
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
        if (Input.GetKeyDown(KeyCode.Space))
        {
            action = 1;
        }
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (!isCrouching)
                action = 2;
            else
                action = 3;
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            action = 4;
        }
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
            rb.velocity = rb.velocity + Vector3.up * Physics.gravity.y * FallMultiplier * Time.deltaTime;
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
        //AddReward(10f);
    }

    public void JumpRigidBody()
    {
        rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
    }
    #endregion
}
