using System.Collections.Generic;
using UnityEngine;

public class LoadingScreenModule : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] GameObject m_loadingScreenPrefab;

    LoadingScreenController m_controller;
    public LoadingScreenController Controller => m_controller;

    private readonly ConfigPaths m_configPaths;
    private readonly Dictionary<ConfigId, string> m_pathLookup =
        new Dictionary<ConfigId, string>();

    public LoadingScreenModule(ConfigPaths configPaths)
    {
        Debug.Assert(configPaths != null, "ResourceLoaderModule: ConfigPaths is null");
        m_configPaths = configPaths;

        foreach (var entry in m_configPaths.Entries)
        {
            if (!m_pathLookup.ContainsKey(entry.Id))
            {
                m_pathLookup.Add(entry.Id, entry.ResourcesPath);
            }
        }
    }

    public T LoadConfig<T>(ConfigId id) where T : ScriptableObject
    {
        Debug.Assert(App.Instance != null, "ResourceLoaderModule: App.Instance is null");

        if (!m_pathLookup.TryGetValue(id, out string path) || string.IsNullOrEmpty(path))
        {
            Debug.Assert(false, $"ResourceLoaderModule: No path configured for ConfigId {id}");
            return null;
        }

        T asset = Resources.Load<T>(path);
        Debug.Assert(asset != null, $"ResourceLoaderModule: Failed to load config {typeof(T).Name} at path '{path}'");

        return asset;
    }

    void Awake()
    {
        if (m_loadingScreenPrefab == null)
        {
            Debug.LogError("LoadingScreenModule: no prefab assigned.");
            return;
        }

        GameObject instance = Instantiate(m_loadingScreenPrefab, transform);
        m_controller = instance.GetComponent<LoadingScreenController>();

        if (m_controller == null)
            Debug.LogError("LoadingScreenModule: prefab has no LoadingScreenController.");
    }

    // Called only by ResourceLoaderModule

    public void Show(string message = "CONNECTING TO BATTLEFIELD...")
    {
        if (m_controller != null)
            m_controller.Show(message);
    }

    public void SetProgress(float value)
    {
        if (m_controller != null)
            m_controller.SetProgress(value);
    }

    public Coroutine PlayClearAndHide()
    {
        if (m_controller == null)
            return null;

        return StartCoroutine(m_controller.PlayClearAndHide());
    }
}
