using System.Collections.Generic;
using UnityEngine;

public enum ObjectiveState
{
    Inactive,
    Active,
    Completed,
    Cancelled
}

public class ObjectiveController
{
    private readonly Dictionary<string, ObjectiveState> m_objectives =
        new Dictionary<string, ObjectiveState>();

    public void SetObjectiveActive(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;

        m_objectives[id] = ObjectiveState.Active;
        Debug.Log($"Objective '{id}' set ACTIVE.");
    }

    public void CompleteObjective(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;

        m_objectives[id] = ObjectiveState.Completed;
        Debug.Log($"Objective '{id}' COMPLETED.");
    }

    public ObjectiveState GetState(string id)
    {
        if (string.IsNullOrEmpty(id))
            return ObjectiveState.Inactive;

        if (m_objectives.TryGetValue(id, out ObjectiveState state))
            return state;

        return ObjectiveState.Inactive;
    }
}
