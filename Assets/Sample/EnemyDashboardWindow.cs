using NexEditor.ScriptableDashboard.Editor;
using System;
using UnityEditor;
using UnityEngine;

public class EnemyDashboardWindow : DashboardWindow<EnemyData>
{
    [MenuItem("Window/Scriptable Dashboard")]
    public static EnemyDashboardWindow ShowWindow()
    {
        var window = GetWindow<EnemyDashboardWindow>();
        window.titleContent = new GUIContent("Enemy Dashboard");
        window.Show();

        return window;
    }
}
