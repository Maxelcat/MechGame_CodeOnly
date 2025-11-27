using System;
using System.Collections.Generic;
using UnityEngine;

namespace TacticsGame.Flow
{
    // ---- World bridge (unchanged) ----

    public interface IFlowWorld
    {
        // Dialogue
        void PlayDialogue(DialogueSequence sequence);
        bool IsDialoguePlaying(DialogueSequence sequence);

        // Objectives
        void ActivateObjective(string objectiveId);
        void CompleteObjective(string objectiveId);

        // Generic condition lookup for Branch nodes
        bool IsConditionTrue(string conditionId);
    }

    // ---- Node & objective enums (unchanged) ----

    public enum FlowNodeType
    {
        Start,
        Dialogue,
        Delay,
        WaitForTrigger,
        Objective,
        SubFlow,
        Branch,
        Sequence,
        Loop
        // GameplayCommand etc will come later
    }

    public enum FlowObjectiveAction
    {
        Activate,
        Complete
    }

    // ---------- NEW: actor slot definition ----------

    [Serializable]
    public class FlowActorSlot
    {
        [Tooltip("Unique id for this actor within the flow (e.g. Door_A, Wave1_Spawner).")]
        public string Id;

        [Tooltip("Default prefab to spawn if nothing in the scenario overrides this slot.")]
        public GameObject DefaultPrefab;

        [Tooltip("Default local position under the scenario root if spawned.")]
        public Vector3 DefaultLocalPosition;

        [Tooltip("Default local rotation under the scenario root if spawned.")]
        public Vector3 DefaultLocalEuler;

        [Tooltip("Spawn this actor automatically when the flow starts.")]
        public bool SpawnOnStart = true;
    }

    // ---- Node data (unchanged for now) ----

    [Serializable]
    public class FlowNodeData
    {
        [Tooltip("Unique ID for this node within the graph.")]
        public string NodeId = "Node";

        [Tooltip("Type of this node.")]
        public FlowNodeType NodeType = FlowNodeType.Dialogue;

        [Tooltip("Next node to go to when this node finishes (for simple linear nodes).")]
        public string NextNodeId;

        // Dialogue
        [Header("Dialogue")]
        public DialogueSequence Dialogue;
        public bool WaitForDialogueComplete = true;

        // Delay
        [Header("Delay")]
        public float DelaySeconds = 1f;

        // WaitForTrigger
        [Header("Trigger")]
        public string TriggerId;

        // Objective
        [Header("Objective")]
        public string ObjectiveId;
        public FlowObjectiveAction ObjectiveAction;

        // SubFlow
        [Header("Sub Flow")]
        public FlowGraph SubFlow;

        // Branch
        [Header("Branch")]
        public string[] BranchOptionTriggerIds;   // treated as condition IDs
        public string[] BranchOptionNextNodeIds; // same index as conditionIds

        // Sequence
        [Header("Sequence")]
        public string[] SequenceNodeIds;

        // Loop
        [Header("Loop")]
        public string LoopBodyNodeId;
        public int LoopCount = 0; // 0 = infinite / until break

#if UNITY_EDITOR
        // Editor-only layout data for the graph view
        public Vector2 EditorPosition;
#endif
    }

    // ---- Runtime runner (unchanged) ----

    public sealed class FlowGraphRunner
    {
        private readonly FlowGraph m_graph;
        private readonly Dictionary<string, FlowNode> m_nodes = new();
        private FlowNode m_current;
        public FlowNode CurrentNode => m_current;

        public IFlowWorld World { get; }  // interface into your game world

        public bool IsFinished => m_current == null;

        private readonly Stack<FlowNode> m_returnStack = new();

        public FlowGraphRunner(FlowGraph graph, IFlowWorld world)
        {
            m_graph = graph;
            World = world;

            BuildNodes();
        }

        private void BuildNodes()
        {
            if (m_graph == null || m_graph.Nodes == null)
                return;

            foreach (var data in m_graph.Nodes)
            {
                if (data == null || string.IsNullOrEmpty(data.NodeId))
                    continue;

                m_nodes[data.NodeId] = CreateNode(data);
            }

            var startData = m_graph.GetStartNode();
            if (startData != null && m_nodes.TryGetValue(startData.NodeId, out var startNode))
            {
                m_current = startNode;
                m_current.OnEnter();
            }
        }

        private FlowNode CreateNode(FlowNodeData data)
        {
            return data.NodeType switch
            {
                FlowNodeType.Start => new StartFlowNode(this, data),
                FlowNodeType.Dialogue => new DialogueFlowNode(this, data),
                FlowNodeType.Delay => new DelayFlowNode(this, data),
                FlowNodeType.WaitForTrigger => new WaitForTriggerFlowNode(this, data),
                FlowNodeType.Objective => new ObjectiveFlowNode(this, data),
                FlowNodeType.SubFlow => new SubFlowNode(this, data),
                FlowNodeType.Branch => new BranchFlowNode(this, data),
                FlowNodeType.Sequence => new SequenceFlowNode(this, data),
                FlowNodeType.Loop => new LoopFlowNode(this, data),
                _ => new DelayFlowNode(this, data),
            };
        }

