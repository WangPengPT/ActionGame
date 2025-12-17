using System;
using System.Collections.Generic;
using UnityEngine;

namespace Network
{
    /// <summary>
    /// 网络模式
    /// </summary>
    public enum NetworkMode
    {
        LAN,    // 局域网直连
        Relay   // 中继服务器
    }
    
    /// <summary>
    /// 中继网络适配器 - 让现有代码透明使用中继模式
    /// 将RelayClient的消息转换为NetworkManager的消息格式
    /// </summary>
    public class RelayNetworkAdapter : MonoBehaviour
    {
        #region 单例
        
        private static RelayNetworkAdapter _instance;
        public static RelayNetworkAdapter Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("RelayNetworkAdapter");
                    _instance = go.AddComponent<RelayNetworkAdapter>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region 属性
        
        /// <summary>
        /// 当前网络模式
        /// </summary>
        private NetworkMode _mode = NetworkMode.LAN;
        public NetworkMode Mode => _mode;
        
        /// <summary>
        /// 是否是中继模式
        /// </summary>
        public bool IsRelayMode => _mode == NetworkMode.Relay;
        
        /// <summary>
        /// 中继服务器地址
        /// </summary>
        [SerializeField] private string _relayServerIP = "127.0.0.1";
        public string RelayServerIP 
        { 
            get => _relayServerIP; 
            set => _relayServerIP = value; 
        }
        
        /// <summary>
        /// 中继服务器端口
        /// </summary>
        [SerializeField] private int _relayServerPort = 7777;
        public int RelayServerPort 
        { 
            get => _relayServerPort; 
            set => _relayServerPort = value; 
        }
        
        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected => IsRelayMode ? RelayClient.Instance.IsConnected : NetworkManager.Instance.IsConnected;
        
        /// <summary>
        /// 是否是房主
        /// </summary>
        public bool IsHost => IsRelayMode ? RelayClient.Instance.IsHost : NetworkManager.Instance.IsHost;
        
        /// <summary>
        /// 本地玩家ID
        /// </summary>
        public int LocalPlayerId => IsRelayMode ? RelayClient.Instance.LocalClientId : NetworkManager.Instance.LocalPlayerId;
        
        #endregion
        
        #region 事件
        
        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<RoomInfo> OnRoomCreated;
        public event Action<RoomInfo> OnRoomJoined;
        public event Action OnRoomLeft;
        public event Action<PlayerInfo> OnPlayerJoined;
        public event Action<int, string> OnPlayerLeft;
        public event Action<int> OnHostChanged;
        public event Action<List<RoomInfo>> OnRoomListUpdated;
        public event Action OnGameStarted;
        public event Action<int, byte[]> OnGameDataReceived;
        public event Action<string, string> OnChatReceived;
        public event Action<string> OnError;
        
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
            // 订阅RelayClient事件
            var relay = RelayClient.Instance;
            relay.OnConnected += () => OnConnected?.Invoke();
            relay.OnDisconnected += (reason) => OnDisconnected?.Invoke(reason);
            relay.OnRoomCreated += (room) => OnRoomCreated?.Invoke(ConvertToRoomInfo(room));
            relay.OnRoomJoined += (room) => OnRoomJoined?.Invoke(ConvertToRoomInfo(room));
            relay.OnRoomLeft += () => OnRoomLeft?.Invoke();
            relay.OnPlayerJoined += (player) => OnPlayerJoined?.Invoke(ConvertToPlayerInfo(player));
            relay.OnPlayerLeft += (id, reason) => OnPlayerLeft?.Invoke(id, reason);
            relay.OnHostChanged += (id) => OnHostChanged?.Invoke(id);
            relay.OnRoomListUpdated += (rooms) => OnRoomListUpdated?.Invoke(ConvertToRoomInfoList(rooms));
            relay.OnGameStarted += () => OnGameStarted?.Invoke();
            relay.OnGameDataReceived += (senderId, data) => OnGameDataReceived?.Invoke(senderId, data);
            relay.OnChatReceived += (sender, content) => OnChatReceived?.Invoke(sender, content);
            relay.OnError += (error) => OnError?.Invoke(error);
        }
        
        #endregion
        
        #region 公共方法
        
        /// <summary>
        /// 设置网络模式
        /// </summary>
        public void SetMode(NetworkMode mode)
        {
            _mode = mode;
            Debug.Log($"网络模式设置为: {mode}");
        }
        
