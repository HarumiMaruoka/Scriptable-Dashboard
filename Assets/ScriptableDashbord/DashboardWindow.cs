using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NexEditor.ScriptableDashboard.Editor
{
    public class DashboardWindow<DataType> : EditorWindow where DataType : ScriptableObject
    {
        private SortedSet<int> selectedIndices = new SortedSet<int>();
        private int lastClickedIndex = -1; // Shift用の起点
        private Vector2 scroll;
        private ScriptableDashboard<DataType> dashboard;

        private List<float> columnWidths = new List<float>();
        private int resizingColumn = -1;
        private float dragStartX, dragStartWidth;

        private int dragSourceIndex = -1;
        private int dragTargetIndex = -1;

        private int sortColumnIndex = -1; // -1は未選択
        private bool isAscending = true;  // trueなら昇順、falseなら降順

        private float temp; // デバッグ用。一時的な変数。
        private Color color = Color.white; // デバッグ用。一時的な変数。

        public void Init(ScriptableDashboard<DataType> dashboard)
        {
            this.dashboard = dashboard;
            titleContent = new GUIContent(dashboard.name);
            minSize = new Vector2(400, 300);
        }

        private void OnGUI()
        {
            // 編集するダッシュボードの設定
            var prev = dashboard;
            dashboard = (ScriptableDashboard<DataType>)EditorGUILayout.ObjectField("Dashboard", dashboard, typeof(ScriptableDashboard<DataType>), false);
            if (dashboard != prev)
            {
                Init(dashboard);
            }
            if (dashboard == null) return;

            EditorGUILayout.BeginHorizontal();

            DrawLeftMenu();

            bool ctrl = Event.current.control || Event.current.command;
            bool shift = Event.current.shift;

            if (Event.current.type == EventType.MouseDown && !ctrl && !shift)
            {
                // マウスが押されたときに、選択されている行をクリア
                selectedIndices.Clear();
                lastClickedIndex = -1;
                Repaint();
            }

            DrawGrid();

            // 要素の移動（ドラッグアンドドロップ）
            if (dragSourceIndex != -1)
            {
                switch (Event.current.type)
                {
                    case EventType.DragUpdated:
                        DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                        Event.current.Use();
                        break;

                    case EventType.DragPerform:
                    case EventType.MouseUp:
                        if (dragTargetIndex != -1 && dragTargetIndex != dragSourceIndex)
                        {
                            // 移動処理
                            dashboard.Move(dragSourceIndex, dragTargetIndex);
                            selectedIndices.Add(dragTargetIndex);
                            selectedIndices.Remove(dragSourceIndex);
                        }
                        dragSourceIndex = -1;
                        dragTargetIndex = -1;
                        DragAndDrop.AcceptDrag();
                        Event.current.Use();
                        break;

                    case EventType.DragExited:
                        dragSourceIndex = -1;
                        dragTargetIndex = -1;
                        Event.current.Use();
                        break;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawLeftMenu()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            if (GUILayout.Button("Add")) { dashboard.Create(); selectedIndices.Clear(); lastClickedIndex = -1; GUI.FocusControl(null); }
            if (GUILayout.Button("Insert")) { dashboard.CreateAndInsert(selectedIndices); selectedIndices.Clear(); lastClickedIndex = -1; GUI.FocusControl(null); }
            if (GUILayout.Button("Delete")) { dashboard.Delete(selectedIndices); selectedIndices.Clear(); lastClickedIndex = -1; GUI.FocusControl(null); }
            if (GUILayout.Button("Sort")) { /* ソート処理 */ }
            if (GUILayout.Button("Filter")) { /* フィルター処理 */ }

            temp = EditorGUILayout.Slider("Temp", temp, 0, 100);
            color = EditorGUILayout.ColorField("Color", color);
            if (dashboard != null) EditorGUILayout.LabelField($"Count: {dashboard.Count}");
            var mousePos = Event.current.mousePosition;
            EditorGUILayout.LabelField($"Mouse Position: {mousePos.x}, {mousePos.y}");

            EditorGUILayout.EndVertical();
        }

        void DrawGrid()
        {
            EditorGUILayout.BeginVertical();

            if (dashboard == null) return;
            if (dashboard.Count == 0) dashboard.Create();

            // カラム名・数取得
            var firstItem = new SerializedObject(dashboard[0]);
            var prop = firstItem.GetIterator();
            float leftSpace = 20;

            // ヘッダー初期化
            List<string> displayNames = new List<string>();
            List<string> fieldNames = new List<string>();
            if (prop.NextVisible(true))
            {
                do { displayNames.Add(prop.displayName); fieldNames.Add(prop.name); } while (prop.NextVisible(false));
            }
            if (columnWidths.Count != displayNames.Count)
            {
                columnWidths = new List<float>(new float[displayNames.Count]);
                for (int i = 0; i < columnWidths.Count; ++i) columnWidths[i] = 150;
            }

            // ヘッダー描画
            EditorGUILayout.BeginHorizontal();
            // 左側のスペースを確保
            GUILayout.Space(leftSpace);
            var headerRect = GUILayoutUtility.GetRect(0, 10000, 18, 18, GUILayout.ExpandWidth(true));
            float x = headerRect.x;
            for (int i = 0; i < displayNames.Count; i++)
            {
                var rect = new Rect(x + 3.5f, headerRect.y, columnWidths[i], headerRect.height);
                EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));

                if (GUI.Button(rect, displayNames[i], EditorStyles.boldLabel))
                {
                    GUI.FocusControl(null);
                    if (sortColumnIndex == i)
                    {
                        isAscending = !isAscending; // 同じ列なら順序反転
                    }
                    else
                    {
                        sortColumnIndex = i;
                        isAscending = true; // 新しい列なら昇順から
                    }

                    SortDashboardByColumn(sortColumnIndex, isAscending, fieldNames[i]);
                }

                // ドラッグハンドル
                var handleRect = new Rect(rect.xMax - 4, rect.y, 8, rect.height);
                EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeHorizontal);
                int id = GUIUtility.GetControlID(FocusType.Passive);
                if (Event.current.type == EventType.MouseDown && handleRect.Contains(Event.current.mousePosition))
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
            int index = 0;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.Space(8);
            foreach (var item in dashboard)
            {
                var so = new SerializedObject(item);
                var p = so.GetIterator();
                so.Update();
                if (p.NextVisible(true))
                {
                    Rect rowRect = EditorGUILayout.BeginHorizontal();

                    // 左側のスペースを確保
                    GUILayout.Space(leftSpace);
                    int idx = 0;
                    do
                    {
                        DrawPropertyCell(p, GUILayout.Width(columnWidths[idx++]));

                    } while (p.NextVisible(false));
                    EditorGUILayout.EndHorizontal();

                    // ハイライト用の矩形のサイズ調整
                    rowRect.y -= 3;
                    rowRect.height += 6;
                    rowRect.width += 8;
                    rowRect.xMin -= 4;

                    // 選択されている行をここでハイライト
                    if (selectedIndices.Contains(index))
                    {
                        EditorGUI.DrawRect(rowRect, new Color(0.24f, 0.48f, 0.90f, 0.1f));
                    }

                    // ドラッグ開始検知
                    if (Event.current.type == EventType.MouseDrag && rowRect.Contains(Event.current.mousePosition))
                    {
                        dragSourceIndex = index;
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new UnityEngine.Object[0];
                        DragAndDrop.StartDrag("DraggingRow");
                        Event.current.Use();
                    }

                    // 挿入位置（黄色のライン）表示
                    DrawInsertionLine(index, ref rowRect);

                    // 行のクリック処理
                    if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
                    {
                        bool ctrl = Event.current.control || Event.current.command;
                        bool shift = Event.current.shift;

                        if (shift && lastClickedIndex != -1)
                        {
                            // 範囲選択
                            int min = Mathf.Min(lastClickedIndex, index);
                            int max = Mathf.Max(lastClickedIndex, index);
                            selectedIndices.Clear();
                            for (int i = min; i <= max; i++) selectedIndices.Add(i);
                        }
                        else if (ctrl)
                        {
                            // トグル選択
                            if (selectedIndices.Contains(index)) selectedIndices.Remove(index);
                            else selectedIndices.Add(index);
                            lastClickedIndex = index;
                        }
                        else
                        {
                            // 単一選択
                            selectedIndices.Clear();
                            selectedIndices.Add(index);
                            lastClickedIndex = index;
                        }

                        Repaint();
                    }
                }
                so.ApplyModifiedProperties();
                EditorGUILayout.Space(3);
                index++;
            }

            // リスト末尾の処理（最後尾への挿入）
            Rect lastRowRect = GUILayoutUtility.GetLastRect();
            if (dragSourceIndex != -1 && Event.current.mousePosition.y > lastRowRect.yMax)
            {
                dragTargetIndex = dashboard.Count;
                Rect lineRect = new Rect(lastRowRect.x, lastRowRect.yMax + 1, lastRowRect.width, 4);
                EditorGUI.DrawRect(lineRect, Color.yellow);
            }

            EditorGUILayout.Space(8);
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

        void DrawInsertionLine(int index, ref Rect rowRect)
        {
            if (index != dragSourceIndex && dragSourceIndex != -1 && rowRect.Contains(Event.current.mousePosition))
            {
                float lineY = rowRect.y;
                if (Event.current.mousePosition.y > rowRect.y + rowRect.height / 2) // 下半分
                {
                    if (dragSourceIndex == index + 1) return;
                    dragTargetIndex = index + 1;
                    lineY += rowRect.height;
                }
                else // 上半分
                {
                    if (dragSourceIndex == index - 1) return;
                    dragTargetIndex = index;
                }

                // 黄色ライン描画
                Rect lineRect = new Rect(rowRect.x, lineY - 2, rowRect.width, 2);
                EditorGUI.DrawRect(lineRect, Color.yellow);
            }
        }

        void SortDashboardByColumn(int columnIndex, bool ascending, string fieldName)
        {
            dashboard.Sort((a, b) =>
            {
                var type = typeof(DataType);
                var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                if (field == null) return 0;

                var valueA = field.GetValue(a);
                var valueB = field.GetValue(b);

                int result = Comparer<object>.Default.Compare(valueA, valueB);

                return ascending ? result : -result;
            });

            Repaint();
        }

    }
}