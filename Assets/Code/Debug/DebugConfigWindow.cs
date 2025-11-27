using UnityEngine;
using TacticsGame.Flow; // <-- import the namespace from FlowGraph / FlowController

public class DebugOverlay : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private KeyCode m_toggleKey = KeyCode.F1;
    [SerializeField] private bool m_startVisible = true;

    private bool m_isVisible;
    private Rect m_windowRect = new Rect(20, 20, 500, 600);

    private enum DebugTab
    {
        Configs,
        Dialogue,
        Flow
    }

    private DebugTab m_currentTab;

    // Config references
    private InputConfig m_inputConfig;
    private TacticalCameraConfig m_cameraConfig;
    private UIConfig m_uiConfig;
    private GameplayConfig m_gameplayConfig;

    // Config foldouts
    private bool m_showInputConfig = true;
    private bool m_showCameraConfig = true;
    private bool m_showUIConfig = true;
    private bool m_showGameplayConfig = true;

    // Dialogue debug
    private DialogueSequence[] m_dialogueSequences;
    private Vector2 m_dialogueScroll;

    // Flow debug
    private FlowGraph[] m_flowGraphs;
    private Vector2 m_flowScroll;
    private string m_flowTriggerText = "";

    private void Awake()
    {
        m_isVisible = m_startVisible;

        Debug.Assert(App.Instance != null, "DebugOverlay: App.Instance is null");
        Debug.Assert(App.Instance.ResourceLoader != null, "DebugOverlay: ResourceLoader is null");

        LoadConfigs();
        LoadDialogueSequences();
        LoadFlowGraphs();
    }

    private void Update()
    {
        if (Input.GetKeyDown(m_toggleKey))
        {
            m_isVisible = !m_isVisible;
        }
    }

    private void OnGUI()
    {
        if (!m_isVisible)
            return;

        m_windowRect = GUILayout.Window(
            GetInstanceID(),
            m_windowRect,
            DrawDebugWindow,
            "Debug Overlay");
    }

    private void DrawDebugWindow(int id)
    {
        if (App.Instance == null || App.Instance.GameController == null)
        {
            GUILayout.Label("App or GameController not initialised.");
            GUI.DragWindow();
            return;
        }

        // Tabs
        GUILayout.BeginHorizontal();
        var tabNames = new[] { "Configs", "Dialogue", "Flow" };
        int tabIndex = (int)m_currentTab;
        tabIndex = GUILayout.Toolbar(tabIndex, tabNames);
        m_currentTab = (DebugTab)tabIndex;
        GUILayout.EndHorizontal();

        GUILayout.Space(5f);

        switch (m_currentTab)
        {
            case DebugTab.Configs:
                DrawConfigsTab();
                break;
            case DebugTab.Dialogue:
                DrawDialogueTab();
                break;
            case DebugTab.Flow:
                DrawFlowTab();
                break;
        }

        GUI.DragWindow();
    }

    // ---------------------------------------------------------------------
    // CONFIGS TAB
    // ---------------------------------------------------------------------

    private void LoadConfigs()
    {
        var loader = App.Instance.ResourceLoader;

        m_inputConfig = loader.LoadConfig<InputConfig>(ConfigId.Input);
        m_cameraConfig = loader.LoadConfig<TacticalCameraConfig>(ConfigId.TacticalCamera);
        m_uiConfig = loader.LoadConfig<UIConfig>(ConfigId.UI);
        m_gameplayConfig = loader.LoadConfig<GameplayConfig>(ConfigId.Gameplay);
    }

    private void DrawConfigsTab()
    {
        GUILayout.BeginVertical();

        if (m_inputConfig == null || m_cameraConfig == null || m_uiConfig == null || m_gameplayConfig == null)
        {
            GUILayout.Label("Configs missing or not loaded.");
            if (GUILayout.Button("Reload Configs"))
            {
                LoadConfigs();
            }
            GUILayout.EndVertical();
            return;
        }

        DrawConfigFoldout(ref m_showInputConfig, "Input Config", DrawInputConfigSection);
        DrawConfigFoldout(ref m_showCameraConfig, "Tactical Camera Config", DrawCameraConfigSection);
        DrawConfigFoldout(ref m_showUIConfig, "UI Config", DrawUIConfigSection);
        DrawConfigFoldout(ref m_showGameplayConfig, "Gameplay Config", DrawGameplayConfigSection);

        GUILayout.EndVertical();
    }

    private void DrawConfigFoldout(ref bool open, string title, System.Action drawContent)
    {
        GUILayout.Space(4f);
        string label = (open ? "▼ " : "▶ ") + title;
        open = GUILayout.Toggle(open, label, "Button");
        if (open)
        {
            GUILayout.BeginVertical("box");
            drawContent?.Invoke();
            GUILayout.EndVertical();
        }
    }

    private void DrawInputConfigSection()
    {
        GUILayout.Label("Dialogue:", GUILayout.ExpandWidth(false));
        GUILayout.BeginHorizontal();
        m_inputConfig.DialogueAdvanceOnLeftClick =
            GUILayout.Toggle(m_inputConfig.DialogueAdvanceOnLeftClick, "Advance with Left Click");
        GUILayout.EndHorizontal();

        GUILayout.Label($"ClickMask: {m_inputConfig.ClickMask.value}", GUILayout.ExpandWidth(false));
        GUILayout.Label("(LayerMask editing is inspector-only for now.)", GUILayout.ExpandWidth(false));
    }

    private void DrawCameraConfigSection()
    {
        GUILayout.Label($"Move Speed: {m_cameraConfig.MoveSpeed:F2}");
        m_cameraConfig.MoveSpeed = GUILayout.HorizontalSlider(m_cameraConfig.MoveSpeed, 1f, 80f);

        GUILayout.Label($"Edge Thickness: {m_cameraConfig.EdgeThickness:F1}");
        m_cameraConfig.EdgeThickness = GUILayout.HorizontalSlider(m_cameraConfig.EdgeThickness, 0f, 100f);

        GUILayout.Space(4f);
        m_cameraConfig.EnableObstructionFade =
            GUILayout.Toggle(m_cameraConfig.EnableObstructionFade, "Enable Obstruction Fade");

        GUILayout.Label($"Radial Fade Radius: {m_cameraConfig.RadialFadeRadius:F3}");
        m_cameraConfig.RadialFadeRadius =
            GUILayout.HorizontalSlider(m_cameraConfig.RadialFadeRadius, 0f, 0.5f);

        GUILayout.Label($"Radial Fade Softness: {m_cameraConfig.RadialFadeSoftness:F3}");
        m_cameraConfig.RadialFadeSoftness =
            GUILayout.HorizontalSlider(m_cameraConfig.RadialFadeSoftness, 0f, 0.5f);

        GUILayout.Space(4f);
        m_cameraConfig.UseBounds =
            GUILayout.Toggle(m_cameraConfig.UseBounds, "Use Bounds");

        if (m_cameraConfig.UseBounds)
        {
            GUILayout.Label($"BoundsX: {m_cameraConfig.BoundsX.x:F1} to {m_cameraConfig.BoundsX.y:F1}");
            GUILayout.Label($"BoundsZ: {m_cameraConfig.BoundsZ.x:F1} to {m_cameraConfig.BoundsZ.y:F1}");
            GUILayout.Label("(Bounds editing currently inspector-only.)");
        }
    }

    private void DrawUIConfigSection()
    {
        GUILayout.Label($"Dialogue Text Speed (chars/sec): {m_uiConfig.DialogueTextSpeed:F1}");
        m_uiConfig.DialogueTextSpeed = GUILayout.HorizontalSlider(m_uiConfig.DialogueTextSpeed, 0f, 60f);

        GUILayout.Label($"Dialogue Auto-Advance Delay (s): {m_uiConfig.DialogueAutoAdvanceDelay:F1}");
        m_uiConfig.DialogueAutoAdvanceDelay =
            GUILayout.HorizontalSlider(m_uiConfig.DialogueAutoAdvanceDelay, 0f, 10f);

        GUILayout.Space(4f);
        m_uiConfig.DialogueAllowManualAdvance =
            GUILayout.Toggle(m_uiConfig.DialogueAllowManualAdvance, "Allow Manual Advance");

        GUILayout.Space(4f);
        GUILayout.Label("Dialogue View Prefab:", GUILayout.ExpandWidth(false));
        GUILayout.Label(
            m_uiConfig.DialogueViewPrefab != null
                ? m_uiConfig.DialogueViewPrefab.name
                : "(None)",
            GUILayout.ExpandWidth(false));
    }

    private void DrawGameplayConfigSection()
    {
        GUILayout.Label("Selection & Movement:");

        GUILayout.Label("Selection Ring Prefab:", GUILayout.ExpandWidth(false));
        GUILayout.Label(
            m_gameplayConfig.SelectionRingPrefab != null
                ? m_gameplayConfig.SelectionRingPrefab.name
                : "(None)",
            GUILayout.ExpandWidth(false));

        GUILayout.Space(2f);

        GUILayout.Label("Destination Marker Prefab:", GUILayout.ExpandWidth(false));
        GUILayout.Label(
            m_gameplayConfig.DestinationMarkerPrefab != null
                ? m_gameplayConfig.DestinationMarkerPrefab.name
                : "(None)",
            GUILayout.ExpandWidth(false));

        GUILayout.Space(4f);
        GUILayout.Label($"Destination Marker Lifetime: {m_gameplayConfig.DestinationMarkerLifetime:F1}s");
        m_gameplayConfig.DestinationMarkerLifetime =
            GUILayout.HorizontalSlider(m_gameplayConfig.DestinationMarkerLifetime, 0f, 10f);
    }

    // ---------------------------------------------------------------------
    // DIALOGUE TAB
    // ---------------------------------------------------------------------

    private void LoadDialogueSequences()
    {
        m_dialogueSequences = Resources.LoadAll<DialogueSequence>("DialogueSequences");
    }

    private void DrawDialogueTab()
    {
        var controller = App.Instance.GameController.DialogueController;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Sequences", GUILayout.Width(150f)))
        {
            LoadDialogueSequences();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        if (m_dialogueSequences == null || m_dialogueSequences.Length == 0)
        {
            GUILayout.Label("No DialogueSequence assets found in Resources/Dialogue.");
            return;
        }

        m_dialogueScroll = GUILayout.BeginScrollView(m_dialogueScroll);

        foreach (var seq in m_dialogueSequences)
        {
            if (seq == null)
                continue;

            GUILayout.BeginHorizontal();

            GUILayout.Label(seq.name, GUILayout.ExpandWidth(true));

            GUI.enabled = controller != null;

            if (GUILayout.Button("Play", GUILayout.Width(80f)))
            {
                controller.PlaySequence(seq);
            }

            GUI.enabled = true;

            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
    }

    // ---------------------------------------------------------------------
    // FLOW TAB
    // ---------------------------------------------------------------------

    private void LoadFlowGraphs()
    {
        m_flowGraphs = Resources.LoadAll<FlowGraph>("FlowGraphs");
    }

    private void DrawFlowTab()
    {
        var flow = App.Instance.GameController.FlowController;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Flows", GUILayout.Width(120f)))
        {
            LoadFlowGraphs();
        }

        // Try to call StopFlow() if it exists
        if (flow != null && GUILayout.Button("Stop Current", GUILayout.Width(120f)))
        {
            var stopMethod = flow.GetType().GetMethod(
                "StopFlow",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);

            if (stopMethod != null)
            {
                stopMethod.Invoke(flow, null);
            }
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        // Current flow status (best-effort via reflection so we don't depend on exact API)
        GUILayout.Space(4f);
        GUILayout.BeginVertical("box");
        GUILayout.Label("Current Flow Status:");

        // Detailed current node info (if we can see the FlowController / runner)
        var flowController = flow as FlowController;
        if (flowController != null && flowController.Runner != null)
        {
            var runner = flowController.Runner;
            var node = runner.CurrentNode;

            GUILayout.BeginVertical("box");
            GUILayout.Label("Current Node:");

            if (node == null)
            {
                GUILayout.Label("(none – runner finished)");
            }
            else
            {
                GUILayout.Label($"Id: {node.NodeId}");
                GUILayout.Label($"Type: {node.NodeType}");
                GUILayout.Label(node.GetDebugInfo());
            }

            GUILayout.EndVertical();
        }


        GUILayout.EndVertical();

        GUILayout.Space(4f);

        // Flow list
        if (m_flowGraphs == null || m_flowGraphs.Length == 0)
        {
            GUILayout.Label("No FlowGraph assets found in Resources/Flow.");
        }
        else
        {
            GUILayout.Label("Available Flows:");
            m_flowScroll = GUILayout.BeginScrollView(m_flowScroll, GUILayout.Height(250f));

            foreach (var graph in m_flowGraphs)
            {
                if (graph == null)
                    continue;

                GUILayout.BeginHorizontal();

                GUILayout.Label(graph.name, GUILayout.ExpandWidth(true));

                GUI.enabled = flow != null;

                if (GUILayout.Button("Start", GUILayout.Width(80f)))
                {
                    if (flow != null)
                    {
                        var startMethod = flow.GetType().GetMethod(
                            "StartFlow",
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.NonPublic,
                            null,
                            new System.Type[] { typeof(FlowGraph) },
                            null);

                        if (startMethod != null)
                        {
                            startMethod.Invoke(flow, new object[] { graph });
                        }
                    }
                }

                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        GUILayout.Space(4f);

        // Manual trigger (best-effort)
        GUILayout.BeginVertical("box");
        GUILayout.Label("Manual Trigger:");

        GUILayout.BeginHorizontal();
        GUILayout.Label("Trigger Id:", GUILayout.Width(70f));
        m_flowTriggerText = GUILayout.TextField(m_flowTriggerText);
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Raise Trigger") &&
            flow != null &&
            !string.IsNullOrEmpty(m_flowTriggerText))
        {
            var triggerMethod = flow.GetType().GetMethod(
                "RaiseTrigger",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic,
                null,
                new System.Type[] { typeof(string) },
                null);

            if (triggerMethod != null)
            {
                triggerMethod.Invoke(flow, new object[] { m_flowTriggerText });
            }
        }

        GUILayout.EndVertical();
    }
}
