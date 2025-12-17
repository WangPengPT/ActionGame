using System;
using UnityEngine;
using GamePlay.Actor;

namespace GamePlay.Manager
{
    /// <summary>
    /// 输入管理器 - 处理玩家输入并转发到角色控制
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        #region 单例
        
        private static InputManager _instance;
        public static InputManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("InputManager");
                    _instance = go.AddComponent<InputManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region 属性
        
        /// <summary>
        /// 是否启用输入
        /// </summary>
        [SerializeField] private bool _inputEnabled = true;
        public bool InputEnabled 
        { 
            get => _inputEnabled; 
            set => _inputEnabled = value; 
        }
        
        /// <summary>
        /// 主相机
        /// </summary>
        private Camera _mainCamera;
        
        /// <summary>
        /// 移动输入
        /// </summary>
        private Vector3 _moveInput;
        public Vector3 MoveInput => _moveInput;
        
        /// <summary>
        /// 鼠标世界位置
        /// </summary>
        private Vector3 _mouseWorldPosition;
        public Vector3 MouseWorldPosition => _mouseWorldPosition;
        
        /// <summary>
        /// 瞄准方向
        /// </summary>
        private Vector3 _aimDirection;
        public Vector3 AimDirection => _aimDirection;
        
        /// <summary>
        /// 地面层级
        /// </summary>
        [SerializeField] private LayerMask _groundLayer = ~0;
        
        #endregion
        
        #region 事件
        
        /// <summary>
        /// 开火事件
        /// </summary>
        public event Action OnFirePressed;
        public event Action OnFireReleased;
        
        /// <summary>
        /// 换弹事件
        /// </summary>
        public event Action OnReloadPressed;
        
        /// <summary>
        /// 交互事件
        /// </summary>
        public event Action OnInteractPressed;
        
        /// <summary>
        /// 拾取事件
        /// </summary>
        public event Action OnPickupPressed;
        
        /// <summary>
        /// 使用物品事件（1-4号快捷栏）
        /// </summary>
        public event Action<int> OnUseItemPressed;
        
        /// <summary>
        /// 切换武器事件
        /// </summary>
        public event Action OnSwitchWeaponPressed;
        
        /// <summary>
        /// 暂停事件
        /// </summary>
        public event Action OnPausePressed;
        
        /// <summary>
        /// 菜单/背包事件
        /// </summary>
        public event Action OnInventoryPressed;
        
        /// <summary>
        /// 闪避/翻滚事件
        /// </summary>
        public event Action OnDodgePressed;
        
        #endregion
        
        #region Unity生命周期
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            _mainCamera = Camera.main;
        }
        
        private void Update()
        {
            if (!_inputEnabled) return;
            
            // 更新相机引用
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }
            
            UpdateMovementInput();
            UpdateAimInput();
            UpdateActionInput();
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion
        
        #region 输入更新
        
        /// <summary>
        /// 更新移动输入
        /// </summary>
        private void UpdateMovementInput()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            
            // 俯视角游戏使用XZ平面移动
            _moveInput = new Vector3(horizontal, 0, vertical).normalized;
            
            // 应用到玩家角色
            var player = ActorManager.Instance?.PlayerCharacter;
            if (player != null)
            {
                player.SetMoveInput(_moveInput);
            }
        }
        
        /// <summary>
        /// 更新瞄准输入
        /// </summary>
        private void UpdateAimInput()
        {
            // 射线检测鼠标位置对应的世界坐标
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, _groundLayer))
            {
                _mouseWorldPosition = hit.point;
            }
            else
            {
                // 如果没有命中地面，使用固定高度的平面
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float distance))
                {
                    _mouseWorldPosition = ray.GetPoint(distance);
                }
            }
            
            // 计算瞄准方向
            var player = ActorManager.Instance?.PlayerCharacter;
            if (player != null)
            {
                _aimDirection = (_mouseWorldPosition - player.transform.position).normalized;
                _aimDirection.y = 0;
                player.SetAimDirection(_aimDirection);
            }
        }
        
        /// <summary>
        /// 更新动作输入
        /// </summary>
        private void UpdateActionInput()
        {
            var player = ActorManager.Instance?.PlayerCharacter;
            
            // 开火（鼠标左键）
            if (Input.GetMouseButtonDown(0))
            {
                OnFirePressed?.Invoke();
            }
            if (Input.GetMouseButton(0))
            {
                player?.Fire();
            }
            if (Input.GetMouseButtonUp(0))
            {
                OnFireReleased?.Invoke();
            }
            
            // 换弹（R键）
            if (Input.GetKeyDown(KeyCode.R))
            {
                OnReloadPressed?.Invoke();
                player?.StartReload();
            }
            
            // 交互（E键）
            if (Input.GetKeyDown(KeyCode.E))
            {
                OnInteractPressed?.Invoke();
                
                // 与选中的Actor交互
                var selected = ActorManager.Instance?.SelectedActor;
                if (selected != null)
                {
                    player?.InteractWith(selected);
                }
            }
            
            // 拾取（F键）
            if (Input.GetKeyDown(KeyCode.F))
            {
                OnPickupPressed?.Invoke();
                SceneElementManager.Instance?.PickupNearestItem(player);
            }
            
            // 快捷栏物品（1-4）
            for (int i = 0; i < 4; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    OnUseItemPressed?.Invoke(i);
                }
            }
            
            // 切换武器（Q键）
            if (Input.GetKeyDown(KeyCode.Q))
            {
                OnSwitchWeaponPressed?.Invoke();
            }
            
            // 闪避/翻滚（空格键）
            if (Input.GetKeyDown(KeyCode.Space))
            {
                OnDodgePressed?.Invoke();
            }
            
            // 背包（Tab或I键）
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))
            {
                OnInventoryPressed?.Invoke();
            }
            
            // 暂停（Escape键）
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnPausePressed?.Invoke();
            }
            
            // 鼠标选中（右键）
            if (Input.GetMouseButtonDown(1))
            {
                ActorManager.Instance?.SelectActorByScreenPoint(Input.mousePosition, _mainCamera);
            }
        }
        
        #endregion
        
        #region 公共方法
        
        /// <summary>
        /// 暂时禁用输入
        /// </summary>
        public void DisableInputTemporarily(float duration)
        {
            StartCoroutine(DisableInputCoroutine(duration));
        }
        
        private System.Collections.IEnumerator DisableInputCoroutine(float duration)
        {
            _inputEnabled = false;
            yield return new WaitForSeconds(duration);
            _inputEnabled = true;
        }
        
        /// <summary>
        /// 震动反馈（如果支持）
        /// </summary>
        public void Vibrate(float duration = 0.1f, float intensity = 0.5f)
        {
            // TODO: 实现手柄震动
        }
        
        #endregion
    }
}

