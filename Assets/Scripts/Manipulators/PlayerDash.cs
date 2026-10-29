using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDash : MonoBehaviour
{
    [Header("Dash")]
    public float dashSpeed = 24f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.2f;
    [Range(0f, 1f)] public float exitSpeedMultiplier = 0.4f; // keeps a bit of momentum after the dash
    public bool eightDirections = true;                      // snap to 8 directions like Celeste

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    // Your movement script should check this and skip its own velocity code while true
    public bool IsDashing { get; private set; }

    Rigidbody2D rb;
    Camera cam;
    float defaultGravity;
    float cooldownTimer;
    bool canDash = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;
        defaultGravity = rb.gravityScale;
    }

    void Update()
    {
        cooldownTimer -= Time.deltaTime;

        // Refresh the dash when touching the ground (Celeste gives you one air dash)
        if (!IsDashing && cooldownTimer <= 0f && IsGrounded())
        {
            canDash = true;
        }

        if (Input.GetMouseButtonDown(0) && canDash && !IsDashing && cooldownTimer <= 0f)
        {
            StartCoroutine(Dash());
        }
    }

    bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    Vector2 GetDashDirection()
    {
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (Vector2)(mouseWorld - transform.position);

        // Fallback if the cursor is right on top of the player
        if (dir.sqrMagnitude < 0.01f)
        {
            return new Vector2(Mathf.Sign(transform.localScale.x), 0f);
        }

        if (eightDirections)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            angle = Mathf.Round(angle / 45f) * 45f;
            float rad = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        return dir.normalized;
    }

    IEnumerator Dash()
    {
        canDash = false;
        IsDashing = true;

        Vector2 dir = GetDashDirection();

        rb.gravityScale = 0f;
        rb.linearVelocity = dir * dashSpeed; // on Unity 6, use rb.linearVelocity instead

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = defaultGravity;
        rb.linearVelocity = dir * dashSpeed * exitSpeedMultiplier;

        IsDashing = false;
        cooldownTimer = dashCooldown;
    }
}