        internal void PushReturnNode(FlowNode node)
        {
            if (node != null)
                m_returnStack.Push(node);
        }

        private FlowNode PopReturnNode()
        {
            while (m_returnStack.Count > 0)
            {
                var n = m_returnStack.Pop();
                if (n != null)
                    return n;
            }
            return null;
        }

        internal void ClearReturnNodesFor(FlowNode node)
        {
            if (m_returnStack.Count == 0 || node == null)
                return;

            var temp = new Stack<FlowNode>();

            // Remove all occurrences of this node from the stack.
            while (m_returnStack.Count > 0)
            {
                var n = m_returnStack.Pop();
                if (!ReferenceEquals(n, node))
                    temp.Push(n);
            }

            // Restore the others in the original order.
            while (temp.Count > 0)
            {
                m_returnStack.Push(temp.Pop());
            }
        }

        public void Tick(float deltaTime)
        {
            if (m_current == null)
                return;

            if (!m_current.Tick(deltaTime))
                return;

            // finished → move to next
            m_current.OnExit();

            var nextId = m_current.NextNodeId;

            // If there is no explicit next but we have a return node,
            // treat this as “end of current sub-chain, go back to caller”.
            if (string.IsNullOrEmpty(nextId))
            {
                var returnNode = PopReturnNode();
                if (returnNode != null)
                {
                    m_current = returnNode;
                    m_current.OnEnter();
                    return;
                }
            }

            if (!string.IsNullOrEmpty(nextId) &&
                m_nodes.TryGetValue(nextId, out var next))
            {
                m_current = next;
                m_current.OnEnter();
            }
            else
            {
                m_current = null;
            }
        }

        // Trigger plumbing for WaitForTriggerFlowNode
        private readonly Dictionary<string, List<WaitForTriggerFlowNode>> m_waitingTriggers = new();

        internal void RegisterTriggerWait(WaitForTriggerFlowNode node, string triggerId)
        {
            if (!m_waitingTriggers.TryGetValue(triggerId, out var list))
            {
                list = new List<WaitForTriggerFlowNode>();
                m_waitingTriggers[triggerId] = list;
            }
            list.Add(node);
        }

        internal void UnregisterTriggerWait(WaitForTriggerFlowNode node, string triggerId)
        {
            if (m_waitingTriggers.TryGetValue(triggerId, out var list))
                list.Remove(node);
        }

        public void SignalTrigger(string triggerId)
        {
            if (!m_waitingTriggers.TryGetValue(triggerId, out var list))
                return;

            foreach (var node in list)
                node.Signal();
        }
    }

    // ---- Graph asset (NOW with actor slots) ----

    [CreateAssetMenu(menuName = "Flow/Graph", fileName = "FlowGraph")]
    public class FlowGraph : ScriptableObject
    {
        [SerializeField]
        private string m_id;

        [SerializeField]
        private FlowNodeData[] m_nodes;

        // ---------- NEW: actor slots on the graph ----------

        [Header("Actors")]
        [SerializeField]
        private FlowActorSlot[] m_actorSlots;

        /// <summary>
        /// Actor slots defined for this flow graph (what actors the scenario can bind/spawn).
        /// Runtime systems will use this later to spawn / bind objects.
        /// </summary>
        public FlowActorSlot[] ActorSlots => m_actorSlots;

        // ---------------------------------------------------

        public FlowNodeData[] Nodes
        {
            get => m_nodes;
#if UNITY_EDITOR
            set => m_nodes = value;
#endif
        }

        public string Id
        {
            get => m_id;
            set => m_id = value;
        }

        public FlowNodeData GetStartNode()
        {
            if (m_nodes == null || m_nodes.Length == 0)
                return null;

            return m_nodes[0];
        }

        public FlowNodeData GetNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || m_nodes == null)
                return null;

            for (int i = 0; i < m_nodes.Length; i++)
            {
                var n = m_nodes[i];
                if (n != null && n.NodeId == nodeId)
                    return n;
            }

            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure there is always a Start node in slot 0
            if (m_nodes == null || m_nodes.Length == 0)
            {
                m_nodes = new FlowNodeData[1];
                m_nodes[0] = new FlowNodeData();
            }

            if (m_nodes[0] == null)
                m_nodes[0] = new FlowNodeData();

            var start = m_nodes[0];
            start.NodeId = "Start";
            start.NodeType = FlowNodeType.Start;
        }
#endif
    }
}
