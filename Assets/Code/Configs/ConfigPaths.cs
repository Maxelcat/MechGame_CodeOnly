using System;
using UnityEngine;

public enum ConfigId
{
    Input,
    TacticalCamera,
    UI,
    Gameplay
    // Add more as needed
}


[CreateAssetMenu(menuName = "Config/Config Path Map", fileName = "ConfigPaths")]
public class ConfigPaths : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public ConfigId Id;
        public string ResourcesPath; // e.g. "Configs/InputConfig"
    }

    public Entry[] Entries;
    
    public string GetPath(ConfigId id)
    {
        for (int i = 0; i < Entries.Length; i++)
        {
            if (Entries[i].Id == id)
            {
                return Entries[i].ResourcesPath;
            }
        }

        Debug.Assert(false, $"ConfigPaths: No path mapped for ConfigId {id}");
        return null;
    }
}
