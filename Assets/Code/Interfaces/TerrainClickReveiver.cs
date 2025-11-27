using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TerrainClickReceiver : MonoBehaviour, IClickReceiver
{
    public void OnClick(RaycastHit hit)
    {
        if (App.Instance == null || App.Instance.GameController == null)
            return;

        App.Instance.GameController.NavigationController.MoveSelectedAgents(hit.point);
    }
}