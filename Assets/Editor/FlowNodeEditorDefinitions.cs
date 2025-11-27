#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TacticsGame.Flow;

namespace TacticsGame.Flow.Editor
{
    /// <summary>
    /// Editor-only definition for how a node type behaves in the graph editor:
    /// - How many output ports
    /// - How edges map into FlowNodeData
    /// - Which fields to show in the inspector
    /// </summary>
    public abstract class FlowNodeEditorDefinition
    {
        public abstract FlowNodeType NodeType { get; }

        /// <summary>How many output ports should this node have?</summary>
        public virtual int GetOutputCount(FlowNodeData data) => 1;

        /// <summary>Create edges for this node based on its data (NextNodeId / arrays).</summary>
        public virtual void BuildEdges(
            FlowNodeData data,
            FlowGraphView view,
            Dictionary<string, FlowNodeView> nodeViews)
        {
            if (data == null || nodeViews == null)
                return;

            if (string.IsNullOrEmpty(data.NextNodeId))
                return;

            if (!nodeViews.TryGetValue(data.NodeId, out var fromView))
                return;
            if (!nodeViews.TryGetValue(data.NextNodeId, out var toView))
                return;

            var outPort = fromView.SingleOutputPort;
            var inPort = toView.InputPort;
            if (outPort == null || inPort == null)
                return;

            var edge = outPort.ConnectTo(inPort);
            view.AddElement(edge);
        }

        /// <summary>Called when an edge is created from one of this node's outputs.</summary>
        public virtual void OnEdgeConnected(FlowNodeData from, int outputIndex, FlowNodeData to)
        {
            if (from == null || to == null)
                return;

            from.NextNodeId = to.NodeId;
        }

        /// <summary>Called when an edge is removed from one of this node's outputs.</summary>
        public virtual void OnEdgeDisconnected(FlowNodeData from, int outputIndex, FlowNodeData to)
        {
            if (from == null || to == null)
                return;

            if (from.NextNodeId == to.NodeId)
                from.NextNodeId = string.Empty;
        }

        /// <summary>Draw type-specific inspector UI (gameplay-facing fields only).</summary>
        public virtual void DrawInspector(FlowNodeData data)
        {
            // Default: no extra fields.
        }
    }

    // ---------- Default fallback ----------

    public sealed class DefaultFlowNodeEditorDefinition : FlowNodeEditorDefinition
    {
        public override FlowNodeType NodeType => 0; // unused

        public override void DrawInspector(FlowNodeData data)
        {
            EditorGUILayout.HelpBox("No editor definition for this node type.", MessageType.Info);
        }
    }

    // ---------- Start ----------

    public sealed class StartFlowNodeEditorDefinition : FlowNodeEditorDefinition
    {
        public override FlowNodeType NodeType => FlowNodeType.Start;

        public override int GetOutputCount(FlowNodeData data) => 1;

        public override void DrawInspector(FlowNodeData data)
        {
            EditorGUILayout.HelpBox("Start node. Entry point to the graph.", MessageType.Info);
        }
    }

    // ---------- Dialogue ----------

    public sealed class DialogueFlowNodeEditorDefinition : FlowNodeEditorDefinition
    {
        public override FlowNodeType NodeType => FlowNodeType.Dialogue;

        public override void DrawInspector(FlowNodeData data)
        {
            data.Dialogue = (DialogueSequence)EditorGUILayout.ObjectField(
                "Dialogue",
                data.Dialogue,
                typeof(DialogueSequence),
                false);

            data.WaitForDialogueComplete = EditorGUILayout.Toggle(
                "Wait For Complete",
                data.WaitForDialogueComplete);
        }
    }

    // ---------- Delay ----------

    public sealed class DelayFlowNodeEditorDefinition : FlowNodeEditorDefinition
    {
        public override FlowNodeType NodeType => FlowNodeType.Delay;

        public override void DrawInspector(FlowNodeData data)
        {
            data.DelaySeconds = EditorGUILayout.FloatField(
                "Delay Seconds",
                data.DelaySeconds);
        }
    }

    // ---------- WaitForTrigger ----------

    public sealed class WaitForTriggerFlowNodeEditorDefinition : FlowNodeEditorDefinition
    {
        public override FlowNodeType NodeType => FlowNodeType.WaitForTrigger;

        public override void DrawInspector(FlowNodeData data)
        {
            data.TriggerId = EditorGUILayout.TextField("Trigger Id", data.TriggerId);
        }
    }

    // ---------- Objective ----------

    public sealed class ObjectiveFlowNodeEditorDefinition : FlowNodeEditorDefinition
    {
        public override FlowNodeType NodeType => FlowNodeType.Objective;

        public override void DrawInspector(FlowNodeData data)
        {
            data.ObjectiveId = EditorGUILayout.TextField("Objective Id", data.ObjectiveId);
            data.ObjectiveAction = (FlowObjectiveAction)EditorGUILayout.EnumPopup(
                "Action",
                data.ObjectiveAction);
        }
    }