        /// <summary>
        /// 连接（中继模式）
        /// </summary>
        public void Connect(string playerName)
        {
            if (_mode == NetworkMode.Relay)
            {
                RelayClient.Instance.Connect(_relayServerIP, _relayServerPort, playerName);
            }
        }
        
        /// <summary>
        /// 连接到指定服务器（中继模式）
        /// </summary>
        public void Connect(string ip, int port, string playerName)
        {
            _mode = NetworkMode.Relay;
            _relayServerIP = ip;
            _relayServerPort = port;
            RelayClient.Instance.Connect(ip, port, playerName);
        }
        
        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            if (_mode == NetworkMode.Relay)
            {
                RelayClient.Instance.Disconnect();
            }
            else
            {
                NetworkManager.Instance.Disconnect();
            }
        }
        
        /// <summary>
        /// 创建房间
        /// </summary>
        public void CreateRoom(string roomName, string playerName, int maxPlayers = 4, string password = "")
        {
            if (_mode == NetworkMode.Relay)
            {
                RelayClient.Instance.PlayerName = playerName;
                RelayClient.Instance.CreateRoom(roomName, maxPlayers, password);
            }
            else
            {
                // 局域网模式使用原有逻辑
                NetworkManager.Instance.StartHost();
                RoomManager.Instance.CreateRoom(roomName, playerName, maxPlayers, password);
            }
        }
        
        /// <summary>
        /// 加入房间
        /// </summary>
        public void JoinRoom(string roomId, string playerName, string password = "")
        {
            if (_mode == NetworkMode.Relay)
            {
                RelayClient.Instance.PlayerName = playerName;
                RelayClient.Instance.JoinRoom(roomId, password);
            }
            else
            {
                RoomManager.Instance.JoinRoom(roomId, playerName, password);
            }
        }
        
        /// <summary>
        /// 离开房间
        /// </summary>
        public void LeaveRoom()
        {
            if (_mode == NetworkMode.Relay)
            {
                RelayClient.Instance.LeaveRoom();
            }
            else
            {
                RoomManager.Instance.LeaveRoom();
            }
        }
        
        /// <summary>
        /// 请求房间列表
        /// </summary>
        public void RequestRoomList()
        {
            if (_mode == NetworkMode.Relay)
            {
                RelayClient.Instance.RequestRoomList();
            }
            else
            {
                RoomManager.Instance.RequestRoomList();
            }
        }
        
        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {
            if (_mode == NetworkMode.Relay)
            {
                RelayClient.Instance.StartGame();
            }
            else
            {
                RoomManager.Instance.StartGame();
            }
        }
        
        /// <summary>
        /// 发送游戏数据
        /// </summary>
        public void SendGameData(byte[] data)
        {
            if (_mode == NetworkMode.Relay)
            {
                RelayClient.Instance.SendGameData(data);
            }
            else
            {
                // 局域网模式直接通过NetworkManager发送
                // 将数据包装为GameData消息
            }
        }
        
        /// <summary>
        /// 发送聊天消息
        /// </summary>
        public void SendChat(string content)
        {
            if (_mode == NetworkMode.Relay)
            {
                RelayClient.Instance.SendChat(content);
            }
            else
            {
                RoomManager.Instance.SendChat(content);
            }
        }
        
        /// <summary>
        /// 踢出玩家
        /// </summary>
        public void KickPlayer(int playerId)
        {
            if (_mode == NetworkMode.Relay)
            {
                RelayClient.Instance.KickPlayer(playerId);
            }
            else
            {
                RoomManager.Instance.KickPlayer(playerId);
            }
        }
        
        #endregion
        
        #region 转换方法
        
        private RoomInfo ConvertToRoomInfo(RelayRoomInfo relayRoom)
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
                room.Players.Add(ConvertToPlayerInfo(p));
            }
            
            return room;
        }
        
        private PlayerInfo ConvertToPlayerInfo(RelayPlayerInfo relayPlayer)
        {
            return new PlayerInfo
            {
                PlayerId = relayPlayer.ClientId,
                PlayerName = relayPlayer.PlayerName,
                IsHost = relayPlayer.IsHost
            };
        }
        
        private List<RoomInfo> ConvertToRoomInfoList(List<RelayRoomInfo> relayRooms)
        {
            var list = new List<RoomInfo>();
            foreach (var r in relayRooms)
            {
                list.Add(ConvertToRoomInfo(r));
            }
            return list;
        }
        
        #endregion
    }
}

