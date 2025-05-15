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

        private List<float> columnWidths = new List<float>();
        private int resizingColumn = -1;
        private float dragStartX, dragStartWidth;

        private float temp; // デバッグ用。一時的な変数。

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

            temp = EditorGUILayout.Slider("Temp", temp, 0, 100);

            EditorGUILayout.EndVertical();
        }

        Rect? selectedRect = null;

        void DrawGrid()
        {
            EditorGUILayout.BeginVertical();

            if (dashboard == null) return;
            if (dashboard.Count == 0) dashboard.Create();

            // カラム名・数取得
            var firstItem = new SerializedObject(dashboard[0]);
            var prop = firstItem.GetIterator();

            // ヘッダー初期化
            List<string> fieldNames = new List<string>();
            if (prop.NextVisible(true))
            {
                do { fieldNames.Add(prop.displayName); } while (prop.NextVisible(false));
            }
            if (columnWidths.Count != fieldNames.Count)
            {
                columnWidths = new List<float>(new float[fieldNames.Count]);
                for (int i = 0; i < columnWidths.Count; ++i) columnWidths[i] = 150;
            }

            // ヘッダー描画
            EditorGUILayout.BeginHorizontal();
            var headerRect = GUILayoutUtility.GetRect(0, 10000, 18, 18, GUILayout.ExpandWidth(true));
            float x = headerRect.x;
            for (int i = 0; i < fieldNames.Count; i++)
            {
                var rect = new Rect(x + 3.5f, headerRect.y, columnWidths[i], headerRect.height);
                EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
                GUI.Label(rect, fieldNames[i], EditorStyles.boldLabel);

                // ドラッグハンドル
                var handleRect = new Rect(rect.xMax - 4, rect.y, 8, rect.height);
                EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeHorizontal);
                int id = GUIUtility.GetControlID(FocusType.Passive);
                if (Event.current.type == EventType.MouseUp && handleRect.Contains(Event.current.mousePosition))
                {
                    resizingColumn = i;
                    dragStartX = Event.current.mousePosition.x;
                    dragStartWidth = columnWidths[i];
                    Event.current.Use();
                }
                if (resizingColumn == i && Event.current.type == EventType.MouseDrag)
                {
                    float delta = Event.current.mousePosition.x - dragStartX;
                    columnWidths[i] = Mathf.Max(40, dragStartWidth + delta);
                    Event.current.Use();
                    Repaint();
                }
                if (resizingColumn == i && Event.current.type == EventType.MouseUp)
                {
                    resizingColumn = -1;
                    Event.current.Use();
                }

                x += columnWidths[i] + 3f; // 3fはカラム間のスペース
            }
            EditorGUILayout.EndHorizontal();

            // データ描画
            if (selectedRect != null)
            {
                Rect adjusted = selectedRect.Value;
                adjusted.x += 200 + 8;   // 左メニューの幅
                adjusted.y += 18 - 3;    // ヘッダーの高さ
                adjusted.y -= scroll.y; //スクロールのオフセット

                EditorGUI.DrawRect(adjusted, new Color(0.24f, 0.48f, 0.90f, 0.3f));
            }

            // データ描画
            int index = 0;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var item in dashboard)
            {
                var so = new SerializedObject(item);
                var p = so.GetIterator();
                so.Update();
                if (p.NextVisible(true))
                {
                    var rowRect = EditorGUILayout.BeginHorizontal();
                    int idx = 0;
                    do
                    {
                        DrawPropertyCell(p, GUILayout.Width(columnWidths[idx++]));
                    } while (p.NextVisible(false));
                    EditorGUILayout.EndHorizontal();

                    rowRect.height += 6;
                    if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
                    {
                        selectedIndex = index;
                        selectedRect = rowRect;

                        Repaint();
                    }
                }
                so.ApplyModifiedProperties();
                EditorGUILayout.Space(3);
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