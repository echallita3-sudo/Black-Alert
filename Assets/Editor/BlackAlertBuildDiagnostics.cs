using System.IO;
using UnityEditor;
using UnityEngine;

namespace BlackAlert.Editor
{
    /// <summary>
    /// Runs automatically when Unity reloads editor assemblies, including Unity Build Automation.
    /// Reports evidence that the expected gameplay boot scene is configured. This is
    /// diagnostic only and does not trigger a build or consume additional cloud minutes.
    /// </summary>
    public static class BlackAlertBuildDiagnostics
    {
        private const string ScenePath = "Assets/Scenes/Entry.unity";

        [InitializeOnLoadMethod]
        public static void ReportProjectHealth()
        {
            Debug.Log("[BLACK ALERT PREFLIGHT] Unity editor: " + Application.unityVersion);
            Debug.Log("[BLACK ALERT PREFLIGHT] Platform: " + EditorUserBuildSettings.activeBuildTarget);

            if (!File.Exists(ScenePath))
            {
                Debug.LogError("[BLACK ALERT PREFLIGHT] Missing required entry scene: " + ScenePath);
                return;
            }

            bool sceneEnabled = false;
            foreach (var entry in EditorBuildSettings.scenes)
            {
                if (entry.enabled && entry.path == ScenePath)
                {
                    sceneEnabled = true;
                    break;
                }
            }

            if (!sceneEnabled)
                Debug.LogError("[BLACK ALERT PREFLIGHT] Entry scene is not enabled in Build Settings: " + ScenePath);
            else
                Debug.Log("[BLACK ALERT PREFLIGHT] Entry scene enabled: " + ScenePath);

            var bootstrap = System.Type.GetType("BlackAlert.BlackAlertBootstrap, Assembly-CSharp");
            var prototype = System.Type.GetType("BlackAlert.BattlefieldPrototype, Assembly-CSharp");
            var factory = System.Type.GetType("BlackAlert.BlackAlertModelFactory, Assembly-CSharp");

            Debug.Log("[BLACK ALERT PREFLIGHT] Assemblies: bootstrap=" + (bootstrap != null) +
                      ", battlefield=" + (prototype != null) +
                      ", model-factory=" + (factory != null));

            if (bootstrap == null || prototype == null || factory == null)
                Debug.LogWarning("[BLACK ALERT PREFLIGHT] A gameplay class was not resolved. Review compiler messages earlier in the Unity log.");

            Debug.Log("[BLACK ALERT PREFLIGHT] Structural preflight finished. NOT an APK runtime test.");
        }

        [MenuItem("Black Alert/Check Project Readiness")]
        public static void CheckProjectReadiness()
        {
            ReportProjectHealth();
        }
    }
}
