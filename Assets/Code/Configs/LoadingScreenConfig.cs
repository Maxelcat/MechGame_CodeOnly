using UnityEngine;

[CreateAssetMenu(
    menuName = "MechGame/Loading Screen Config",
    fileName = "LoadingScreenConfig")]
public class LoadingScreenConfig : ScriptableObject
{
    [Header("Noise Settings")]
    public float m_noiseScale = 300f;          // How fine the noise is
    public float m_noiseSpeed = 1.5f;          // Scroll speed / jitter
    public float m_noiseIntensity = 1f;        // Brightness multiplier

    public Color m_colorA = Color.white;       // Low-end tint
    public Color m_colorB = new Color(0.6f, 0.8f, 1f); // High-end tint

    [Header("Clear Effect")]
    public float m_clearDuration = 1.5f;       // Duration of patchy clear
    public float m_clearNoiseScale = 4f;       // Size of “patches” that clear

    [Header("Timing")]
    [Tooltip("Minimum on-screen time for the loading screen (seconds), even on fast loads.")]
    public float m_minDisplayTime = 1.5f;

    [Header("Audio")]
    public AudioClip m_whiteNoiseClip;

    [Range(0f, 1f)]
    public float m_targetVolume = 0.25f;

    public float m_fadeInTime = 0.25f;
    public float m_fadeOutTime = 0.3f;
}
