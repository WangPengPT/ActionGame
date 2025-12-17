using System;
using System.Collections.Generic;
using UnityEngine;
using GamePlay.Actor;
using GamePlay.Data;
using GamePlay.Manager;

namespace Network
{
    /// <summary>
    /// 网络同步管理器 - 管理所有网络对象的同步
    /// </summary>
    public class NetworkSyncManager : MonoBehaviour
    {
        #region 单例
        
        private static NetworkSyncManager _instance;
        public static NetworkSyncManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("NetworkSyncManager");
                    _instance = go.AddComponent<NetworkSyncManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region 属性
        
        /// <summary>
        /// 所有网络对象
        /// </summary>
        private Dictionary<int, NetworkIdentity> _networkObjects = new Dictionary<int, NetworkIdentity>();
        
        /// <summary>
        /// 网络ID生成器
        /// </summary>
        private int _nextNetworkId = 1;
        
        /// <summary>
        /// 远程玩家对象
        /// </summary>
        private Dictionary<int, Character> _remotePlayers = new Dictionary<int, Character>();
        
        /// <summary>
        /// 同步间隔
        /// </summary>
        [SerializeField] private float _syncInterval = 0.05f; // 20Hz
        private float _syncTimer;
        
        /// <summary>
        /// 插值速度
        /// </summary>
        [SerializeField] private float _interpolationSpeed = 15f;
        
        /// <summary>
        /// 位置同步阈值
        /// </summary>
        [SerializeField] private float _positionThreshold = 0.01f;
        
        /// <summary>
        /// 旋转同步阈值
        /// </summary>
        [SerializeField] private float _rotationThreshold = 1f;
        
        /// <summary>
        /// 远程玩家目标位置
        /// </summary>
        private Dictionary<int, Vector3> _remoteTargetPositions = new Dictionary<int, Vector3>();
        private Dictionary<int, float> _remoteTargetRotations = new Dictionary<int, float>();
        
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
            DontDestroyOnLoad(gameObject);
        }
        
        private void Start()
        {
            RegisterMessageHandlers();
        }
        
        private void Update()
        {
            if (!NetworkManager.Instance.IsConnected) return;
            
            // 同步本地玩家
            _syncTimer += Time.deltaTime;
            if (_syncTimer >= _syncInterval)
            {
                _syncTimer = 0;
                SyncLocalPlayer();
                
                // 房主同步服务器对象（怪物、NPC等）
                if (NetworkManager.Instance.IsHost)
                {
                    SyncServerObjects();
                }
            }
            
            // 插值远程玩家
            InterpolateRemotePlayers();
        }
        
