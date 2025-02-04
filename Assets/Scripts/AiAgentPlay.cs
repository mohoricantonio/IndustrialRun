using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;

public class AiAgentPlay : Agent
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
    private bool isDoubleJumping;

    private float originalZValue;

    private BoxCollider boxColider;
    private Vector3 originalColiderSize;
    private Vector3 originalColiderCenter;

    private bool canDoubleJump;

    //AI parameters
    public Transform TargetTransform;
    public Transform FinalTargetTransform;

    private bool isCrouching = false;
    private float distanceToGoal;
    private float originalDistanceToGoal;
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

            if (MathF.Abs(transform.localPosition.x - TargetTransform.localPosition.x) < 15f)
            {
                float newXValue = transform.localPosition.x + (originalDistanceToGoal / 2f);
                if (newXValue > FinalTargetTransform.localPosition.x)
                {
                    newXValue = FinalTargetTransform.localPosition.x - 1f;
                }
                TargetTransform.localPosition = new Vector3(newXValue, TargetTransform.localPosition.y, TargetTransform.localPosition.z);
                distanceToGoal = originalDistanceToGoal / 2f;
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
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "Finish")
        {
            reachedFinalTarget = true;
        }
    }
    private void Jump()
    {
        if (CanJump())
        {
            animator.SetBool("IsJumping", true);
            animator.SetBool("FinishedJump", false);
            animator.SetTrigger("Jump");
        }
    }

    private bool CanJump()
    {
        if (isGrounded && !isCrouching)
        {
            if ((animator.GetBool("IsJumping") == false) && (animator.GetBool("FinishedJump") == true))
            {
                return true;
            }
        }
        return false;
    }

    private void DoubleJump()
    {
        if (CanDoubleJump())
        {
            isDoubleJumping = true;
            animator.SetBool("IsJumping", true);
            animator.SetBool("FinishedJump", false);
            animator.SetTrigger("Jump");
            readyToDoubleJump = false;
            animator.SetTrigger("Double jump");
            rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
            Invoke(nameof(ResetDoubleJumpCooldown), DoubleJumpCooldown);
        }
    }

    private bool CanDoubleJump()
    {
        if (isGrounded && readyToDoubleJump && !isCrouching)
        {
            if ((animator.GetBool("IsJumping") == false) && (animator.GetBool("FinishedJump") == true))
            {
                return true;
            }
        }
        return false;
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
        isDoubleJumping = false;

        originalColiderSize = boxColider.size;
        originalColiderCenter = boxColider.center;
        SetAnimParameters();

        distanceToGoal = Mathf.Abs(TargetTransform.localPosition.x - transform.localPosition.x);
        originalDistanceToGoal = distanceToGoal;

        reachedFinalTarget = false;
    }
    private void FixedUpdate()
    {
        CheckGroundStatus();
        MoveRigidBody();

    }

    private void CheckGroundStatus()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, GroundCheckDistance, GroundLayer);
        if (isGrounded && (animator.GetBool("FinishedJump") == true))
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
        if (isDoubleJumping)
        {
            isDoubleJumping = false;
        }
        else
        {
            animator.SetBool("FinishedJump", true);
        }
    }

    public void JumpRigidBody()
    {
        rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
    }
    #endregion
}
