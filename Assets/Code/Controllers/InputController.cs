using UnityEngine;

public class InputController
{
    private readonly Camera m_camera;
    private readonly InputConfig m_config;
    private readonly SelectionController m_selectionController;
    private readonly NavigationController m_navigationController;

    public Vector2 CameraMoveInput { get; private set; }
    public bool DialogueAdvancePressed { get; private set; }

    private bool m_dialogueIsActive;

    public InputController(
        Camera camera,
        SelectionController selectionController,
        NavigationController navigationController)
    {
        Debug.Assert(App.Instance != null, "InputController: App.Instance is null");
        Debug.Assert(App.Instance.ResourceLoader != null, "InputController: ResourceLoader is null");

        m_camera = camera;
        m_selectionController = selectionController;
        m_navigationController = navigationController;

        m_config = App.Instance.ResourceLoader.LoadConfig<InputConfig>(ConfigId.Input);
        Debug.Assert(m_config != null, "InputController: InputConfig could not be loaded");
    }

    public void SetDialogueActive(bool active)
    {
        m_dialogueIsActive = active;
    }

    public void Tick(float deltaTime)
    {
        if (m_camera == null || m_config == null)
            return;

        // Reset per-frame state
        DialogueAdvancePressed = false;

        HandleCameraMovementInput();
        HandleDialogueInput();
        HandleMouseInput(deltaTime);
    }

    private void HandleCameraMovementInput()
    {
        float horizontal = 0f;
        float vertical = 0f;

        // Primary keys
        if (Input.GetKey(m_config.CameraLeftKey))
            horizontal -= 1f;
        if (Input.GetKey(m_config.CameraRightKey))
            horizontal += 1f;
        if (Input.GetKey(m_config.CameraForwardKey))
            vertical += 1f;
        if (Input.GetKey(m_config.CameraBackwardKey))
            vertical -= 1f;

        // Optional alt keys
        if (m_config.UseAltCameraKeys)
        {
            if (Input.GetKey(m_config.CameraLeftAltKey))
                horizontal -= 1f;
            if (Input.GetKey(m_config.CameraRightAltKey))
                horizontal += 1f;
            if (Input.GetKey(m_config.CameraForwardAltKey))
                vertical += 1f;
            if (Input.GetKey(m_config.CameraBackwardAltKey))
                vertical -= 1f;
        }

        Vector2 input = new Vector2(horizontal, vertical);
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        CameraMoveInput = input;
    }

    private void HandleDialogueInput()
    {
        if (!m_dialogueIsActive)
            return;

        bool keyPressed = Input.GetKeyDown(m_config.DialogueAdvanceKey);
        bool clickPressed = m_config.DialogueAdvanceOnLeftClick && Input.GetMouseButtonDown(0);

        DialogueAdvancePressed = keyPressed || clickPressed;
    }

    private void HandleMouseInput(float deltaTime)
    {
        // If dialogue is active AND this frame's click is used for dialogue, don't also use it as a world click
        if (m_dialogueIsActive && DialogueAdvancePressed)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, m_config.ClickMask))
            {
                // Try selection first
                NavAgent agent = hit.collider.GetComponentInParent<NavAgent>();
                if (agent != null)
                {
                    m_selectionController?.SetSingleSelection(agent);
                }
                else
                {
                    // Otherwise, move selected agents to clicked position
                    m_navigationController?.MoveSelectedAgents(hit.point);
                }
            }
        }
    }
}
