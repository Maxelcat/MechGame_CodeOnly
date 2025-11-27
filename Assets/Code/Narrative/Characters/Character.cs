using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Character", fileName = "DialogueCharacter")]
public class DialogueCharacter : ScriptableObject
{
    [Header("Identity")]
    public string Id;           // Optional internal ID
    public string DisplayName;

    [Header("Visuals")]
    public Sprite Portrait;
}
