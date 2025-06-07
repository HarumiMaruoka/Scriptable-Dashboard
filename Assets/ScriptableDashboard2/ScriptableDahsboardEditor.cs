using NexEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class ScriptableDahsboardEditor<DataType> : EditorWindow where DataType : ScriptableObject
{
    [SerializeField]
    protected VisualTreeAsset visualTreeAsset = default;
    [SerializeField]
    protected StyleSheet horizontalResizeStyleSheet = default;

    private ScriptableDashboard<DataType> dashboard;
    private Dictionary<DataType, SerializedObject> serializedObjects = new Dictionary<DataType, SerializedObject>();

    private ListView listView;
    private TextField filterField;
    private VisualElement headerRow;

    // フィールド情報
    List<string> displayNames = new List<string>();
    List<string> fieldNames = new List<string>();
    private FieldInfo[] fieldInfos;
    private Dictionary<string, FieldInfo> fieldNameToInfo = new Dictionary<string, FieldInfo>();

    // 列幅
    private List<float> columnWidths = new List<float>();
    private int resizingColumn = -1;
    private float dragStartX, dragStartWidth;

    // ソート
    private int sortColumnIndex = -1; // -1は未選択
    private bool isAscending = true;  // trueなら昇順、falseなら降順

    public void Init(ScriptableDashboard<DataType> dashboard)
    {
        this.dashboard = dashboard;
        ReloadList();
        serializedObjects = new Dictionary<DataType, SerializedObject>();
        RefreshSerializedObjects();
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        visualTreeAsset.CloneTree(root);

        listView = rootVisualElement.Q<ListView>("DashboardList");
        filterField = rootVisualElement.Q<TextField>("FilterField");
        headerRow = rootVisualElement.Q<VisualElement>("HeaderRow");

        // CreateGUI() で listView を取得した直後など
        listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;

        InitializeFieldInfos(displayNames);
        CreateHeaderButtons();
        RefreshSerializedObjects();

        // Dashboardのセットアップやバインド
        ReloadList();

        // フィルタ変更時
        //filterField.RegisterValueChangedCallback(e => ReloadList());

        // 選択・削除・追加ボタンもrootVisualElement.Q<Button>("AddButton")等で取得してイベント登録

    }

    // フィールド情報の初期化
    private void InitializeFieldInfos(List<string> displayNames)
    {
        displayNames.Clear();
        var type = typeof(DataType);
        fieldInfos = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        foreach (var f in fieldInfos)
        {
            fieldNames.Add(f.Name);
            displayNames.Add(ObjectNames.NicifyVariableName(f.Name));
            fieldNameToInfo[f.Name] = f;
        }
    }

    // ヘッダー行の作成
    private void CreateHeaderButtons()
    {
        headerRow.Clear();
        for (int i = 0; i < displayNames.Count; ++i)
        {
            var idx = i; // クロージャ対策

            var headerCell = new VisualElement();
            headerCell.style.flexDirection = FlexDirection.Row;
            if (columnWidths.Count > idx)
            {
                headerCell.style.width = columnWidths[idx];
            }
            else
            {
                columnWidths.Add(100); // デフォルト幅
                headerCell.style.width = 100;
            }
            headerCell.style.flexGrow = 0;
            headerCell.style.flexShrink = 0;

            var sortBtn = new Button(/*() => OnSortColumn(idx)*/) { text = displayNames[i] };
            sortBtn.style.flexGrow = 1;
            sortBtn.style.flexShrink = 1;
            headerCell.Add(sortBtn);

            // リサイズハンドル
            var resizeHandle = new VisualElement();
            resizeHandle.style.width = 6;
            resizeHandle.style.minWidth = 6;
            resizeHandle.style.maxWidth = 6;
            resizeHandle.style.backgroundColor = new Color(0.7f, 0.7f, 0.7f, 0.4f);
            resizeHandle.styleSheets.Add(horizontalResizeStyleSheet);
            resizeHandle.RegisterCallback<MouseDownEvent>(e =>
            {
                resizingColumn = idx;
                dragStartX = e.mousePosition.x;
                dragStartWidth = columnWidths[idx];
                e.StopPropagation();
            });
            rootVisualElement.RegisterCallback<MouseMoveEvent>(e =>
            {
                if (resizingColumn == idx && e.pressedButtons == 1)
                {
                    float delta = e.mousePosition.x - dragStartX;
                    columnWidths[idx] = Mathf.Max(40, dragStartWidth + delta);
                    headerCell.style.width = columnWidths[idx];
                    listView.RefreshItems();
                }
            });
            rootVisualElement.RegisterCallback<MouseUpEvent>(e =>
            {
                resizingColumn = -1;
            });

            headerCell.Add(resizeHandle);
            headerRow.Add(headerCell);
        }
    }

    // リスト内の要素
    private void ReloadList()
    {
        if (dashboard == null) return;

        var items = string.IsNullOrEmpty(filterField?.value)
            ? dashboard.Collection
            : dashboard.Collection.Where(x => x.name.Contains(filterField.value)).ToList();

        listView.itemsSource = items;
        listView.makeItem = () =>
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            for (int col = 0; col < displayNames.Count; ++col)
            {
                var pf = new PropertyField
                {
                    name = $"pf_{col}", // 後から取り出す用
                };
                pf.label = ""; // ラベルはヘッダーにあるので消す
                pf.style.width = columnWidths[col];
                pf.style.flexShrink = 0;
                row.Add(pf);
            }
            return row;
        };

        listView.bindItem = (ve, itemIndex) =>
        {
            var row = (VisualElement)ve;
            var data = (DataType)listView.itemsSource[itemIndex];   // その行の ScriptableObject
            var so = serializedObjects[data];                     // キャッシュ済み SerializedObject
            so.Update();                                             // 念のため

            for (int col = 0; col < displayNames.Count; ++col)
            {
                var pf = (PropertyField)row.ElementAt(col);
                var prop = so.FindProperty(fieldNames[col]);
                // PropertyField には *必ず Unbind → BindProperty* でバインドし直す
                pf.Unbind();
                pf.BindProperty(prop);
                pf.style.width = columnWidths[col];
            }
        }; 

        listView.selectionType = SelectionType.Multiple;
    }

    void RefreshSerializedObjects()
    {
        if (dashboard == null) return;

        // 既存キーセットを取得
        var keys = new HashSet<DataType>(serializedObjects.Keys);

        // Collectionにあるものを全てキャッシュ、なければ新規作成
        foreach (var obj in dashboard.Collection)
        {
            if (!serializedObjects.ContainsKey(obj))
            {
                serializedObjects[obj] = new SerializedObject(obj);
            }
            keys.Remove(obj); // 生きてるものは消していく
        }

        // Collectionに存在しないキャッシュは削除
        foreach (var obj in keys)
        {
            serializedObjects.Remove(obj);
        }
    }
}
