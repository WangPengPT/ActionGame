using UnityEngine;
using GamePlay.Actor;
using GamePlay.Manager;

namespace Network
{
    /// <summary>
    /// 网络玩家控制器 - 处理本地玩家的网络同步
    /// </summary>
    public class NetworkPlayerController : MonoBehaviour
    {
        #region 属性
        
        /// <summary>
        /// 关联的角色
        /// </summary>
        private Character _character;
        
        /// <summary>
        /// 网络身份
        /// </summary>
        private NetworkIdentity _networkIdentity;
        
        /// <summary>
        /// 是否是本地玩家
        /// </summary>
        public bool IsLocalPlayer => _networkIdentity?.IsLocalOwned ?? true;
        
        /// <summary>
        /// 上次同步的位置
        /// </summary>
        private Vector3 _lastSyncedPosition;
        
        /// <summary>
        /// 上次同步的旋转
        /// </summary>
        private float _lastSyncedRotation;
        
        /// <summary>
        /// 位置变化阈值
        /// </summary>
        [SerializeField] private float _positionThreshold = 0.1f;
        
        /// <summary>
        /// 旋转变化阈值
        /// </summary>
        [SerializeField] private float _rotationThreshold = 5f;
        
        /// <summary>
        /// 瞄准同步间隔
        /// </summary>
        [SerializeField] private float _aimSyncInterval = 0.1f;
        private float _aimSyncTimer;
        
        /// <summary>
        /// 上次瞄准方向
        /// </summary>
        private Vector3 _lastAimDirection;
        
        #endregion
        
        #region Unity生命周期
        
        private void Awake()
        {
            _character = GetComponent<Character>();
            _networkIdentity = GetComponent<NetworkIdentity>();
        }
        
        private void Start()
        {
            if (_character != null)
            {
                // 订阅角色事件
                _character.OnFired += OnCharacterFired;
                _character.OnReloaded += OnCharacterReloaded;
                _character.OnDeath += OnCharacterDeath;
                _character.OnDamaged += OnCharacterDamaged;
            }
            
            _lastSyncedPosition = transform.position;
            _lastSyncedRotation = transform.eulerAngles.y;
        }
        
        private void OnDestroy()
        {
            if (_character != null)
            {
                _character.OnFired -= OnCharacterFired;
                _character.OnReloaded -= OnCharacterReloaded;
                _character.OnDeath -= OnCharacterDeath;
                _character.OnDamaged -= OnCharacterDamaged;
            }
        }
        
        private void Update()
        {
            if (!NetworkGameManager.Instance.IsNetworkMode) return;
            if (!IsLocalPlayer) return;
            
            // 检查位置变化并同步
            CheckAndSyncPosition();
            
            // 同步瞄准方向
            SyncAimDirection();
        }
        
        #endregion
        
        #region 位置同步
        
        /// <summary>
        /// 检查并同步位置
        /// </summary>
        private void CheckAndSyncPosition()
        {
            Vector3 currentPos = transform.position;
            float currentRot = transform.eulerAngles.y;
            
            bool positionChanged = Vector3.Distance(currentPos, _lastSyncedPosition) > _positionThreshold;
            bool rotationChanged = Mathf.Abs(Mathf.DeltaAngle(currentRot, _lastSyncedRotation)) > _rotationThreshold;
            
            if (positionChanged || rotationChanged)
            {
                _lastSyncedPosition = currentPos;
                _lastSyncedRotation = currentRot;
                
                // 位置同步在NetworkSyncManager中处理，这里不需要额外发送
            }
        }
        
        #endregion
        
        #region 瞄准同步
        
        /// <summary>
        /// 同步瞄准方向
        /// </summary>
        private void SyncAimDirection()
        {
            _aimSyncTimer += Time.deltaTime;
            if (_aimSyncTimer < _aimSyncInterval) return;
            _aimSyncTimer = 0;
            
            Vector3 currentAim = transform.forward;
            if (Vector3.Angle(currentAim, _lastAimDirection) > 5f)
            {
                _lastAimDirection = currentAim;
                NetworkSyncManager.Instance.SendAimDirection(currentAim);
            }
        }
        
        #endregion
        
        #region 事件回调
        
        /// <summary>
        /// 角色开火回调
        /// </summary>
        private void OnCharacterFired()
        {
            if (!IsLocalPlayer) return;
            
            Vector3 origin = transform.position + Vector3.up * 1.5f; // 枪口位置
            Vector3 direction = transform.forward;
            int weaponId = _character.CurrentWeapon?.ConfigId ?? 0;
            
            NetworkSyncManager.Instance.SendFireEvent(origin, direction, weaponId);
        }
        
        /// <summary>
        /// 角色换弹回调
        /// </summary>
        private void OnCharacterReloaded()
        {
            if (!IsLocalPlayer) return;
            
            // 可以发送换弹消息给其他玩家播放换弹动画
        }
        
        /// <summary>
        /// 角色死亡回调
        /// </summary>
        private void OnCharacterDeath(ActorBase killer)
        {
            if (!IsLocalPlayer) return;
            
            var msg = new PlayerDeathMessage
            {
                PlayerId = NetworkManager.Instance.LocalPlayerId,
                KillerId = killer?.ActorId ?? -1
            };
            NetworkManager.Instance.Send(msg);
        }
        
        /// <summary>
        /// 角色受伤回调
        /// </summary>
        private void OnCharacterDamaged(float damage, ActorBase attacker)
        {
            // 如果是房主，广播伤害信息
            if (NetworkManager.Instance.IsHost)
            {
                var msg = new PlayerHitMessage
                {
                    TargetPlayerId = NetworkManager.Instance.LocalPlayerId,
                    AttackerPlayerId = attacker?.ActorId ?? -1,
                    Damage = damage,
                    IsCritical = false, // TODO: 从战斗系统获取
                    HitX = transform.position.x,
                    HitY = transform.position.y + 1f,
                    HitZ = transform.position.z
                };
                NetworkManager.Instance.BroadcastToClients(msg);
            }
        }
        
        #endregion
        
        #region 远程玩家控制
        
        /// <summary>
        /// 设置目标位置（远程玩家使用）
        /// </summary>
        public void SetTargetPosition(Vector3 position)
        {
            if (IsLocalPlayer) return;
            // 由NetworkSyncManager处理插值
        }
        
        /// <summary>
        /// 设置目标旋转（远程玩家使用）
        /// </summary>
        public void SetTargetRotation(float rotY)
        {
            if (IsLocalPlayer) return;
            // 由NetworkSyncManager处理插值
        }
        
        /// <summary>
        /// 播放开火效果（远程玩家使用）
        /// </summary>
        public void PlayFireEffect()
        {
            if (IsLocalPlayer) return;
            
            // TODO: 播放枪口火焰、声音等
            Debug.Log($"远程玩家 {_networkIdentity?.NetworkId} 开火效果");
        }
        
        /// <summary>
        /// 播放受击效果
        /// </summary>
        public void PlayHitEffect(Vector3 hitPoint, bool isCritical)
        {
            // TODO: 播放受击效果
            Debug.Log($"玩家受击效果: {hitPoint}, 暴击: {isCritical}");
        }
        
        #endregion
    }
}

