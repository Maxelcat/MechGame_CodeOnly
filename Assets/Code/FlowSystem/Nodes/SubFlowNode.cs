namespace TacticsGame.Flow
{
    public sealed class SubFlowNode : FlowNode
    {
        private FlowGraphRunner m_subRunner;

        public SubFlowNode(FlowGraphRunner runner, FlowNodeData data)
            : base(runner, data) { }

        public override void OnEnter()
        {
            m_subRunner = null;

            if (m_data.SubFlow != null)
            {
                m_subRunner = new FlowGraphRunner(m_data.SubFlow, m_runner.World);
            }
        }

        public override bool Tick(float deltaTime)
        {
            if (m_subRunner == null)
                return true;

            if (m_subRunner.IsFinished)
                return true;

            m_subRunner.Tick(deltaTime);
            return m_subRunner.IsFinished;
        }

        public override void OnExit()
        {
            m_subRunner = null;
        }
    }
}
