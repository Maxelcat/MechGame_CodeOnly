using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueView : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject m_rootPanel;

    [Header("UI References")]
    [SerializeField] private Image m_portraitImage;
    [SerializeField] private TextMeshProUGUI m_nameText;
    [SerializeField] private TextMeshProUGUI m_bodyText;

    private void Awake()
    {
        if (m_rootPanel == null)
        {
            m_rootPanel = gameObject;
        }

        Hide();
    }

    public void Show()
    {
        if (m_rootPanel != null)
        {
            m_rootPanel.SetActive(true);
        }
    }

    public void Hide()
    {
        if (m_rootPanel != null)
        {
            m_rootPanel.SetActive(false);
        }
    }

    public void SetLine(DialogueCharacter speaker, string visibleText)
    {
        if (m_rootPanel != null && !m_rootPanel.activeSelf)
        {
            m_rootPanel.SetActive(true);
        }

        if (m_nameText != null)
        {
            m_nameText.text = speaker != null ? speaker.DisplayName : string.Empty;
        }

        if (m_portraitImage != null)
        {
            if (speaker != null && speaker.Portrait != null)
            {
                m_portraitImage.sprite = speaker.Portrait;
                m_portraitImage.enabled = true;
            }
            else
            {
                m_portraitImage.enabled = false;
            }
        }

        if (m_bodyText != null)
        {
            m_bodyText.text = visibleText ?? string.Empty;
        }
    }
}
