namespace TacticsGame.Flow
{
    public sealed class DialogueFlowNode : FlowNode
    {
        private bool m_started;

        public DialogueFlowNode(FlowGraphRunner runner, FlowNodeData data)
            : base(runner, data) { }

        public override void OnEnter()
        {
            m_started = false;
        }

        public override bool Tick(float deltaTime)
        {
            var world = m_runner.World;

            // No dialogue or no world → complete immediately.
            if (m_data.Dialogue == null || world == null)
                return true;

            if (!m_started)
            {
                m_started = true;
                world.PlayDialogue(m_data.Dialogue);

                if (!m_data.WaitForDialogueComplete)
                    return true;
            }

            // Wait for dialogue to finish.
            if (!m_data.WaitForDialogueComplete)
                return true;

            return !world.IsDialoguePlaying(m_data.Dialogue);
        }

        public override string GetDebugInfo()
        {
            var seqName = m_data.Dialogue != null ? m_data.Dialogue.name : "(none)";
            string mode = m_data.WaitForDialogueComplete ? "wait-for-complete" : "fire-and-forget";

            bool playing = false;
            if (m_data.Dialogue != null && m_runner.World != null)
            {
                playing = m_runner.World.IsDialoguePlaying(m_data.Dialogue);
            }

            string state = !m_started
                ? "pending start"
                : (playing ? "playing" : "finished");

            return $"Dialogue '{NodeId}': {seqName} [{mode}, {state}]";
        }
    }
}
