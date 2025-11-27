namespace TacticsGame.Flow
{
    public sealed class LoopFlowNode : FlowNode
    {
        private int m_iteration;
        private bool m_finished;

        public LoopFlowNode(FlowGraphRunner runner, FlowNodeData data)
            : base(runner, data) { }

        public override void OnEnter()
        {
            if (m_iteration == 0)
            {
                // First entry, just set up.
            }
            else
            {
                // Subsequent entries – we're about to start another iteration.
            }

            // Increment iteration *before* we run the body.
            m_iteration++;

            if (m_data.LoopCount > 0 && m_iteration > m_data.LoopCount)
            {
                m_finished = true;
            }
            else
            {
                m_finished = false;
            }
        }

        public override bool Tick(float deltaTime)
        {
            // No waiting; we just decide where to go.
            return true;
        }

        public override string NextNodeId
        {
            get
            {
                if (!m_finished && !string.IsNullOrEmpty(m_data.LoopBodyNodeId))
                    return m_data.LoopBodyNodeId;

                // Loop finished → use normal NextNodeId
                return base.NextNodeId;
            }
        }

        public override string GetDebugInfo()
        {
            string mode = m_data.LoopCount <= 0
                ? "infinite"
                : $"{m_iteration}/{m_data.LoopCount}";

            return $"Loop '{NodeId}': iteration={m_iteration}, mode={mode}, finished={m_finished}";
        }
    }
}
