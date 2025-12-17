using System;
using UnityEngine;
using GamePlay.Actor;

namespace Network
{
    /// <summary>
    /// 网络身份标识 - 标识一个可网络同步的对象
    /// </summary>
    public class NetworkIdentity : MonoBehaviour
    {
        #region 属性
        
        /// <summary>
        /// 网络ID
        /// </summary>
        [SerializeField] private int _networkId;
        public int NetworkId 
        { 
            get => _networkId; 
            set => _networkId = value; 
        }
        
        /// <summary>
        /// 所有者玩家ID（-1表示服务器拥有）
        /// </summary>
        [SerializeField] private int _ownerId = -1;
        public int OwnerId 
        { 
            get => _ownerId; 
            set => _ownerId = value; 
        }
        
        /// <summary>
        /// 是否是本地拥有
        /// </summary>
        public bool IsLocalOwned => _ownerId == NetworkManager.Instance.LocalPlayerId;
        
        /// <summary>
        /// 是否是服务器拥有
        /// </summary>
        public bool IsServerOwned => _ownerId == -1 || _ownerId == 0;
        
        /// <summary>
        /// 是否有权限控制此对象
        /// </summary>
        public bool HasAuthority
        {
            get
            {
                if (NetworkManager.Instance.IsHost)
                {
                    // 房主对服务器拥有的对象有权限
                    return IsServerOwned || IsLocalOwned;
                }
                return IsLocalOwned;
            }
        }
        
        /// <summary>
        /// 关联的Actor
        /// </summary>
        private ActorBase _actor;
        public ActorBase Actor => _actor;
        
        /// <summary>
        /// 是否已初始化
        /// </summary>
        private bool _isInitialized;
        public bool IsInitialized => _isInitialized;
        
        #endregion
        
        #region 事件
        
        /// <summary>
        /// 所有权变更事件
        /// </summary>
        public event Action<int, int> OnOwnershipChanged; // oldOwner, newOwner
        
        /// <summary>
        /// 网络生成事件
        /// </summary>
        public event Action OnNetworkSpawn;
        
        /// <summary>
        /// 网络销毁事件
        /// </summary>
        public event Action OnNetworkDespawn;
        
        #endregion
        
        #region Unity生命周期
        
        private void Awake()
        {
            _actor = GetComponent<ActorBase>();
        }
        
        private void OnDestroy()
        {
            if (_isInitialized)
            {
                OnNetworkDespawn?.Invoke();
                NetworkSyncManager.Instance?.UnregisterNetworkObject(this);
            }
        }
        
        #endregion
        
        #region 初始化
        
        /// <summary>
        /// 初始化网络身份
        /// </summary>
        public void Initialize(int networkId, int ownerId)
        {
            _networkId = networkId;
            _ownerId = ownerId;
            _isInitialized = true;
            
            NetworkSyncManager.Instance?.RegisterNetworkObject(this);
            OnNetworkSpawn?.Invoke();
        }
        
        /// <summary>
        /// 转移所有权
        /// </summary>
        public void TransferOwnership(int newOwnerId)
        {
            if (_ownerId != newOwnerId)
            {
                int oldOwner = _ownerId;
                _ownerId = newOwnerId;
                OnOwnershipChanged?.Invoke(oldOwner, newOwnerId);
            }
        }
        
        #endregion
    }
}

