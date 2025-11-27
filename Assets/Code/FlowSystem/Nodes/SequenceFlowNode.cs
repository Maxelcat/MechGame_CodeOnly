using System;
using UnityEngine;

namespace TacticsGame.Flow
{
    /// <summary>
    /// Executes a series of sub-chains in order.
    ///
    /// SequenceNodeIds[] defines the heads of each step.
    ///
    /// Runtime behaviour:
    /// - First time this node is entered → stepIndex = 0.
    /// - Each time it's re-entered (via the return stack) → stepIndex++.
    /// - While stepIndex is in range [0, SequenceNodeIds.Length-1],
    ///   NextNodeId points at that step's head.
    /// - When stepIndex runs past the last step, this node returns null
    ///   and does NOT use m_data.NextNodeId at all.
    ///
    /// Editor workflow:
    /// - Set SequenceNodeIds[] to the first node of each step.
    /// - You do NOT use NextNodeId on this node; any value there is ignored.
    ///   (We clear it at runtime to be safe.)
    /// - The final step's chain ending (NextNodeId == null) will cause the
    ///   runner to return here once more, advance past the last step, and
    ///   then return null to end the sequence.
    /// </summary>
    public sealed class SequenceFlowNode : FlowNode
    {
        private int m_stepIndex = -1;
        private readonly bool m_hasSteps;

        public SequenceFlowNode(FlowGraphRunner runner, FlowNodeData data)
            : base(runner, data)
        {
            m_hasSteps = m_data.SequenceNodeIds != null &&
                         m_data.SequenceNodeIds.Length > 0;

            // Belt-and-braces: nuke any stray NextNodeId on this node.
            if (!string.IsNullOrEmpty(m_data.NextNodeId))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning(
                    $"[Flow] Sequence node '{NodeId}' has NextNodeId='{m_data.NextNodeId}' set. " +
                    "Sequence nodes ignore NextNodeId; clearing it at runtime.");
#endif
                m_data.NextNodeId = null;
            }
        }

        public override void OnEnter()
        {
            if (!m_hasSteps)
                return;

            if (m_stepIndex < 0)
            {
                // First time we ever hit this node.
                m_stepIndex = 0;
            }
            else
            {
                // Re-entered from the end of a sub-chain.
                m_stepIndex++;
            }

            // Clamp so we don't overflow. When m_stepIndex >= Length we are "done".
            int len = m_data.SequenceNodeIds != null ? m_data.SequenceNodeIds.Length : 0;
            if (m_stepIndex > len)
            {
                m_stepIndex = len;
            }
        }

        public override bool Tick(float deltaTime)
        {
            // Sequence itself is instantaneous; it just decides where to go next.
            return true;
        }

        public override string NextNodeId
        {
            get
            {
                if (!m_hasSteps)
                    return null;

                var steps = m_data.SequenceNodeIds;
                if (steps == null || steps.Length == 0)
                    return null;

                // Out of range → sequence is finished. Do NOT fall back to m_data.NextNodeId.
                if (m_stepIndex < 0 || m_stepIndex >= steps.Length)
                    return null;

                var id = steps[m_stepIndex];
                if (string.IsNullOrEmpty(id))
                    return null;

                return id;
            }
        }

        public override void OnExit()
        {
            if (!m_hasSteps || m_data.SequenceNodeIds == null)
                return;

            // Only push while we still have valid steps.
            if (m_stepIndex >= 0 && m_stepIndex < m_data.SequenceNodeIds.Length)
            {
                m_runner.PushReturnNode(this);
            }
        }

        public override string GetDebugInfo()
        {
            int total = m_data.SequenceNodeIds != null ? m_data.SequenceNodeIds.Length : 0;

            if (!m_hasSteps)
                return $"Sequence '{NodeId}': (no steps configured)";

            int current = Mathf.Clamp(m_stepIndex, 0, Math.Max(total - 1, 0));

            bool complete = m_stepIndex >= total;
            return complete
                ? $"Sequence '{NodeId}': complete ({total} steps)"
                : $"Sequence '{NodeId}': step {current}/{total}";
        }
    }
}
