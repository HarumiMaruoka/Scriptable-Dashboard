#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using NexEditor.ScriptableDashboard;

namespace Enemy
{
    public class EnemyDashboardWindow : ScriptableDashboardEditor<EnemyData>
    {
        [MenuItem("Window/Scriptable Dashboard/EnemyDashboardWindow")]
        public static void ShowWindow()
        {
            EnemyDashboardWindow window = GetWindow<EnemyDashboardWindow>();
            string[] guids = AssetDatabase.FindAssets("t:EnemyDashboard");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var dashboard = AssetDatabase.LoadAssetAtPath<EnemyDashboard>(path);
                if (dashboard != null)
                {
                    window.Setup(dashboard);
                }
                else
                {
                    Debug.LogWarning("Sample2Dashboard not found at path: " + path);
                }
            }
            else
            {
                Debug.LogWarning("No Sample2Dashboard found in the project.");
            }
        }
        public static void ShowWindow(EnemyDashboard dashboard)
        {
            EnemyDashboardWindow window = GetWindow<EnemyDashboardWindow>();
            window.Setup(dashboard);
        }
    }

    [UnityEditor.CustomEditor(typeof(EnemyDashboard))]
    public class EnemyDashboardDrawer : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Open Window"))
            {
                var dashboard = target as EnemyDashboard;
                if (dashboard != null)
                {
                    EnemyDashboardWindow.ShowWindow(dashboard);
                }
            }
        }

        [UnityEditor.Callbacks.OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            if (UnityEditor.Selection.activeObject is EnemyDashboard dashboard)
            {
                EnemyDashboardWindow.ShowWindow(dashboard);
                return true;
            }
            return false;
        }
    }
}
#endif