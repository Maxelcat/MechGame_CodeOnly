using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CodeSyncUtility
{
    // Menu item: Tools → Sync Code To Git Repo
    [MenuItem("Tools/Sync Code To Git Repo")]
    public static void SyncCodeToGitRepo()
    {
        // Project root is the parent of the Assets folder
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;

        // Our PowerShell script location (ProjectRoot/Tools/Sync-UnityCodeToGit.ps1)
        string scriptPath = Path.Combine(projectRoot, "Tools", "Sync-UnityCodeToGit.ps1");

        if (!File.Exists(scriptPath))
        {
            UnityEngine.Debug.LogError("Sync script not found at: " + scriptPath);
            return;
        }

        // Build PowerShell arguments
        string arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"";

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        try
        {
            using (var process = Process.Start(psi))
            {
                if (process == null)
                {
                    UnityEngine.Debug.LogError("Failed to start PowerShell process.");
                    return;
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrWhiteSpace(output))
                    UnityEngine.Debug.Log("[Code Sync] " + output);

                if (!string.IsNullOrWhiteSpace(error))
                    UnityEngine.Debug.LogError("[Code Sync ERROR] " + error);
                else
                    UnityEngine.Debug.Log("[Code Sync] Finished with exit code " + process.ExitCode);
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError("Exception while running sync script: " + ex.Message);
        }
    }
}
