namespace TacticsGame.Flow
{
    public sealed class DelayFlowNode : FlowNode
    {
        private float m_remaining;

        public DelayFlowNode(FlowGraphRunner runner, FlowNodeData data)
            : base(runner, data) { }

        public override void OnEnter()
        {
            m_remaining = m_data.DelaySeconds;
        }

        public override bool Tick(float deltaTime)
        {
            m_remaining -= deltaTime;
            return m_remaining <= 0f;
        }

        public override string GetDebugInfo()
        {
            return $"Delay '{NodeId}': {m_remaining:F2} / {m_data.DelaySeconds:F2} seconds remaining";
        }
    }
}
