using UnityEngine;
using TacticsGame.Flow;

public class FlowController : IFlowWorld
{
    private readonly ObjectiveController m_objectiveController;
    private readonly DialogueController m_dialogueController;

    private FlowGraphRunner m_runner;

    public bool IsRunning => m_runner != null && !m_runner.IsFinished;
    public FlowGraph CurrentGraph { get; private set; }

    public FlowGraphRunner Runner => m_runner;
    public FlowNode CurrentNode => m_runner != null ? m_runner.CurrentNode : null;

    public FlowController(ObjectiveController objectiveController, DialogueController dialogueController)
    {
        m_objectiveController = objectiveController;
        m_dialogueController = dialogueController;
    }

    public void Tick(float deltaTime)
    {
        if (m_runner == null)
            return;

        m_runner.Tick(deltaTime);
    }

    public void StartFlow(FlowGraph graph)
    {
        if (graph == null)
        {
            Debug.LogError("FlowController.StartFlow: graph is null");
            return;
        }

        CurrentGraph = graph;
        m_runner = new FlowGraphRunner(graph, this);
    }

    public void StopFlow()
    {
        m_runner = null;
        CurrentGraph = null;
    }

    // --- IFlowWorld implementation ---

    public void PlayDialogue(DialogueSequence sequence)
    {
        if (m_dialogueController == null || sequence == null)
        {
            Debug.LogWarning("FlowController: Dialogue world call without controller or sequence");
            return;
        }

        // If you need callbacks, you can embed that in DialogueFlowNode instead
        m_dialogueController.PlaySequence(sequence, null);
    }

    public bool IsDialoguePlaying(DialogueSequence sequence)
    {
        if (m_dialogueController == null || sequence == null)
            return false;

        return m_dialogueController.IsPlaying;
    }

    public void ActivateObjective(string objectiveId)
    {
        if (m_objectiveController == null || string.IsNullOrEmpty(objectiveId))
            return;

        m_objectiveController.SetObjectiveActive(objectiveId);
    }

    public void CompleteObjective(string objectiveId)
    {
        if (m_objectiveController == null || string.IsNullOrEmpty(objectiveId))
            return;

        m_objectiveController.CompleteObjective(objectiveId);
    }

    public bool IsConditionTrue(string conditionId)
    {
        // Hook this up to whatever global condition system you want
        // For now: always false unless you have something concrete
        return false;
    }

    // If you still need trigger-style behaviour, add a method like:
    public void RaiseTrigger(string triggerId)
    {
        // Forward to a dedicated WaitForTriggerFlowNode implementation
        // e.g. a static trigger bus or something the node listens to.
        // We can wire this next once you’re ready.
    }
}
