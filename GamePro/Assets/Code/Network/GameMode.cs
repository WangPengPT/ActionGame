using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GamePlay.Manager;
using GamePlay.Actor;

namespace Network
{
    /// <summary>
    /// 游戏模式
    /// </summary>
    public enum GameModeType
    {
        /// <summary>
        /// 单机模式 - 开发测试用
        /// </summary>
        Offline,
        
        /// <summary>
        /// 局域网模式 - 直连
        /// </summary>
        LAN,
        
        /// <summary>
        /// 中继服务器模式 - 公网联机
        /// </summary>
        Relay
    }
    
    /// <summary>
    /// 游戏模式管理器 - 统一管理单机/联网模式
    /// 开发时用Offline，发布时用Relay
    /// </summary>
    public class GameModeManager : MonoBehaviour
    {
        #region 单例
        
        private static GameModeManager _instance;
        public static GameModeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("GameModeManager");
                    _instance = go.AddComponent<GameModeManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region 配置
        
        /// <summary>
        /// 当前游戏模式
        /// </summary>
        [SerializeField] private GameModeType _currentMode = GameModeType.Offline;
        public GameModeType CurrentMode => _currentMode;
        
        /// <summary>
        /// 是否是单机模式
        /// </summary>
        public bool IsOffline => _currentMode == GameModeType.Offline;
        
        /// <summary>
        /// 是否是联网模式
        /// </summary>
        public bool IsOnline => _currentMode != GameModeType.Offline;
        
        /// <summary>
        /// 是否是房主（单机模式永远是房主）
        /// </summary>
        public bool IsHost
        {
            get
            {
                if (IsOffline) return true;
                if (_currentMode == GameModeType.Relay) return RelayClient.Instance.IsHost;
                return NetworkManager.Instance.IsHost;
            }
        }
        
        /// <summary>
        /// 本地玩家ID（单机模式为0）
        /// </summary>
        public int LocalPlayerId
        {
            get
            {
                if (IsOffline) return 0;
                if (_currentMode == GameModeType.Relay) return RelayClient.Instance.LocalClientId;
                return NetworkManager.Instance.LocalPlayerId;
            }
        }
        
