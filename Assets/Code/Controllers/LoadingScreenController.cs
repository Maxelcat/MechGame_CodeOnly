using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingScreenController : MonoBehaviour
{
    [Header("Config")]
    public LoadingScreenConfig m_config;

    [Header("UI")]
    public CanvasGroup m_canvasGroup;
    public Image m_staticImage;
    public Image m_progressFill;
    public TMP_Text m_statusLabel;

    [Header("Audio")]
    public AudioSource m_audioSource;

    Material m_materialInstance;

    // Shader property IDs
    static readonly int s_noiseScaleId = Shader.PropertyToID("_NoiseScale");
    static readonly int s_noiseSpeedId = Shader.PropertyToID("_NoiseSpeed");
    static readonly int s_intensityId = Shader.PropertyToID("_Intensity");
    static readonly int s_colorAId = Shader.PropertyToID("_ColorA");
    static readonly int s_colorBId = Shader.PropertyToID("_ColorB");
    static readonly int s_clearAmountId = Shader.PropertyToID("_ClearAmount");
    static readonly int s_clearNoiseScaleId = Shader.PropertyToID("_ClearNoiseScale");

    public LoadingScreenConfig Config => m_config;

    void Awake()
    {
        if (m_staticImage != null)
        {
            m_materialInstance = new Material(m_staticImage.material);
            m_staticImage.material = m_materialInstance;
        }

        ApplyConfig();
        SetVisible(false);
    }

    void ApplyConfig()
    {
        if (m_config == null || m_materialInstance == null)
            return;

        m_materialInstance.SetFloat(s_noiseScaleId, m_config.m_noiseScale);
        m_materialInstance.SetFloat(s_noiseSpeedId, m_config.m_noiseSpeed);
        m_materialInstance.SetFloat(s_intensityId, m_config.m_noiseIntensity);
        m_materialInstance.SetColor(s_colorAId, m_config.m_colorA);
        m_materialInstance.SetColor(s_colorBId, m_config.m_colorB);
        m_materialInstance.SetFloat(s_clearNoiseScaleId, m_config.m_clearNoiseScale);
        m_materialInstance.SetFloat(s_clearAmountId, 0f);

        if (m_audioSource != null && m_config.m_whiteNoiseClip != null)
        {
            m_audioSource.clip = m_config.m_whiteNoiseClip;
            m_audioSource.loop = true;
            m_audioSource.playOnAwake = false;
        }
    }

    void SetVisible(bool visible)
    {
        if (m_canvasGroup == null)
            return;

        m_canvasGroup.alpha = visible ? 1f : 0f;
        m_canvasGroup.blocksRaycasts = visible;
        m_canvasGroup.interactable = visible;
    }

    // ---------------------------------------------------------------------
    // Public API used by ResourceLoaderModule

    public void Show(string message = "CONNECTING TO BATTLEFIELD...")
    {
        StopAllCoroutines();
        ApplyConfig();

        if (m_statusLabel != null)
            m_statusLabel.text = message;

        if (m_progressFill != null)
            m_progressFill.fillAmount = 0f;

        if (m_materialInstance != null)
            m_materialInstance.SetFloat(s_clearAmountId, 0f);

        SetVisible(true);

        if (m_audioSource != null &&
            m_config != null &&
            m_config.m_whiteNoiseClip != null)
        {
            m_audioSource.volume = 0f;
            m_audioSource.Play();
            StartCoroutine(FadeAudio(m_config.m_targetVolume, m_config.m_fadeInTime));
        }
    }

    public void SetProgress(float value)
    {
        if (m_progressFill != null)
            m_progressFill.fillAmount = Mathf.Clamp01(value);
    }

    /// <summary>
    /// Patchy clear + fade out.
    /// </summary>
    public IEnumerator PlayClearAndHide()
    {
        float duration = (m_config != null) ? m_config.m_clearDuration : 1f;
        float t = 0f;

        if (m_audioSource != null &&
            m_audioSource.isPlaying &&
            m_config != null)
        {
            StartCoroutine(FadeAudio(0f, m_config.m_fadeOutTime, true));
        }

        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);

            if (m_materialInstance != null)
                m_materialInstance.SetFloat(s_clearAmountId, a);

            if (m_canvasGroup != null)
                m_canvasGroup.alpha = 1f - a;

            yield return null;
        }

        if (m_materialInstance != null)
            m_materialInstance.SetFloat(s_clearAmountId, 1f);

        SetVisible(false);
    }

    IEnumerator FadeAudio(float targetVolume, float duration, bool stopWhenDone = false)
    {
        if (m_audioSource == null)
            yield break;

        float start = m_audioSource.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = (duration > 0f) ? (t / duration) : 1f;
            m_audioSource.volume = Mathf.Lerp(start, targetVolume, k);
            yield return null;
        }

        m_audioSource.volume = targetVolume;

        if (stopWhenDone && Mathf.Approximately(targetVolume, 0f))
            m_audioSource.Stop();
    }
}