    // ---------- SubFlow ----------

    public sealed class SubFlowNodeEditorDefinition : FlowNodeEditorDefinition
    {
        public override FlowNodeType NodeType => FlowNodeType.SubFlow;

        public override void DrawInspector(FlowNodeData data)
        {
            data.SubFlow = (FlowGraph)EditorGUILayout.ObjectField(
                "Sub Flow",
                data.SubFlow,
                typeof(FlowGraph),
                false);
        }
    }

    // ---------- Sequence (multi-output) ----------

    public sealed class SequenceFlowNodeEditorDefinition : FlowNodeEditorDefinition
    {
        public override FlowNodeType NodeType => FlowNodeType.Sequence;

        public override int GetOutputCount(FlowNodeData data)
        {
            int len = data.SequenceNodeIds != null ? data.SequenceNodeIds.Length : 0;
            // At least one output even if no ids defined yet
            return Mathf.Max(1, len);
        }

        public override void BuildEdges(
            FlowNodeData data,
            FlowGraphView view,
            Dictionary<string, FlowNodeView> nodeViews)
        {
            if (data == null || nodeViews == null)
                return;

            if (!nodeViews.TryGetValue(data.NodeId, out var fromView))
                return;

            var ids = data.SequenceNodeIds;
            if (ids == null || ids.Length == 0)
                return;

            for (int i = 0; i < ids.Length; i++)
            {
                string targetId = ids[i];
                if (string.IsNullOrEmpty(targetId))
                    continue;

                if (!nodeViews.TryGetValue(targetId, out var toView))
                    continue;
                if (toView.InputPort == null)
                    continue;

                var outPort = fromView.GetOutputPortForIndex(i);
                if (outPort == null)
                    continue;

                var edge = outPort.ConnectTo(toView.InputPort);
                view.AddElement(edge);
            }
        }

        public override void OnEdgeConnected(FlowNodeData from, int outputIndex, FlowNodeData to)
        {
            if (from == null || to == null)
                return;
            if (outputIndex < 0)
                return;

            if (from.SequenceNodeIds == null || outputIndex >= from.SequenceNodeIds.Length)
            {
                Array.Resize(ref from.SequenceNodeIds, outputIndex + 1);
            }

            from.SequenceNodeIds[outputIndex] = to.NodeId;
        }

        public override void OnEdgeDisconnected(FlowNodeData from, int outputIndex, FlowNodeData to)
        {
            if (from == null || to == null)
                return;
            if (from.SequenceNodeIds == null)
                return;
            if (outputIndex < 0 || outputIndex >= from.SequenceNodeIds.Length)
                return;

            if (from.SequenceNodeIds[outputIndex] == to.NodeId)
                from.SequenceNodeIds[outputIndex] = string.Empty;
        }

        public override void DrawInspector(FlowNodeData data)
        {
            int count = data.SequenceNodeIds != null ? data.SequenceNodeIds.Length : 0;

            int newCount = EditorGUILayout.IntField("Count", count);
            newCount = Mathf.Max(0, newCount);

            if (newCount != count)
            {
                Array.Resize(ref data.SequenceNodeIds, newCount);
            }
        }
    }

    // ---------- Branch (multi-output) ----------

    public sealed class BranchFlowNodeEditorDefinition : FlowNodeEditorDefinition
    {
        public override FlowNodeType NodeType => FlowNodeType.Branch;

        public override int GetOutputCount(FlowNodeData data)
        {
            int lenA = data.BranchOptionTriggerIds != null ? data.BranchOptionTriggerIds.Length : 0;
            int lenB = data.BranchOptionNextNodeIds != null ? data.BranchOptionNextNodeIds.Length : 0;
            int len = Mathf.Max(lenA, lenB);

            // Default to 2 options if nothing configured.
            return Mathf.Max(2, len);
        }

        public override void BuildEdges(
            FlowNodeData data,
            FlowGraphView view,
            Dictionary<string, FlowNodeView> nodeViews)
        {
            if (data == null || nodeViews == null)
                return;

            if (!nodeViews.TryGetValue(data.NodeId, out var fromView))
                return;

            var nexts = data.BranchOptionNextNodeIds;
            if (nexts == null || nexts.Length == 0)
                return;

            for (int i = 0; i < nexts.Length; i++)
            {
                string targetId = nexts[i];
                if (string.IsNullOrEmpty(targetId))
                    continue;

                if (!nodeViews.TryGetValue(targetId, out var toView))
                    continue;
                if (toView.InputPort == null)
                    continue;

                var outPort = fromView.GetOutputPortForIndex(i);
                if (outPort == null)
                    continue;

                var edge = outPort.ConnectTo(toView.InputPort);
                view.AddElement(edge);
            }
        }

