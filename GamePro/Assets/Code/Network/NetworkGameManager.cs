using System;
using UnityEngine;
using GamePlay.Manager;
using GamePlay.Actor;

namespace Network
{
    /// <summary>
    /// 网络游戏管理器 - 管理网络游戏的完整流程
    /// </summary>
    public class NetworkGameManager : MonoBehaviour
    {
        #region 单例
        
        private static NetworkGameManager _instance;
        public static NetworkGameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("NetworkGameManager");
                    _instance = go.AddComponent<NetworkGameManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region 属性
        
        /// <summary>
        /// 是否是联网模式
        /// </summary>
        private bool _isNetworkMode;
        public bool IsNetworkMode => _isNetworkMode;
        
        /// <summary>
        /// 本地玩家名称
        /// </summary>
        private string _localPlayerName = "Player";
        public string LocalPlayerName 
        { 
            get => _localPlayerName; 
            set => _localPlayerName = value; 
        }
        
        /// <summary>
        /// 本地玩家配置ID
        /// </summary>
        private int _localPlayerConfigId = 1;
        public int LocalPlayerConfigId 
        { 
            get => _localPlayerConfigId; 
            set => _localPlayerConfigId = value; 
        }
        
        /// <summary>
        /// 玩家出生点
        /// </summary>
        [SerializeField] private Vector3[] _spawnPoints = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(5, 0, 0),
            new Vector3(-5, 0, 0),
            new Vector3(0, 0, 5),
            new Vector3(0, 0, -5),
            new Vector3(5, 0, 5),
            new Vector3(-5, 0, -5),
            new Vector3(5, 0, -5)
        };
        
        #endregion
        
        #region 事件
        
        /// <summary>
        /// 网络游戏开始
        /// </summary>
        public event Action OnNetworkGameStarted;
        
        /// <summary>
        /// 网络游戏结束
        /// </summary>
        public event Action OnNetworkGameEnded;
        
        /// <summary>
        /// 所有玩家加载完成
        /// </summary>
        public event Action OnAllPlayersReady;
        
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
            // 订阅房间事件
            RoomManager.Instance.OnGameStarted += OnRoomGameStarted;
            RoomManager.Instance.OnPlayerJoined += OnPlayerJoinedRoom;
            RoomManager.Instance.OnPlayerLeft += OnPlayerLeftRoom;
            RoomManager.Instance.OnRoomLeft += OnLeftRoom;
            
