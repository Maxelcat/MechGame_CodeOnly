namespace TacticsGame.Flow
{
    public sealed class BranchFlowNode : FlowNode
    {
        private int m_chosenIndex = -1;

        public BranchFlowNode(FlowGraphRunner runner, FlowNodeData data)
            : base(runner, data) { }

        public override void OnEnter()
        {
            m_chosenIndex = -1;

            var world = m_runner.World;
            var conds = m_data.BranchOptionTriggerIds;
            var nexts = m_data.BranchOptionNextNodeIds;

            if (world == null || conds == null || nexts == null)
                return;

            int count = System.Math.Min(conds.Length, nexts.Length);
            for (int i = 0; i < count; i++)
            {
                var condId = conds[i];
                if (string.IsNullOrEmpty(condId))
                    continue;

                if (world.IsConditionTrue(condId))
                {
                    m_chosenIndex = i;
                    break;
                }
            }
        }

        public override bool Tick(float deltaTime)
        {
            // Decision happens in OnEnter; nothing to wait for.
            return true;
        }

        public override string NextNodeId
        {
            get
            {
                var nexts = m_data.BranchOptionNextNodeIds;
                if (m_chosenIndex >= 0 &&
                    nexts != null &&
                    m_chosenIndex < nexts.Length &&
                    !string.IsNullOrEmpty(nexts[m_chosenIndex]))
                {
                    return nexts[m_chosenIndex];
                }

                // Fallback to simple NextNodeId if no branch was taken.
                return base.NextNodeId;
            }
        }
    }
}