        public override void OnEdgeConnected(FlowNodeData from, int outputIndex, FlowNodeData to)
        {
            if (from == null || to == null)
                return;
            if (outputIndex < 0)
                return;

            if (from.BranchOptionNextNodeIds == null || outputIndex >= from.BranchOptionNextNodeIds.Length)
            {
                int lenA = from.BranchOptionTriggerIds != null ? from.BranchOptionTriggerIds.Length : 0;
                int newLen = Mathf.Max(lenA, outputIndex + 1);
                Array.Resize(ref from.BranchOptionNextNodeIds, newLen);
                if (from.BranchOptionTriggerIds == null)
                    from.BranchOptionTriggerIds = new string[newLen];
                else if (from.BranchOptionTriggerIds.Length < newLen)
                    Array.Resize(ref from.BranchOptionTriggerIds, newLen);
            }

            from.BranchOptionNextNodeIds[outputIndex] = to.NodeId;
        }

        public override void OnEdgeDisconnected(FlowNodeData from, int outputIndex, FlowNodeData to)
        {
            if (from == null || to == null)
                return;
            if (from.BranchOptionNextNodeIds == null)
                return;
            if (outputIndex < 0 || outputIndex >= from.BranchOptionNextNodeIds.Length)
                return;

            if (from.BranchOptionNextNodeIds[outputIndex] == to.NodeId)
                from.BranchOptionNextNodeIds[outputIndex] = string.Empty;
        }

        public override void DrawInspector(FlowNodeData data)
        {
            int lenA = data.BranchOptionTriggerIds != null ? data.BranchOptionTriggerIds.Length : 0;
            int lenB = data.BranchOptionNextNodeIds != null ? data.BranchOptionNextNodeIds.Length : 0;
            int count = Mathf.Max(lenA, lenB);

            // Default to 2 options
            if (count < 2) count = 2;

            int newCount = EditorGUILayout.IntField("Options", count);
            newCount = Mathf.Max(2, newCount);

            if (newCount != count)
            {
                Array.Resize(ref data.BranchOptionTriggerIds, newCount);
                Array.Resize(ref data.BranchOptionNextNodeIds, newCount);
            }

            if (data.BranchOptionTriggerIds == null || data.BranchOptionTriggerIds.Length < newCount)
                Array.Resize(ref data.BranchOptionTriggerIds, newCount);
            if (data.BranchOptionNextNodeIds == null || data.BranchOptionNextNodeIds.Length < newCount)
                Array.Resize(ref data.BranchOptionNextNodeIds, newCount);

            EditorGUI.indentLevel++;
            for (int i = 0; i < newCount; i++)
            {
                data.BranchOptionTriggerIds[i] = EditorGUILayout.TextField(
                    $"Condition {i}",
                    data.BranchOptionTriggerIds[i]);
            }
            EditorGUI.indentLevel--;
        }
    }

    // ---------- Loop ----------

    public sealed class LoopFlowNodeEditorDefinition : FlowNodeEditorDefinition
    {
        public override FlowNodeType NodeType => FlowNodeType.Loop;

        public override void DrawInspector(FlowNodeData data)
        {
            data.LoopBodyNodeId = EditorGUILayout.TextField("Loop Body Node Id", data.LoopBodyNodeId);
            data.LoopCount = EditorGUILayout.IntField("Loop Count (0 = infinite)", data.LoopCount);
        }
    }

    // ---------- Registry ----------

    public static class FlowNodeEditorRegistry
    {
        private static readonly Dictionary<FlowNodeType, FlowNodeEditorDefinition> s_definitions;
        private static readonly FlowNodeEditorDefinition s_default = new DefaultFlowNodeEditorDefinition();

        static FlowNodeEditorRegistry()
        {
            s_definitions = new Dictionary<FlowNodeType, FlowNodeEditorDefinition>
            {
                { FlowNodeType.Start,         new StartFlowNodeEditorDefinition() },
                { FlowNodeType.Dialogue,      new DialogueFlowNodeEditorDefinition() },
                { FlowNodeType.Delay,         new DelayFlowNodeEditorDefinition() },
                { FlowNodeType.WaitForTrigger,new WaitForTriggerFlowNodeEditorDefinition() },
                { FlowNodeType.Objective,     new ObjectiveFlowNodeEditorDefinition() },
                { FlowNodeType.SubFlow,       new SubFlowNodeEditorDefinition() },
                { FlowNodeType.Sequence,      new SequenceFlowNodeEditorDefinition() },
                { FlowNodeType.Branch,        new BranchFlowNodeEditorDefinition() },
                { FlowNodeType.Loop,          new LoopFlowNodeEditorDefinition() },
            };
        }

        public static FlowNodeEditorDefinition Get(FlowNodeType type)
        {
            if (s_definitions.TryGetValue(type, out var def))
                return def;
            return s_default;
        }
    }
}
#endif
