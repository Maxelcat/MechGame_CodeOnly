using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CodeSyncUtility
{
    [MenuItem("Tools/Sync Code To Git Repo")]
    public static void SyncCodeToGitRepo()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string scriptPath = Path.Combine(projectRoot, "Tools", "Sync-UnityCodeToGit.ps1");

        if (!File.Exists(scriptPath))
        {
            UnityEngine.Debug.LogError("Sync script not found at: " + scriptPath);
            return;
        }

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

                bool success = process.ExitCode == 0;

                if (!string.IsNullOrWhiteSpace(output))
                    UnityEngine.Debug.Log("[Code Sync] " + output);

                if (!string.IsNullOrWhiteSpace(error))
                {
                    if (success)
                    {
                        // Git often writes normal progress to stderr, so just log it as info
                        UnityEngine.Debug.Log("[Code Sync git] " + error);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"[Code Sync ERROR] ExitCode {process.ExitCode}\n{error}");
                    }
                }

                if (success)
                {
                    UnityEngine.Debug.Log("[Code Sync] Finished successfully (exit code " + process.ExitCode + ")");
                }
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError("Exception while running sync script: " + ex.Message);
        }
    }
}
