using UnityEngine;

/// <summary>
/// Changes the player's "Stop Friction" while they're in a trigger (ice, mud, etc.), or on demand
/// from any UnityEvent. Attach it to an object with a trigger collider, like your Trigger prefab.
/// Works through PlayerController.instance, so PlayerController doesn't need any changes.
/// </summary>
public class StopFrictionZone : MonoBehaviour
{
    [Tooltip("The stop friction to apply. 0 is very slippery (ice); higher values stop the player faster. The controller's default is 3.")]
    public float stopFriction = 0f;

    [Space]
    [Header("Which movement states to change")]
    [Tooltip("Ground Movement Values. This is the one that makes the player slide on the floor.")]
    public bool changeGround = true;
    [Tooltip("Air Movement Values. Controls how quickly the player's speed fades in the air when not pressing left/right.")]
    public bool changeAir = false;
    [Tooltip("Wall Movement Values.")]
    public bool changeWall = false;

    [Space]
    [Header("Automatic trigger behavior")]
    [Tooltip("Apply the friction when the player enters this object's trigger collider. Turn off if you'd rather call the methods from UnityEvents.")]
    public bool applyOnPlayerEnter = true;
    [Tooltip("Restore the original friction when the player leaves this object's trigger collider.")]
    public bool resetOnPlayerExit = true;

    [Space]
    public bool consoleLogOnChange = false;

    private bool hasOriginals = false;
    private float originalGround;
    private float originalAir;
    private float originalWall;

    void Start()
    {
        CaptureOriginals();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (applyOnPlayerEnter && other.GetComponentInParent<PlayerController>() != null) ApplyStopFriction();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (resetOnPlayerExit && other.GetComponentInParent<PlayerController>() != null) ResetStopFriction();
    }

    // --------- UnityEvent-friendly methods ---------

    /// <summary>Applies the Stop Friction value set in this component's inspector.</summary>
    public void ApplyStopFriction()
    {
        SetStopFriction(stopFriction);
    }

    /// <summary>Applies a specific value (type it in the UnityEvent's value box).</summary>
    public void SetStopFriction(float value)
    {
        PlayerController player = PlayerController.instance;
        if (player == null) return;
        CaptureOriginals();

        if (changeGround) player.groundMovementValues.stopFriction = value;
        if (changeAir) player.airMovementValues.stopFriction = value;
        if (changeWall) player.wallMovementValues.stopFriction = value;

        if (consoleLogOnChange) Debug.Log("Stop friction set to " + value + " by: " + gameObject.name);
    }

    /// <summary>Restores the stop friction values the player had when the level started.</summary>
    public void ResetStopFriction()
    {
        PlayerController player = PlayerController.instance;
        if (player == null || !hasOriginals) return;

        if (changeGround) player.groundMovementValues.stopFriction = originalGround;
        if (changeAir) player.airMovementValues.stopFriction = originalAir;
        if (changeWall) player.wallMovementValues.stopFriction = originalWall;

        if (consoleLogOnChange) Debug.Log("Stop friction reset by: " + gameObject.name);
    }

    private void CaptureOriginals()
    {
        if (hasOriginals) return;
        PlayerController player = PlayerController.instance;
        if (player == null) return;

        originalGround = player.groundMovementValues.stopFriction;
        originalAir = player.airMovementValues.stopFriction;
        originalWall = player.wallMovementValues.stopFriction;
        hasOriginals = true;
    }
}
