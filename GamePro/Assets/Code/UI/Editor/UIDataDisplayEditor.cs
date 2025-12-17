using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UIDataBinding;

namespace UIDataBindingEditor
{
    /// <summary>
    /// UIDataDisplay组件的自定义编辑器
    /// 提供类名、成员变量、显示方法的下拉选择
    /// </summary>
    [CustomEditor(typeof(UIDataDisplay))]
    public class UIDataDisplayEditor : Editor
    {
        private SerializedProperty _dataClassNameProp;
        private SerializedProperty _memberNameProp;
        private SerializedProperty _displayMethodNameProp;
        private SerializedProperty _targetUIComponentProp;

        private string[] _availableTypeNames;
        private string[] _availableMemberNames;
        private string[] _availableMethodNames;

        private int _selectedTypeIndex = -1;
        private int _selectedMemberIndex = -1;
        private int _selectedMethodIndex = -1;

        private bool _needsRefresh = true;

        private void OnEnable()
        {
            _dataClassNameProp = serializedObject.FindProperty("dataClassName");
            _memberNameProp = serializedObject.FindProperty("memberName");
            _displayMethodNameProp = serializedObject.FindProperty("displayMethodName");
            _targetUIComponentProp = serializedObject.FindProperty("targetUIComponent");

            RefreshTypeLists();
        }

        private void RefreshTypeLists()
        {
            // 获取所有可用类型
            var typeNames = new List<string> { "(None)" };
            typeNames.AddRange(UIDataDisplay.GetAvailableTypeNames());
            _availableTypeNames = typeNames.ToArray();

            // 获取所有显示方法
            var methodNames = new List<string>();
            methodNames.AddRange(UIDisplay.GetDisplayMethodNames());
            _availableMethodNames = methodNames.ToArray();

            // 更新选中索引
            UpdateSelectedIndices();
        }

        private void UpdateSelectedIndices()
        {
            // 类型索引
            string currentType = _dataClassNameProp.stringValue;
            _selectedTypeIndex = 0;
            if (!string.IsNullOrEmpty(currentType))
            {
                for (int i = 0; i < _availableTypeNames.Length; i++)
                {
                    if (_availableTypeNames[i] == currentType)
                    {
                        _selectedTypeIndex = i;
                        break;
                    }
                }
            }

            // 刷新成员列表
            RefreshMemberList();

            // 方法索引
            string currentMethod = _displayMethodNameProp.stringValue;
            _selectedMethodIndex = 0;
            if (!string.IsNullOrEmpty(currentMethod))
            {
                for (int i = 0; i < _availableMethodNames.Length; i++)
                {
                    if (_availableMethodNames[i] == currentMethod)
                    {
                        _selectedMethodIndex = i;
                        break;
                    }
                }
            }
        }

        private void RefreshMemberList()
        {
            var memberNames = new List<string> { "(None)" };

            if (_selectedTypeIndex > 0 && _selectedTypeIndex < _availableTypeNames.Length)
            {
                string typeName = _availableTypeNames[_selectedTypeIndex];
                memberNames.AddRange(UIDataDisplay.GetMemberNames(typeName));
            }

            _availableMemberNames = memberNames.ToArray();

            // 成员索引
            string currentMember = _memberNameProp.stringValue;
            _selectedMemberIndex = 0;
            if (!string.IsNullOrEmpty(currentMember))
            {
                for (int i = 0; i < _availableMemberNames.Length; i++)
                {
                    if (_availableMemberNames[i] == currentMember)
                    {
                        _selectedMemberIndex = i;
                        break;
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("数据绑定设置", EditorStyles.boldLabel);

            // 刷新按钮
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("刷新类型列表", GUILayout.Width(100)))
            {
                RefreshTypeLists();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 类型选择
            EditorGUI.BeginChangeCheck();
            int newTypeIndex = EditorGUILayout.Popup("数据类型", _selectedTypeIndex, _availableTypeNames);
            if (EditorGUI.EndChangeCheck())
            {
                _selectedTypeIndex = newTypeIndex;
                if (_selectedTypeIndex > 0)
                {
                    _dataClassNameProp.stringValue = _availableTypeNames[_selectedTypeIndex];
                }
                else
                {
                    _dataClassNameProp.stringValue = "";
                }

                // 类型改变时重置成员选择
                _memberNameProp.stringValue = "";
                _selectedMemberIndex = 0;
                RefreshMemberList();

                // 重置缓存
                ResetTargetCache();
            }

            // 也允许手动输入类名
            EditorGUI.BeginChangeCheck();
            string manualTypeName = EditorGUILayout.TextField("手动输入类名", _dataClassNameProp.stringValue);
            if (EditorGUI.EndChangeCheck())
            {
                _dataClassNameProp.stringValue = manualTypeName;
                UpdateSelectedIndices();
                ResetTargetCache();
            }

            EditorGUILayout.Space();

            // 成员变量选择
            if (_availableMemberNames != null && _availableMemberNames.Length > 0)
            {
                EditorGUI.BeginChangeCheck();
                int newMemberIndex = EditorGUILayout.Popup("成员变量", _selectedMemberIndex, _availableMemberNames);
                if (EditorGUI.EndChangeCheck())
                {
                    _selectedMemberIndex = newMemberIndex;
                    if (_selectedMemberIndex > 0)
                    {
                        _memberNameProp.stringValue = _availableMemberNames[_selectedMemberIndex];
                    }
                    else
                    {
                        _memberNameProp.stringValue = "";
                    }
                    ResetTargetCache();
                }
            }

            // 也允许手动输入成员名
            EditorGUI.BeginChangeCheck();
            string manualMemberName = EditorGUILayout.TextField("手动输入成员名", _memberNameProp.stringValue);
            if (EditorGUI.EndChangeCheck())
            {
                _memberNameProp.stringValue = manualMemberName;
                RefreshMemberList();
                ResetTargetCache();
            }

            EditorGUILayout.Space();

            // 显示方法选择
            if (_availableMethodNames != null && _availableMethodNames.Length > 0)
            {
                EditorGUI.BeginChangeCheck();
                int newMethodIndex = EditorGUILayout.Popup("显示方法", _selectedMethodIndex, _availableMethodNames);
                if (EditorGUI.EndChangeCheck())
                {
                    _selectedMethodIndex = newMethodIndex;
                    _displayMethodNameProp.stringValue = _availableMethodNames[_selectedMethodIndex];
                    ResetTargetCache();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("UI组件设置", EditorStyles.boldLabel);

            // 目标UI组件
            EditorGUILayout.PropertyField(_targetUIComponentProp, new GUIContent("目标UI组件", "留空则自动检测"));

            /*// 显示当前配置信息
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("配置信息", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.TextField("类型", _dataClassNameProp.stringValue);
                EditorGUILayout.TextField("成员", _memberNameProp.stringValue);
                EditorGUILayout.TextField("方法", _displayMethodNameProp.stringValue);
            } */

            serializedObject.ApplyModifiedProperties();
        }

        private void ResetTargetCache()
        {
            var display = target as UIDataDisplay;
            if (display != null)
            {
                display.ResetCache();
            }
        }
    }
}