        /// <summary>
        /// 是否已连接（单机模式永远true）
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (IsOffline) return true;
                if (_currentMode == GameModeType.Relay) return RelayClient.Instance.IsConnected;
                return NetworkManager.Instance.IsConnected;
            }
        }
        
        /// <summary>
        /// 玩家名称
        /// </summary>
        [SerializeField] private string _playerName = "Player";
        public string PlayerName
        {
            get => _playerName;
            set => _playerName = value;
        }
        
        /// <summary>
        /// 玩家配置ID
        /// </summary>
        [SerializeField] private int _playerConfigId = 1;
        public int PlayerConfigId
        {
            get => _playerConfigId;
            set => _playerConfigId = value;
        }
        
        /// <summary>
        /// 中继服务器IP
        /// </summary>
        [SerializeField] private string _relayServerIP = "127.0.0.1";
        public string RelayServerIP
        {
            get => _relayServerIP;
            set => _relayServerIP = value;
        }
        
        /// <summary>
        /// 服务器端口
        /// </summary>
        [SerializeField] private int _serverPort = 7777;
        public int ServerPort
        {
            get => _serverPort;
            set => _serverPort = value;
        }
        
        #endregion
        
        #region 单机模式模拟数据
        
        /// <summary>
        /// 单机模式下的模拟房间
        /// </summary>
        private RoomInfo _offlineRoom;
        
        /// <summary>
        /// 单机模式下的模拟玩家列表
        /// </summary>
        private List<PlayerInfo> _offlinePlayers = new List<PlayerInfo>();
        
        #endregion
        
        #region 事件
        
        /// <summary>
        /// 模式切换事件
        /// </summary>
        public event Action<GameModeType> OnModeChanged;
        
        /// <summary>
        /// 连接成功
        /// </summary>
        public event Action OnConnected;
        
        /// <summary>
        /// 断开连接
        /// </summary>
        public event Action<string> OnDisconnected;
        
        /// <summary>
        /// 房间创建成功
        /// </summary>
        public event Action<RoomInfo> OnRoomCreated;
        
        /// <summary>
        /// 加入房间成功
        /// </summary>
        public event Action<RoomInfo> OnRoomJoined;
        
        /// <summary>
        /// 离开房间
        /// </summary>
        public event Action OnRoomLeft;
        
        /// <summary>
        /// 玩家加入
        /// </summary>
        public event Action<PlayerInfo> OnPlayerJoined;
        
        /// <summary>
        /// 玩家离开
        /// </summary>
        public event Action<int, string> OnPlayerLeft;
        
        /// <summary>
        /// 游戏开始
        /// </summary>
        public event Action OnGameStarted;
        
        /// <summary>
        /// 房间列表更新
        /// </summary>
        public event Action<List<RoomInfo>> OnRoomListUpdated;
        
        /// <summary>
        /// 聊天消息
        /// </summary>
        public event Action<string, string> OnChatReceived;
        
        /// <summary>
        /// 错误
        /// </summary>
        public event Action<string> OnError;
        
        /// <summary>
        /// 重连中
        /// </summary>
        public event Action<int, int> OnReconnecting;
        
        /// <summary>
        /// 重连成功
        /// </summary>
        public event Action OnReconnected;
        
        /// <summary>
        /// 重连失败
        /// </summary>
        public event Action OnReconnectFailed;
        
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
            // 从配置文件加载默认设置
            LoadFromConfig();
            
            // 订阅中继客户端事件
            SubscribeRelayEvents();
            
            // 订阅局域网事件
            SubscribeLANEvents();
            
            Debug.Log($"[GameMode] 初始化完成，当前模式: {_currentMode}");
        }
        
        /// <summary>
        /// 从配置文件加载默认设置
        /// </summary>
        private void LoadFromConfig()
        {
            var config = GameConfig.Instance;
            if (config != null)
            {
                _currentMode = config.DefaultMode;
                _relayServerIP = config.RelayServerIP;
                _serverPort = config.ServerPort;
                _playerName = config.DefaultPlayerName;
                _playerConfigId = config.DefaultPlayerConfigId;
                Debug.Log($"[GameMode] 从配置加载: 模式={config.DefaultMode}, 服务器={config.RelayServerIP}:{config.ServerPort}");
            }
        }
        
        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
        
        #endregion
        
        #region 事件订阅
        
        private void SubscribeRelayEvents()
        {
            var relay = RelayClient.Instance;
            relay.OnConnected += () => { if (_currentMode == GameModeType.Relay) OnConnected?.Invoke(); };
            relay.OnDisconnected += (reason) => { if (_currentMode == GameModeType.Relay) OnDisconnected?.Invoke(reason); };
            relay.OnRoomCreated += (room) => { if (_currentMode == GameModeType.Relay) OnRoomCreated?.Invoke(ConvertRoom(room)); };
            relay.OnRoomJoined += (room) => { if (_currentMode == GameModeType.Relay) OnRoomJoined?.Invoke(ConvertRoom(room)); };
            relay.OnRoomLeft += () => { if (_currentMode == GameModeType.Relay) OnRoomLeft?.Invoke(); };
            relay.OnPlayerJoined += (p) => { if (_currentMode == GameModeType.Relay) OnPlayerJoined?.Invoke(ConvertPlayer(p)); };
            relay.OnPlayerLeft += (id, reason) => { if (_currentMode == GameModeType.Relay) OnPlayerLeft?.Invoke(id, reason); };
            relay.OnGameStarted += () => { if (_currentMode == GameModeType.Relay) OnGameStarted?.Invoke(); };
            relay.OnRoomListUpdated += (rooms) => { if (_currentMode == GameModeType.Relay) OnRoomListUpdated?.Invoke(ConvertRoomList(rooms)); };
            relay.OnChatReceived += (sender, content) => { if (_currentMode == GameModeType.Relay) OnChatReceived?.Invoke(sender, content); };
            relay.OnError += (error) => { if (_currentMode == GameModeType.Relay) OnError?.Invoke(error); };
            relay.OnReconnecting += (cur, max) => { if (_currentMode == GameModeType.Relay) OnReconnecting?.Invoke(cur, max); };
            relay.OnReconnected += () => { if (_currentMode == GameModeType.Relay) OnReconnected?.Invoke(); };
            relay.OnReconnectFailed += () => { if (_currentMode == GameModeType.Relay) OnReconnectFailed?.Invoke(); };
        }
        
        private void SubscribeLANEvents()
        {
            var room = RoomManager.Instance;
            room.OnRoomCreated += (r) => { if (_currentMode == GameModeType.LAN) OnRoomCreated?.Invoke(r); };
            room.OnRoomJoined += (r) => { if (_currentMode == GameModeType.LAN) OnRoomJoined?.Invoke(r); };
            room.OnRoomLeft += () => { if (_currentMode == GameModeType.LAN) OnRoomLeft?.Invoke(); };
            room.OnPlayerJoined += (p) => { if (_currentMode == GameModeType.LAN) OnPlayerJoined?.Invoke(p); };
            room.OnPlayerLeft += (id, reason) => { if (_currentMode == GameModeType.LAN) OnPlayerLeft?.Invoke(id, reason); };
            room.OnGameStarted += (mapId, mode) => { if (_currentMode == GameModeType.LAN) OnGameStarted?.Invoke(); };
            room.OnRoomListUpdated += (rooms) => { if (_currentMode == GameModeType.LAN) OnRoomListUpdated?.Invoke(rooms); };
            room.OnChatReceived += (sender, content) => { if (_currentMode == GameModeType.LAN) OnChatReceived?.Invoke(sender, content); };
            room.OnError += (error) => { if (_currentMode == GameModeType.LAN) OnError?.Invoke(error); };
            
            var net = NetworkManager.Instance;
            net.OnConnected += () => { if (_currentMode == GameModeType.LAN) OnConnected?.Invoke(); };
            net.OnDisconnected += (reason) => { if (_currentMode == GameModeType.LAN) OnDisconnected?.Invoke(reason); };
        }
        
        #endregion
        
        #region 模式切换
        
        /// <summary>
        /// 设置游戏模式
        /// </summary>
        public void SetMode(GameModeType mode)
        {
            if (_currentMode == mode) return;
            
            // 断开当前连接
            Disconnect();
            
            _currentMode = mode;
            OnModeChanged?.Invoke(mode);
            
            Debug.Log($"[GameMode] 模式切换为: {mode}");
        }
        
        /// <summary>
        /// 切换到单机模式
        /// </summary>
        public void SetOfflineMode()
        {
            SetMode(GameModeType.Offline);
        }
        
        /// <summary>
        /// 切换到局域网模式
        /// </summary>
        public void SetLANMode()
        {
            SetMode(GameModeType.LAN);
        }
        
        /// <summary>
        /// 切换到中继服务器模式
        /// </summary>
        public void SetRelayMode()
        {
            SetMode(GameModeType.Relay);
        }
        
        #endregion
        
        #region 统一API
        
        /// <summary>
        /// 连接（仅联网模式需要）
        /// </summary>
        public void Connect()
        {
            switch (_currentMode)
            {
                case GameModeType.Offline:
                    // 单机模式直接触发连接成功
                    OnConnected?.Invoke();
                    break;
                    
                case GameModeType.LAN:
                    // 局域网模式作为客户端连接
                    // 需要先调用HostGame或JoinGame
                    break;
                    
                case GameModeType.Relay:
                    RelayClient.Instance.Connect(_relayServerIP, _serverPort, _playerName);
                    break;
            }
        }
        
        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            switch (_currentMode)
            {
                case GameModeType.Offline:
                    _offlineRoom = null;
                    _offlinePlayers.Clear();
                    break;
                    
                case GameModeType.LAN:
                    RoomManager.Instance.LeaveRoom();
                    NetworkManager.Instance.Disconnect();
                    break;
                    
                case GameModeType.Relay:
                    RelayClient.Instance.Disconnect(true);
                    break;
            }
        }
        
        /// <summary>
        /// 创建房间/开始游戏
        /// </summary>
        public void CreateRoom(string roomName, int maxPlayers = 4, string password = "")
        {
            switch (_currentMode)
            {
                case GameModeType.Offline:
                    // 单机模式直接创建本地房间
                    CreateOfflineRoom(roomName);
                    break;
                    
                case GameModeType.LAN:
                    NetworkManager.Instance.StartHost(_serverPort);
                    RoomManager.Instance.CreateRoom(roomName, _playerName, maxPlayers, password);
                    break;
                    
                case GameModeType.Relay:
                    RelayClient.Instance.CreateRoom(roomName, maxPlayers, password);
                    break;
            }
        }
        
        /// <summary>
        /// 加入房间
        /// </summary>
        public void JoinRoom(string roomId, string password = "")
        {
            switch (_currentMode)
            {
                case GameModeType.Offline:
                    OnError?.Invoke("单机模式无法加入房间");
                    break;
                    
                case GameModeType.LAN:
                    RoomManager.Instance.JoinRoom(roomId, _playerName, password);
                    break;
                    
                case GameModeType.Relay:
                    RelayClient.Instance.JoinRoom(roomId, password);
                    break;
            }
        }
        
        /// <summary>
        /// 离开房间
        /// </summary>
        public void LeaveRoom()
        {
            switch (_currentMode)
            {
                case GameModeType.Offline:
                    _offlineRoom = null;
                    OnRoomLeft?.Invoke();
                    break;
                    
                case GameModeType.LAN:
                    RoomManager.Instance.LeaveRoom();
                    break;
                    
                case GameModeType.Relay:
                    RelayClient.Instance.LeaveRoom();
                    break;
            }
        }
        
        /// <summary>
        /// 请求房间列表
        /// </summary>
        public void RequestRoomList()
        {
            switch (_currentMode)
            {
                case GameModeType.Offline:
                    OnRoomListUpdated?.Invoke(new List<RoomInfo>());
                    break;
                    
                case GameModeType.LAN:
                    RoomManager.Instance.RequestRoomList();
                    break;
                    
                case GameModeType.Relay:
                    RelayClient.Instance.RequestRoomList();
                    break;
            }
        }
        
        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {
            switch (_currentMode)
            {
                case GameModeType.Offline:
                    // 单机模式直接开始
                    StartOfflineGame();
                    break;
                    
                case GameModeType.LAN:
                    RoomManager.Instance.StartGame();
                    break;
                    
                case GameModeType.Relay:
                    RelayClient.Instance.StartGame();
                    break;
            }
        }
        
        /// <summary>
        /// 快速开始单机游戏（一键启动，用于开发测试）
        /// </summary>
        public void QuickStartOffline()
        {
            SetOfflineMode();
            CreateRoom("单机测试");
            StartGame();
        }
        
        /// <summary>
        /// 发送聊天消息
        /// </summary>
        public void SendChat(string content)
        {
            switch (_currentMode)
            {
                case GameModeType.Offline:
                    OnChatReceived?.Invoke(_playerName, content);
                    break;
                    
                case GameModeType.LAN:
                    RoomManager.Instance.SendChat(content);
                    break;
                    
                case GameModeType.Relay:
                    RelayClient.Instance.SendChat(content);
                    break;
            }
        }
        
        /// <summary>
        /// 踢出玩家
        /// </summary>
        public void KickPlayer(int playerId)
        {
            if (!IsHost) return;
            
            switch (_currentMode)
            {
                case GameModeType.Offline:
                    // 单机模式没有其他玩家
                    break;
                    
                case GameModeType.LAN:
                    RoomManager.Instance.KickPlayer(playerId);
                    break;
                    
                case GameModeType.Relay:
                    RelayClient.Instance.KickPlayer(playerId);
                    break;
            }
        }
        
        #endregion
        
        #region 单机模式实现
        
        /// <summary>
        /// 创建单机房间
        /// </summary>
        private void CreateOfflineRoom(string roomName)
        {
            _offlineRoom = new RoomInfo
            {
                RoomId = "OFFLINE",
                RoomName = roomName,
                HostPlayerId = 0,
                HostName = _playerName,
                CurrentPlayers = 1,
                MaxPlayers = 1,
                HasPassword = false,
                IsInGame = false
            };
            
            var player = new PlayerInfo
            {
                PlayerId = 0,
                PlayerName = _playerName,
                IsHost = true,
                IsReady = true
            };
            _offlineRoom.Players.Add(player);
            _offlinePlayers.Clear();
            _offlinePlayers.Add(player);
            
            OnRoomCreated?.Invoke(_offlineRoom);
            Debug.Log($"[GameMode] 单机房间创建: {roomName}");
        }
        
        /// <summary>
        /// 开始单机游戏
        /// </summary>
        private void StartOfflineGame()
        {
            if (_offlineRoom == null)
            {
                CreateOfflineRoom("单机游戏");
            }
            
            _offlineRoom.IsInGame = true;
            OnGameStarted?.Invoke();
            
            // 启动游戏
            InitializeGame();
            
            Debug.Log("[GameMode] 单机游戏开始");
        }
        
        /// <summary>
        /// 初始化游戏（创建玩家等）
        /// 委托给 GameManager 统一处理游戏逻辑启动
        /// 
        /// 注意：这个方法主要用于旧的直接启动流程（不通过大厅）
        /// 新的流程通过 SceneManager 加载关卡，SceneManager 会调用 GameManager.StartGame()
        /// </summary>
        private void InitializeGame()
        {
            // 检查当前场景类型
            var currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            
            // 如果当前不在游戏关卡场景，说明可能是通过 SceneManager 加载的
            // SceneManager 会负责初始化，这里跳过
            if (currentSceneName != "Main" && !currentSceneName.Contains("Level") && !currentSceneName.Contains("Game"))
            {
                Debug.Log($"[GameMode] 当前场景不是游戏关卡 ({currentSceneName})，跳过 InitializeGame");
                return;
            }
            
            // 调用 GameManager 的 StartGame，传入当前模式的配置
            // GameManager 会负责：清理、创建玩家、加载刷怪点、生成NPC、生成测试物品等
            Vector3 spawnPos = Vector3.zero;
            GamePlay.Manager.GameManager.Instance.StartGame(_playerConfigId, _playerName, spawnPos);
            
            Debug.Log($"[GameMode] 游戏初始化完成，玩家: {_playerName}");
        }
        
        #endregion
        
        #region 工具方法
        
        /// <summary>
        /// 转换RelayRoomInfo到RoomInfo
        /// </summary>
        private RoomInfo ConvertRoom(RelayRoomInfo relayRoom)
        {
            if (relayRoom == null) return null;
            
            var room = new RoomInfo
            {
                RoomId = relayRoom.RoomId,
                RoomName = relayRoom.RoomName,
                HostPlayerId = relayRoom.HostClientId,
                HostName = relayRoom.HostName,
                CurrentPlayers = relayRoom.CurrentPlayers,
                MaxPlayers = relayRoom.MaxPlayers,
                HasPassword = relayRoom.HasPassword,
                IsInGame = relayRoom.IsInGame
            };
            
            foreach (var p in relayRoom.Players)
            {
                room.Players.Add(ConvertPlayer(p));
            }
            
            return room;
        }
        
        /// <summary>
        /// 转换RelayPlayerInfo到PlayerInfo
        /// </summary>
        private PlayerInfo ConvertPlayer(RelayPlayerInfo relayPlayer)
        {
            return new PlayerInfo
            {
                PlayerId = relayPlayer.ClientId,
                PlayerName = relayPlayer.PlayerName,
                IsHost = relayPlayer.IsHost
            };
        }
        
        /// <summary>
        /// 转换房间列表
        /// </summary>
        private List<RoomInfo> ConvertRoomList(List<RelayRoomInfo> relayRooms)
        {
            var list = new List<RoomInfo>();
            foreach (var r in relayRooms)
            {
                list.Add(ConvertRoom(r));
            }
            return list;
        }
        
        /// <summary>
        /// 获取当前房间信息
        /// </summary>
        public RoomInfo GetCurrentRoom()
        {
            switch (_currentMode)
            {
                case GameModeType.Offline:
                    return _offlineRoom;
                case GameModeType.LAN:
                    return RoomManager.Instance.CurrentRoom;
                case GameModeType.Relay:
                    return ConvertRoom(RelayClient.Instance.CurrentRoom);
                default:
                    return null;
            }
        }
        
        /// <summary>
        /// 获取所有玩家
        /// </summary>
        public List<PlayerInfo> GetAllPlayers()
        {
            switch (_currentMode)
            {
                case GameModeType.Offline:
                    return _offlinePlayers;
                case GameModeType.LAN:
                    return RoomManager.Instance.GetAllPlayers();
                case GameModeType.Relay:
                    var relayPlayers = RelayClient.Instance.CurrentRoom?.Players ?? new List<RelayPlayerInfo>();
                    return relayPlayers.Select(p => ConvertPlayer(p)).ToList();
                default:
                    return new List<PlayerInfo>();
            }
        }
        
        #endregion
    }
}

