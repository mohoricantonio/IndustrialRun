using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;

public class AIAgentPlay : Agent
{

    #region Properties
    public float MovementSpeed = 8f, JumpForce = 10f, RotationSpeed = 800f, FallMultiplier = 3f, GroundCheckDistance = 0.5f, DoubleJumpCooldown = 2.5f, MinYPos = -143.8166f;
    public LayerMask GroundLayer;

    private Animator animator;
    private Rigidbody rb;
    private Vector3 movementDirection;
    private Quaternion rotationDirection;

    public bool readyToDoubleJump;

    private bool isGrounded;
    private float originalMovementSpeed;

    private float originalZValue;

    private BoxCollider boxColider;
    private Vector3 originalColiderSize;
    private Vector3 originalColiderCenter;

    private bool canJump;
    private bool canDoubleJump;

    //AI parameters
    public Transform TargetTransform;
    public Transform FinalTargetTransform;

    private int actionToPerform;

    private bool isCrouching = false;
    private float distanceToGoal;
    private bool reachedFinalTarget;
    #endregion

    #region Methods
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (reachedFinalTarget == false)
        {
            int move = actions.DiscreteActions[0];
            int action = actions.DiscreteActions[1];
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

            switch (action)
            {
                case 0:
                    break;
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
                    DoubleJump();
                    break;
            }

            if (MathF.Abs(transform.localPosition.x - TargetTransform.localPosition.x) < 5f)
            {
                float newXValue = transform.localPosition.x + 5f;
                if (newXValue > FinalTargetTransform.localPosition.x)
                {
                    newXValue = FinalTargetTransform.localPosition.x - 1f;
                }
                TargetTransform.localPosition = new Vector3(newXValue, TargetTransform.localPosition.y, TargetTransform.localPosition.z);
                distanceToGoal = MathF.Abs(transform.localPosition.x - TargetTransform.localPosition.x);
            }
            if (MathF.Abs(transform.localPosition.x - TargetTransform.localPosition.x) < distanceToGoal)
            {
                distanceToGoal = MathF.Abs(transform.localPosition.x - TargetTransform.localPosition.x);
            }
        }
        else
        {
            SetAnimParameters();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition.x - TargetTransform.localPosition.x);
        sensor.AddObservation(distanceToGoal);
        sensor.AddObservation(isCrouching);
        sensor.AddObservation(CanJump());
        sensor.AddObservation(CanDoubleJump());
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
        actions[1] = actionToPerform;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Finish")
        {
            reachedFinalTarget = true;
        }
    }

    private void Jump()
    {
        actionToPerform = 0;
        if (CanJump())
        {
            animator.SetBool("IsJumping", true);
            animator.SetBool("FinishedJump", false);
            animator.SetTrigger("Jump");
        }
    }

    private bool CanJump()
    {
        return isGrounded && !isCrouching && animator.GetBool("FinishedJump") == true;
    }

    private void DoubleJump()
    {
        actionToPerform = 0;
        if (CanDoubleJump())
        {
            animator.SetBool("IsJumping", true);
            animator.SetBool("FinishedJump", false);
            animator.SetTrigger("Jump");
            readyToDoubleJump = false;
            animator.SetTrigger("Double jump");
            animator.SetBool("FinishedJump", false);
            rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
            Invoke(nameof(ResetDoubleJumpCooldown), DoubleJumpCooldown);
        }
    }

    private bool CanDoubleJump()
    {
        return readyToDoubleJump && !isCrouching && animator.GetBool("FinishedJump") == true;
    }

    private void Crouch()
    {
        actionToPerform = 0;
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
        actionToPerform = 0;
        if (isCrouching)
        {
            animator.SetBool("isCrouching", false);
            MovementSpeed = originalMovementSpeed;
            boxColider.center = originalColiderCenter;
            boxColider.size = originalColiderSize;
            isCrouching = false;
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
        originalZValue = transform.localPosition.z;
        boxColider = GetComponent<BoxCollider>();
        isCrouching = false;
        canJump = true;

        originalColiderSize = boxColider.size;
        originalColiderCenter = boxColider.center;
        distanceToGoal = Mathf.Abs(TargetTransform.localPosition.x - transform.localPosition.x);
        SetAnimParameters();
    }
    private void FixedUpdate()
    {
        CheckGroundStatus();
        MoveRigidBody();

    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            actionToPerform = 1;
        }
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (!isCrouching)
                actionToPerform = 2;
            else
                actionToPerform = 3;
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            actionToPerform = 4;
        }
        canJump = CanJump();
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
    private void MoveRigidBody()
    {
        if (animator.GetBool("isMoving") && movementDirection.x != 0f)
        {
            rotationDirection = Quaternion.LookRotation(movementDirection, Vector3.up);
            transform.position = transform.position + movementDirection * Time.deltaTime * MovementSpeed;
        }
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationDirection, RotationSpeed * Time.deltaTime);
        float ypos = transform.localPosition.y;
        if (ypos <= MinYPos)
        {
            ypos = MinYPos + 0.001f;
        }
        transform.localPosition = new Vector3(transform.localPosition.x, ypos, originalZValue);

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
        animator.SetBool("FinishedJump", true);
        animator.SetBool("isCrouching", false);
    }
    private void ResetDoubleJumpCooldown()
    {
        readyToDoubleJump = true;
    }

    public void FinishedJump()
    {
        animator.SetBool("FinishedJump", true);
    }

    public void JumpRigidBody()
    {
        rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
    }
    #endregion
}
