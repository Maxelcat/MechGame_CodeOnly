using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NavAgent))]
public class UnitClickReceiver : MonoBehaviour, IClickReceiver
{
    private NavAgent m_navAgent;

    private void Awake()
    {
        m_navAgent = GetComponent<NavAgent>();
    }

    public void OnClick(RaycastHit hit)
    {
        if (App.Instance == null || App.Instance.GameController == null)
            return;

        SelectionController selection = App.Instance.GameController.SelectionController;

        //bool addToSelection =
        //    Input.GetKey(KeyCode.LeftShift) ||
        //    Input.GetKey(KeyCode.RightShift);

        //if (addToSelection)
        //{
        //    selection.AddToSelection(m_navAgent);
        //}
        //else
        //{
            selection.SetSingleSelection(m_navAgent);
        //}
    }
}
