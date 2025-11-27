namespace TacticsGame.Flow
{
    public sealed class WaitForTriggerFlowNode : FlowNode
    {
        private bool m_fired;

        public WaitForTriggerFlowNode(FlowGraphRunner runner, FlowNodeData data)
            : base(runner, data) { }

        public override void OnEnter()
        {
            m_fired = false;

            if (!string.IsNullOrEmpty(m_data.TriggerId))
            {
                m_runner.RegisterTriggerWait(this, m_data.TriggerId);
            }
        }

        public override bool Tick(float deltaTime)
        {
            return m_fired;
        }

        public override void OnExit()
        {
            if (!string.IsNullOrEmpty(m_data.TriggerId))
            {
                m_runner.UnregisterTriggerWait(this, m_data.TriggerId);
            }
        }

        public void Signal()
        {
            m_fired = true;
        }

        public override string GetDebugInfo()
        {
            return $"WaitForTrigger '{NodeId}': Trigger='{m_data.TriggerId}', Fired={m_fired}";
        }
    }
}
