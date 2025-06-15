using NexEditor;
using System;
using UnityEditor;
using UnityEngine;

public class EnemyDashboardWindow2 : ScriptableDashboardEditor<EnemyData>
{
    public static void ShowWindow(EnemyDashboard dashboard)
    {
        EnemyDashboardWindow2 window = GetWindow<EnemyDashboardWindow2>();
        window.Setup(dashboard);
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(EnemyDashboard))]
public class EnemySheetDrawer : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Open Window"))
        {
            var dashboard = target as EnemyDashboard;
            if (dashboard != null)
            {
                EnemyDashboardWindow2.ShowWindow(dashboard);
            }
        }

        base.OnInspectorGUI();
    }

    [UnityEditor.Callbacks.OnOpenAsset]
    public static bool OnOpenAsset(int instanceId, int line)
    {
        if (UnityEditor.Selection.activeObject is EnemyDashboard enemyDashboard)
        {
            EnemyDashboardWindow2.ShowWindow(enemyDashboard);
            return true;
        }
        return false;
    }
}
#endif