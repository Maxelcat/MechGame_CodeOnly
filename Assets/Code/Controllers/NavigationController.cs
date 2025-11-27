using UnityEngine;

public class NavigationController
{
    private readonly SelectionController m_selectionController;
    private readonly GameplayConfig m_gameplayConfig;

    public NavigationController(SelectionController selectionController)
    {
        Debug.Assert(App.Instance != null, "NavigationController: App.Instance is null");
        Debug.Assert(App.Instance.ResourceLoader != null, "NavigationController: ResourceLoader is null");

        m_selectionController = selectionController;
        Debug.Assert(m_selectionController != null, "NavigationController: SelectionController is null");

        m_gameplayConfig = App.Instance.ResourceLoader.LoadConfig<GameplayConfig>(ConfigId.Gameplay);
        Debug.Assert(m_gameplayConfig != null, "NavigationController: GameplayConfig could not be loaded");
    }

    public void MoveSelectedAgents(Vector3 destination)
    {
        NavAgent agent = m_selectionController.SelectedAgent;
        if (agent == null)
            return;

        agent.MoveTo(destination);

        SpawnDestinationMarker(destination);
    }

    private void SpawnDestinationMarker(Vector3 worldPos)
    {
        if (m_gameplayConfig.DestinationMarkerPrefab == null)
            return;

        Vector3 spawnPos = worldPos;
        spawnPos.y += 0.05f; // tiny offset to avoid z-fighting

        GameObject marker = Object.Instantiate(
            m_gameplayConfig.DestinationMarkerPrefab,
            spawnPos,
            Quaternion.identity);

        if (m_gameplayConfig.DestinationMarkerLifetime > 0f)
        {
            Object.Destroy(marker, m_gameplayConfig.DestinationMarkerLifetime);
        }
    }
}
