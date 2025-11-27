using UnityEngine;

[CreateAssetMenu(menuName = "Config/Gameplay Config", fileName = "GameplayConfig")]
public class GameplayConfig : ScriptableObject
{
    [Header("Selection")]
    [Tooltip("Prefab to show under selected units (e.g. a ring mesh or decal).")]
    public GameObject SelectionRingPrefab;

    [Header("Movement")]
    [Tooltip("Prefab to show at the clicked destination when issuing a move command.")]
    public GameObject DestinationMarkerPrefab;

    [Tooltip("How long the destination marker should live before being destroyed.")]
    public float DestinationMarkerLifetime = 1.5f;
}
