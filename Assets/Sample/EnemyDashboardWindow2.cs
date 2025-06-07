using NexEditor;
using System;
using UnityEditor;
using UnityEngine;

public class EnemyDashboardWindow2 : ScriptableDahsboardEditor<EnemyData>
{
    public static void ShowWindow(ScriptableDashboard<EnemyData> dashboard)
    {
        EnemyDashboardWindow2 wnd = GetWindow<EnemyDashboardWindow2>();
        wnd.Init(dashboard);
        wnd.titleContent = new GUIContent("ScriptableDahsboardEditor");
    }
}
