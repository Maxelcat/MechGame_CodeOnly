using System.Collections.Generic;
using UnityEngine;

public class TacticalCameraController
{
    private const int MAX_HOLES = 16;

    private readonly Camera m_camera;
    private readonly Transform m_cameraTransform;
    private readonly TacticalCameraConfig m_config;
    private readonly CameraObstructionController m_obstructionController;

    public TacticalCameraConfig Config => m_config;

    public TacticalCameraController(Camera camera)
    {
        Debug.Assert(App.Instance != null, "TacticalCameraController: App.Instance is null");
        Debug.Assert(App.Instance.ResourceLoader != null, "TacticalCameraController: ResourceLoader is null");

        m_camera = camera;
        m_cameraTransform = camera != null ? camera.transform : null;

        m_config = App.Instance.ResourceLoader.LoadConfig<TacticalCameraConfig>(ConfigId.TacticalCamera);
        Debug.Assert(m_config != null, "TacticalCameraController: TacticalCameraConfig could not be loaded");

        if (m_camera != null && m_config != null)
        {
            m_obstructionController = new CameraObstructionController(m_camera, m_config);
        }
    }

    public void Tick(float deltaTime, Vector2 keyboardMoveInput)
    {
        if (m_cameraTransform == null || m_config == null)
            return;

        HandleMovement(deltaTime, keyboardMoveInput);
        m_obstructionController?.Tick(deltaTime);
        UpdateRadialFadeCenters();
    }

    private void HandleMovement(float deltaTime, Vector2 keyboardMoveInput)
    {
        Vector3 mousePos = Input.mousePosition;

        float edgeHorizontal = 0f;
        float edgeVertical = 0f;

        if (mousePos.x >= 0f && mousePos.x <= Screen.width &&
            mousePos.y >= 0f && mousePos.y <= Screen.height)
        {
            if (mousePos.x <= m_config.EdgeThickness)
                edgeHorizontal -= 1f;
            else if (mousePos.x >= Screen.width - m_config.EdgeThickness)
                edgeHorizontal += 1f;

            if (mousePos.y <= m_config.EdgeThickness)
                edgeVertical -= 1f;
            else if (mousePos.y >= Screen.height - m_config.EdgeThickness)
                edgeVertical += 1f;
        }

        float horizontal = keyboardMoveInput.x + edgeHorizontal;
        float vertical = keyboardMoveInput.y + edgeVertical;

        if (Mathf.Approximately(horizontal, 0f) && Mathf.Approximately(vertical, 0f))
            return;

        Vector3 forward = m_cameraTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = m_cameraTransform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 moveDir = (forward * vertical) + (right * horizontal);
        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        Vector3 newPos = m_cameraTransform.position + moveDir * m_config.MoveSpeed * deltaTime;

        if (m_config.UseBounds)
        {
            newPos.x = Mathf.Clamp(newPos.x, m_config.BoundsX.x, m_config.BoundsX.y);
            newPos.z = Mathf.Clamp(newPos.z, m_config.BoundsZ.x, m_config.BoundsZ.y);
        }

        m_cameraTransform.position = newPos;
    }

    private void UpdateRadialFadeCenters()
    {
        if (!m_config.EnableObstructionFade || m_camera == null || m_obstructionController == null)
        {
            Shader.SetGlobalInt("_HoleCount", 0);
            Shader.SetGlobalFloat("_HoleRadius", 0f);
            return;
        }

        IReadOnlyList<Vector3> occluded = m_obstructionController.OccludedAgentPositions;
        if (occluded == null || occluded.Count == 0)
        {
            Shader.SetGlobalInt("_HoleCount", 0);
            Shader.SetGlobalFloat("_HoleRadius", 0f);
            return;
        }

        int count = Mathf.Min(occluded.Count, MAX_HOLES);
        Vector4[] centers = new Vector4[count];

        for (int i = 0; i < count; i++)
        {
            Vector3 worldPos = occluded[i];
            Vector3 vp = m_camera.WorldToViewportPoint(worldPos);

            if (vp.z <= 0f)
            {
                centers[i] = Vector4.zero;
                continue;
            }

            centers[i] = new Vector4(vp.x, vp.y, 0f, 0f);
        }

        Shader.SetGlobalVectorArray("_HoleScreenCenters", centers);
        Shader.SetGlobalInt("_HoleCount", count);
        Shader.SetGlobalFloat("_HoleRadius", m_config.RadialFadeRadius);
        Shader.SetGlobalFloat("_HoleSoftness", m_config.RadialFadeSoftness);
    }
}
