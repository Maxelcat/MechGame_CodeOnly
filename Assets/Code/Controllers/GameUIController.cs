using UnityEngine;

public class GameUIController
{
    private readonly DialogueController m_dialogueController;

    public DialogueController DialogueController => m_dialogueController;

    public GameUIController(Transform uiParent)
    {
        Debug.Assert(uiParent != null, "GameUIController: uiParent is null");
        m_dialogueController = new DialogueController(uiParent);
    }

    public void Tick(float deltaTime, bool dialogueAdvanceRequested)
    {
        m_dialogueController.Tick(deltaTime, dialogueAdvanceRequested);
    }
}
