using UnityEngine;

[CreateAssetMenu(menuName = "Config/UI Config", fileName = "UIConfig")]
public class UIConfig : ScriptableObject
{
    [Header("Dialogue View")]
    [Tooltip("Prefab for the DialogueView UI. Will be instantiated by DialogueController.")]
    public GameObject DialogueViewPrefab;

    [Header("Dialogue Behaviour")]
    [Tooltip("Characters per second for typewriter text reveal.")]
    public float DialogueTextSpeed = 30f;

    [Tooltip("Seconds to keep a fully revealed line on screen before auto-advancing. 0 = no auto advance.")]
    public float DialogueAutoAdvanceDelay = 2.0f;

    [Tooltip("If true, player can advance dialogue manually (space/click).")]
    public bool DialogueAllowManualAdvance = true;
}
