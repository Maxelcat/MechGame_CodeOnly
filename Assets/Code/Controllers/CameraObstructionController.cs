using System.Collections.Generic;
using UnityEngine;

public class CameraObstructionController
{
    private readonly Camera m_camera;
    private readonly Transform m_cameraTransform;
    private readonly TacticalCameraConfig m_config;

    private class ObstructedRendererState
    {
        public Renderer Renderer;
        public GameObject LayerObject;
        public int OriginalLayer;
        public bool IsObstructed;
    }

    private readonly Dictionary<Renderer, ObstructedRendererState> m_obstructedRenderers =
        new Dictionary<Renderer, ObstructedRendererState>();

    private readonly List<Vector3> m_occludedAgentPositions = new List<Vector3>();
    public IReadOnlyList<Vector3> OccludedAgentPositions => m_occludedAgentPositions;

    public CameraObstructionController(Camera camera, TacticalCameraConfig config)
    {
        m_camera = camera;
        m_cameraTransform = camera != null ? camera.transform : null;
        m_config = config;
    }

    public void Tick(float deltaTime)
    {
        if (m_cameraTransform == null || m_camera == null || m_config == null || !m_config.EnableObstructionFade)
        {
            RestoreAll();
            m_occludedAgentPositions.Clear();
            return;
        }

        IReadOnlyList<NavAgent> agents = NavAgent.AllAgents;
        if (agents == null || agents.Count == 0)
        {
            RestoreAll();
            m_occludedAgentPositions.Clear();
            return;
        }

        foreach (var kvp in m_obstructedRenderers)
        {
            kvp.Value.IsObstructed = false;
        }
        m_occludedAgentPositions.Clear();

        int mask = m_config.ObstructionMask.value;
        int clickLayerBit = 1 << m_config.ClickThroughLayer;
        if ((mask & clickLayerBit) == 0)
            mask |= clickLayerBit;

        foreach (NavAgent agent in agents)
        {
            if (agent == null)
                continue;

            Transform agentTf = agent.transform;
            if (agentTf == null)
                continue;

            Vector3 agentWorldPos = agentTf.position + Vector3.up * 1.0f;

            Vector3 origin = m_cameraTransform.position;
            Vector3 toAgent = agentWorldPos - origin;
            float distanceToAgent = toAgent.magnitude;
            if (distanceToAgent <= 0f)
                continue;

            Vector3 dirToAgent = toAgent / distanceToAgent;

            bool agentOccluded = false;

            // Central ray to decide if there is any obstruction at all
            RaycastHit[] centerHits = Physics.RaycastAll(
                origin,
                dirToAgent,
                distanceToAgent,
                mask,
                QueryTriggerInteraction.Ignore);

            ProcessHits(centerHits, ref agentOccluded);

            if (!agentOccluded)
                continue;

            m_occludedAgentPositions.Add(agentWorldPos);

            // Sample multiple rays across the hole area for click-through
            Vector3 vp = m_camera.WorldToViewportPoint(agentWorldPos);
            if (vp.z <= 0f)
                continue;

            float r = m_config.RadialFadeRadius;
            float rDiag = r * 0.7071f;

            Vector2[] offsets =
            {
                new Vector2(0f, 0f),
                new Vector2( r,  0f),
                new Vector2(-r,  0f),
                new Vector2( 0f,  r),
                new Vector2( 0f, -r),
                new Vector2( rDiag,  rDiag),
                new Vector2(-rDiag, rDiag),
                new Vector2( rDiag, -rDiag),
                new Vector2(-rDiag,-rDiag)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2 uv = new Vector2(vp.x, vp.y) + offsets[i];

                if (uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f)
                    continue;

                Ray ray = m_camera.ViewportPointToRay(new Vector3(uv.x, uv.y, 0f));

                RaycastHit[] areaHits = Physics.RaycastAll(
                    ray,
                    distanceToAgent,
                    mask,
                    QueryTriggerInteraction.Ignore);

                bool dummy = false;
                ProcessHits(areaHits, ref dummy);
            }
        }

        // Restore any renderers that are no longer obstructed
        List<Renderer> toRemove = new List<Renderer>();
        foreach (var kvp in m_obstructedRenderers)
        {
            ObstructedRendererState state = kvp.Value;
            if (!state.IsObstructed)
            {
                if (state.LayerObject != null)
                {
                    state.LayerObject.layer = state.OriginalLayer;
                }
                toRemove.Add(state.Renderer);
            }
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            m_obstructedRenderers.Remove(toRemove[i]);
        }
    }

    private void ProcessHits(RaycastHit[] hits, ref bool agentOccludedFlag)
    {
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null)
                continue;

            if (col.GetComponentInParent<NavAgent>() != null)
                continue;

            Renderer rend = col.GetComponentInChildren<Renderer>();
            if (rend == null)
                continue;

            if (!m_obstructedRenderers.TryGetValue(rend, out ObstructedRendererState state))
            {
                state = new ObstructedRendererState
                {
                    Renderer = rend,
                    LayerObject = col.gameObject,
                    OriginalLayer = col.gameObject.layer,
                    IsObstructed = true
                };
                m_obstructedRenderers.Add(rend, state);

                if (state.LayerObject != null)
                {
                    state.LayerObject.layer = m_config.ClickThroughLayer;
                }
            }

            state.IsObstructed = true;
            agentOccludedFlag = true;
        }
    }

    private void RestoreAll()
    {
        foreach (var kvp in m_obstructedRenderers)
        {
            ObstructedRendererState state = kvp.Value;
            if (state.LayerObject != null)
            {
                state.LayerObject.layer = state.OriginalLayer;
            }
        }

        m_obstructedRenderers.Clear();
    }
}
