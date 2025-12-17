using System;
using System.Reflection;
using UnityEngine;

namespace UIDataBinding
{
    /// <summary>
    /// UI数据显示组件
    /// 可以选择class和成员变量，以及显示方法
    /// </summary>
    public class UIDataDisplay : MonoBehaviour
    {
        [Tooltip("数据类名（只需输入类名，不需要命名空间）")]
        public string dataClassName;

        [Tooltip("成员变量名")]
        public string memberName;

        [Tooltip("显示方法名（留空使用Default方法）")]
        public string displayMethodName = "Default";

        [Tooltip("目标UI组件（留空则自动检测）")]
        public Component targetUIComponent;

        // 缓存
        private Type _cachedType;
        private FieldInfo _cachedField;
        private PropertyInfo _cachedProperty;
        private MethodInfo _cachedDisplayMethod;
        private bool _isInitialized;

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// 初始化缓存
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            CacheType();
            CacheMember();
            CacheDisplayMethod();
            CacheUIComponent();

            _isInitialized = true;
        }

        /// <summary>
        /// 缓存数据类型
        /// </summary>
        private void CacheType()
        {
            if (string.IsNullOrEmpty(dataClassName))
            {
                _cachedType = null;
                return;
            }

            _cachedType = FindTypeByName(dataClassName);

            if (_cachedType == null)
            {
                Debug.LogWarning($"[UIDataDisplay] 找不到类型: {dataClassName}", this);
            }
        }

        /// <summary>
        /// 缓存成员信息
        /// </summary>
        private void CacheMember()
        {
            _cachedField = null;
            _cachedProperty = null;

            if (_cachedType == null || string.IsNullOrEmpty(memberName)) return;

            // 尝试获取字段
            _cachedField = _cachedType.GetField(memberName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            if (_cachedField == null)
            {
                // 尝试获取属性
                _cachedProperty = _cachedType.GetProperty(memberName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            }

            if (_cachedField == null && _cachedProperty == null)
            {
                Debug.LogWarning($"[UIDataDisplay] 在类型 {dataClassName} 中找不到成员: {memberName}", this);
            }
        }

        /// <summary>
        /// 缓存显示方法
        /// </summary>
        private void CacheDisplayMethod()
        {
            string methodName = string.IsNullOrEmpty(displayMethodName) ? "Default" : displayMethodName;
            _cachedDisplayMethod = UIDisplay.GetDisplayMethod(methodName);

            if (_cachedDisplayMethod == null)
            {
                Debug.LogWarning($"[UIDataDisplay] 找不到显示方法: {methodName}，使用Default", this);
                _cachedDisplayMethod = UIDisplay.GetDisplayMethod("Default");
            }
        }

        /// <summary>
        /// 缓存UI组件
        /// </summary>
        private void CacheUIComponent()
        {
            if (targetUIComponent != null) return;

            // 自动检测UI组件
            targetUIComponent = GetComponent<UnityEngine.UI.Text>();
            if (targetUIComponent != null) return;

            targetUIComponent = GetComponent<TMPro.TextMeshProUGUI>();
            if (targetUIComponent != null) return;

            targetUIComponent = GetComponent<TMPro.TextMeshPro>();
            if (targetUIComponent != null) return;

            targetUIComponent = GetComponent<UnityEngine.UI.Image>();
            if (targetUIComponent != null) return;

            targetUIComponent = GetComponent<UnityEngine.UI.Slider>();
            if (targetUIComponent != null) return;

            targetUIComponent = GetComponent<UnityEngine.UI.Toggle>();
            if (targetUIComponent != null) return;

            targetUIComponent = GetComponent<UnityEngine.UI.InputField>();
            if (targetUIComponent != null) return;

            targetUIComponent = GetComponent<TMPro.TMP_InputField>();
        }

        /// <summary>
        /// 根据类名查找类型（只查找用户创建的类）
        /// </summary>
        public static Type FindTypeByName(string className)
        {
            if (string.IsNullOrEmpty(className)) return null;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // 只查找用户脚本程序集
                string assemblyName = assembly.GetName().Name;
                if (!IsUserAssembly(assemblyName))
                {
                    continue;
                }

                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name == className && !type.IsAbstract && !type.IsInterface)
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 判断是否为用户脚本程序集
        /// </summary>
        private static bool IsUserAssembly(string assemblyName)
        {
            // 只包含用户创建的脚本程序集
            return assemblyName == "Assembly-CSharp" ||
                   assemblyName == "Assembly-CSharp-firstpass" ||
                   assemblyName.StartsWith("Assembly-CSharp-Editor");
        }

        /// <summary>
        /// 获取所有可用的用户类型名称（只返回用户创建的类）
        /// </summary>
        public static string[] GetAvailableTypeNames()
        {
            var typeNames = new System.Collections.Generic.List<string>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string assemblyName = assembly.GetName().Name;
                
                // 只查找用户脚本程序集
                if (!IsUserAssembly(assemblyName))
                {
                    continue;
                }

                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (!type.IsAbstract && !type.IsInterface && !type.IsEnum &&
                            !type.Name.StartsWith("<") && // 排除编译器生成的类
                            !type.IsNested) // 排除嵌套类
                        {
                            typeNames.Add(type.Name);
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // 忽略加载失败的程序集
                }
            }

            typeNames.Sort();
            return typeNames.ToArray();
        }

        /// <summary>
        /// 获取指定类型的所有成员名称（只获取自己声明的，不包括父类）
        /// </summary>
        public static string[] GetMemberNames(string className)
        {
            var type = FindTypeByName(className);
            if (type == null) return new string[0];

            var memberNames = new System.Collections.Generic.List<string>();

            // 获取公共字段（只获取当前类声明的，不包括继承的）
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var field in fields)
            {
                memberNames.Add(field.Name);
            }

            // 获取公共属性（只获取当前类声明的，不包括继承的）
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var prop in properties)
            {
                if (prop.CanRead)
                {
                    memberNames.Add(prop.Name);
                }
            }

            return memberNames.ToArray();
        }

        /// <summary>
        /// 更新显示（由UIDataSource调用）
        /// </summary>
        public void UpdateDisplay(object dataSource)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            if (dataSource == null || targetUIComponent == null) return;

            // 验证数据源类型
            if (_cachedType != null && !_cachedType.IsInstanceOfType(dataSource))
            {
                return;
            }

            object value = GetMemberValue(dataSource);
            if (value == null) return;

            // 调用显示方法
            if (_cachedDisplayMethod != null)
            {
                try
                {
                    _cachedDisplayMethod.Invoke(null, new object[] { targetUIComponent, value });
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UIDataDisplay] 调用显示方法失败: {e.Message}", this);
                }
            }
        }

        /// <summary>
        /// 获取成员值
        /// </summary>
        private object GetMemberValue(object dataSource)
        {
            try
            {
                if (_cachedField != null)
                {
                    return _cachedField.GetValue(dataSource);
                }
                else if (_cachedProperty != null)
                {
                    return _cachedProperty.GetValue(dataSource);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIDataDisplay] 获取成员值失败: {e.Message}", this);
            }

            return null;
        }

        /// <summary>
        /// 重置缓存（编辑器中修改参数后调用）
        /// </summary>
        public void ResetCache()
        {
            _isInitialized = false;
            _cachedType = null;
            _cachedField = null;
            _cachedProperty = null;
            _cachedDisplayMethod = null;
        }
    }
}

