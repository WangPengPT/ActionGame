using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Network
{
    /// <summary>
    /// 房间状态
    /// </summary>
    public enum RoomState
    {
        None,
        Waiting,    // 等待中
        Starting,   // 开始中
        InGame,     // 游戏中
        Finished    // 已结束
    }
    
    /// <summary>
    /// 房间管理器 - 管理房间创建、加入、离开等
    /// </summary>
    public class RoomManager : MonoBehaviour
    {
        #region 单例
        
        private static RoomManager _instance;
        public static RoomManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("RoomManager");
                    _instance = go.AddComponent<RoomManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region 属性
        
        /// <summary>
        /// 当前房间信息
        /// </summary>
        private RoomInfo _currentRoom;
        public RoomInfo CurrentRoom => _currentRoom;
        
        /// <summary>
        /// 房间状态
        /// </summary>
        private RoomState _roomState = RoomState.None;
        public RoomState RoomState => _roomState;
        
        /// <summary>
        /// 是否在房间中
        /// </summary>
        public bool IsInRoom => _currentRoom != null;
        
        /// <summary>
        /// 是否是房主
        /// </summary>
        public bool IsHost => _currentRoom != null && 
            _currentRoom.HostPlayerId == NetworkManager.Instance.LocalPlayerId;
        
        /// <summary>
        /// 本地玩家信息
        /// </summary>
        private PlayerInfo _localPlayer;
        public PlayerInfo LocalPlayer => _localPlayer;
        
        /// <summary>
        /// 所有房间列表（服务器端保存）
        /// </summary>
        private Dictionary<string, RoomInfo> _rooms = new Dictionary<string, RoomInfo>();
        
        /// <summary>
        /// 可用房间列表（客户端查询结果）
        /// </summary>
        private List<RoomInfo> _availableRooms = new List<RoomInfo>();
        public IReadOnlyList<RoomInfo> AvailableRooms => _availableRooms;
        
        #endregion
        
        #region 事件
        
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
        /// 玩家加入房间
        /// </summary>
        public event Action<PlayerInfo> OnPlayerJoined;
        
        /// <summary>
        /// 玩家离开房间
        /// </summary>
        public event Action<int, string> OnPlayerLeft;
        
        /// <summary>
        /// 房主变更
        /// </summary>
        public event Action<int> OnHostChanged;
        
        /// <summary>
        /// 游戏开始
        /// </summary>
        public event Action<int, int> OnGameStarted; // mapId, gameMode
        
        /// <summary>
        /// 房间列表更新
        /// </summary>
        public event Action<List<RoomInfo>> OnRoomListUpdated;
        
        /// <summary>
        /// 收到聊天消息
        /// </summary>
        public event Action<string, string> OnChatReceived; // senderName, content
        
        /// <summary>
        /// 错误事件
        /// </summary>
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
            RegisterMessageHandlers();
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
            nm.RegisterHandler(NetworkMessageType.RoomCreate, HandleRoomCreate);
            nm.RegisterHandler(NetworkMessageType.RoomCreateResponse, HandleRoomCreateResponse);
            nm.RegisterHandler(NetworkMessageType.RoomJoin, HandleRoomJoin);
            nm.RegisterHandler(NetworkMessageType.RoomJoinResponse, HandleRoomJoinResponse);
            nm.RegisterHandler(NetworkMessageType.RoomLeave, HandleRoomLeave);
            nm.RegisterHandler(NetworkMessageType.RoomList, HandleRoomList);
            nm.RegisterHandler(NetworkMessageType.RoomListResponse, HandleRoomListResponse);
            nm.RegisterHandler(NetworkMessageType.RoomPlayerJoined, HandlePlayerJoined);
            nm.RegisterHandler(NetworkMessageType.RoomPlayerLeft, HandlePlayerLeft);
            nm.RegisterHandler(NetworkMessageType.RoomHostChanged, HandleHostChanged);
            nm.RegisterHandler(NetworkMessageType.RoomStartGame, HandleStartGame);
            nm.RegisterHandler(NetworkMessageType.RoomGameStarted, HandleGameStarted);
            nm.RegisterHandler(NetworkMessageType.RoomChat, HandleChat);
            
            nm.OnClientConnected += OnClientConnected;
            nm.OnClientDisconnected += OnClientDisconnected;
        }
        
        private void UnregisterMessageHandlers()
        {
            var nm = NetworkManager.Instance;
            if (nm == null) return;
            
            nm.UnregisterHandler(NetworkMessageType.RoomCreate);
            nm.UnregisterHandler(NetworkMessageType.RoomCreateResponse);
            nm.UnregisterHandler(NetworkMessageType.RoomJoin);
            nm.UnregisterHandler(NetworkMessageType.RoomJoinResponse);
            nm.UnregisterHandler(NetworkMessageType.RoomLeave);
            nm.UnregisterHandler(NetworkMessageType.RoomList);
            nm.UnregisterHandler(NetworkMessageType.RoomListResponse);
            nm.UnregisterHandler(NetworkMessageType.RoomPlayerJoined);
            nm.UnregisterHandler(NetworkMessageType.RoomPlayerLeft);
            nm.UnregisterHandler(NetworkMessageType.RoomHostChanged);
            nm.UnregisterHandler(NetworkMessageType.RoomStartGame);
            nm.UnregisterHandler(NetworkMessageType.RoomGameStarted);
            nm.UnregisterHandler(NetworkMessageType.RoomChat);
            
            nm.OnClientConnected -= OnClientConnected;
            nm.OnClientDisconnected -= OnClientDisconnected;
        }
        
        #endregion
        
        #region 房间操作 - 客户端
        
        /// <summary>
        /// 创建房间（作为房主）
        /// </summary>
        public void CreateRoom(string roomName, string playerName, int maxPlayers = 4, string password = "")
        {
            if (NetworkManager.Instance.IsHost)
            {
                // 本地创建房间（作为服务器）
                CreateRoomLocal(roomName, playerName, maxPlayers, password);
            }
            else
            {
                // 发送创建请求到服务器
                var msg = new RoomCreateMessage
                {
                    RoomName = roomName,
                    HostName = playerName,
                    MaxPlayers = maxPlayers,
                    Password = password
                };
                NetworkManager.Instance.SendToServer(msg);
            }
        }
        
        /// <summary>
        /// 加入房间
        /// </summary>
        public void JoinRoom(string roomId, string playerName, string password = "")
        {
            var msg = new RoomJoinMessage
            {
                RoomId = roomId,
                PlayerName = playerName,
                Password = password
            };
            NetworkManager.Instance.SendToServer(msg);
        }
        
        /// <summary>
        /// 离开房间
        /// </summary>
        public void LeaveRoom()
        {
            if (!IsInRoom) return;
            
            if (NetworkManager.Instance.IsHost)
            {
                // 房主离开，关闭房间
                CloseRoom();
            }
            else
            {
                // 发送离开请求
                var msg = new RoomPlayerLeftMessage
                {
                    PlayerId = NetworkManager.Instance.LocalPlayerId,
                    Reason = "主动离开"
                };
                NetworkManager.Instance.SendToServer(msg);
            }
            
            ClearCurrentRoom();
            OnRoomLeft?.Invoke();
        }
        
        /// <summary>
        /// 请求房间列表
        /// </summary>
        public void RequestRoomList()
        {
            NetworkManager.Instance.SendToServer(new RoomListRequestMessage());
        }
        
        /// <summary>
        /// 发起开始游戏（房主专用）
        /// </summary>
        public void StartGame(int mapId = 0, int gameMode = 0)
        {
            if (!IsHost)
            {
                OnError?.Invoke("只有房主可以开始游戏");
                return;
            }
            
            if (_currentRoom.CurrentPlayers < 1)
            {
                OnError?.Invoke("房间内没有其他玩家");
                return;
            }
            
            _roomState = RoomState.Starting;
            _currentRoom.IsInGame = true;
            
            // 广播游戏开始
            var msg = new RoomStartGameMessage
            {
                MapId = mapId,
                GameMode = gameMode
            };
            NetworkManager.Instance.BroadcastToClients(msg);
            
            // 本地也触发
            OnGameStarted?.Invoke(mapId, gameMode);
            _roomState = RoomState.InGame;
        }
        
        /// <summary>
        /// 踢出玩家（房主专用）
        /// </summary>
        public void KickPlayer(int playerId)
        {
            if (!IsHost)
            {
                OnError?.Invoke("只有房主可以踢出玩家");
                return;
            }
            
            var msg = new RoomPlayerLeftMessage
            {
                PlayerId = playerId,
                Reason = "被房主踢出"
            };
            
            // 通知被踢玩家
            NetworkManager.Instance.SendToClient(playerId, msg);
            
            // 从房间移除
            RemovePlayerFromRoom(playerId, "被房主踢出");
        }
        
        /// <summary>
        /// 发送聊天消息
        /// </summary>
        public void SendChat(string content)
        {
            if (!IsInRoom) return;
            
            var msg = new RoomChatMessage
            {
                Content = content,
                SenderName = _localPlayer?.PlayerName ?? "Unknown"
            };
            
            if (NetworkManager.Instance.IsHost)
            {
                // 房主广播
                NetworkManager.Instance.BroadcastToClients(msg);
                OnChatReceived?.Invoke(msg.SenderName, msg.Content);
            }
            else
            {
                NetworkManager.Instance.SendToServer(msg);
            }
        }
        
        #endregion
        
        #region 房间操作 - 服务器端
        
        /// <summary>
        /// 本地创建房间（服务器端）
        /// </summary>
        private void CreateRoomLocal(string roomName, string hostName, int maxPlayers, string password)
        {
            string roomId = Guid.NewGuid().ToString("N").Substring(0, 8);
            
            var room = new RoomInfo
            {
                RoomId = roomId,
                RoomName = roomName,
                HostPlayerId = 0, // 房主ID为0
                HostName = hostName,
                MaxPlayers = maxPlayers,
                HasPassword = !string.IsNullOrEmpty(password),
                CurrentPlayers = 1,
                IsInGame = false
            };
            
            // 添加房主到玩家列表
            var hostPlayer = new PlayerInfo
            {
                PlayerId = 0,
                PlayerName = hostName,
                IsHost = true,
                IsReady = true
            };
            room.Players.Add(hostPlayer);
            
            _rooms[roomId] = room;
            _currentRoom = room;
            _localPlayer = hostPlayer;
            _roomState = RoomState.Waiting;
            
            NetworkManager.Instance.SetLocalPlayerId(0);
            
            OnRoomCreated?.Invoke(room);
            Debug.Log($"房间创建成功: {roomName} ({roomId})");
        }
        
        /// <summary>
        /// 关闭房间
        /// </summary>
        private void CloseRoom()
        {
            if (_currentRoom == null) return;
            
            // 通知所有玩家房间关闭
            var msg = new RoomPlayerLeftMessage
            {
                PlayerId = -1,
                Reason = "房间已关闭"
            };
            NetworkManager.Instance.BroadcastToClients(msg);
            
            _rooms.Remove(_currentRoom.RoomId);
            ClearCurrentRoom();
        }
        
        /// <summary>
        /// 将玩家添加到当前房间
        /// </summary>
        private bool AddPlayerToRoom(int clientId, string playerName, out string error)
        {
            error = null;
            
            if (_currentRoom == null)
            {
                error = "房间不存在";
                return false;
            }
            
            if (_currentRoom.CurrentPlayers >= _currentRoom.MaxPlayers)
            {
                error = "房间已满";
                return false;
            }
            
            if (_currentRoom.IsInGame)
            {
                error = "游戏已开始";
                return false;
            }
            
            var player = new PlayerInfo
            {
                PlayerId = clientId,
                PlayerName = playerName,
                IsHost = false,
                IsReady = false
            };
            
            _currentRoom.Players.Add(player);
            _currentRoom.CurrentPlayers++;
            
            return true;
        }
        
        /// <summary>
        /// 从房间移除玩家
        /// </summary>
        private void RemovePlayerFromRoom(int playerId, string reason)
        {
            if (_currentRoom == null) return;
            
            var player = _currentRoom.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player != null)
            {
                _currentRoom.Players.Remove(player);
                _currentRoom.CurrentPlayers--;
                
                // 广播玩家离开
                var msg = new RoomPlayerLeftMessage
                {
                    PlayerId = playerId,
                    Reason = reason
                };
                NetworkManager.Instance.BroadcastToClients(msg, playerId);
                
                OnPlayerLeft?.Invoke(playerId, reason);
                
                // 如果房间空了，关闭房间
                if (_currentRoom.CurrentPlayers == 0)
                {
                    _rooms.Remove(_currentRoom.RoomId);
                    ClearCurrentRoom();
                }
            }
        }
        
        /// <summary>
        /// 转移房主
        /// </summary>
        private void TransferHost(int newHostId)
        {
            if (_currentRoom == null) return;
            
            var newHost = _currentRoom.Players.FirstOrDefault(p => p.PlayerId == newHostId);
            if (newHost != null)
            {
                // 更新房主信息
                foreach (var p in _currentRoom.Players)
                {
                    p.IsHost = p.PlayerId == newHostId;
                }
                
                _currentRoom.HostPlayerId = newHostId;
                _currentRoom.HostName = newHost.PlayerName;
                
                // 广播房主变更
                var msg = new RoomHostChangedMessage
                {
                    NewHostPlayerId = newHostId
                };
                NetworkManager.Instance.BroadcastToClients(msg);
                
                OnHostChanged?.Invoke(newHostId);
            }
        }
        
        #endregion
        
        #region 消息处理
        
        /// <summary>
        /// 处理创建房间请求（服务器端）
        /// </summary>
        private void HandleRoomCreate(int senderId, NetworkMessage message)
        {
            if (!NetworkManager.Instance.IsHost) return;
            
            var msg = message as RoomCreateMessage;
            
            // 创建房间
            string roomId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var room = new RoomInfo
            {
                RoomId = roomId,
                RoomName = msg.RoomName,
                HostPlayerId = senderId,
                HostName = msg.HostName,
                MaxPlayers = msg.MaxPlayers,
                HasPassword = !string.IsNullOrEmpty(msg.Password),
                CurrentPlayers = 1,
                IsInGame = false
            };
            
            var player = new PlayerInfo
            {
                PlayerId = senderId,
                PlayerName = msg.HostName,
                IsHost = true,
                IsReady = true
            };
            room.Players.Add(player);
            
            _rooms[roomId] = room;
            
            // 发送响应
            var response = new RoomCreateResponseMessage
            {
                Success = true,
                RoomId = roomId
            };
            NetworkManager.Instance.SendToClient(senderId, response);
        }
        
        /// <summary>
        /// 处理创建房间响应（客户端）
        /// </summary>
        private void HandleRoomCreateResponse(int senderId, NetworkMessage message)
        {
            var msg = message as RoomCreateResponseMessage;
            
            if (msg.Success)
            {
                Debug.Log($"房间创建成功: {msg.RoomId}");
                // 需要再请求加入来获取完整房间信息
            }
            else
            {
                OnError?.Invoke(msg.ErrorMessage);
            }
        }
        
        /// <summary>
        /// 处理加入房间请求（服务器端）
        /// </summary>
        private void HandleRoomJoin(int senderId, NetworkMessage message)
        {
            if (!NetworkManager.Instance.IsHost) return;
            
            var msg = message as RoomJoinMessage;
            var response = new RoomJoinResponseMessage();
            
            if (AddPlayerToRoom(senderId, msg.PlayerName, out string error))
            {
                response.Success = true;
                response.RoomInfo = _currentRoom;
                response.AssignedPlayerId = senderId;
                
                // 通知其他玩家
                var joinedMsg = new RoomPlayerJoinedMessage
                {
                    Player = _currentRoom.Players.Last()
                };
                NetworkManager.Instance.BroadcastToClients(joinedMsg, senderId);
                
                OnPlayerJoined?.Invoke(joinedMsg.Player);
            }
            else
            {
                response.Success = false;
                response.ErrorMessage = error;
            }
            
            NetworkManager.Instance.SendToClient(senderId, response);
        }
        
        /// <summary>
        /// 处理加入房间响应（客户端）
        /// </summary>
        private void HandleRoomJoinResponse(int senderId, NetworkMessage message)
        {
            var msg = message as RoomJoinResponseMessage;
            
            if (msg.Success)
            {
                _currentRoom = msg.RoomInfo;
                _localPlayer = _currentRoom.Players.FirstOrDefault(p => p.PlayerId == msg.AssignedPlayerId);
                NetworkManager.Instance.SetLocalPlayerId(msg.AssignedPlayerId);
                _roomState = RoomState.Waiting;
                
                OnRoomJoined?.Invoke(_currentRoom);
                Debug.Log($"加入房间成功: {_currentRoom.RoomName}");
            }
            else
            {
                OnError?.Invoke(msg.ErrorMessage);
            }
        }
        
        /// <summary>
        /// 处理离开房间（服务器端）
        /// </summary>
        private void HandleRoomLeave(int senderId, NetworkMessage message)
        {
            if (!NetworkManager.Instance.IsHost) return;
            
            RemovePlayerFromRoom(senderId, "主动离开");
        }
        
        /// <summary>
        /// 处理房间列表请求（服务器端）
        /// </summary>
        private void HandleRoomList(int senderId, NetworkMessage message)
        {
            if (!NetworkManager.Instance.IsHost) return;
            
            var response = new RoomListResponseMessage
            {
                Rooms = _rooms.Values.Where(r => !r.IsInGame).ToList()
            };
            NetworkManager.Instance.SendToClient(senderId, response);
        }
        
        /// <summary>
        /// 处理房间列表响应（客户端）
        /// </summary>
        private void HandleRoomListResponse(int senderId, NetworkMessage message)
        {
            var msg = message as RoomListResponseMessage;
            _availableRooms = msg.Rooms;
            OnRoomListUpdated?.Invoke(_availableRooms);
        }
        
        /// <summary>
        /// 处理玩家加入通知
        /// </summary>
        private void HandlePlayerJoined(int senderId, NetworkMessage message)
        {
            var msg = message as RoomPlayerJoinedMessage;
            
            if (_currentRoom != null && !_currentRoom.Players.Any(p => p.PlayerId == msg.Player.PlayerId))
            {
                _currentRoom.Players.Add(msg.Player);
                _currentRoom.CurrentPlayers++;
            }
            
            OnPlayerJoined?.Invoke(msg.Player);
        }
        
        /// <summary>
        /// 处理玩家离开通知
        /// </summary>
        private void HandlePlayerLeft(int senderId, NetworkMessage message)
        {
            var msg = message as RoomPlayerLeftMessage;
            
            // 检查是否是自己被踢或房间关闭
            if (msg.PlayerId == NetworkManager.Instance.LocalPlayerId || msg.PlayerId == -1)
            {
                ClearCurrentRoom();
                OnRoomLeft?.Invoke();
                return;
            }
            
            if (_currentRoom != null)
            {
                var player = _currentRoom.Players.FirstOrDefault(p => p.PlayerId == msg.PlayerId);
                if (player != null)
                {
                    _currentRoom.Players.Remove(player);
                    _currentRoom.CurrentPlayers--;
                }
            }
            
            OnPlayerLeft?.Invoke(msg.PlayerId, msg.Reason);
        }
        
        /// <summary>
        /// 处理房主变更
        /// </summary>
        private void HandleHostChanged(int senderId, NetworkMessage message)
        {
            var msg = message as RoomHostChangedMessage;
            
            if (_currentRoom != null)
            {
                _currentRoom.HostPlayerId = msg.NewHostPlayerId;
                foreach (var p in _currentRoom.Players)
                {
                    p.IsHost = p.PlayerId == msg.NewHostPlayerId;
                }
            }
            
            OnHostChanged?.Invoke(msg.NewHostPlayerId);
        }
        
        /// <summary>
        /// 处理开始游戏请求（服务器端）
        /// </summary>
        private void HandleStartGame(int senderId, NetworkMessage message)
        {
            if (!NetworkManager.Instance.IsHost) return;
            
            var msg = message as RoomStartGameMessage;
            
            // 验证是否是房主
            if (_currentRoom?.HostPlayerId != senderId)
            {
                return;
            }
            
            StartGame(msg.MapId, msg.GameMode);
        }
        
        /// <summary>
        /// 处理游戏开始通知（客户端）
        /// </summary>
        private void HandleGameStarted(int senderId, NetworkMessage message)
        {
            var msg = message as RoomStartGameMessage;
            
            if (_currentRoom != null)
            {
                _currentRoom.IsInGame = true;
            }
            _roomState = RoomState.InGame;
            
            OnGameStarted?.Invoke(msg.MapId, msg.GameMode);
        }
        
        /// <summary>
        /// 处理聊天消息
        /// </summary>
        private void HandleChat(int senderId, NetworkMessage message)
        {
            var msg = message as RoomChatMessage;
            
            if (NetworkManager.Instance.IsHost)
            {
                // 服务器转发给其他玩家
                NetworkManager.Instance.BroadcastToClients(msg, senderId);
            }
            
            OnChatReceived?.Invoke(msg.SenderName, msg.Content);
        }
        
        #endregion
        
        #region 客户端连接/断开回调
        
        private void OnClientConnected(int clientId)
        {
            Debug.Log($"客户端 {clientId} 连接到服务器");
        }
        
        private void OnClientDisconnected(int clientId)
        {
            // 从房间移除断开的玩家
            RemovePlayerFromRoom(clientId, "断开连接");
        }
        
        #endregion
        
        #region 工具方法
        
        /// <summary>
        /// 清除当前房间
        /// </summary>
        private void ClearCurrentRoom()
        {
            _currentRoom = null;
            _localPlayer = null;
            _roomState = RoomState.None;
        }
        
        /// <summary>
        /// 获取玩家信息
        /// </summary>
        public PlayerInfo GetPlayer(int playerId)
        {
            return _currentRoom?.Players.FirstOrDefault(p => p.PlayerId == playerId);
        }
        
        /// <summary>
        /// 获取所有玩家
        /// </summary>
        public List<PlayerInfo> GetAllPlayers()
        {
            return _currentRoom?.Players ?? new List<PlayerInfo>();
        }
        
        #endregion
    }
    
    /// <summary>
    /// 简单的基础网络消息（用于无数据的消息类型）
    /// </summary>
    [Serializable]
    public class SimpleNetworkMessage : NetworkMessage
    {
        public SimpleNetworkMessage(NetworkMessageType type) : base(type) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data) { }
    }
}

