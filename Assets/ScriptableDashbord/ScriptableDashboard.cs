using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NexEditor
{
    public class ScriptableDashboard<T> : ScriptableObject, IEnumerable<T> where T : ScriptableObject
    {
        [SerializeField]
        private List<T> _collection;
        public IReadOnlyList<T> Collection => _collection ??= new List<T>();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        public T this[int i] => _collection[i];

        public int Count => _collection.Count;

        public T Create()
        {
            var instance = CreateInstance<T>();

#if UNITY_EDITOR
            // Undoの開始（親アセットの変更）
            Undo.RecordObject(this, "Update Dashboard");
#endif
            // コレクションに追加
            _collection.Add(instance);

#if UNITY_EDITOR
            // サブアセットとして登録
            AssetDatabase.AddObjectToAsset(instance, this);

            // サブアセット追加をUndoに登録
            Undo.RegisterCreatedObjectUndo(instance, "Create Asset");

            // 保存
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
            return instance;
        }

        public void Delete(T data)
        {
#if UNITY_EDITOR
            // アセットの変更をUndoに登録
            Undo.RecordObject(this, "Delete Asset From Dashboard");
#endif
            // 親のリストから削除
            _collection.Remove(data);

#if UNITY_EDITOR
            // サブアセット削除をUndoに登録
            Undo.DestroyObjectImmediate(data);

            // 保存
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }
    }
}