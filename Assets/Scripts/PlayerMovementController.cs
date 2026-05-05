using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovementController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 10f;
    public float runSpeed = 15f;
    public float initialSprintSpeed = 20f;
    public float speedIncreasePerSecond = 5f;
    public float delayBeforeSpeedIncrease = 2f;

    [Header("Jump Settings")]
    public float jumpForce = 12f;
    public float jumpTime = 0.3f;
    public float doubleJumpForce = 8f;
    public float coyoteTime = 0.15f;


    [Header("Dash Settings")]
    public float dashForce = 25f;
    public float dashDuration = 0.2f;
    public float downDashVerticalForce = 30f;
    public float downDashHorizontalSpeed = 5f;

    [Header("Skid Settings")]
    public float skidDeceleration = 10f;
    public float counterSteerMultiplier = 3f;
    public float skidThreshold = 16f;
    public float minSprintTimeForSkid = 0.5f;

    [Header("Recovery Settings")]
    public float stopStunDuration = 1f;

    [Header("Wall Settings")]
    public Transform wallCheck;
    public float wallCheckRadius = 0.2f;
    public LayerMask wallLayer;
    public float wallSlideSpeed = 2f; 

    private bool isWallTouching;
    private bool isWallSliding;

    [Header("Snappiness Settings")]
    public float fallMultiplier = 4f;
    public float lowJumpMultiplier = 3f;

    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;


    [Header("General Settings")]
    public float doubleTapTime = 0.3f;

    private Rigidbody2D rb;
    private float lastTapTimeA;
    private float lastTapTimeD;
    private bool isRunning;
    private float moveDirection;

    private bool isJumping;
    private float jumpTimeCounter;
    private float speedAtJumpStart;
    private float lockedDirection;
    private bool canDoubleJump;
    private bool canDash;
    public bool isGrounded;

    private bool isDashing;
    private bool isDownDashing;
    private bool isSkidding;
    private bool isRecovering;
    private float skidVelocity;
    private float skidDirection;
    private float coyoteTimeCounter;
    private float recoveryTimer;

    private float currentSprintTimer;
    private float dynamicSprintSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        CheckGrounded();
        ReadInput();
        CheckDoubleTap();
        WallSlide();

        if (isRecovering)
        {
            recoveryTimer -= Time.deltaTime;
            if (recoveryTimer <= 0)
            {
                isRecovering = false;
                Debug.Log("Player Recovery Finished");
            }
            return;
        }

        HandleJumpInput();
        HandleDashInput();
        HandleMovementLogic();

        if (isGrounded)
        {
            if (isDownDashing)
            {
                Debug.Log("Player Landed from Down Dash");
                isDownDashing = false;
            }
            coyoteTimeCounter = coyoteTime;
            canDash = true;
            canDoubleJump = true;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        if (isDownDashing || isDashing || isRecovering || isWallSliding) return;
        if (rb.linearVelocity.y < 0)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        else if (rb.linearVelocity.y > 0 && !Keyboard.current.spaceKey.isPressed)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;

        if (isSkidding && isGrounded) Skid();
    }

    void HandleMovementLogic()
    {
        if (isDashing || isDownDashing || isRecovering) return;

        float currentTargetSpeed = walkSpeed;
        bool isMoving = moveDirection != 0;
        bool sprintKeyHeld = Keyboard.current.enterKey.isPressed;
        bool sprintKeyReleased = Keyboard.current.enterKey.wasReleasedThisFrame;

        if (isRunning && sprintKeyHeld && isMoving)
        {
            currentSprintTimer += Time.deltaTime;

            // Maintain initial sprint speed for the first 2 seconds, then increase
            if (currentSprintTimer > delayBeforeSpeedIncrease)
            {
                float timeIncreasing = currentSprintTimer - delayBeforeSpeedIncrease;
                dynamicSprintSpeed = initialSprintSpeed + (timeIncreasing * speedIncreasePerSecond);
            }
            else
            {
                dynamicSprintSpeed = initialSprintSpeed;
            }

            currentTargetSpeed = dynamicSprintSpeed;
            Debug.Log("Sprinting at speed: " + currentTargetSpeed);
        }
        else
        {
            bool highSpeed = Mathf.Abs(rb.linearVelocity.x) > skidThreshold;
            bool sprintedLongEnough = currentSprintTimer >= minSprintTimeForSkid;

            if (isGrounded && isRunning && (sprintKeyReleased || !isMoving) && highSpeed && sprintedLongEnough && !isSkidding)
            {
                Debug.Log("Sprint Stop: Skidding started");
                isSkidding = true;
                skidVelocity = rb.linearVelocity.x;
                skidDirection = Mathf.Sign(skidVelocity);
            }

            currentSprintTimer = 0f;
            dynamicSprintSpeed = initialSprintSpeed;
        }

        if (isSkidding) return;

        if (isRunning && !sprintKeyHeld)
        {
            currentTargetSpeed = runSpeed;
            if (isMoving) Debug.Log("Player is Running");
        }
        else if (!isRunning && isMoving)
        {
            Debug.Log("Player is Walking");
        }

        if (isGrounded && moveDirection == 0 && Mathf.Abs(rb.linearVelocity.x) < 0.1f)
        {
            isRunning = false;
        }

        if (isGrounded)
        {
            speedAtJumpStart = currentTargetSpeed;
            lockedDirection = moveDirection;
        }

        ApplyMovement(speedAtJumpStart, lockedDirection);
    }

    void ApplyMovement(float speed, float direction)
    {
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
    }

    void StartJump()
    {
        Debug.Log("Player is Jumping");
        isJumping = true;
        jumpTimeCounter = jumpTime;
        coyoteTimeCounter = 0f;
        lockedDirection = moveDirection;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    void PerformDoubleJump()
    {
        Debug.Log("Player is Double Jumping");
        isJumping = false;
        canDoubleJump = false;
        lockedDirection = moveDirection;
        rb.linearVelocity = new Vector2(lockedDirection * speedAtJumpStart, doubleJumpForce);
    }

    void Dash() { StartCoroutine(PerformDashRoutine()); }

    private System.Collections.IEnumerator PerformDashRoutine()
    {
        canDash = false; isDashing = true;
        float dashDir = moveDirection != 0 ? moveDirection : (lockedDirection != 0 ? lockedDirection : 1f);
        lockedDirection = dashDir;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0;
        rb.linearVelocity = new Vector2(dashDir * dashForce, 0);
        yield return new WaitForSeconds(dashDuration);
        rb.gravityScale = originalGravity;
        isDashing = false;
    }

    void DownDash()
    {
        Debug.Log("Player is Down Dashing");
        isDownDashing = true; isJumping = false; canDash = false;
        rb.linearVelocity = new Vector2(moveDirection * downDashHorizontalSpeed, -downDashVerticalForce);
    }

    void Skid()
    {
        float deceleration = skidDeceleration;
        bool isCounterSteering = (skidDirection > 0 && moveDirection < 0) || (skidDirection < 0 && moveDirection > 0);
        if (isCounterSteering) deceleration *= counterSteerMultiplier;

        skidVelocity = Mathf.MoveTowards(skidVelocity, 0, deceleration * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(skidVelocity, rb.linearVelocity.y);

        if (Mathf.Abs(skidVelocity) < 0.1f) TriggerRecovery();
        else if (moveDirection == skidDirection) isSkidding = false;
    }

    void TriggerRecovery()
    {
        Debug.Log("Player is Stunned/Recovering");
        isRecovering = true; isSkidding = false; isRunning = false;
        recoveryTimer = stopStunDuration;
        rb.linearVelocity = Vector2.zero;
    }

    void ReadInput()
    {
        float left = Keyboard.current.aKey.isPressed ? -1f : 0f;
        float right = Keyboard.current.dKey.isPressed ? 1f : 0f;
        moveDirection = left + right;
    }

    void CheckGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void CheckDoubleTap()
    {
        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            if (Time.time - lastTapTimeA < doubleTapTime) isRunning = true;
            lastTapTimeA = Time.time;
        }
        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            if (Time.time - lastTapTimeD < doubleTapTime) isRunning = true;
            lastTapTimeD = Time.time;
        }
    }

    void HandleJumpInput()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (isGrounded || coyoteTimeCounter > 0) StartJump();
            else if (canDoubleJump) PerformDoubleJump();
        }
        if (Keyboard.current.spaceKey.isPressed && isJumping)
        {
            if (jumpTimeCounter > 0) { rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce); jumpTimeCounter -= Time.deltaTime; }
            else isJumping = false;
        }
        if (Keyboard.current.spaceKey.wasReleasedThisFrame) isJumping = false;
    }

    void HandleDashInput()
    {
        if (Keyboard.current.enterKey.wasPressedThisFrame && canDash)
        {
            if (isGrounded) { if (!isRunning) Dash(); }
            else { if (Keyboard.current.sKey.isPressed) DownDash(); else Dash(); }
        }
    }

    void WallSlide()
    {
        // Check if we are touching a wall
        isWallTouching = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, wallLayer);

        // Wall slide if: touching wall, NOT on ground, and moving downward
        if (isWallTouching && !isGrounded && rb.linearVelocity.y < 0)
        {
            isWallSliding = true;
            Debug.Log("Player is Wall Sliding");

            // Clamp the downward velocity to the slide speed
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, -wallSlideSpeed, float.MaxValue));
        }
        else
        {
            isWallSliding = false;
        }
    }

}
