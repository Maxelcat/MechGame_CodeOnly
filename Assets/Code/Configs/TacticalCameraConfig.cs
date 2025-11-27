using UnityEngine;

[CreateAssetMenu(menuName = "Config/Tactical Camera Config", fileName = "TacticalCameraConfig")]
public class TacticalCameraConfig : ScriptableObject
{
    [Header("Movement")]
    public float MoveSpeed = 25f;
    public float EdgeThickness = 10f;

    [Header("Bounds")]
    public bool UseBounds = false;
    public Vector2 BoundsX = new Vector2(-50f, 50f);
    public Vector2 BoundsZ = new Vector2(-50f, 50f);

    [Header("Obstruction / Click Through")]
    public bool EnableObstructionFade = true;

    [Tooltip("Which layers count as level geo that can obstruct the view.")]
    public LayerMask ObstructionMask;

    [Tooltip("Layer to put obstructing geo on so clicks ignore it.")]
    public int ClickThroughLayer = 2;

    [Header("Screen-Space Radial Fade")]
    [Tooltip("Screen-space radius of the visibility bubble around each occluded unit (0..0.5).")]
    [Range(0f, 0.5f)]
    public float RadialFadeRadius = 0.12f;

    [Tooltip("Screen-space width of the soft edge (0..0.5).")]
    [Range(0f, 0.5f)]
    public float RadialFadeSoftness = 0.06f;
}