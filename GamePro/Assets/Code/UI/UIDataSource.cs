using System;
using UnityEngine;

namespace UIDataBinding
{
    /// <summary>
    /// UI数据源组件
    /// 管理数据源对象，当数据变化时更新子物体上的所有显示组件
    /// </summary>
    public class UIDataSource : MonoBehaviour
    {
        [Tooltip("数据源对象（可以通过代码设置）")]
        [SerializeField]
        private UnityEngine.Object _unityObjectSource;

        [Tooltip("是否每帧检测变化")]
        public bool updateEveryFrame = true;

        [Tooltip("检测间隔（秒），0表示每帧检测")]
        public float updateInterval = 0f;

        // 实际数据源（可以是任意对象）
        private object _dataSource;

        // 缓存的显示组件
        private UIDataDisplay[] _displays;

        // 上次更新时间
        private float _lastUpdateTime;

        // 数据变化检测
        private int _lastDataHash;

        /// <summary>
        /// 数据源对象
        /// </summary>
        public object DataSource
        {
            get => _dataSource ?? _unityObjectSource;
            set
            {
                _dataSource = value;
                RefreshDisplays();
            }
        }

        private void Awake()
        {
            CacheDisplayComponents();
        }

        private void Start()
        {
            // 如果没有设置数据源，使用当前对象的UIBase组件作为数据源
            if (_unityObjectSource == null && _dataSource == null)
            {
                var uiBase = GetComponent<UIBase>();
                if (uiBase != null)
                {
                    _unityObjectSource = uiBase;
                }
            }

            // 初始刷新
            RefreshDisplays();
        }

        private void Update()
        {
            if (!updateEveryFrame) return;

            // 检查更新间隔
            if (updateInterval > 0)
            {
                if (Time.time - _lastUpdateTime < updateInterval)
                {
                    return;
                }
                _lastUpdateTime = Time.time;
            }

            // 检测数据变化
            if (HasDataChanged())
            {
                RefreshDisplays();
            }
        }

        /// <summary>
        /// 缓存所有子物体上的显示组件
        /// </summary>
        private void CacheDisplayComponents()
        {
            _displays = GetComponentsInChildren<UIDataDisplay>(true);
        }

        /// <summary>
        /// 检测数据是否发生变化
        /// </summary>
        private bool HasDataChanged()
        {
            object source = DataSource;
            if (source == null) return false;

            int currentHash = CalculateDataHash(source);
            if (currentHash != _lastDataHash)
            {
                _lastDataHash = currentHash;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 计算数据哈希值（简单实现，可根据需求扩展）
        /// </summary>
        private int CalculateDataHash(object data)
        {
            if (data == null) return 0;

            int hash = 17;
            var type = data.GetType();

            // 获取所有公共字段和属性的值来计算哈希
            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var field in fields)
            {
                try
                {
                    var value = field.GetValue(data);
                    hash = hash * 31 + (value?.GetHashCode() ?? 0);
                }
                catch
                {
                    // 忽略获取失败的字段
                }
            }

            var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (!prop.CanRead) continue;
                try
                {
                    var value = prop.GetValue(data);
                    hash = hash * 31 + (value?.GetHashCode() ?? 0);
                }
                catch
                {
                    // 忽略获取失败的属性
                }
            }

            return hash;
        }

        /// <summary>
        /// 刷新所有显示组件
        /// </summary>
        public void RefreshDisplays()
        {
            if (_displays == null)
            {
                CacheDisplayComponents();
            }

            object source = DataSource;
            if (source == null) return;

            foreach (var display in _displays)
            {
                if (display != null && display.enabled)
                {
                    display.UpdateDisplay(source);
                }
            }

            // 更新哈希值
            _lastDataHash = CalculateDataHash(source);
        }

        /// <summary>
        /// 强制刷新（重新缓存显示组件并刷新）
        /// </summary>
        public void ForceRefresh()
        {
            CacheDisplayComponents();
            RefreshDisplays();
        }

        /// <summary>
        /// 设置数据源并刷新
        /// </summary>
        public void SetDataSource<T>(T data) where T : class
        {
            _dataSource = data;
            RefreshDisplays();
        }

        /// <summary>
        /// 设置Unity对象作为数据源
        /// </summary>
        public void SetUnityObjectSource(UnityEngine.Object obj)
        {
            _unityObjectSource = obj;
            _dataSource = null;
            RefreshDisplays();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 编辑器中修改时刷新
            if (Application.isPlaying)
            {
                CacheDisplayComponents();
            }
        }
#endif
    }
}

