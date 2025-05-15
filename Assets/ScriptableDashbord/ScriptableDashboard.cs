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
            // Undo�̊J�n�i�e�A�Z�b�g�̕ύX�j
            Undo.RecordObject(this, "Update Dashboard");
#endif
            // �R���N�V�����ɒǉ�
            _collection.Add(instance);

#if UNITY_EDITOR
            // �T�u�A�Z�b�g�Ƃ��ēo�^
            AssetDatabase.AddObjectToAsset(instance, this);

            // �T�u�A�Z�b�g�ǉ���Undo�ɓo�^
            Undo.RegisterCreatedObjectUndo(instance, "Create Asset");

            // �ۑ�
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
            return instance;
        }

        public void Delete(T data)
        {
#if UNITY_EDITOR
            // �A�Z�b�g�̕ύX��Undo�ɓo�^
            Undo.RecordObject(this, "Delete Asset From Dashboard");
#endif
            // �e�̃��X�g����폜
            _collection.Remove(data);

#if UNITY_EDITOR
            // �T�u�A�Z�b�g�폜��Undo�ɓo�^
            Undo.DestroyObjectImmediate(data);

            // �ۑ�
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }
    }
}