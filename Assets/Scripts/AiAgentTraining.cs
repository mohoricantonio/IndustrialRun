using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;

public class AIAgentTraining : Agent
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
    public Transform EndEpizodeCollider;

    public Transform BoxesToJumpOver1;
    public Transform BoxesToJumpOver2;
    public Transform BoxesToJumpOver3;

    public Transform DuckUnder1;

    private float boxesToJumpOverRotationY;
    private int actionToPerform;

    private bool isCrouching = false;
    private float distanceToGoal;
    private int count = 0;
    #endregion

    #region Methods
    public override void OnActionReceived(ActionBuffers actions)
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
            //don't do anything
            case 0:
                break;
            //jump
            case 1:
                //Debug.Log("Jump");
                Jump();
                break;
            //crouch
            case 2:
                //Debug.Log("Crouch");
                Crouch();
                break;
            //get up
            case 3:
                //Debug.Log("Get up");
                GetUp();
                break;
            //double jump
            case 4:
                //Debug.Log("Double Jump");
                DoubleJump();
                break;
        }

        // Add reward if closer to the goal
        if (MathF.Abs(transform.localPosition.x - TargetTransform.localPosition.x) < distanceToGoal)
        {
            AddReward(5f / MaxStep);
            distanceToGoal = MathF.Abs(transform.localPosition.x - TargetTransform.localPosition.x);
        }

        if (MathF.Abs(transform.localPosition.x - TargetTransform.localPosition.x) < distanceToGoal)
        {
            distanceToGoal = MathF.Abs(transform.localPosition.x - TargetTransform.localPosition.x);
        }
        // Penalty given each step to encourage agent to finish task quickly.

        AddReward(-1f / MaxStep);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition.x - TargetTransform.localPosition.x);
        sensor.AddObservation(distanceToGoal);
        sensor.AddObservation(isCrouching);
        sensor.AddObservation(CanJump());
        sensor.AddObservation(CanDoubleJump());
    }

    public override void OnEpisodeBegin()
    {
        System.Random random = new System.Random();
        int randomValue = random.Next(0, 2);
        var duckUnder2 = DuckUnder1.Find("DuckUnder2");
        count++;
        if (count % 9 == 0 && count % 10 != 0)
        {
            BoxesToJumpOver1.gameObject.SetActive(false);
            BoxesToJumpOver2.gameObject.SetActive(false);
            BoxesToJumpOver3.gameObject.SetActive(false);
            DuckUnder1.gameObject.SetActive(false);
            duckUnder2.gameObject.SetActive(false);
        }
        else if (count % 10 == 0)
        {
            BoxesToJumpOver1.gameObject.SetActive(true);
            BoxesToJumpOver2.gameObject.SetActive(true);
            BoxesToJumpOver3.gameObject.SetActive(true);
            DuckUnder1.gameObject.SetActive(true);
            duckUnder2.gameObject.SetActive(true);
        }
        else
        {
            BoxesToJumpOver1.gameObject.SetActive(random.Next(0, 2) == 0);
            BoxesToJumpOver2.gameObject.SetActive(random.Next(0, 2) == 0);
            BoxesToJumpOver3.gameObject.SetActive(random.Next(0, 2) == 0);
            DuckUnder1.gameObject.SetActive(random.Next(0, 2) == 0);
            duckUnder2.gameObject.SetActive(random.Next(0, 2) == 0);
        }


        if (randomValue == 0)
        {

            transform.localPosition = new Vector3(-30, transform.localPosition.y, transform.localPosition.z);

            if (count % 15 == 0)
            {
                TargetTransform.localPosition = new Vector3(-100, TargetTransform.localPosition.y, TargetTransform.localPosition.z);
            }
            else
            {
                TargetTransform.localPosition = new Vector3(random.Next(-100, -35), TargetTransform.localPosition.y, TargetTransform.localPosition.z);
            }


            EndEpizodeCollider.localPosition = new Vector3(-20, EndEpizodeCollider.localPosition.y, EndEpizodeCollider.localPosition.z);

            BoxesToJumpOver1.localPosition = new Vector3(-90, BoxesToJumpOver1.localPosition.y, BoxesToJumpOver1.localPosition.z);

            BoxesToJumpOver1.rotation = Quaternion.Euler(0, boxesToJumpOverRotationY + 180, 0);

            DuckUnder1.localPosition = new Vector3(-5, DuckUnder1.localPosition.y, DuckUnder1.localPosition.z);

        }
        else
        {
            transform.localPosition = new Vector3(-85, transform.localPosition.y, transform.localPosition.z);

            if (count % 15 == 0)
            {
                TargetTransform.localPosition = new Vector3(-20, TargetTransform.localPosition.y, TargetTransform.localPosition.z);
            }
            else
            {
                TargetTransform.localPosition = new Vector3(random.Next(-80, -20), TargetTransform.localPosition.y, TargetTransform.localPosition.z);
            }
            EndEpizodeCollider.localPosition = new Vector3(-100, EndEpizodeCollider.localPosition.y, EndEpizodeCollider.localPosition.z);

            BoxesToJumpOver1.localPosition = new Vector3(-10, BoxesToJumpOver1.localPosition.y, BoxesToJumpOver1.localPosition.z);

            BoxesToJumpOver1.rotation = Quaternion.Euler(0, boxesToJumpOverRotationY, 0);

            DuckUnder1.localPosition = new Vector3(-20, DuckUnder1.localPosition.y, DuckUnder1.localPosition.z);
        }

        GetUp();
        isCrouching = false;
        canJump = true;

        distanceToGoal = Mathf.Abs(TargetTransform.localPosition.x - transform.localPosition.x);
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
        actions[1] = actionToPerform;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Finish")
        {
            AddReward(5f);
            EndEpisode();
        }
        if (other.gameObject.tag == "EndEpisode")
        {
            AddReward(-10f);
            EndEpisode();
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Obstacle")
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                Vector3 contactNormal = contact.normal;

                // Collision from left or right
                if (Mathf.Abs(contactNormal.y) < Mathf.Abs(contactNormal.x))
                {
                    AddReward(-5f / MaxStep);
                    break;
                }
            }
        }
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.tag == "Obstacle")
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                Vector3 contactNormal = contact.normal;
                // Collision from left or right
                if (Mathf.Abs(contactNormal.y) < Mathf.Abs(contactNormal.x))
                {
                    AddReward(-0.5f / MaxStep);
                    break;
                }
            }
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

        boxesToJumpOverRotationY = BoxesToJumpOver1.rotation.y;

        originalColiderSize = boxColider.size;
        originalColiderCenter = boxColider.center;
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
