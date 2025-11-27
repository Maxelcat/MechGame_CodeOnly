#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TacticsGame.Flow;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TacticsGame.Flow.Editor
{
    public class FlowGraphEditorWindow : EditorWindow
    {
        private FlowGraphView m_graphView;
        private FlowGraph m_currentGraph;

        private IMGUIContainer m_inspectorContainer;
        private FlowNodeData m_selectedNode;
        private FlowNodeView m_selectedNodeView;
        private FlowNodeView m_previousSelectedNodeView;

        private bool m_showGraphBindings = false;
        private bool m_showDetails = false;

        private bool m_uiInitialised;

        // New: keep a reference so we can sync it when loading graphs
        private ObjectField m_graphField;
        private ToolbarToggle m_graphBindingsToggle;

        // ----- Entry points -----

        [MenuItem("Window/Tactics/Flow Graph")]
        public static void OpenMenu()
        {
            OpenAndLoad(null);
        }

        public static FlowGraphEditorWindow OpenAndLoad(FlowGraph graph)
        {
            var window = GetWindow<FlowGraphEditorWindow>();
            window.titleContent = new GUIContent("Flow Graph");

            window.InitUI();
            window.LoadGraph(graph);

            window.Show();
            return window;
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var graph = EditorUtility.InstanceIDToObject(instanceID) as FlowGraph;
            if (graph != null)
            {
                OpenAndLoad(graph);
                return true;
            }

            return false;
        }

        private void OnEnable()
        {
            InitUI();
        }

        private void InitUI()
        {
            if (m_uiInitialised)
                return;

            rootVisualElement.Clear();

            // Toolbar
            var toolbar = CreateToolbar();
            rootVisualElement.Add(toolbar);

            // Main area: graph (left) + inspector (right)
            var main = new VisualElement();
            main.style.flexGrow = 1;
            main.style.flexDirection = FlexDirection.Row;
            rootVisualElement.Add(main);

            // Graph view (left)
            m_graphView = new FlowGraphView();
            m_graphView.name = "Flow Graph View";
            m_graphView.style.flexGrow = 1;
            m_graphView.style.flexShrink = 1;
            m_graphView.style.position = Position.Relative;
            m_graphView.OnNodeSelected += OnNodeSelected;
            main.Add(m_graphView);

            // Inspector (right)
            m_inspectorContainer = new IMGUIContainer(DrawInspector);
            m_inspectorContainer.style.width = 320;
            m_inspectorContainer.style.flexShrink = 0;
            m_inspectorContainer.style.flexGrow = 0;
            m_inspectorContainer.style.position = Position.Relative;
            m_inspectorContainer.style.borderLeftWidth = 1;
            m_inspectorContainer.style.borderLeftColor = new Color(0.2f, 0.2f, 0.2f);
            main.Add(m_inspectorContainer);

            m_uiInitialised = true;
        }

        private Toolbar CreateToolbar()
        {
            var toolbar = new Toolbar();

            m_graphField = new ObjectField("Graph")
            {
                objectType = typeof(FlowGraph),
                allowSceneObjects = false
            };
            m_graphField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == evt.previousValue)
                    return;

                LoadGraph(evt.newValue as FlowGraph);
            });
            toolbar.Add(m_graphField);

            var refreshButton = new Button(() =>
            {
                if (m_graphView != null)
                    m_graphView.Populate(m_currentGraph); // no framing
            })
            { text = "Refresh" };
            toolbar.Add(refreshButton);

            var saveButton = new Button(() =>
            {
                if (m_currentGraph != null)
                {
                    EditorUtility.SetDirty(m_currentGraph);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            })
            { text = "Save" };
            toolbar.Add(saveButton);

            // Graph Bindings toggle
            m_graphBindingsToggle = new ToolbarToggle { text = "Graph Bindings" };
            m_graphBindingsToggle.value = m_showGraphBindings;
            m_graphBindingsToggle.RegisterValueChangedCallback(evt =>
            {
                m_showGraphBindings = evt.newValue;

                if (m_showGraphBindings)
                {
                    // Entering Graph Bindings mode: clear node selection
                    m_selectedNodeView = null;
                    m_selectedNode = null;
                    m_graphView?.ClearSelection();
                }

                m_inspectorContainer?.MarkDirtyRepaint();
            });
            toolbar.Add(m_graphBindingsToggle);

            // Show Details toggle
            var showDetailsToggle = new ToolbarToggle { text = "Show Details" };
            showDetailsToggle.value = m_showDetails;
            showDetailsToggle.RegisterValueChangedCallback(evt =>
            {
                m_showDetails = evt.newValue;
                m_inspectorContainer?.MarkDirtyRepaint();
            });
            toolbar.Add(showDetailsToggle);

            return toolbar;
        }

        public void LoadGraph(FlowGraph graph)
        {
            if (!m_uiInitialised)
                InitUI();

            m_currentGraph = graph;
            m_selectedNode = null;
            m_selectedNodeView = null;
            m_previousSelectedNodeView = null;

            // Keep toolbar object field in sync (for double-click open, etc.)
            if (m_graphField != null)
            {
                m_graphField.SetValueWithoutNotify(graph);
            }

            if (m_graphView != null)
                m_graphView.Populate(m_currentGraph, frameOnStart: true);

            m_inspectorContainer?.MarkDirtyRepaint();
        }

        private void OnNodeSelected(FlowNodeView view)
        {
            // If we were in Graph Bindings mode, leave it
            if (m_showGraphBindings)
            {
                m_showGraphBindings = false;
                if (m_graphBindingsToggle != null)
                    m_graphBindingsToggle.SetValueWithoutNotify(false);
            }

            m_selectedNodeView = view;
            m_selectedNode = view != null ? view.NodeData : null;

            m_inspectorContainer?.MarkDirtyRepaint();
        }


        // ----- Inspector drawing -----

        private void DrawInspector()
        {
            if (m_currentGraph == null)
            {
                GUILayout.Label("No graph selected.");
                return;
            }

            if (m_showGraphBindings)
            {
                DrawGraphBindingsInspector();
                return;
            }

            if (m_selectedNode == null)
            {
                GUILayout.Label("No node selected.");
                return;
            }

            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(m_currentGraph, "Edit Flow Node");

            GUILayout.Label("Node Inspector", EditorStyles.boldLabel);

            // Node type is informational only – prevent switching types in-place
            EditorGUILayout.LabelField("Node Type", m_selectedNode.NodeType.ToString());

            // Type-specific inspector
            var def = FlowNodeEditorRegistry.Get(m_selectedNode.NodeType);
            def?.DrawInspector(m_selectedNode);

            // Internal block (read-only) at the bottom
            if (m_showDetails)
            {
                EditorGUILayout.Space();
                GUILayout.Label("Internal", EditorStyles.miniBoldLabel);

                using (new EditorGUI.DisabledScope(true))
                {
                    // Always show Node Id
                    m_selectedNode.NodeId =
                        EditorGUILayout.TextField("Node Id", m_selectedNode.NodeId);

                    // Show NextNodeId for all types EXCEPT Sequence
                    if (m_selectedNode.NodeType != FlowNodeType.Sequence)
                    {
                        m_selectedNode.NextNodeId =
                            EditorGUILayout.TextField("Next Node Id", m_selectedNode.NextNodeId);
                    }

                    switch (m_selectedNode.NodeType)
                    {
                        case FlowNodeType.Sequence:
                            DrawSequenceInternalDetails();
                            break;

                        case FlowNodeType.Branch:
                            DrawBranchInternalDetails();
                            break;
                    }
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (m_currentGraph != null)
                    EditorUtility.SetDirty(m_currentGraph);

                m_selectedNodeView?.RefreshTitle();

                // Rebuild ports/edges as needed, but do NOT re-frame the view
                m_graphView?.Populate(m_currentGraph);
            }
        }

        private void DrawGraphBindingsInspector()
        {
            GUILayout.Label("Graph Bindings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (m_currentGraph == null)
            {
                GUILayout.Label("No graph selected.");
                return;
            }

            var so = new SerializedObject(m_currentGraph);
            so.Update();

            var actorSlotsProp = so.FindProperty("m_actorSlots");
            if (actorSlotsProp != null)
            {
                EditorGUILayout.PropertyField(actorSlotsProp, new GUIContent("Actor Slots"), true);
            }
            else
            {
                GUILayout.Label("No actor slots field (m_actorSlots) found on FlowGraph.");
            }

            if (so.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(m_currentGraph);
            }
        }

        private void DrawSequenceInternalDetails()
        {
            string[] array = m_selectedNode.SequenceNodeIds;
            GUILayout.Label("Sequence Node Ids", EditorStyles.boldLabel);

            int count = array != null ? array.Length : 0;

            EditorGUI.indentLevel++;
            for (int i = 0; i < count; i++)
            {
                EditorGUILayout.TextField(array[i]);
            }
            EditorGUI.indentLevel--;
        }

        private void DrawBranchInternalDetails()
        {
            var ids = m_selectedNode.BranchOptionTriggerIds;
            var nexts = m_selectedNode.BranchOptionNextNodeIds;

            int lenA = ids != null ? ids.Length : 0;
            int lenB = nexts != null ? nexts.Length : 0;
            int count = Mathf.Max(lenA, lenB);

            GUILayout.Label("Branch Options", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            for (int i = 0; i < count; i++)
            {
                string cond = (ids != null && i < ids.Length) ? ids[i] : "";
                string nxt = (nexts != null && i < nexts.Length) ? nexts[i] : "";

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.TextField("Condition", cond);
                EditorGUILayout.TextField("Next Node", nxt);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }
    }

    // ----- GraphView + NodeView -----

    public class FlowGraphView : GraphView
    {
        private FlowGraph m_graph;
        private readonly Dictionary<string, FlowNodeView> m_nodeViews = new();

        public Action<FlowNodeView> OnNodeSelected;

        internal bool SuppressNextContextMenu;

        private bool m_isPopulating;

        public FlowGraphView()
        {
            style.flexGrow = 1;

            this.SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new RightMousePanManipulator());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            graphViewChanged = OnGraphViewChanged;

            RegisterCallback<MouseDownEvent>(OnMouseDownInGraph);
            RegisterCallback<MouseUpEvent>(OnMouseUpInGraph);
        }

        private void OnMouseDownInGraph(MouseDownEvent evt)
        {
            // Left click only
            if (evt.button != 0)
                return;

            // If we clicked on a node, let normal selection handling do its thing
            var ve = evt.target as VisualElement;
            var nodeView = ve?.GetFirstOfType<FlowNodeView>();
            if (nodeView != null)
                return;

            // Clicked on empty background → clear selection + inspector
            ClearSelection();
            OnNodeSelected?.Invoke(null);
        }

        private void OnMouseUpInGraph(MouseUpEvent evt)
        {
            if (evt.button != 0)
                return;

            // If, after this click, there are no selected elements,
            // clear the inspector / selection state in the editor window.
            if (selection.Count == 0)
            {
                OnNodeSelected?.Invoke(null);
            }
        }

        public void Populate(FlowGraph graph, bool frameOnStart = false)
        {
            m_isPopulating = true;

            DeleteElements(graphElements.ToList());
            m_nodeViews.Clear();
            m_graph = graph;

            // Only reset pan/zoom when we explicitly ask to frame the graph
            if (frameOnStart)
            {
                UpdateViewTransform(Vector3.zero, Vector3.one);
            }

            if (graph == null || graph.Nodes == null)
            {
                m_isPopulating = false;
                return;
            }

            // Create node views
            foreach (var nodeData in graph.Nodes)
            {
                if (nodeData == null)
                    continue;

                var nodeView = new FlowNodeView(this, graph, nodeData);
                nodeView.Selected += nv => OnNodeSelected?.Invoke(nv);
                AddElement(nodeView);
                m_nodeViews[nodeData.NodeId] = nodeView;
            }

            // Create edges via definitions
            foreach (var nodeData in graph.Nodes)
            {
                if (nodeData == null)
                    continue;

                var def = FlowNodeEditorRegistry.Get(nodeData.NodeType);
                def.BuildEdges(nodeData, this, m_nodeViews);
            }

            // Optionally centre on the Start node when requested
            if (frameOnStart && graph.Nodes.Length > 0)
            {
                var startData = graph.GetStartNode();
                if (startData != null &&
                    m_nodeViews.TryGetValue(startData.NodeId, out var startView))
                {
                    this.schedule.Execute(() =>
                    {
                        Rect nodeRect = startView.GetPosition();
                        Vector2 center = nodeRect.center;
                        Vector2 viewSize = layout.size;
                        if (viewSize == Vector2.zero)
                            viewSize = new Vector2(800, 600);
                        Vector2 viewCenter = viewSize * 0.5f;

                        Vector3 pan = (Vector3)(viewCenter - center);
                        UpdateViewTransform(pan, Vector3.one);
                    }).ExecuteLater(0);
                }
            }

            m_isPopulating = false;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var result = new List<Port>();

            ports.ForEach(port =>
            {
                if (port == startPort)
                    return;
                if (port.node == startPort.node)
                    return;
                if (port.direction == startPort.direction)
                    return;

                result.Add(port);
            });

            return result;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (m_graph == null)
                return;

            // Skip menu once after a pan
            if (SuppressNextContextMenu)
            {
                SuppressNextContextMenu = false;
                return;
            }

            base.BuildContextualMenu(evt);

            var graphPosition = contentViewContainer.WorldToLocal(evt.mousePosition);

            var ve = evt.target as VisualElement;
            var nodeView = ve?.GetFirstOfType<FlowNodeView>();

            if (nodeView == null)
            {
                // Empty space → add nodes
                AppendAddNodeActions(evt.menu, graphPosition, null, -1);
            }
            else
            {
                evt.menu.AppendAction("Delete Node",
                    a => DeleteNode(nodeView));
            }
        }

        // Shared helper to build menu items
        private void BuildAddNodeMenuItems(
            Vector2 graphPosition,
            FlowNodeView fromNode,
            int fromOutputIndex,
            Action<string, FlowNodeType> addItem)
        {
            addItem("Dialogue Node", FlowNodeType.Dialogue);
            addItem("Delay Node", FlowNodeType.Delay);
            addItem("WaitForTrigger Node", FlowNodeType.WaitForTrigger);
            addItem("Objective Node", FlowNodeType.Objective);
            addItem("Branch Node", FlowNodeType.Branch);
            addItem("Sequence Node", FlowNodeType.Sequence);
            addItem("Loop Node", FlowNodeType.Loop);
        }

        // Used by right-click context menu (DropdownMenu)
        internal void AppendAddNodeActions(
            DropdownMenu menu,
            Vector2 graphPosition,
            FlowNodeView fromNode,
            int fromOutputIndex)
        {
            BuildAddNodeMenuItems(graphPosition, fromNode, fromOutputIndex,
                (label, type) =>
                {
                    menu.AppendAction(label, action =>
                    {
                        var newView = CreateNode(type, graphPosition);
                        if (fromNode != null && fromOutputIndex >= 0 &&
                            newView != null && newView.InputPort != null)
                        {
                            var outPort = fromNode.GetOutputPortForIndex(fromOutputIndex);
                            if (outPort != null)
                            {
                                var edge = outPort.ConnectTo(newView.InputPort);
                                AddElement(edge);
                            }
                        }
                    },
                    _ => DropdownMenuAction.Status.Normal);
                });
        }

        // Used by edge drag → empty space (GenericMenu)
        internal void AppendAddNodeActions(
            GenericMenu menu,
            Vector2 graphPosition,
            FlowNodeView fromNode,
            int fromOutputIndex)
        {
            BuildAddNodeMenuItems(graphPosition, fromNode, fromOutputIndex,
                (label, type) =>
                {
                    menu.AddItem(new GUIContent(label), false, () =>
                    {
                        var newView = CreateNode(type, graphPosition);
                        if (fromNode != null && fromOutputIndex >= 0 &&
                            newView != null && newView.InputPort != null)
                        {
                            var outPort = fromNode.GetOutputPortForIndex(fromOutputIndex);
                            if (outPort != null && m_graph != null)
                            {
                                var def = FlowNodeEditorRegistry.Get(fromNode.NodeData.NodeType);

                        // 🔹 Remove existing edges on this output
                        foreach (var existing in outPort.connections.ToList())
                                {
                                    var oldToView = existing.input?.node as FlowNodeView;
                                    if (oldToView != null)
                                    {
                                        def.OnEdgeDisconnected(fromNode.NodeData, fromOutputIndex, oldToView.NodeData);
                                    }
                                    RemoveElement(existing);
                                }

                        // Create the new edge
                        var edge = outPort.ConnectTo(newView.InputPort);
                                AddElement(edge);

                        // Persist the new connection
                        def.OnEdgeConnected(fromNode.NodeData, fromOutputIndex, newView.NodeData);
                                EditorUtility.SetDirty(m_graph);
                            }
                        }
                    });
                });
        }

        internal FlowNodeView CreateNode(FlowNodeType type, Vector2 graphPosition)
        {
            if (m_graph == null)
                return null;

            Undo.RecordObject(m_graph, "Add Flow Node");

            var nodes = m_graph.Nodes;
            var list = nodes != null ? nodes.ToList() : new List<FlowNodeData>();

            var data = new FlowNodeData
            {
                NodeId = Guid.NewGuid().ToString("N"),
                NodeType = type,
#if UNITY_EDITOR
                EditorPosition = graphPosition
#endif
            };

            list.Add(data);
            m_graph.Nodes = list.ToArray();

            var view = new FlowNodeView(this, m_graph, data);
            view.Selected += nv => OnNodeSelected?.Invoke(nv);
            AddElement(view);

            m_nodeViews[data.NodeId] = view;

            EditorUtility.SetDirty(m_graph);
            return view;
        }

        private void DeleteNode(FlowNodeView view)
        {
            if (m_graph == null || view == null)
                return;

            // Update the asset data
            RemoveNodeData(view);

            // Remove the visual node from the graph view
            RemoveElement(view);
        }

        private void RemoveNodeData(FlowNodeView view)
        {
            if (m_graph == null || view == null)
                return;

            Undo.RecordObject(m_graph, "Delete Flow Node");

            // Remove from array
            var nodes = m_graph.Nodes != null ? m_graph.Nodes.ToList() : new List<FlowNodeData>();
            if (nodes.Remove(view.NodeData))
            {
                m_graph.Nodes = nodes.ToArray();
            }

            // Clear references in other nodes
            if (m_graph.Nodes != null)
            {
                foreach (var n in m_graph.Nodes)
                {
                    if (n == null) continue;

                    if (n.NextNodeId == view.NodeData.NodeId)
                        n.NextNodeId = string.Empty;

                    if (n.SequenceNodeIds != null)
                    {
                        for (int i = 0; i < n.SequenceNodeIds.Length; i++)
                        {
                            if (n.SequenceNodeIds[i] == view.NodeData.NodeId)
                                n.SequenceNodeIds[i] = string.Empty;
                        }
                    }

                    if (n.BranchOptionNextNodeIds != null)
                    {
                        for (int i = 0; i < n.BranchOptionNextNodeIds.Length; i++)
                        {
                            if (n.BranchOptionNextNodeIds[i] == view.NodeData.NodeId)
                                n.BranchOptionNextNodeIds[i] = string.Empty;
                        }
                    }

                    if (!string.IsNullOrEmpty(n.LoopBodyNodeId) &&
                        n.LoopBodyNodeId == view.NodeData.NodeId)
                    {
                        n.LoopBodyNodeId = string.Empty;
                    }
                }
            }

            m_nodeViews.Remove(view.NodeData.NodeId);
            EditorUtility.SetDirty(m_graph);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (m_graph == null)
                return change;

            if (m_isPopulating)
                return change;

            // New edges → update data via definitions
            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    var fromView = edge.output.node as FlowNodeView;
                    var toView = edge.input.node as FlowNodeView;

                    if (fromView == null || toView == null)
                        continue;

                    var outPort = edge.output as Port;
                    var def = FlowNodeEditorRegistry.Get(fromView.NodeData.NodeType);
                    int index = fromView.GetOutputPortIndex(edge.output);

                    // 🔹 Enforce single connection on this output:
                    // remove all other edges from this port and clear their data links
                    if (outPort != null)
                    {
                        foreach (var existing in outPort.connections.ToList())
                        {
                            if (existing == edge)
                                continue;

                            var oldToView = existing.input?.node as FlowNodeView;
                            if (oldToView != null)
                            {
                                def.OnEdgeDisconnected(fromView.NodeData, index, oldToView.NodeData);
                            }

                            RemoveElement(existing);
                        }
                    }

                    // Now record the new connection in data
                    def.OnEdgeConnected(fromView.NodeData, index, toView.NodeData);

                    EditorUtility.SetDirty(m_graph);
                }
            }

            // Removed elements → edges removed → clear data
            if (change.elementsToRemove != null)
            {
                foreach (var element in change.elementsToRemove)
                {
                    if (element is Edge edge)
                    {
                        var fromView = edge.output.node as FlowNodeView;
                        var toView = edge.input.node as FlowNodeView;

                        if (fromView == null || toView == null)
                            continue;

                        var def = FlowNodeEditorRegistry.Get(fromView.NodeData.NodeType);
                        int index = fromView.GetOutputPortIndex(edge.output);
                        def.OnEdgeDisconnected(fromView.NodeData, index, toView.NodeData);

                        EditorUtility.SetDirty(m_graph);
                    }
                    else if (element is FlowNodeView nodeView)
                    {
                        // Node deleted via Delete key / GraphView menu → remove from asset too.
                        RemoveNodeData(nodeView);
                    }
                }
            }

            return change;
        }
    }

    // ----- Edge connector listener for "drag to empty space" -----

    public class FlowEdgeConnectorListener : IEdgeConnectorListener
    {
        private readonly FlowGraphView m_view;

        public FlowEdgeConnectorListener(FlowGraphView view)
        {
            m_view = view;
        }

        public void OnDrop(GraphView graphView, Edge edge)
        {
            // Normal connection between two ports
            if (edge?.input != null && edge.output != null)
            {
                graphView.AddElement(edge);
            }
        }

        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            if (m_view == null || edge == null || edge.output == null)
                return;

            var fromView = edge.output.node as FlowNodeView;
            if (fromView == null)
                return;

            var outPort = edge.output as Port;
            int outputIndex = fromView.GetOutputPortIndex(outPort);

            // Convert mouse position to graph-local coordinates
            Vector2 graphPos = m_view.contentViewContainer.WorldToLocal(position);

            var menu = new GenericMenu();
            m_view.AppendAddNodeActions(menu, graphPos, fromView, outputIndex);
            menu.ShowAsContext();
        }
    }

    // ----- Right-mouse pan manipulator -----

    public class RightMousePanManipulator : MouseManipulator
    {
        private bool m_active;
        private Vector2 m_lastMousePosition;
        private const float k_DragThreshold = 3f;

        public RightMousePanManipulator()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (!CanStartManipulation(evt))
                return;

            m_active = true;
            m_lastMousePosition = evt.mousePosition;
            target.CaptureMouse();
            evt.StopPropagation();
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (!m_active)
                return;

            var graphView = target as FlowGraphView;
            if (graphView == null)
                return;

            Vector2 delta = evt.mousePosition - m_lastMousePosition;

            if (delta.sqrMagnitude > k_DragThreshold * k_DragThreshold)
            {
                graphView.SuppressNextContextMenu = true;
            }

            graphView.viewTransform.position += (Vector3)delta;
            m_lastMousePosition = evt.mousePosition;
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (!m_active || !CanStopManipulation(evt))
                return;

            m_active = false;
            target.ReleaseMouse();
            evt.StopPropagation();
        }
    }

    // ----- Node view -----

    public class FlowNodeView : Node
    {
        private readonly FlowGraphView m_view;
        private readonly FlowGraph m_graph;
        private readonly FlowNodeData m_node;
        private readonly FlowNodeEditorDefinition m_def;

        private readonly List<Port> m_outputPorts = new();

        public Port InputPort { get; private set; }

        public FlowNodeData NodeData => m_node;

        public IReadOnlyList<Port> OutputPorts => m_outputPorts;
        public Port SingleOutputPort => m_outputPorts.Count > 0 ? m_outputPorts[0] : null;

        public Action<FlowNodeView> Selected;

        public FlowNodeView(FlowGraphView view, FlowGraph graph, FlowNodeData node)
        {
            m_view = view;
            m_graph = graph;
            m_node = node;
            m_def = FlowNodeEditorRegistry.Get(node.NodeType);

            RefreshTitle();
            CreatePorts();
            ApplyStyle();

            RefreshExpandedState();
            RefreshPorts();

#if UNITY_EDITOR
            Vector2 pos = node.EditorPosition;
            if (pos == Vector2.zero)
                pos = new Vector2(UnityEngine.Random.Range(0, 400), UnityEngine.Random.Range(0, 400));

            SetPosition(new Rect(pos, new Vector2(200, 120)));
#endif
        }

        private void CreatePorts()
        {
            // Input
            if (m_node.NodeType != FlowNodeType.Start)
            {
                InputPort = InstantiatePort(
                    Orientation.Horizontal,
                    Direction.Input,
                    Port.Capacity.Single,
                    typeof(bool));
                InputPort.portName = "";
                inputContainer.Add(InputPort);
            }

            // Outputs (via definition)
            m_outputPorts.Clear();
            outputContainer.Clear();

            int outputCount = m_def != null ? m_def.GetOutputCount(m_node) : 1;
            outputCount = Mathf.Max(1, outputCount);

            for (int i = 0; i < outputCount; i++)
            {
                var port = InstantiatePort(
                    Orientation.Horizontal,
                    Direction.Output,
                    Port.Capacity.Single,
                    typeof(bool));
                port.portName = outputCount > 1 ? (i + 1).ToString() : "";

                // Add edge connector so dragging to empty space opens add-node menu
                var connector = new EdgeConnector<Edge>(new FlowEdgeConnectorListener(m_view));
                port.AddManipulator(connector);

                m_outputPorts.Add(port);
                outputContainer.Add(port);
            }
        }

        private void ApplyStyle()
        {
            // Simple per-type colour
            Color bg = new Color(0.2f, 0.2f, 0.2f);

            switch (m_node.NodeType)
            {
                case FlowNodeType.Start: bg = new Color(0.2f, 0.45f, 0.2f); break;
                case FlowNodeType.Dialogue: bg = new Color(0.25f, 0.35f, 0.6f); break;
                case FlowNodeType.Delay: bg = new Color(0.4f, 0.3f, 0.2f); break;
                case FlowNodeType.WaitForTrigger: bg = new Color(0.4f, 0.4f, 0.15f); break;
                case FlowNodeType.Objective: bg = new Color(0.35f, 0.2f, 0.45f); break;
                case FlowNodeType.SubFlow: bg = new Color(0.25f, 0.45f, 0.45f); break;
                case FlowNodeType.Branch: bg = new Color(0.45f, 0.3f, 0.1f); break;
                case FlowNodeType.Sequence: bg = new Color(0.1f, 0.4f, 0.4f); break;
                case FlowNodeType.Loop: bg = new Color(0.4f, 0.1f, 0.4f); break;
            }

            titleContainer.style.backgroundColor = bg;
        }

        public void RefreshTitle()
        {
            title = m_node.NodeType.ToString();
        }

        public Port GetOutputPortForIndex(int index)
        {
            if (index < 0 || index >= m_outputPorts.Count)
                return null;
            return m_outputPorts[index];
        }

        public int GetOutputPortIndex(Port port)
        {
            return m_outputPorts.IndexOf(port);
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
#if UNITY_EDITOR
            Undo.RecordObject(m_graph, "Move Flow Node");
            m_node.EditorPosition = newPos.position;
            EditorUtility.SetDirty(m_graph);
#endif
        }

        public override void OnSelected()
        {
            base.OnSelected();
            Selected?.Invoke(this);
        }
    }
}
#endif
