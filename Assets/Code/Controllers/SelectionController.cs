using UnityEngine;

public class SelectionController
{
    private readonly GameplayConfig m_gameplayConfig;

    private NavAgent m_selectedAgent;
    private GameObject m_selectionRingInstance;

    public NavAgent SelectedAgent => m_selectedAgent;

    public SelectionController()
    {
        Debug.Assert(App.Instance != null, "SelectionController: App.Instance is null");
        Debug.Assert(App.Instance.ResourceLoader != null, "SelectionController: ResourceLoader is null");

        m_gameplayConfig = App.Instance.ResourceLoader.LoadConfig<GameplayConfig>(ConfigId.Gameplay);
        Debug.Assert(m_gameplayConfig != null, "SelectionController: GameplayConfig could not be loaded");
    }

    public void SetSingleSelection(NavAgent agent)
    {
        if (m_selectedAgent == agent)
            return;

        ClearSelection();

        m_selectedAgent = agent;

        if (m_selectedAgent != null && m_gameplayConfig.SelectionRingPrefab != null)
        {
            m_selectionRingInstance = Object.Instantiate(
                m_gameplayConfig.SelectionRingPrefab,
                m_selectedAgent.transform);

            // Optionally adjust local position if your agent pivot is at feet/centre.
            m_selectionRingInstance.transform.localPosition = Vector3.zero;
        }
    }

    public void ClearSelection()
    {
        if (m_selectionRingInstance != null)
        {
            Object.Destroy(m_selectionRingInstance);
            m_selectionRingInstance = null;
        }

        if (m_selectedAgent != null)
        {
            m_selectedAgent = null;
        }
    }
}
