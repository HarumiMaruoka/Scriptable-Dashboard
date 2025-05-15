using System;
using UnityEngine;
using UnityEditor;
using NexEditor;
using NexEditor.ScriptableDashboard.Editor;

[CreateAssetMenu(fileName = "EnemyDashboard", menuName = "Sample/EnemyDashboard")]
public class EnemyDashboard : ScriptableDashboard<EnemyData>
{

}

[CustomEditor(typeof(EnemyDashboard))]
public class EnemyDashboardEditor : Editor
{
    private int index = 0;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("OpenEditWindow"))
        {
            var window = EnemyDashboardWindow.ShowWindow();
            window.Init((EnemyDashboard)target);
        }

        if (GUILayout.Button("Create Enemy"))
        {
            var enemy = ((EnemyDashboard)target).Create();
            enemy.name = "New Enemy";
            EditorUtility.SetDirty(enemy);
        }

        index = EditorGUILayout.IntSlider("Index", index, 0, ((EnemyDashboard)target).Count - 1);

        if (GUILayout.Button("Delete Enemy"))
        {
            var enemy = ((EnemyDashboard)target)[index];
            ((EnemyDashboard)target).Delete(enemy);
            EditorUtility.SetDirty(target);
        }
    }
}