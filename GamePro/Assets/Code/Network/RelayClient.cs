using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Network
{
    /// <summary>
    /// 中继消息类型（与服务器保持一致）
    /// </summary>
    public enum RelayMessageType
    {
        // 系统消息
        Welcome = 0,
        Error = 1,
        Ping = 2,
        Pong = 3,
        
        // 房间消息
        CreateRoom = 10,
        RoomCreated = 11,
        JoinRoom = 12,
        RoomJoined = 13,
        LeaveRoom = 14,
        GetRoomList = 15,
        RoomList = 16,
        PlayerJoined = 17,
        PlayerLeft = 18,
        HostChanged = 19,
        Kicked = 20,
        
        // 游戏消息
        StartGame = 30,
        GameStarted = 31,
        GameData = 32,
        
        // 其他
        Chat = 50,
        SetPlayerName = 51,
        KickPlayer = 52,
    }
    
    /// <summary>
    /// 中继消息
    /// </summary>
    [Serializable]
    public class RelayMessage
    {
        public RelayMessageType Type;
        public int ClientId;
        public string RoomId;
        public string Data;
        public byte[] RawData;

        public byte[] Serialize()
        {
            var roomIdBytes = string.IsNullOrEmpty(RoomId) ? new byte[0] : Encoding.UTF8.GetBytes(RoomId);
            var dataBytes = string.IsNullOrEmpty(Data) ? new byte[0] : Encoding.UTF8.GetBytes(Data);
            var rawData = RawData ?? new byte[0];

            int totalLen = 4 + 4 + roomIdBytes.Length + 4 + dataBytes.Length + 4 + rawData.Length;
            byte[] result = new byte[totalLen];
            int offset = 0;

            BitConverter.GetBytes(ClientId).CopyTo(result, offset); offset += 4;
            BitConverter.GetBytes(roomIdBytes.Length).CopyTo(result, offset); offset += 4;
            roomIdBytes.CopyTo(result, offset); offset += roomIdBytes.Length;
            BitConverter.GetBytes(dataBytes.Length).CopyTo(result, offset); offset += 4;
            dataBytes.CopyTo(result, offset); offset += dataBytes.Length;
            BitConverter.GetBytes(rawData.Length).CopyTo(result, offset); offset += 4;
            rawData.CopyTo(result, offset);

            return result;
        }

        public static RelayMessage Deserialize(byte[] data)
        {
            var msg = new RelayMessage();
            int offset = 0;

            msg.ClientId = BitConverter.ToInt32(data, offset); offset += 4;
            
            int roomIdLen = BitConverter.ToInt32(data, offset); offset += 4;
            msg.RoomId = roomIdLen > 0 ? Encoding.UTF8.GetString(data, offset, roomIdLen) : null;
            offset += roomIdLen;

            int dataLen = BitConverter.ToInt32(data, offset); offset += 4;
            msg.Data = dataLen > 0 ? Encoding.UTF8.GetString(data, offset, dataLen) : null;
            offset += dataLen;

            int rawLen = BitConverter.ToInt32(data, offset); offset += 4;
            msg.RawData = rawLen > 0 ? new byte[rawLen] : null;
            if (rawLen > 0)
            {
                Array.Copy(data, offset, msg.RawData, 0, rawLen);
            }

            return msg;
        }
    }
    
    /// <summary>
    /// 中继房间信息
    /// </summary>
    public class RelayRoomInfo
    {
        public string RoomId;
        public string RoomName;
        public int CurrentPlayers;
        public int MaxPlayers;
        public bool HasPassword;
        public string HostName;
        public int HostClientId;
        public bool IsInGame;
        public List<RelayPlayerInfo> Players = new List<RelayPlayerInfo>();
    }
    
    /// <summary>
    /// 中继玩家信息
    /// </summary>
    public class RelayPlayerInfo
    {
        public int ClientId;
        public string PlayerName;
        public bool IsHost;
    }
    
    /// <summary>
    /// 中继客户端 - 连接到中继服务器
    /// </summary>
    public class RelayClient : MonoBehaviour
    {
        #region 单例
        
        private static RelayClient _instance;
        public static RelayClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("RelayClient");
                    _instance = go.AddComponent<RelayClient>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region 属性
        
        private TcpClient _client;
        private NetworkStream _stream;
        private bool _isRunning;
        
        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected => _client != null && _client.Connected;
        
        /// <summary>
        /// 本地客户端ID（由服务器分配）
        /// </summary>
        private int _localClientId = -1;
        public int LocalClientId => _localClientId;
        
        /// <summary>
        /// 玩家名称
        /// </summary>
        private string _playerName = "Player";
        public string PlayerName 
        { 
            get => _playerName;
            set
            {
                _playerName = value;
                if (IsConnected)
                {
                    Send(new RelayMessage { Type = RelayMessageType.SetPlayerName, Data = value });
                }
            }
        }
        
        /// <summary>
        /// 当前房间
        /// </summary>
        private RelayRoomInfo _currentRoom;
        public RelayRoomInfo CurrentRoom => _currentRoom;
        
        /// <summary>
        /// 是否在房间中
        /// </summary>
        public bool IsInRoom => _currentRoom != null;
        
        /// <summary>
        /// 是否是房主
        /// </summary>
        public bool IsHost => _currentRoom != null && _currentRoom.HostClientId == _localClientId;
        
        /// <summary>
        /// 消息队列
        /// </summary>
        private Queue<RelayMessage> _messageQueue = new Queue<RelayMessage>();
        private readonly object _queueLock = new object();
        
        #region 心跳和重连配置
        
        /// <summary>
        /// 心跳发送间隔（秒）
        /// </summary>
        [SerializeField] private float _heartbeatInterval = 5f;
        
        /// <summary>
        /// 心跳计时器
        /// </summary>
        private float _heartbeatTimer;
        
        /// <summary>
        /// 是否启用自动重连
        /// </summary>
        [SerializeField] private bool _autoReconnect = true;
        public bool AutoReconnect 
        { 
            get => _autoReconnect; 
            set => _autoReconnect = value; 
        }
        
        /// <summary>
        /// 最大重连次数
        /// </summary>
        [SerializeField] private int _maxReconnectAttempts = 5;
        
        /// <summary>
        /// 重连间隔（秒）
        /// </summary>
        [SerializeField] private float _reconnectInterval = 3f;
        
        /// <summary>
        /// 当前重连次数
        /// </summary>
        private int _reconnectAttempts;
        
        /// <summary>
        /// 是否正在重连
        /// </summary>
        private bool _isReconnecting;
        public bool IsReconnecting => _isReconnecting;
        
        /// <summary>
        /// 上次连接的服务器信息（用于重连）
        /// </summary>
        private string _lastServerIP;
        private int _lastServerPort;
        
        /// <summary>
        /// 上次所在房间ID（用于重连后自动加入）
        /// </summary>
        private string _lastRoomId;
        
        #endregion
        
        /// <summary>
        /// 可用房间列表
        /// </summary>
        private List<RelayRoomInfo> _availableRooms = new List<RelayRoomInfo>();
        public IReadOnlyList<RelayRoomInfo> AvailableRooms => _availableRooms;
        
        #endregion
        
        #region 事件
        
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
        public event Action<RelayRoomInfo> OnRoomCreated;
        
        /// <summary>
        /// 加入房间成功
        /// </summary>
        public event Action<RelayRoomInfo> OnRoomJoined;
        
        /// <summary>
        /// 离开房间
        /// </summary>
        public event Action OnRoomLeft;
        
        /// <summary>
        /// 玩家加入
        /// </summary>
        public event Action<RelayPlayerInfo> OnPlayerJoined;
        
        /// <summary>
        /// 玩家离开
        /// </summary>
        public event Action<int, string> OnPlayerLeft;
        
        /// <summary>
        /// 房主变更
        /// </summary>
        public event Action<int> OnHostChanged;
        
        /// <summary>
        /// 被踢出
        /// </summary>
        public event Action<string> OnKicked;
        
        /// <summary>
        /// 房间列表更新
        /// </summary>
        public event Action<List<RelayRoomInfo>> OnRoomListUpdated;
        
        /// <summary>
        /// 游戏开始
        /// </summary>
        public event Action OnGameStarted;
        
        /// <summary>
        /// 收到游戏数据
        /// </summary>
        public event Action<int, byte[]> OnGameDataReceived;
        
        /// <summary>
        /// 收到聊天消息
        /// </summary>
        public event Action<string, string> OnChatReceived;
        
        /// <summary>
        /// 错误
        /// </summary>
        public event Action<string> OnError;
        
        /// <summary>
        /// 开始重连
        /// </summary>
        public event Action<int, int> OnReconnecting; // 当前次数, 最大次数
        
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
        
        private void Update()
        {
            ProcessMessageQueue();
            
            // 心跳发送
            if (IsConnected && !_isReconnecting)
            {
                _heartbeatTimer += Time.deltaTime;
                if (_heartbeatTimer >= _heartbeatInterval)
                {
                    _heartbeatTimer = 0;
                    SendHeartbeat();
                }
            }
        }
        
        /// <summary>
        /// 发送心跳
        /// </summary>
        private void SendHeartbeat()
        {
            try
            {
                Send(new RelayMessage { Type = RelayMessageType.Ping });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"发送心跳失败: {e.Message}");
            }
        }
        
        private void OnDestroy()
        {
            Disconnect();
            if (_instance == this) _instance = null;
        }
        
        private void OnApplicationQuit()
        {
            Disconnect();
        }
        
        #endregion
        
        #region 连接
        
        /// <summary>
        /// 连接到中继服务器
        /// </summary>
        public void Connect(string ip, int port, string playerName)
        {
            if (IsConnected)
            {
                Debug.LogWarning("已经连接到服务器");
                return;
            }
            
            _playerName = playerName;
            _lastServerIP = ip;
            _lastServerPort = port;
            
            try
            {
                _client = new TcpClient();
                _client.BeginConnect(ip, port, OnConnectCallback, null);
                Debug.Log($"正在连接到 {ip}:{port}...");
            }
            catch (Exception e)
            {
                Debug.LogError($"连接失败: {e.Message}");
                OnError?.Invoke(e.Message);
                
                // 如果是重连模式，尝试下一次
                if (_isReconnecting)
                {
                    TryReconnect();
                }
            }
        }
        
        private void OnConnectCallback(IAsyncResult ar)
        {
            try
            {
                _client.EndConnect(ar);
                _stream = _client.GetStream();
                _isRunning = true;
                
                // 启动接收线程
                Thread receiveThread = new Thread(ReceiveThread);
                receiveThread.IsBackground = true;
                receiveThread.Start();
                
                // 如果是重连成功
                if (_isReconnecting)
                {
                    _isReconnecting = false;
                    _reconnectAttempts = 0;
                    EnqueueCallback(() =>
                    {
                        OnReconnected?.Invoke();
                        Debug.Log("重连成功！");
                    });
                }
                
                Debug.Log("连接成功，等待服务器分配ID...");
            }
            catch (Exception e)
            {
                EnqueueCallback(() =>
                {
                    OnError?.Invoke(e.Message);
                    
                    // 如果是重连模式，继续尝试
                    if (_isReconnecting)
                    {
                        TryReconnect();
                    }
                    else
                    {
                        OnDisconnected?.Invoke(e.Message);
                    }
                });
            }
        }
        
        /// <summary>
        /// 断开连接
        /// </summary>
        /// <param name="disableReconnect">是否禁用自动重连</param>
        public void Disconnect(bool disableReconnect = true)
        {
            if (disableReconnect)
            {
                _isReconnecting = false;
                _reconnectAttempts = 0;
            }
            
            _isRunning = false;
            _localClientId = -1;
            
            // 保存房间ID用于重连
            if (_currentRoom != null && !disableReconnect)
            {
                _lastRoomId = _currentRoom.RoomId;
            }
            else
            {
                _lastRoomId = null;
            }
            _currentRoom = null;
            
            if (_client != null)
            {
                try { _client.Close(); } catch { }
                _client = null;
                _stream = null;
            }
        }
        
        /// <summary>
        /// 开始重连
        /// </summary>
        public void StartReconnect()
        {
            if (_isReconnecting) return;
            if (string.IsNullOrEmpty(_lastServerIP)) return;
            
            _isReconnecting = true;
            _reconnectAttempts = 0;
            TryReconnect();
        }
        
        /// <summary>
        /// 尝试重连
        /// </summary>
        private void TryReconnect()
        {
            if (!_isReconnecting) return;
            
            _reconnectAttempts++;
            
            if (_reconnectAttempts > _maxReconnectAttempts)
            {
                _isReconnecting = false;
                Debug.LogWarning($"重连失败，已达最大尝试次数 ({_maxReconnectAttempts})");
                OnReconnectFailed?.Invoke();
                return;
            }
            
            Debug.Log($"正在尝试重连... ({_reconnectAttempts}/{_maxReconnectAttempts})");
            OnReconnecting?.Invoke(_reconnectAttempts, _maxReconnectAttempts);
            
            // 延迟重连
            StartCoroutine(ReconnectCoroutine());
        }
        
        /// <summary>
        /// 重连协程
        /// </summary>
        private System.Collections.IEnumerator ReconnectCoroutine()
        {
            yield return new WaitForSeconds(_reconnectInterval);
            
            if (_isReconnecting && !IsConnected)
            {
                Connect(_lastServerIP, _lastServerPort, _playerName);
            }
        }
        
        /// <summary>
        /// 停止重连
        /// </summary>
        public void StopReconnect()
        {
            _isReconnecting = false;
            _reconnectAttempts = 0;
            StopAllCoroutines();
        }
        
        #endregion
        
        #region 发送消息
        
        /// <summary>
        /// 发送消息到服务器
        /// </summary>
        public void Send(RelayMessage message)
        {
            if (!IsConnected) return;
            
            try
            {
                byte[] payload = message.Serialize();
                byte[] header = new byte[8];
                BitConverter.GetBytes((int)message.Type).CopyTo(header, 0);
                BitConverter.GetBytes(payload.Length).CopyTo(header, 4);
                
                _stream.Write(header, 0, 8);
                _stream.Write(payload, 0, payload.Length);
            }
            catch (Exception e)
            {
                Debug.LogError($"发送消息失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 发送游戏数据
        /// </summary>
        public void SendGameData(byte[] data)
        {
            Send(new RelayMessage
            {
                Type = RelayMessageType.GameData,
                RawData = data
            });
        }
        
        #endregion
        
        #region 房间操作
        
        /// <summary>
        /// 创建房间
        /// </summary>
        public void CreateRoom(string roomName, int maxPlayers = 4, string password = "")
        {
            Send(new RelayMessage
            {
                Type = RelayMessageType.CreateRoom,
                Data = $"{roomName}|{maxPlayers}|{password}"
            });
        }
        
        /// <summary>
        /// 加入房间
        /// </summary>
        public void JoinRoom(string roomId, string password = "")
        {
            Send(new RelayMessage
            {
                Type = RelayMessageType.JoinRoom,
                Data = $"{roomId}|{password}"
            });
        }
        
        /// <summary>
        /// 离开房间
        /// </summary>
        public void LeaveRoom()
        {
            Send(new RelayMessage { Type = RelayMessageType.LeaveRoom });
            _currentRoom = null;
        }
        
        /// <summary>
        /// 请求房间列表
        /// </summary>
        public void RequestRoomList()
        {
            Send(new RelayMessage { Type = RelayMessageType.GetRoomList });
        }
        
        /// <summary>
        /// 开始游戏（房主）
        /// </summary>
        public void StartGame()
        {
            if (!IsHost)
            {
                OnError?.Invoke("只有房主可以开始游戏");
                return;
            }
            Send(new RelayMessage { Type = RelayMessageType.StartGame });
        }
        
        /// <summary>
        /// 发送聊天
        /// </summary>
        public void SendChat(string content)
        {
            Send(new RelayMessage { Type = RelayMessageType.Chat, Data = content });
        }
        
        /// <summary>
        /// 踢出玩家（房主）
        /// </summary>
        public void KickPlayer(int clientId)
        {
            if (!IsHost) return;
            Send(new RelayMessage { Type = RelayMessageType.KickPlayer, Data = clientId.ToString() });
        }
        
        #endregion
        
        #region 接收消息
        
        private void ReceiveThread()
        {
            byte[] headerBuffer = new byte[8];
            
            while (_isRunning && _client != null && _client.Connected)
            {
                try
                {
                    int bytesRead = _stream.Read(headerBuffer, 0, 8);
                    if (bytesRead == 0) break;
                    
                    int msgType = BitConverter.ToInt32(headerBuffer, 0);
                    int length = BitConverter.ToInt32(headerBuffer, 4);
                    
                    byte[] payload = new byte[length];
                    int totalRead = 0;
                    while (totalRead < length)
                    {
                        bytesRead = _stream.Read(payload, totalRead, length - totalRead);
                        if (bytesRead == 0) break;
                        totalRead += bytesRead;
                    }
                    
                    var message = RelayMessage.Deserialize(payload);
                    message.Type = (RelayMessageType)msgType;
                    
                    lock (_queueLock)
                    {
                        _messageQueue.Enqueue(message);
                    }
                }
                catch (Exception e)
                {
                    if (_isRunning)
                    {
                        Debug.LogWarning($"接收数据错误: {e.Message}");
                    }
                    break;
                }
            }
            
            EnqueueCallback(() =>
            {
                // 保存房间信息
                string lastRoom = _currentRoom?.RoomId;
                
                OnDisconnected?.Invoke("与服务器断开连接");
                Disconnect(false); // 不禁用重连
                
                // 自动重连
                if (_autoReconnect && !string.IsNullOrEmpty(_lastServerIP))
                {
                    _lastRoomId = lastRoom;
                    Debug.Log("连接断开，将尝试自动重连...");
                    StartReconnect();
                }
            });
        }
        
        private Queue<Action> _callbackQueue = new Queue<Action>();
        
        private void EnqueueCallback(Action callback)
        {
            lock (_queueLock)
            {
                _callbackQueue.Enqueue(callback);
            }
        }
        
        private void ProcessMessageQueue()
        {
            // 处理回调
            lock (_queueLock)
            {
                while (_callbackQueue.Count > 0)
                {
                    _callbackQueue.Dequeue()?.Invoke();
                }
            }
            
            // 处理消息
            lock (_queueLock)
            {
                while (_messageQueue.Count > 0)
                {
                    var msg = _messageQueue.Dequeue();
                    HandleMessage(msg);
                }
            }
        }
        
        private void HandleMessage(RelayMessage msg)
        {
            switch (msg.Type)
            {
                case RelayMessageType.Welcome:
                    _localClientId = msg.ClientId;
                    Debug.Log($"服务器分配ID: {_localClientId}");
                    // 设置玩家名
                    Send(new RelayMessage { Type = RelayMessageType.SetPlayerName, Data = _playerName });
                    OnConnected?.Invoke();
                    
                    // 如果是重连，尝试重新加入之前的房间
                    if (!string.IsNullOrEmpty(_lastRoomId))
                    {
                        Debug.Log($"尝试重新加入房间: {_lastRoomId}");
                        JoinRoom(_lastRoomId);
                        _lastRoomId = null;
                    }
                    break;
                    
                case RelayMessageType.Ping:
                    // 收到服务器心跳，回复Pong
                    Send(new RelayMessage { Type = RelayMessageType.Pong });
                    break;
                    
                case RelayMessageType.Pong:
                    // 收到服务器心跳回复，连接正常
                    break;
                    
                case RelayMessageType.Error:
                    OnError?.Invoke(msg.Data);
                    break;
                    
                case RelayMessageType.RoomCreated:
                    _currentRoom = new RelayRoomInfo
                    {
                        RoomId = msg.RoomId,
                        RoomName = msg.Data,
                        HostClientId = _localClientId,
                        CurrentPlayers = 1,
                        MaxPlayers = 4
                    };
                    _currentRoom.Players.Add(new RelayPlayerInfo
                    {
                        ClientId = _localClientId,
                        PlayerName = _playerName,
                        IsHost = true
                    });
                    OnRoomCreated?.Invoke(_currentRoom);
                    break;
                    
                case RelayMessageType.RoomJoined:
                    _currentRoom = ParseRoomInfo(msg.Data);
                    OnRoomJoined?.Invoke(_currentRoom);
                    break;
                    
                case RelayMessageType.PlayerJoined:
                    if (_currentRoom != null)
                    {
                        var newPlayer = new RelayPlayerInfo
                        {
                            ClientId = msg.ClientId,
                            PlayerName = msg.Data,
                            IsHost = false
                        };
                        _currentRoom.Players.Add(newPlayer);
                        _currentRoom.CurrentPlayers++;
                        OnPlayerJoined?.Invoke(newPlayer);
                    }
                    break;
                    
                case RelayMessageType.PlayerLeft:
                    if (_currentRoom != null)
                    {
                        _currentRoom.Players.RemoveAll(p => p.ClientId == msg.ClientId);
                        _currentRoom.CurrentPlayers--;
                        OnPlayerLeft?.Invoke(msg.ClientId, msg.Data);
                    }
                    break;
                    
                case RelayMessageType.HostChanged:
                    if (_currentRoom != null)
                    {
                        _currentRoom.HostClientId = msg.ClientId;
                        foreach (var p in _currentRoom.Players)
                        {
                            p.IsHost = p.ClientId == msg.ClientId;
                        }
                        OnHostChanged?.Invoke(msg.ClientId);
                    }
                    break;
                    
                case RelayMessageType.Kicked:
                    _currentRoom = null;
                    OnKicked?.Invoke(msg.Data);
                    break;
                    
                case RelayMessageType.RoomList:
                    _availableRooms = ParseRoomList(msg.Data);
                    OnRoomListUpdated?.Invoke(_availableRooms);
                    break;
                    
                case RelayMessageType.GameStarted:
                    if (_currentRoom != null)
                    {
                        _currentRoom.IsInGame = true;
                    }
                    OnGameStarted?.Invoke();
                    break;
                    
                case RelayMessageType.GameData:
                    OnGameDataReceived?.Invoke(msg.ClientId, msg.RawData);
                    break;
                    
                case RelayMessageType.Chat:
                    if (!string.IsNullOrEmpty(msg.Data))
                    {
                        var parts = msg.Data.Split('|');
                        if (parts.Length >= 2)
                        {
                            OnChatReceived?.Invoke(parts[0], parts[1]);
                        }
                    }
                    break;
            }
        }
        
        #endregion
        
        #region 解析方法
        
        private RelayRoomInfo ParseRoomInfo(string data)
        {
            // 格式: roomId|roomName|hostClientId|maxPlayers|isInGame|player1Id:name:isHost,player2Id:name:isHost
            var parts = data.Split('|');
            if (parts.Length < 6) return null;
            
            var room = new RelayRoomInfo
            {
                RoomId = parts[0],
                RoomName = parts[1],
                HostClientId = int.Parse(parts[2]),
                MaxPlayers = int.Parse(parts[3]),
                IsInGame = parts[4] == "True"
            };
            
            var playerParts = parts[5].Split(',');
            foreach (var pp in playerParts)
            {
                var playerData = pp.Split(':');
                if (playerData.Length >= 3)
                {
                    room.Players.Add(new RelayPlayerInfo
                    {
                        ClientId = int.Parse(playerData[0]),
                        PlayerName = playerData[1],
                        IsHost = playerData[2] == "1"
                    });
                }
            }
            room.CurrentPlayers = room.Players.Count;
            
            return room;
        }
        
        private List<RelayRoomInfo> ParseRoomList(string data)
        {
            var list = new List<RelayRoomInfo>();
            if (string.IsNullOrEmpty(data)) return list;
            
            var lines = data.Split('\n');
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;
                
                // 格式: roomId|roomName|currentPlayers|maxPlayers|hasPassword|hostName
                var parts = line.Split('|');
                if (parts.Length >= 6)
                {
                    list.Add(new RelayRoomInfo
                    {
                        RoomId = parts[0],
                        RoomName = parts[1],
                        CurrentPlayers = int.Parse(parts[2]),
                        MaxPlayers = int.Parse(parts[3]),
                        HasPassword = parts[4] == "True",
                        HostName = parts[5]
                    });
                }
            }
            
            return list;
        }
        
        #endregion
    }
}

