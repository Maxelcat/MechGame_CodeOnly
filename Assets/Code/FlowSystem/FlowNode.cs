using System;

namespace TacticsGame.Flow
{
    public abstract class FlowNode
    {
        protected readonly FlowGraphRunner m_runner;
        protected readonly FlowNodeData m_data;

        public string NodeId => m_data.NodeId;
        public virtual string NextNodeId => m_data.NextNodeId;
        public FlowNodeType NodeType => m_data.NodeType;
        public FlowNodeData Data => m_data;

        protected FlowNode(FlowGraphRunner runner, FlowNodeData data)
        {
            m_runner = runner ?? throw new ArgumentNullException(nameof(runner));
            m_data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public virtual void OnEnter() { }

        public abstract bool Tick(float deltaTime);

        public virtual void OnExit() { }

        /// <summary>
        /// Optional debug string for overlay / logging.
        /// </summary>
        public virtual string GetDebugInfo()
        {
            return $"{NodeType} '{NodeId}'";
        }
    }
}
