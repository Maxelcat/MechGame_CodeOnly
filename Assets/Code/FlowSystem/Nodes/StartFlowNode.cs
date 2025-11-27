namespace TacticsGame.Flow
{
    /// <summary>
    /// Start node – currently just passes through immediately.
    /// You can make this do intro work later if you want.
    /// </summary>
    public sealed class StartFlowNode : FlowNode
    {
        public StartFlowNode(FlowGraphRunner runner, FlowNodeData data)
            : base(runner, data) { }

        public override bool Tick(float deltaTime)
        {
            // Immediately complete to NextNodeId
            return true;
        }
    }
}
