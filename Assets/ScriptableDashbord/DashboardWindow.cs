using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NexEditor.ScriptableDashboard.Editor
{
    public class DashboardWindow<DataType> : EditorWindow where DataType : ScriptableObject
    {
        private int selectedIndex = -1;
        private Vector2 scroll;
        private ScriptableDashboard<DataType> dashboard;

        public void Init(ScriptableDashboard<DataType> dashboard)
        {
            this.dashboard = dashboard;
            titleContent = new GUIContent(dashboard.name);
            minSize = new Vector2(400, 300);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            DrawLeftMenu();
            DrawGrid();

            EditorGUILayout.EndHorizontal();
        }

        void DrawLeftMenu()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            if (GUILayout.Button("Add")) { dashboard.Create(); }
            if (GUILayout.Button("Insert")) { /* インサート処理 */ }
            if (GUILayout.Button("Delete")) { /* 削除処理 */ }
            if (GUILayout.Button("Sort")) { /* ソート処理 */ }
            if (GUILayout.Button("Filter")) { /* フィルター処理 */ }
            EditorGUILayout.EndVertical();
        }

        void DrawGrid()
        {
            EditorGUILayout.BeginVertical();

            if (dashboard == null) return;

            if (dashboard.Count == 0)
            {
                dashboard.Create();
            }

            var firstItem = new SerializedObject(dashboard[0]);
            var prop = firstItem.GetIterator();
            if (prop.NextVisible(true))
            {
                EditorGUILayout.BeginHorizontal();
                do
                {
                    EditorGUILayout.LabelField(prop.displayName, GUILayout.Width(150));
                } while (prop.NextVisible(false));
                EditorGUILayout.EndHorizontal();
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            foreach (var item in dashboard)
            {
                var so = new SerializedObject(item);

                var p = so.GetIterator();
                so.Update();
                if (p.NextVisible(true))
                {
                    EditorGUILayout.BeginHorizontal();
                    do
                    {
                        DrawPropertyCell(p, GUILayout.Width(150));
                    } while (p.NextVisible(false));
                    EditorGUILayout.EndHorizontal();
                }
                so.ApplyModifiedProperties();

                EditorGUILayout.Space(3); // 行間のスペース
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawPropertyCell(SerializedProperty prop, params GUILayoutOption[] options)
        {
            if (prop.propertyType == SerializedPropertyType.String)
            {
                var field = prop.serializedObject.targetObject.GetType().GetField(prop.name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                {
                    if (Attribute.IsDefined(field, typeof(TextAreaAttribute)))
                    {
                        prop.stringValue = EditorGUILayout.TextArea(prop.stringValue, options);
                    }
                    else
                    {
                        prop.stringValue = EditorGUILayout.TextField(prop.stringValue, options);
                    }
                }
            }
            else if (prop.propertyType == SerializedPropertyType.ObjectReference &&
                     prop.objectReferenceValue is Sprite sprite)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(50));
                EditorGUILayout.PropertyField(prop, GUIContent.none, options);
                var tex = AssetPreview.GetAssetPreview(sprite);
                GUILayout.Label(tex, GUILayout.Width(50), GUILayout.Height(50));
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.PropertyField(prop, GUIContent.none, options);
            }
        }
    }
}