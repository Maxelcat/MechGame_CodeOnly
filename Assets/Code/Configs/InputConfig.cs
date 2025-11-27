using UnityEngine;

[CreateAssetMenu(menuName = "Config/Input Config", fileName = "InputConfig")]
public class InputConfig : ScriptableObject
{
    [Header("Raycast")]
    [Tooltip("Layers the input ray can hit for selection / movement.")]
    public LayerMask ClickMask;
    public float MaxClickRayDistance = 1000f;


    [Header("Camera Movement Keys")]
    public KeyCode CameraForwardKey = KeyCode.W;
    public KeyCode CameraBackwardKey = KeyCode.S;
    public KeyCode CameraLeftKey = KeyCode.A;
    public KeyCode CameraRightKey = KeyCode.D;

    [Tooltip("Optional secondary keys for camera movement (arrow keys).")]
    public bool UseAltCameraKeys = true;
    public KeyCode CameraForwardAltKey = KeyCode.UpArrow;
    public KeyCode CameraBackwardAltKey = KeyCode.DownArrow;
    public KeyCode CameraLeftAltKey = KeyCode.LeftArrow;
    public KeyCode CameraRightAltKey = KeyCode.RightArrow;

    [Header("Dialogue")]
    [Tooltip("Key to advance dialogue lines.")]
    public KeyCode DialogueAdvanceKey = KeyCode.Space;

    [Tooltip("If true, left mouse click can also advance dialogue when one is playing.")]
    public bool DialogueAdvanceOnLeftClick = true;
}
