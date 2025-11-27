namespace TacticsGame.Flow
{
    public sealed class ObjectiveFlowNode : FlowNode
    {
        public ObjectiveFlowNode(FlowGraphRunner runner, FlowNodeData data)
            : base(runner, data) { }

        public override bool Tick(float deltaTime)
        {
            var world = m_runner.World;

            if (world != null && !string.IsNullOrEmpty(m_data.ObjectiveId))
            {
                switch (m_data.ObjectiveAction)
                {
                    case FlowObjectiveAction.Activate:
                        world.ActivateObjective(m_data.ObjectiveId);
                        break;

                    case FlowObjectiveAction.Complete:
                        world.CompleteObjective(m_data.ObjectiveId);
                        break;
                }
            }

            // Fire-and-forget for now; if you want "wait until complete"
            // you can combine with a WaitForTrigger node.
            return true;
        }
    }
}
