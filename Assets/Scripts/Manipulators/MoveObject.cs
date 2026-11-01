using UnityEngine;

/// <summary>
/// Lets UnityEvents (like TriggerWithKey's) change just the X or Y of an object.
/// Put this on the object you want to move, then call these methods from an event.
/// </summary>
public class MoveObject : MonoBehaviour
{
    [Tooltip("If on, positions are relative to the parent. Otherwise they're world positions.")]
    public bool useLocalPosition = false;

    // Jump to an exact coordinate on one axis.
    public void SetX(float x) { SetPosition(new Vector3(x, GetPosition().y, GetPosition().z)); }
    public void SetY(float y) { SetPosition(new Vector3(GetPosition().x, y, GetPosition().z)); }

    // Shift by an amount on one axis (negative moves left/down).
    public void MoveX(float amount) { SetPosition(GetPosition() + new Vector3(amount, 0, 0)); }
    public void MoveY(float amount) { SetPosition(GetPosition() + new Vector3(0, amount, 0)); }

    private Vector3 GetPosition()
    {
        return useLocalPosition ? transform.localPosition : transform.position;
    }

    private void SetPosition(Vector3 position)
    {
        if (useLocalPosition) transform.localPosition = position;
        else transform.position = position;
    }
}
