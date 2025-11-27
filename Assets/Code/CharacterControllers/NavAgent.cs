using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavAgent : MonoBehaviour
{
    private NavMeshAgent m_agent;

    [SerializeField]
    private Renderer m_visual;

    [SerializeField]
    private Color m_selectedColor = Color.green;

    private Color m_defaultColor;

    // Global registry of all agents
    private static readonly List<NavAgent> s_allAgents = new List<NavAgent>();
    public static IReadOnlyList<NavAgent> AllAgents => s_allAgents;

    private void OnEnable()
    {
        if (!s_allAgents.Contains(this))
        {
            s_allAgents.Add(this);
        }
    }

    private void OnDisable()
    {
        s_allAgents.Remove(this);
    }

    private void Awake()
    {
        m_agent = GetComponent<NavMeshAgent>();

        if (m_visual == null)
        {
            m_visual = GetComponentInChildren<Renderer>();
        }

        if (m_visual != null)
        {
            m_defaultColor = m_visual.material.color;
        }
    }

    public void MoveTo(Vector3 destination)
    {
        if (m_agent != null)
        {
            m_agent.SetDestination(destination);
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (m_visual == null)
            return;

        m_visual.material.color = isSelected ? m_selectedColor : m_defaultColor;
    }
}