        private void OnDestroy()
        {
            UnregisterMessageHandlers();
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion
        
        #region 消息处理注册
        
        private void RegisterMessageHandlers()
        {
            var nm = NetworkManager.Instance;
            nm.RegisterHandler(NetworkMessageType.PlayerSpawn, HandlePlayerSpawn);
            nm.RegisterHandler(NetworkMessageType.PlayerDespawn, HandlePlayerDespawn);
            nm.RegisterHandler(NetworkMessageType.PlayerMove, HandlePlayerMove);
            nm.RegisterHandler(NetworkMessageType.PlayerAim, HandlePlayerAim);
            nm.RegisterHandler(NetworkMessageType.PlayerFire, HandlePlayerFire);
            nm.RegisterHandler(NetworkMessageType.PlayerHit, HandlePlayerHit);
            nm.RegisterHandler(NetworkMessageType.PlayerDeath, HandlePlayerDeath);
            nm.RegisterHandler(NetworkMessageType.PlayerStateSync, HandlePlayerStateSync);
            
            nm.RegisterHandler(NetworkMessageType.ActorSpawn, HandleActorSpawn);
            nm.RegisterHandler(NetworkMessageType.ActorDespawn, HandleActorDespawn);
            nm.RegisterHandler(NetworkMessageType.ActorMove, HandleActorMove);
            nm.RegisterHandler(NetworkMessageType.ActorDamage, HandleActorDamage);
            nm.RegisterHandler(NetworkMessageType.ActorDeath, HandleActorDeath);
            
            nm.RegisterHandler(NetworkMessageType.ItemSpawn, HandleItemSpawn);
            nm.RegisterHandler(NetworkMessageType.ItemPickedUp, HandleItemPickedUp);
        }
        
        private void UnregisterMessageHandlers()
        {
            var nm = NetworkManager.Instance;
            if (nm == null) return;
            
            nm.UnregisterHandler(NetworkMessageType.PlayerSpawn);
            nm.UnregisterHandler(NetworkMessageType.PlayerDespawn);
            nm.UnregisterHandler(NetworkMessageType.PlayerMove);
            nm.UnregisterHandler(NetworkMessageType.PlayerAim);
            nm.UnregisterHandler(NetworkMessageType.PlayerFire);
            nm.UnregisterHandler(NetworkMessageType.PlayerHit);
            nm.UnregisterHandler(NetworkMessageType.PlayerDeath);
            nm.UnregisterHandler(NetworkMessageType.PlayerStateSync);
            
            nm.UnregisterHandler(NetworkMessageType.ActorSpawn);
            nm.UnregisterHandler(NetworkMessageType.ActorDespawn);
            nm.UnregisterHandler(NetworkMessageType.ActorMove);
            nm.UnregisterHandler(NetworkMessageType.ActorDamage);
            nm.UnregisterHandler(NetworkMessageType.ActorDeath);
            
            nm.UnregisterHandler(NetworkMessageType.ItemSpawn);
            nm.UnregisterHandler(NetworkMessageType.ItemPickedUp);
        }
        
        #endregion
        
        #region 网络对象注册
        
        /// <summary>
        /// 生成网络ID
        /// </summary>
        public int GenerateNetworkId()
        {
            return _nextNetworkId++;
        }
        
        /// <summary>
        /// 注册网络对象
        /// </summary>
        public void RegisterNetworkObject(NetworkIdentity identity)
        {
            if (!_networkObjects.ContainsKey(identity.NetworkId))
            {
                _networkObjects[identity.NetworkId] = identity;
            }
        }
        
        /// <summary>
        /// 注销网络对象
        /// </summary>
        public void UnregisterNetworkObject(NetworkIdentity identity)
        {
            _networkObjects.Remove(identity.NetworkId);
        }
        
        /// <summary>
        /// 获取网络对象
        /// </summary>
        public NetworkIdentity GetNetworkObject(int networkId)
        {
            _networkObjects.TryGetValue(networkId, out var identity);
            return identity;
        }
        
        #endregion
        
        #region 玩家同步
        
        /// <summary>
        /// 同步本地玩家
        /// </summary>
        private void SyncLocalPlayer()
        {
            var player = ActorManager.Instance?.PlayerCharacter;
            if (player == null) return;
            
            // 发送位置更新
            var moveMsg = new PlayerMoveMessage
            {
                PlayerId = NetworkManager.Instance.LocalPlayerId,
                PosX = player.transform.position.x,
                PosY = player.transform.position.y,
                PosZ = player.transform.position.z,
                RotY = player.transform.eulerAngles.y,
                State = (int)player.CurrentState
            };
            
            NetworkManager.Instance.Send(moveMsg);
        }
        
        /// <summary>
        /// 生成本地玩家到网络
        /// </summary>
        public void SpawnLocalPlayer(int configId, Vector3 position, string playerName)
        {
            var msg = new PlayerSpawnMessage
            {
                PlayerId = NetworkManager.Instance.LocalPlayerId,
                ConfigId = configId,
                PlayerName = playerName,
                PosX = position.x,
                PosY = position.y,
                PosZ = position.z,
                RotY = 0
            };
            
            NetworkManager.Instance.Send(msg);
        }
        
        /// <summary>
        /// 发送开火事件
        /// </summary>
        public void SendFireEvent(Vector3 origin, Vector3 direction, int weaponId)
        {
            var msg = new PlayerFireMessage
            {
                PlayerId = NetworkManager.Instance.LocalPlayerId,
                OriginX = origin.x,
                OriginY = origin.y,
                OriginZ = origin.z,
                DirX = direction.x,
                DirY = direction.y,
                DirZ = direction.z,
                WeaponId = weaponId
            };
            
            NetworkManager.Instance.Send(msg);
        }
        
        /// <summary>
        /// 发送命中事件
        /// </summary>
        public void SendHitEvent(int targetId, float damage, bool isCritical, Vector3 hitPoint)
        {
            var msg = new PlayerHitMessage
            {
                TargetPlayerId = targetId,
                AttackerPlayerId = NetworkManager.Instance.LocalPlayerId,
                Damage = damage,
                IsCritical = isCritical,
                HitX = hitPoint.x,
                HitY = hitPoint.y,
                HitZ = hitPoint.z
            };
            
            NetworkManager.Instance.Send(msg);
        }
        
        /// <summary>
        /// 发送瞄准方向
        /// </summary>
        public void SendAimDirection(Vector3 aimDir)
        {
            var msg = new PlayerAimMessage
            {
                PlayerId = NetworkManager.Instance.LocalPlayerId,
                AimX = aimDir.x,
                AimY = aimDir.y,
                AimZ = aimDir.z
            };
            
            NetworkManager.Instance.Send(msg);
        }
        
        /// <summary>
        /// 插值远程玩家
        /// </summary>
        private void InterpolateRemotePlayers()
        {
            foreach (var pair in _remotePlayers)
            {
                if (pair.Value == null) continue;
                
                if (_remoteTargetPositions.TryGetValue(pair.Key, out var targetPos))
                {
                    pair.Value.transform.position = Vector3.Lerp(
                        pair.Value.transform.position,
                        targetPos,
                        Time.deltaTime * _interpolationSpeed
                    );
                }
                
                if (_remoteTargetRotations.TryGetValue(pair.Key, out var targetRot))
                {
                    var currentRot = pair.Value.transform.eulerAngles;
                    currentRot.y = Mathf.LerpAngle(currentRot.y, targetRot, Time.deltaTime * _interpolationSpeed);
                    pair.Value.transform.eulerAngles = currentRot;
                }
            }
        }
        
        #endregion
        
        #region 服务器对象同步
        
        /// <summary>
        /// 同步服务器对象（怪物、NPC等）
        /// </summary>
        private void SyncServerObjects()
        {
            // 同步所有怪物
            var monsters = ActorManager.Instance.GetAllMonsters();
            foreach (var monster in monsters)
            {
                var msg = new ActorMoveMessage
                {
                    ActorId = monster.ActorId,
                    PosX = monster.transform.position.x,
                    PosY = monster.transform.position.y,
                    PosZ = monster.transform.position.z,
                    RotY = monster.transform.eulerAngles.y,
                    State = (int)monster.CurrentState
                };
                
                NetworkManager.Instance.BroadcastToClients(msg);
            }
        }
        
        /// <summary>
        /// 广播Actor生成
        /// </summary>
        public void BroadcastActorSpawn(ActorBase actor, int actorType)
        {
            if (!NetworkManager.Instance.IsHost) return;
            
            var msg = new ActorSpawnMessage
            {
                ActorId = actor.ActorId,
                ConfigId = actor.ConfigId,
                ActorType = actorType,
                PosX = actor.transform.position.x,
                PosY = actor.transform.position.y,
                PosZ = actor.transform.position.z,
                RotY = actor.transform.eulerAngles.y
            };
            
            NetworkManager.Instance.BroadcastToClients(msg);
        }
        
        /// <summary>
        /// 广播Actor受伤
        /// </summary>
        public void BroadcastActorDamage(int actorId, int attackerId, float damage, float remainingHealth, bool isCritical)
        {
            var msg = new ActorDamageMessage
            {
                ActorId = actorId,
                AttackerId = attackerId,
                Damage = damage,
                RemainingHealth = remainingHealth,
                IsCritical = isCritical
            };
            
            NetworkManager.Instance.Send(msg);
        }
        
        /// <summary>
        /// 广播Actor死亡
        /// </summary>
        public void BroadcastActorDeath(int actorId, int killerId, int expReward, int goldReward)
        {
            var msg = new ActorDeathMessage
            {
                ActorId = actorId,
                KillerId = killerId,
                ExpReward = expReward,
                GoldReward = goldReward
            };
            
            NetworkManager.Instance.Send(msg);
        }
        
        #endregion
        
        #region 消息处理
        
        /// <summary>
        /// 处理玩家生成
        /// </summary>
        private void HandlePlayerSpawn(int senderId, NetworkMessage message)
        {
            var msg = message as PlayerSpawnMessage;
            
            // 不处理自己
            if (msg.PlayerId == NetworkManager.Instance.LocalPlayerId) return;
            
            // 创建远程玩家
            if (!_remotePlayers.ContainsKey(msg.PlayerId))
            {
                Vector3 pos = new Vector3(msg.PosX, msg.PosY, msg.PosZ);
                
                // 使用ActorManager创建远程玩家
                var remotePlayer = ActorManager.Instance.CreatePlayer(
                    msg.ConfigId,
                    pos,
                    msg.PlayerName
                );
                
                // 设置为远程玩家（禁用本地控制）
                // 添加NetworkIdentity
                var identity = remotePlayer.gameObject.AddComponent<NetworkIdentity>();
                identity.Initialize(msg.PlayerId, msg.PlayerId);
                
                _remotePlayers[msg.PlayerId] = remotePlayer;
                _remoteTargetPositions[msg.PlayerId] = pos;
                _remoteTargetRotations[msg.PlayerId] = msg.RotY;
                
                Debug.Log($"远程玩家生成: {msg.PlayerName} (ID: {msg.PlayerId})");
            }
            
            // 房主转发给其他客户端
            if (NetworkManager.Instance.IsHost)
            {
                NetworkManager.Instance.BroadcastToClients(msg, senderId);
            }
        }
        
        /// <summary>
        /// 处理玩家消失
        /// </summary>
        private void HandlePlayerDespawn(int senderId, NetworkMessage message)
        {
            // 移除远程玩家
            var msg = message as PlayerSpawnMessage;
            if (_remotePlayers.TryGetValue(msg.PlayerId, out var player))
            {
                if (player != null)
                {
                    Destroy(player.gameObject);
                }
                _remotePlayers.Remove(msg.PlayerId);
                _remoteTargetPositions.Remove(msg.PlayerId);
                _remoteTargetRotations.Remove(msg.PlayerId);
            }
        }
        
        /// <summary>
        /// 处理玩家移动
        /// </summary>
        private void HandlePlayerMove(int senderId, NetworkMessage message)
        {
            var msg = message as PlayerMoveMessage;
            
            // 不处理自己
            if (msg.PlayerId == NetworkManager.Instance.LocalPlayerId) return;
            
            // 更新远程玩家目标位置
            _remoteTargetPositions[msg.PlayerId] = new Vector3(msg.PosX, msg.PosY, msg.PosZ);
            _remoteTargetRotations[msg.PlayerId] = msg.RotY;
            
            // 房主转发
            if (NetworkManager.Instance.IsHost)
            {
                NetworkManager.Instance.BroadcastToClients(msg, senderId);
            }
        }
        
        /// <summary>
        /// 处理玩家瞄准
        /// </summary>
        private void HandlePlayerAim(int senderId, NetworkMessage message)
        {
            var msg = message as PlayerAimMessage;
            
            if (msg.PlayerId == NetworkManager.Instance.LocalPlayerId) return;
            
            if (_remotePlayers.TryGetValue(msg.PlayerId, out var player))
            {
                // 更新远程玩家瞄准方向
                Vector3 aimDir = new Vector3(msg.AimX, msg.AimY, msg.AimZ);
                player.SetAimDirection(aimDir);
            }
            
            // 房主转发
            if (NetworkManager.Instance.IsHost)
            {
                NetworkManager.Instance.BroadcastToClients(msg, senderId);
            }
        }
        
        /// <summary>
        /// 处理玩家开火
        /// </summary>
        private void HandlePlayerFire(int senderId, NetworkMessage message)
        {
            var msg = message as PlayerFireMessage;
            
            if (msg.PlayerId == NetworkManager.Instance.LocalPlayerId) return;
            
            if (_remotePlayers.TryGetValue(msg.PlayerId, out var player))
            {
                // 播放远程玩家开火效果
                // TODO: 播放枪口火焰、声音等
                Debug.Log($"远程玩家 {msg.PlayerId} 开火");
            }
            
            // 房主转发
            if (NetworkManager.Instance.IsHost)
            {
                NetworkManager.Instance.BroadcastToClients(msg, senderId);
            }
        }
        
        /// <summary>
        /// 处理玩家受击
        /// </summary>
        private void HandlePlayerHit(int senderId, NetworkMessage message)
        {
            var msg = message as PlayerHitMessage;
            
            // 如果是本地玩家被击中
            if (msg.TargetPlayerId == NetworkManager.Instance.LocalPlayerId)
            {
                var localPlayer = ActorManager.Instance.PlayerCharacter;
                if (localPlayer != null)
                {
                    localPlayer.TakeDamage(msg.Damage, null);
                }
            }
            // 远程玩家被击中
            else if (_remotePlayers.TryGetValue(msg.TargetPlayerId, out var player))
            {
                player.TakeDamage(msg.Damage, null);
            }
            
            // 房主转发
            if (NetworkManager.Instance.IsHost)
            {
                NetworkManager.Instance.BroadcastToClients(msg, senderId);
            }
        }
        
        /// <summary>
        /// 处理玩家死亡
        /// </summary>
        private void HandlePlayerDeath(int senderId, NetworkMessage message)
        {
            var msg = message as PlayerDeathMessage;
            
            if (msg.PlayerId == NetworkManager.Instance.LocalPlayerId)
            {
                // 本地玩家死亡
                var localPlayer = ActorManager.Instance.PlayerCharacter;
                localPlayer?.Die(null);
            }
            else if (_remotePlayers.TryGetValue(msg.PlayerId, out var player))
            {
                player?.Die(null);
            }
            
            // 房主转发
            if (NetworkManager.Instance.IsHost)
            {
                NetworkManager.Instance.BroadcastToClients(msg, senderId);
            }
        }
        
        /// <summary>
        /// 处理玩家状态同步
        /// </summary>
        private void HandlePlayerStateSync(int senderId, NetworkMessage message)
        {
            var msg = message as PlayerStateSyncMessage;
            
            if (msg.PlayerId == NetworkManager.Instance.LocalPlayerId) return;
            
            if (_remotePlayers.TryGetValue(msg.PlayerId, out var player))
            {
                // 更新远程玩家状态
                player.Attribute.CurrentHealth = msg.Health;
            }
        }
        
        /// <summary>
        /// 处理Actor生成
        /// </summary>
        private void HandleActorSpawn(int senderId, NetworkMessage message)
        {
            if (NetworkManager.Instance.IsHost) return; // 房主自己创建
            
            var msg = message as ActorSpawnMessage;
            Vector3 pos = new Vector3(msg.PosX, msg.PosY, msg.PosZ);
            
            if (msg.ActorType == 0) // Monster
            {
                ActorManager.Instance.CreateMonster(msg.ConfigId, pos, MonsterType.Normal, null);
            }
            else if (msg.ActorType == 1) // NPC
            {
                ActorManager.Instance.CreateNPC(msg.ConfigId, pos, NPCType.Normal, null);
            }
        }
        
        /// <summary>
        /// 处理Actor消失
        /// </summary>
        private void HandleActorDespawn(int senderId, NetworkMessage message)
        {
            if (NetworkManager.Instance.IsHost) return;
            
            var msg = message as ActorSpawnMessage;
            ActorManager.Instance.DestroyActor(msg.ActorId);
        }
        
        /// <summary>
        /// 处理Actor移动
        /// </summary>
        private void HandleActorMove(int senderId, NetworkMessage message)
        {
            if (NetworkManager.Instance.IsHost) return;
            
            var msg = message as ActorMoveMessage;
            var actor = ActorManager.Instance.GetActorById(msg.ActorId);
            
            if (actor != null)
            {
                // 插值移动
                actor.transform.position = Vector3.Lerp(
                    actor.transform.position,
                    new Vector3(msg.PosX, msg.PosY, msg.PosZ),
                    0.5f
                );
                
                var rotation = actor.transform.eulerAngles;
                rotation.y = msg.RotY;
                actor.transform.eulerAngles = rotation;
            }
        }
        
        /// <summary>
        /// 处理Actor受伤
        /// </summary>
        private void HandleActorDamage(int senderId, NetworkMessage message)
        {
            var msg = message as ActorDamageMessage;
            var actor = ActorManager.Instance.GetActorById(msg.ActorId);
            
            if (actor != null && !NetworkManager.Instance.IsHost)
            {
                // 客户端只更新显示，不重复计算伤害
                actor.Attribute.CurrentHealth = msg.RemainingHealth;
            }
            
            // 房主转发
            if (NetworkManager.Instance.IsHost)
            {
                NetworkManager.Instance.BroadcastToClients(msg, senderId);
            }
        }
        
        /// <summary>
        /// 处理Actor死亡
        /// </summary>
        private void HandleActorDeath(int senderId, NetworkMessage message)
        {
            var msg = message as ActorDeathMessage;
            var actor = ActorManager.Instance.GetActorById(msg.ActorId);
            
            if (actor != null && !NetworkManager.Instance.IsHost)
            {
                actor.Die(null);
            }
            
            // 给击杀者奖励
            if (msg.KillerId == NetworkManager.Instance.LocalPlayerId)
            {
                var player = ActorManager.Instance.PlayerCharacter;
                if (player != null)
                {
                    player.AddExperience(msg.ExpReward);
                    player.AddGold(msg.GoldReward);
                }
            }
            
            // 房主转发
            if (NetworkManager.Instance.IsHost)
            {
                NetworkManager.Instance.BroadcastToClients(msg, senderId);
            }
        }
        
        /// <summary>
        /// 处理物品生成
        /// </summary>
        private void HandleItemSpawn(int senderId, NetworkMessage message)
        {
            if (NetworkManager.Instance.IsHost) return;
            
            var msg = message as ItemSpawnMessage;
            Vector3 pos = new Vector3(msg.PosX, msg.PosY, msg.PosZ);
            
            SceneElementManager.Instance.SpawnItem(msg.ConfigId, pos, msg.Count);
        }
        
        /// <summary>
        /// 处理物品被拾取
        /// </summary>
        private void HandleItemPickedUp(int senderId, NetworkMessage message)
        {
            var msg = message as ItemPickedUpMessage;
            
            // 如果不是自己拾取的，从场景移除物品显示
            if (msg.PlayerId != NetworkManager.Instance.LocalPlayerId)
            {
                // TODO: 移除物品显示
            }
            
            // 房主转发
            if (NetworkManager.Instance.IsHost)
            {
                NetworkManager.Instance.BroadcastToClients(msg, senderId);
            }
        }
        
        #endregion
        
        #region 工具方法
        
        /// <summary>
        /// 获取远程玩家
        /// </summary>
        public Character GetRemotePlayer(int playerId)
        {
            _remotePlayers.TryGetValue(playerId, out var player);
            return player;
        }
        
        /// <summary>
        /// 获取所有远程玩家
        /// </summary>
        public List<Character> GetAllRemotePlayers()
        {
            return new List<Character>(_remotePlayers.Values);
        }
        
        /// <summary>
        /// 清除所有远程玩家
        /// </summary>
        public void ClearRemotePlayers()
        {
            foreach (var pair in _remotePlayers)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value.gameObject);
                }
            }
            _remotePlayers.Clear();
            _remoteTargetPositions.Clear();
            _remoteTargetRotations.Clear();
        }
        
        #endregion
    }
}

