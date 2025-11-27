using System;
using UnityEngine;

public class DialogueController
{
    private readonly DialogueView m_view;
    private readonly UIConfig m_uiConfig;

    private DialogueSequence m_currentSequence;
    private DialogueSequence.DialogueLine m_currentLine;
    private int m_currentIndex;
    private bool m_isPlaying;
    private Action m_onComplete;

    private string m_fullText = string.Empty;
    private float m_charIndexFloat;
    private bool m_lineFullyRevealed;
    private float m_fullLineTimer;

    // NEW: events so parent/game can react to state changes
    public event Action DialogueStarted;
    public event Action DialogueStopped;

    public bool IsPlaying => m_isPlaying;

    public DialogueController(Transform uiParent)
    {
        Debug.Assert(App.Instance != null, "DialogueController: App.Instance is null");
        Debug.Assert(App.Instance.ResourceLoader != null, "DialogueController: ResourceLoader is null");

        m_uiConfig = App.Instance.ResourceLoader.LoadConfig<UIConfig>(ConfigId.UI);
        Debug.Assert(m_uiConfig != null, "DialogueController: UIConfig could not be loaded");
        Debug.Assert(m_uiConfig.DialogueViewPrefab != null,
            "DialogueController: UIConfig.DialogueViewPrefab is not assigned");
        Debug.Assert(uiParent != null, "DialogueController: uiParent is null");

        GameObject instance = UnityEngine.Object.Instantiate(m_uiConfig.DialogueViewPrefab, uiParent);
        m_view = instance.GetComponent<DialogueView>();
        Debug.Assert(m_view != null, "DialogueController: DialogueView component missing on prefab instance");
    }

    public void Tick(float deltaTime, bool advanceRequested)
    {
        if (!m_isPlaying || m_view == null || m_currentSequence == null)
            return;

        bool allowManual = (m_uiConfig == null) || m_uiConfig.DialogueAllowManualAdvance;

        if (allowManual && advanceRequested)
        {
            if (!m_lineFullyRevealed)
            {
                RevealFullCurrentLine();
                return;
            }
            else
            {
                AdvanceLine();
                return;
            }
        }

        UpdateTypewriter(deltaTime);
    }

    public void PlaySequence(DialogueSequence sequence, Action onComplete = null)
    {
        if (m_view == null)
            return;

        Debug.Assert(App.Instance != null, "DialogueController.PlaySequence: App.Instance is null");

        if (sequence == null || sequence.Lines == null || sequence.Lines.Length == 0)
        {
            Debug.LogWarning("DialogueController.PlaySequence: sequence is null or empty.");
            return;
        }

        // If something was already playing, stop it first
        if (m_isPlaying)
        {
            StopInternal(invokeEvent: true);
        }

        m_currentSequence = sequence;
        m_currentIndex = 0;
        m_onComplete = onComplete;
        m_isPlaying = true;

        // NEW: notify listeners that dialogue just started
        DialogueStarted?.Invoke();

        ShowCurrentLine();
    }

    public void Stop()
    {
        StopInternal(invokeEvent: true);
    }

    private void StopInternal(bool invokeEvent)
    {
        bool wasPlaying = m_isPlaying;

        m_isPlaying = false;
        m_currentSequence = null;
        m_currentLine = null;
        m_currentIndex = 0;
        m_fullText = string.Empty;
        m_charIndexFloat = 0f;
        m_lineFullyRevealed = false;
        m_fullLineTimer = 0f;
        m_onComplete = null;

        if (m_view != null)
        {
            m_view.Hide();
        }

        // Only fire the event if it was actually playing
        if (invokeEvent && wasPlaying)
        {
            DialogueStopped?.Invoke();
        }
    }

    private void ShowCurrentLine()
    {
        if (m_currentSequence == null || m_view == null)
            return;

        if (m_currentIndex < 0 || m_currentIndex >= m_currentSequence.Lines.Length)
            return;

        m_currentLine = m_currentSequence.Lines[m_currentIndex];
        m_fullText = m_currentLine != null ? (m_currentLine.Text ?? string.Empty) : string.Empty;
        m_charIndexFloat = 0f;
        m_lineFullyRevealed = false;
        m_fullLineTimer = 0f;

        m_view.Show();
        m_view.SetLine(m_currentLine.Speaker, string.Empty);
    }

    private void UpdateTypewriter(float deltaTime)
    {
        if (m_lineFullyRevealed)
        {
            float autoDelay = (m_uiConfig != null) ? m_uiConfig.DialogueAutoAdvanceDelay : 0f;

            if (autoDelay > 0f)
            {
                m_fullLineTimer += deltaTime;
                if (m_fullLineTimer >= autoDelay)
                {
                    AdvanceLine();
                }
            }

            return;
        }

        float speed = (m_uiConfig != null) ? m_uiConfig.DialogueTextSpeed : 30f;

        if (speed <= 0f)
        {
            RevealFullCurrentLine();
            return;
        }

        m_charIndexFloat += speed * deltaTime;
        int charCount = Mathf.Clamp(Mathf.FloorToInt(m_charIndexFloat), 0, m_fullText.Length);

        if (charCount >= m_fullText.Length)
        {
            RevealFullCurrentLine();
        }
        else
        {
            string visible = m_fullText.Substring(0, charCount);
            m_view.SetLine(m_currentLine.Speaker, visible);
        }
    }

    private void RevealFullCurrentLine()
    {
        m_lineFullyRevealed = true;
        m_charIndexFloat = m_fullText.Length;
        m_fullLineTimer = 0f;

        if (m_view != null && m_currentLine != null)
        {
            m_view.SetLine(m_currentLine.Speaker, m_fullText);
        }
    }

    private void AdvanceLine()
    {
        if (!m_isPlaying || m_currentSequence == null)
            return;

        m_currentIndex++;

        if (m_currentIndex >= m_currentSequence.Lines.Length)
        {
            Action completedCallback = m_onComplete;
            StopInternal(invokeEvent: true);
            completedCallback?.Invoke();
            return;
        }

        ShowCurrentLine();
    }
}
