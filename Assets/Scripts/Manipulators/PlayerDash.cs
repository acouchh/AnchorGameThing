using UnityEngine;
using System.Reflection;

/// <summary>
/// Celeste-style dash that sits alongside PlayerController without modifying it.
///
/// Setup:
///   1. Add this component to the same object as PlayerController.
///   2. Add a TriggerWithKey (same object or elsewhere) and set its key / gamepad button.
///      Button 1 is the jump button, so use 2 or 3 for dash.
///   3. In TriggerWithKey's "Key Press Event", add the player and choose PlayerDash > TryDash.
/// </summary>
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(100)] // Runs after PlayerController, so our velocity/gravity values win each FixedUpdate.
public class PlayerDash : MonoBehaviour
{
    [Header("Dash")]
    [Tooltip("Whether or not the player can dash. You can toggle this with a trigger via EnableDash.")]
    public bool dashEnabled = true;
    [Tooltip("How many dashes the player has before they must touch the ground to get them back.")]
    public int maxDashes = 1;
    [Tooltip("How fast the player moves during the dash. For reference, the default ground max speed is 16.")]
    public float dashSpeed = 35f;
    [Tooltip("How many seconds the dash lasts. Distance is roughly speed x duration.")]
    public float dashDuration = 0.15f;
    [Tooltip("A brief 'freeze' in seconds before the dash launches, like Celeste's. Set to 0 to disable.")]
    public float startupFreezeTime = 0.04f;
    [Tooltip("Seconds after a dash ends before the player can dash again.")]
    public float dashCooldown = 0.1f;
    [Range(0f, 1f)]
    [Tooltip("How much of the dash speed the player keeps when it ends (0 = stop dead). Horizontal speed is still capped by the player's max speed.")]
    public float endSpeedMultiplier = 0.4f;
    [Tooltip("If on, the dash snaps to 8 directions (like Celeste). If off, it uses the exact stick/key direction.")]
    public bool eightDirectionsOnly = true;
    [Tooltip("How far the stick must be pushed before it counts as a direction. Otherwise the player dashes the way they're facing.")]
    public float inputDeadzone = 0.3f;
    [Tooltip("Whether touching the ground refills the player's dashes.")]
    public bool rechargeOnGround = true;

    [Space]
    [Header("Feedback")]
    [Tooltip("The sound that plays when the player dashes.")]
    public AudioClip dashSound;
    [Tooltip("A particle system that plays when the player dashes (optional).")]
    public ParticleSystem dashParticle;
    [Tooltip("Animator trigger set when a dash starts. Leave blank to skip (avoids warnings if your animator doesn't have it).")]
    public string dashTriggerName = "";
    [Tooltip("Animator bool that is true while dashing. Leave blank to skip.")]
    public string dashingBoolName = "";

    [Space]
    public bool consoleLogOnDash = false;

    private enum DashState { Idle, Startup, Dashing }

    private DashState state = DashState.Idle;
    private float stateTimer = 0f;
    private float cooldownTimer = 0f;
    private int dashesRemaining;
    private Vector2 dashDirection = Vector2.right;

    private PlayerController controller;
    private Rigidbody2D body;
    private AudioSource audioSource;
    private Animator[] animators;
    private readonly ContactPoint2D[] contactBuffer = new ContactPoint2D[8];

