using UnityEngine;

public class GameController
{
    private readonly SelectionController m_selectionController;
    private readonly NavigationController m_navigationController;
    private readonly InputController m_inputController;
    private readonly TacticalCameraController m_tacticalCameraController;
    private readonly GameUIController m_gameUIController;
    private readonly ObjectiveController m_objectiveController;
    private readonly FlowController m_flowController;

    public SelectionController SelectionController => m_selectionController;
    public NavigationController NavigationController => m_navigationController;
    public InputController InputController => m_inputController;
    public TacticalCameraController TacticalCameraController => m_tacticalCameraController;
    public GameUIController GameUIController => m_gameUIController;
    public DialogueController DialogueController => m_gameUIController.DialogueController;
    public ObjectiveController ObjectiveController => m_objectiveController;
    public FlowController FlowController => m_flowController;

    public GameController(Camera camera, Transform uiParent)
    {
        Debug.Assert(camera != null, "GameController: camera is null");
        Debug.Assert(uiParent != null, "GameController: uiParent is null");

        m_selectionController = new SelectionController();
        m_navigationController = new NavigationController(m_selectionController);

        m_inputController = new InputController(camera, m_selectionController, m_navigationController);
        m_tacticalCameraController = new TacticalCameraController(camera);

        m_gameUIController = new GameUIController(uiParent);

        // Wire dialogue events into input state
        var dialogue = m_gameUIController.DialogueController;
        if (dialogue != null)
        {
            dialogue.DialogueStarted += OnDialogueStarted;
            dialogue.DialogueStopped += OnDialogueStopped;
        }

        m_objectiveController = new ObjectiveController();
        m_flowController = new FlowController(m_objectiveController, m_gameUIController.DialogueController);
    }

    public void Tick(float deltaTime)
    {
        m_inputController.Tick(deltaTime);

        m_tacticalCameraController.Tick(deltaTime, m_inputController.CameraMoveInput);
        m_gameUIController.Tick(deltaTime, m_inputController.DialogueAdvancePressed);

        m_flowController.Tick(deltaTime);
    }

    private void OnDialogueStarted()
    {
        m_inputController.SetDialogueActive(true);
    }

    private void OnDialogueStopped()
    {
        m_inputController.SetDialogueActive(false);
    }
}
