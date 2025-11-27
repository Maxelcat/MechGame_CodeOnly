using System.Collections.Generic;
using UnityEngine;

public class ResourceLoaderModule
{
    private readonly ConfigPaths m_configPaths;
    private readonly Dictionary<ConfigId, string> m_pathLookup =
        new Dictionary<ConfigId, string>();

    public ResourceLoaderModule(ConfigPaths configPaths)
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
}
