using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Sequence", fileName = "DialogueSequence")]
public class DialogueSequence : ScriptableObject
{
    [System.Serializable]
    public class DialogueLine
    {
        public DialogueCharacter Speaker;

        [TextArea(2, 5)]
        public string Text;
    }

    [Header("Identity")]
    public string Id;

    [Header("Lines")]
    public DialogueLine[] Lines;
}