    // PlayerController keeps its "is control enabled" flag private. We read it so we never dash
    // during a respawn or cutscene lock. If the field is ever renamed this falls back to "enabled".
    private static readonly FieldInfo controlEnabledField =
        typeof(PlayerController).GetField("controlEnabled", BindingFlags.NonPublic | BindingFlags.Instance);

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        body = GetComponent<Rigidbody2D>();
        audioSource = GetComponentInChildren<AudioSource>();
        animators = GetComponentsInChildren<Animator>(true);
        dashesRemaining = maxDashes;
    }

    void OnDisable()
    {
        // Don't leave the player locked out of control if this gets disabled mid-dash.
        if (state != DashState.Idle) CancelDash(true);
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (cooldownTimer > 0f) cooldownTimer -= dt;

        if (state == DashState.Idle)
        {
            if (rechargeOnGround && dashesRemaining < maxDashes && IsGrounded()) RefreshDashes();
            return;
        }

        // Respawning turns the body Kinematic and handles control itself, so just let go.
        if (body.bodyType != RigidbodyType2D.Dynamic)
        {
            CancelDash(false);
            return;
        }

        stateTimer -= dt;

        if (state == DashState.Startup)
        {
            HoldInPlace();
            if (stateTimer > 0f) return;
            state = DashState.Dashing;
            stateTimer = dashDuration;
        }

        if (stateTimer <= 0f)
        {
            EndDash();
            return;
        }

        body.linearVelocity = dashDirection * dashSpeed;
        body.gravityScale = 0f;
        body.linearDamping = 0f;
    }

    // --------- Input (hook these up to TriggerWithKey's UnityEvents) ---------

    /// <summary>Attempts a dash in the currently held direction. Call this from TriggerWithKey.</summary>
    public void TryDash()
    {
        if (!dashEnabled || state != DashState.Idle) return;
        if (dashesRemaining <= 0 || cooldownTimer > 0f) return;
        if (!ControlIsEnabled() || body.bodyType != RigidbodyType2D.Dynamic) return;

        StartDash();
    }

    // --------- Dash logic ---------

    private void StartDash()
    {
        dashDirection = GetDashDirection();
        dashesRemaining--;

        // Locks out movement, jumping, and crouching in PlayerController, and clears any queued jump.
        controller.EnablePlayerControl(false);

        if (startupFreezeTime > 0f)
        {
            state = DashState.Startup;
            stateTimer = startupFreezeTime;
        }
        else
        {
            state = DashState.Dashing;
            stateTimer = dashDuration;
        }

        SetAnimatorTrigger(dashTriggerName);
        SetAnimatorBool(dashingBoolName, true);
        if (dashParticle != null) dashParticle.Play();
        if (audioSource != null && dashSound != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(dashSound, controller.systemVariables.sfxVolume);
        }
        if (consoleLogOnDash) Debug.Log("Dash started: " + dashDirection + " (" + dashesRemaining + " left)");
    }

    private void EndDash()
    {
        state = DashState.Idle;
        cooldownTimer = dashCooldown;
        body.linearVelocity = dashDirection * dashSpeed * endSpeedMultiplier;
        SetAnimatorBool(dashingBoolName, false);
        controller.EnablePlayerControl(true);
        // Gravity and drag are restored by PlayerController on its next FixedUpdate.
    }

    private void CancelDash(bool restoreControl)
    {
        state = DashState.Idle;
        SetAnimatorBool(dashingBoolName, false);
        if (restoreControl && controller != null) controller.EnablePlayerControl(true);
    }

    private void HoldInPlace()
    {
        body.linearVelocity = Vector2.zero;
        body.gravityScale = 0f;
        body.linearDamping = 0f;
    }

    private Vector2 GetDashDirection()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (input.magnitude < inputDeadzone)
        {
            return new Vector2(controller.IsFacingLeft() ? -1f : 1f, 0f);
        }

        if (eightDirectionsOnly)
        {
            float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
            angle = Mathf.Round(angle / 45f) * 45f;
            return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        }

        return input.normalized;
    }

    private bool ControlIsEnabled()
    {
        if (controlEnabledField == null) return true;
        return (bool)controlEnabledField.GetValue(controller);
    }

    private bool IsGrounded()
    {
        // Reuses the ground filter and slope threshold from PlayerController so both agree on what "ground" is.
        ContactFilter2D filter = controller.systemVariables.groundFilter;
        filter.useTriggers = false;
        int count = body.GetContacts(filter, contactBuffer);
        for (int i = 0; i < count; i++)
        {
            if (contactBuffer[i].normal.y > controller.groundSlopeThreshold) return true;
        }
        return false;
    }

    // --------- External Manipulation (all UnityEvent-friendly, matching PlayerController's style) ---------

    public void EnableDash(bool enable)
    {
        dashEnabled = enable;
    }

    public void SetDashCount(int amount)
    {
        maxDashes = Mathf.Max(0, amount);
    }

    public void AdjustDashCount(int amount)
    {
        maxDashes = Mathf.Max(0, maxDashes + amount);
    }

    /// <summary>Refills all dashes. Use this for Celeste-style dash crystals.</summary>
    public void RefreshDashes()
    {
        dashesRemaining = maxDashes;
    }

    public bool IsDashing()
    {
        return state != DashState.Idle;
    }

    public int GetDashesRemaining()
    {
        return dashesRemaining;
    }

    // --------- Animation ---------

    private void SetAnimatorTrigger(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        foreach (Animator each in animators)
        {
            if (each != null && each.gameObject.activeInHierarchy) each.SetTrigger(name);
        }
    }

    private void SetAnimatorBool(string name, bool value)
    {
        if (string.IsNullOrEmpty(name)) return;
        foreach (Animator each in animators)
        {
            if (each != null && each.gameObject.activeInHierarchy) each.SetBool(name, value);
        }
    }
}
