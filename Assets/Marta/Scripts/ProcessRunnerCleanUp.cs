/*#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VRBuilder.Core;
using System;

[InitializeOnLoad]
public static class ProcessRunnerCleanup
{
    static ProcessRunnerCleanup()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        UnityEngine.Debug.Log("[ProcessRunnerCleanup] Initialized.");
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            try
            {
                // 1️⃣ Deactivate current process
                if (ProcessRunner.Current != null)
                {
                    try
                    {
                        ProcessRunner.Current.LifeCycle.Deactivate();
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning("[Cleanup] Error deactivating process: " + ex);
                    }
                }

                // 2️⃣ Clear ProcessRunner.instance (private static)
                var instanceField = typeof(ProcessRunner)
                    .GetField("instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

                if (instanceField != null)
                {
                    instanceField.SetValue(null, null);
                    UnityEngine.Debug.Log("[Cleanup] Cleared ProcessRunner.instance");
                }

                // 3️⃣ Clear ProcessRunner.events (private static)
                var eventsField = typeof(ProcessRunner)
                    .GetField("events", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

                if (eventsField != null)
                {
                    eventsField.SetValue(null, null);
                    UnityEngine.Debug.Log("[Cleanup] Cleared ProcessRunner.events");
                }

                // 4️⃣ Force GC (solo debug!)
                GC.Collect();
                GC.WaitForPendingFinalizers();

                UnityEngine.Debug.Log("[Cleanup] Memory cleanup completed after exiting Play Mode!");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[Cleanup] FAILURE: " + e);
            }
        }
    }
}
#endif*/