            NetworkManager.Instance.OnDisconnected += OnNetworkDisconnected;
        }
        
        private void OnDestroy()
        {
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.OnGameStarted -= OnRoomGameStarted;
                RoomManager.Instance.OnPlayerJoined -= OnPlayerJoinedRoom;
                RoomManager.Instance.OnPlayerLeft -= OnPlayerLeftRoom;
                RoomManager.Instance.OnRoomLeft -= OnLeftRoom;
            }
            
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnDisconnected -= OnNetworkDisconnected;
            }
            
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion
        
        #region 主机操作
        
        /// <summary>
        /// 创建并托管游戏（作为房主）
        /// </summary>
        public bool HostGame(string roomName, string playerName, int maxPlayers = 4, int port = 7777)
        {
            _localPlayerName = playerName;
            
            // 启动服务器
            if (!NetworkManager.Instance.StartHost(port))
            {
                Debug.LogError("启动服务器失败");
                return false;
            }
            
            // 创建房间
            RoomManager.Instance.CreateRoom(roomName, playerName, maxPlayers);
            
            _isNetworkMode = true;
            Debug.Log($"创建房间成功: {roomName}");
            
            return true;
        }
        
        /// <summary>
        /// 加入游戏
        /// </summary>
        public void JoinGame(string ip, int port, string playerName, string roomId = "", string password = "")
        {
            _localPlayerName = playerName;
            
            // 连接服务器
            NetworkManager.Instance.Connect(ip, port);
            NetworkManager.Instance.OnConnected += () =>
            {
                // 连接成功后加入房间
                if (string.IsNullOrEmpty(roomId))
                {
                    // 请求房间列表
                    RoomManager.Instance.RequestRoomList();
                }
                else
                {
                    // 直接加入指定房间
                    RoomManager.Instance.JoinRoom(roomId, playerName, password);
                }
            };
            
            _isNetworkMode = true;
        }
        
        /// <summary>
        /// 离开游戏
        /// </summary>
        public void LeaveGame()
        {
            RoomManager.Instance.LeaveRoom();
            NetworkManager.Instance.Disconnect();
            NetworkSyncManager.Instance.ClearRemotePlayers();
            
            _isNetworkMode = false;
        }
        
        #endregion
        
        #region 游戏流程
        
        /// <summary>
        /// 房间游戏开始回调
        /// </summary>
        private void OnRoomGameStarted(int mapId, int gameMode)
        {
            Debug.Log($"网络游戏开始: Map={mapId}, Mode={gameMode}");
            
            // 初始化游戏
            StartNetworkGame();
        }
        
        /// <summary>
        /// 开始网络游戏
        /// </summary>
        private void StartNetworkGame()
        {
            // 清理
            ActorManager.Instance.ClearAll();
            SceneElementManager.Instance.ClearAll();
            
            // 创建本地玩家
            int spawnIndex = NetworkManager.Instance.LocalPlayerId % _spawnPoints.Length;
            Vector3 spawnPos = _spawnPoints[spawnIndex];
            
            var player = ActorManager.Instance.CreatePlayer(
                _localPlayerConfigId,
                spawnPos,
                _localPlayerName
            );
            
            // 广播本地玩家生成
            NetworkSyncManager.Instance.SpawnLocalPlayer(_localPlayerConfigId, spawnPos, _localPlayerName);
            
            // 房主负责生成怪物和物品
            if (NetworkManager.Instance.IsHost)
            {
                SpawnGameEntities();
            }
            
            OnNetworkGameStarted?.Invoke();
        }
        
        /// <summary>
        /// 生成游戏实体（房主执行）
        /// </summary>
        private void SpawnGameEntities()
        {
            // 从配置加载刷怪点
            var spawnConfigs = ConfigHelper.GetAllSpawnPointConfigs();
            foreach (var config in spawnConfigs)
            {
                SceneElementManager.Instance.AddSpawnPoint(config);
            }
            
            // 生成一些测试物品
            // TODO: 从关卡配置读取
        }
        
        /// <summary>
        /// 玩家加入房间
        /// </summary>
        private void OnPlayerJoinedRoom(PlayerInfo player)
        {
            Debug.Log($"玩家加入房间: {player.PlayerName} (ID: {player.PlayerId})");
            
            // 如果游戏已经开始，新玩家需要同步当前状态
            if (RoomManager.Instance.RoomState == RoomState.InGame && NetworkManager.Instance.IsHost)
            {
                // 发送完整的游戏状态给新玩家
                SendFullGameState(player.PlayerId);
            }
        }
        
        /// <summary>
        /// 玩家离开房间
        /// </summary>
        private void OnPlayerLeftRoom(int playerId, string reason)
        {
            Debug.Log($"玩家离开房间: {playerId} ({reason})");
            
            // 移除该玩家的角色
            var remotePlayer = NetworkSyncManager.Instance.GetRemotePlayer(playerId);
            if (remotePlayer != null)
            {
                ActorManager.Instance.DestroyActor(remotePlayer);
            }
        }
        
        /// <summary>
        /// 离开房间
        /// </summary>
        private void OnLeftRoom()
        {
            Debug.Log("已离开房间");
            
            // 清理网络状态
            NetworkSyncManager.Instance.ClearRemotePlayers();
            
            // 返回单机模式
            _isNetworkMode = false;
            
            OnNetworkGameEnded?.Invoke();
        }
        
        /// <summary>
        /// 网络断开
        /// </summary>
        private void OnNetworkDisconnected(string reason)
        {
            Debug.Log($"网络断开: {reason}");
            
            _isNetworkMode = false;
            NetworkSyncManager.Instance.ClearRemotePlayers();
            
            OnNetworkGameEnded?.Invoke();
        }
        
        /// <summary>
        /// 发送完整游戏状态给新玩家
        /// </summary>
        private void SendFullGameState(int playerId)
        {
            // 发送所有现有玩家信息
            foreach (var player in RoomManager.Instance.GetAllPlayers())
            {
                if (player.PlayerId == playerId) continue;
                
                // 查找玩家对象
                Character character = null;
                if (player.PlayerId == NetworkManager.Instance.LocalPlayerId)
                {
                    character = ActorManager.Instance.PlayerCharacter;
                }
                else
                {
                    character = NetworkSyncManager.Instance.GetRemotePlayer(player.PlayerId);
                }
                
                if (character != null)
                {
                    var spawnMsg = new PlayerSpawnMessage
                    {
                        PlayerId = player.PlayerId,
                        ConfigId = character.ConfigId,
                        PlayerName = player.PlayerName,
                        PosX = character.transform.position.x,
                        PosY = character.transform.position.y,
                        PosZ = character.transform.position.z,
                        RotY = character.transform.eulerAngles.y
                    };
                    NetworkManager.Instance.SendToClient(playerId, spawnMsg);
                }
            }
            
            // 发送所有怪物信息
            var monsters = ActorManager.Instance.GetAllMonsters();
            foreach (var monster in monsters)
            {
                var spawnMsg = new ActorSpawnMessage
                {
                    ActorId = monster.ActorId,
                    ConfigId = monster.ConfigId,
                    ActorType = 0, // Monster
                    PosX = monster.transform.position.x,
                    PosY = monster.transform.position.y,
                    PosZ = monster.transform.position.z,
                    RotY = monster.transform.eulerAngles.y
                };
                NetworkManager.Instance.SendToClient(playerId, spawnMsg);
            }
            
            // 发送所有NPC信息
            var npcs = ActorManager.Instance.GetAllNPCs();
            foreach (var npc in npcs)
            {
                var spawnMsg = new ActorSpawnMessage
                {
                    ActorId = npc.ActorId,
                    ConfigId = npc.ConfigId,
                    ActorType = 1, // NPC
                    PosX = npc.transform.position.x,
                    PosY = npc.transform.position.y,
                    PosZ = npc.transform.position.z,
                    RotY = npc.transform.eulerAngles.y
                };
                NetworkManager.Instance.SendToClient(playerId, spawnMsg);
            }
        }
        
        #endregion
        
        #region 工具方法
        
        /// <summary>
        /// 获取本机IP
        /// </summary>
        public string GetLocalIP()
        {
            return NetworkManager.Instance.GetLocalIP();
        }
        
        /// <summary>
        /// 检查是否可以开始游戏（房主检查）
        /// </summary>
        public bool CanStartGame()
        {
            if (!RoomManager.Instance.IsHost) return false;
            if (RoomManager.Instance.RoomState != RoomState.Waiting) return false;
            
            return true;
        }
        
        /// <summary>
        /// 开始游戏（房主调用）
        /// </summary>
        public void StartGame()
        {
            if (CanStartGame())
            {
                RoomManager.Instance.StartGame(0, 0);
            }
        }
        
        #endregion
    }
}

