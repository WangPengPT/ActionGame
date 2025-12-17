using UnityEngine;
using UnityEditor;
using UIDataBinding;

namespace UIDataBindingEditor
{
    /// <summary>
    /// UIDataSource组件的自定义编辑器
    /// </summary>
    [CustomEditor(typeof(UIDataSource))]
    public class UIDataSourceEditor : Editor
    {
        private SerializedProperty _unityObjectSourceProp;
        private SerializedProperty _updateEveryFrameProp;
        private SerializedProperty _updateIntervalProp;

        private void OnEnable()
        {
            _unityObjectSourceProp = serializedObject.FindProperty("_unityObjectSource");
            _updateEveryFrameProp = serializedObject.FindProperty("updateEveryFrame");
            _updateIntervalProp = serializedObject.FindProperty("updateInterval");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("数据源设置", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_unityObjectSourceProp, new GUIContent("数据源对象", "设置一个Unity对象作为数据源"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("更新设置", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_updateEveryFrameProp, new GUIContent("每帧更新", "是否每帧检测数据变化"));

            if (_updateEveryFrameProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_updateIntervalProp, new GUIContent("更新间隔", "检测间隔（秒），0表示每帧检测"));
                EditorGUI.indentLevel--;
            }

            // 显示子物体上的显示组件数量
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("信息", EditorStyles.boldLabel);

            var source = target as UIDataSource;
            if (source != null)
            {
                var displays = source.GetComponentsInChildren<UIDataDisplay>(true);
                EditorGUILayout.LabelField($"显示组件数量: {displays.Length}");
            }

            // 操作按钮
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("刷新显示"))
            {
                if (Application.isPlaying)
                {
                    var dataSource = target as UIDataSource;
                    if (dataSource != null)
                    {
                        dataSource.ForceRefresh();
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "只能在运行时刷新显示", "确定");
                }
            }

            if (GUILayout.Button("查找显示组件"))
            {
                var dataSource = target as UIDataSource;
                if (dataSource != null)
                {
                    var displays = dataSource.GetComponentsInChildren<UIDataDisplay>(true);
                    if (displays.Length > 0)
                    {
                        Selection.objects = new Object[displays.Length];
                        for (int i = 0; i < displays.Length; i++)
                        {
                            Selection.objects[i] = displays[i].gameObject;
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("提示", "没有找到显示组件", "确定");
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
}

