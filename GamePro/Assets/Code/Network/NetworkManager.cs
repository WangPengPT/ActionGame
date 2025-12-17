using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Network
{
    /// <summary>
    /// 网络连接状态
    /// </summary>
    public enum NetworkState
    {
        Disconnected,
        Connecting,
        Connected,
        InRoom,
        InGame
    }
    
    /// <summary>
    /// 网络角色
    /// </summary>
    public enum NetworkRole
    {
        None,
        Host,       // 房主（同时是服务器）
        Client      // 客户端
    }
    
    /// <summary>
    /// 网络管理器 - 处理网络连接和消息收发
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        #region 单例
        
        private static NetworkManager _instance;
        public static NetworkManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("NetworkManager");
                    _instance = go.AddComponent<NetworkManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region 属性
        
        /// <summary>
        /// 当前网络状态
        /// </summary>
        [SerializeField] private NetworkState _state = NetworkState.Disconnected;
        public NetworkState State => _state;
        
        /// <summary>
        /// 网络角色
        /// </summary>
        private NetworkRole _role = NetworkRole.None;
        public NetworkRole Role => _role;
        
        /// <summary>
        /// 是否是房主/服务器
        /// </summary>
        public bool IsHost => _role == NetworkRole.Host;
        
        /// <summary>
        /// 是否是客户端
        /// </summary>
        public bool IsClient => _role == NetworkRole.Client;
        
        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected => _state >= NetworkState.Connected;
        
        /// <summary>
        /// 本地玩家ID
        /// </summary>
        private int _localPlayerId = -1;
        public int LocalPlayerId => _localPlayerId;
        
        /// <summary>
        /// 服务器端口
        /// </summary>
        [SerializeField] private int _serverPort = 7777;
        public int ServerPort => _serverPort;
        
        /// <summary>
        /// 服务器监听器
        /// </summary>
        private TcpListener _server;
        
        /// <summary>
        /// 客户端连接
        /// </summary>
        private TcpClient _client;
        private NetworkStream _clientStream;
        
        /// <summary>
        /// 已连接的客户端（服务器端使用）
        /// </summary>
        private Dictionary<int, ClientConnection> _connectedClients = new Dictionary<int, ClientConnection>();
        
        /// <summary>
        /// 下一个客户端ID
        /// </summary>
        private int _nextClientId = 1;
        
        /// <summary>
        /// 消息队列（主线程处理）
        /// </summary>
        private Queue<(int senderId, NetworkMessage message)> _messageQueue = new Queue<(int, NetworkMessage)>();
        private readonly object _queueLock = new object();
        
        /// <summary>
        /// 消息处理器
        /// </summary>
        private Dictionary<NetworkMessageType, Action<int, NetworkMessage>> _messageHandlers 
            = new Dictionary<NetworkMessageType, Action<int, NetworkMessage>>();
        
        /// <summary>
        /// 网络线程运行标志
        /// </summary>
        private bool _isRunning;
        
        /// <summary>
        /// 心跳间隔
        /// </summary>
        [SerializeField] private float _heartbeatInterval = 5f;
        private float _heartbeatTimer;
        
        #endregion
        
        #region 事件
        
        /// <summary>
        /// 状态改变事件
        /// </summary>
        public event Action<NetworkState> OnStateChanged;
        
        /// <summary>
        /// 连接成功事件
        /// </summary>
        public event Action OnConnected;
        
        /// <summary>
        /// 断开连接事件
        /// </summary>
        public event Action<string> OnDisconnected;
        
        /// <summary>
        /// 客户端连接事件（服务器端）
        /// </summary>
        public event Action<int> OnClientConnected;
        
        /// <summary>
        /// 客户端断开事件（服务器端）
        /// </summary>
        public event Action<int> OnClientDisconnected;
        
        /// <summary>
        /// 收到消息事件
        /// </summary>
        public event Action<int, NetworkMessage> OnMessageReceived;
        
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
            UpdateHeartbeat();
        }
        
        private void OnDestroy()
        {
            Disconnect();
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        private void OnApplicationQuit()
        {
            Disconnect();
        }
        
        #endregion
        
        #region 服务器操作
        
        /// <summary>
        /// 启动服务器（作为房主）
        /// </summary>
        public bool StartHost(int port = 0)
        {
            if (_state != NetworkState.Disconnected)
            {
                Debug.LogWarning("已经连接，无法启动服务器");
                return false;
            }
            
            try
            {
                int usePort = port > 0 ? port : _serverPort;
                _server = new TcpListener(IPAddress.Any, usePort);
                _server.Start();
                
                _role = NetworkRole.Host;
                _localPlayerId = 0; // 房主ID为0
                _isRunning = true;
                
                // 启动接受连接线程
                Thread acceptThread = new Thread(AcceptClientsThread);
                acceptThread.IsBackground = true;
                acceptThread.Start();
                
                SetState(NetworkState.Connected);
                Debug.Log($"服务器启动成功，端口: {usePort}");
                
                OnConnected?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"启动服务器失败: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 接受客户端连接线程
        /// </summary>
        private void AcceptClientsThread()
        {
            while (_isRunning)
            {
                try
                {
                    if (_server != null && _server.Pending())
                    {
                        TcpClient client = _server.AcceptTcpClient();
                        int clientId = _nextClientId++;
                        
                        var connection = new ClientConnection
                        {
                            ClientId = clientId,
                            TcpClient = client,
                            Stream = client.GetStream()
                        };
                        
                        lock (_connectedClients)
                        {
                            _connectedClients[clientId] = connection;
                        }
                        
                        // 启动该客户端的接收线程
                        Thread receiveThread = new Thread(() => ReceiveFromClientThread(connection));
                        receiveThread.IsBackground = true;
                        receiveThread.Start();
                        
                        // 通知主线程
                        EnqueueCallback(() => OnClientConnected?.Invoke(clientId));
                        
                        Debug.Log($"客户端 {clientId} 已连接");
                    }
                    
                    Thread.Sleep(10);
                }
                catch (Exception e)
                {
                    if (_isRunning)
                    {
                        Debug.LogError($"接受连接错误: {e.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 从客户端接收数据线程
        /// </summary>
        private void ReceiveFromClientThread(ClientConnection connection)
        {
            byte[] headerBuffer = new byte[6];
            
            while (_isRunning && connection.TcpClient.Connected)
            {
                try
                {
                    // 读取消息头
                    int bytesRead = connection.Stream.Read(headerBuffer, 0, 6);
                    if (bytesRead == 0)
                    {
                        break; // 连接断开
                    }
                    
                    var (type, length) = NetworkSerializer.UnpackHeader(headerBuffer);
                    
                    // 读取消息体
                    byte[] payload = new byte[length];
                    int totalRead = 0;
                    while (totalRead < length)
                    {
                        bytesRead = connection.Stream.Read(payload, totalRead, length - totalRead);
                        if (bytesRead == 0) break;
                        totalRead += bytesRead;
                    }
                    
                    // 创建消息对象
                    var message = CreateMessage(type, payload);
                    if (message != null)
                    {
                        message.SenderId = connection.ClientId;
                        EnqueueMessage(connection.ClientId, message);
                    }
                }
                catch (Exception e)
                {
                    if (_isRunning)
                    {
                        Debug.LogWarning($"接收客户端 {connection.ClientId} 数据错误: {e.Message}");
                    }
                    break;
                }
            }
            
            // 客户端断开
            RemoveClient(connection.ClientId);
        }
        
        /// <summary>
        /// 移除客户端
        /// </summary>
        private void RemoveClient(int clientId)
        {
            lock (_connectedClients)
            {
                if (_connectedClients.TryGetValue(clientId, out var connection))
                {
                    connection.TcpClient?.Close();
                    _connectedClients.Remove(clientId);
                    EnqueueCallback(() => OnClientDisconnected?.Invoke(clientId));
                    Debug.Log($"客户端 {clientId} 已断开");
                }
            }
        }
        
        #endregion
        
        #region 客户端操作
        
        /// <summary>
        /// 连接到服务器
        /// </summary>
        public void Connect(string ip, int port = 0)
        {
            if (_state != NetworkState.Disconnected)
            {
                Debug.LogWarning("已经连接");
                return;
            }
            
            SetState(NetworkState.Connecting);
            
            int usePort = port > 0 ? port : _serverPort;
            
            try
            {
                _client = new TcpClient();
                _client.BeginConnect(ip, usePort, OnConnectCallback, null);
            }
            catch (Exception e)
            {
                Debug.LogError($"连接失败: {e.Message}");
                SetState(NetworkState.Disconnected);
                OnDisconnected?.Invoke(e.Message);
            }
        }
        
        /// <summary>
        /// 连接回调
        /// </summary>
        private void OnConnectCallback(IAsyncResult ar)
        {
            try
            {
                _client.EndConnect(ar);
                _clientStream = _client.GetStream();
                _role = NetworkRole.Client;
                _isRunning = true;
                
                // 启动接收线程
                Thread receiveThread = new Thread(ReceiveFromServerThread);
                receiveThread.IsBackground = true;
                receiveThread.Start();
                
                EnqueueCallback(() =>
                {
                    SetState(NetworkState.Connected);
                    OnConnected?.Invoke();
                });
                
                Debug.Log("连接服务器成功");
            }
            catch (Exception e)
            {
                EnqueueCallback(() =>
                {
                    SetState(NetworkState.Disconnected);
                    OnDisconnected?.Invoke(e.Message);
                });
                Debug.LogError($"连接失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 从服务器接收数据线程
        /// </summary>
        private void ReceiveFromServerThread()
        {
            byte[] headerBuffer = new byte[6];
            
            while (_isRunning && _client != null && _client.Connected)
            {
                try
                {
                    // 读取消息头
                    int bytesRead = _clientStream.Read(headerBuffer, 0, 6);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    
                    var (type, length) = NetworkSerializer.UnpackHeader(headerBuffer);
                    
                    // 读取消息体
                    byte[] payload = new byte[length];
                    int totalRead = 0;
                    while (totalRead < length)
                    {
                        bytesRead = _clientStream.Read(payload, totalRead, length - totalRead);
                        if (bytesRead == 0) break;
                        totalRead += bytesRead;
                    }
                    
                    // 创建消息对象
                    var message = CreateMessage(type, payload);
                    if (message != null)
                    {
                        message.SenderId = 0; // 来自服务器
                        EnqueueMessage(0, message);
                    }
                }
                catch (Exception e)
                {
                    if (_isRunning)
                    {
                        Debug.LogWarning($"接收服务器数据错误: {e.Message}");
                    }
                    break;
                }
            }
            
            // 断开连接
            EnqueueCallback(() =>
            {
                Disconnect();
                OnDisconnected?.Invoke("与服务器断开连接");
            });
        }
        
        #endregion
        
        #region 消息发送
        
        /// <summary>
        /// 发送消息到服务器（客户端使用）
        /// </summary>
        public void SendToServer(NetworkMessage message)
        {
            if (!IsClient || _clientStream == null) return;
            
            try
            {
                message.SenderId = _localPlayerId;
                byte[] packet = NetworkSerializer.Pack(message);
                _clientStream.Write(packet, 0, packet.Length);
            }
            catch (Exception e)
            {
                Debug.LogError($"发送消息失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 发送消息到指定客户端（服务器使用）
        /// </summary>
        public void SendToClient(int clientId, NetworkMessage message)
        {
            if (!IsHost) return;
            
            lock (_connectedClients)
            {
                if (_connectedClients.TryGetValue(clientId, out var connection))
                {
                    try
                    {
                        message.SenderId = 0; // 来自服务器
                        byte[] packet = NetworkSerializer.Pack(message);
                        connection.Stream.Write(packet, 0, packet.Length);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"发送消息到客户端 {clientId} 失败: {e.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 广播消息到所有客户端（服务器使用）
        /// </summary>
        public void BroadcastToClients(NetworkMessage message, int exceptClientId = -1)
        {
            if (!IsHost) return;
            
            lock (_connectedClients)
            {
                foreach (var pair in _connectedClients)
                {
                    if (pair.Key != exceptClientId)
                    {
                        SendToClient(pair.Key, message);
                    }
                }
            }
        }
        
        /// <summary>
        /// 发送消息（自动判断角色）
        /// </summary>
        public void Send(NetworkMessage message, int targetId = -1)
        {
            if (IsHost)
            {
                if (targetId >= 0)
                {
                    SendToClient(targetId, message);
                }
                else
                {
                    BroadcastToClients(message);
                }
            }
            else if (IsClient)
            {
                SendToServer(message);
            }
        }
        
        #endregion
        
        #region 消息处理
        
        /// <summary>
        /// 注册消息处理器
        /// </summary>
        public void RegisterHandler(NetworkMessageType type, Action<int, NetworkMessage> handler)
        {
            _messageHandlers[type] = handler;
        }
        
        /// <summary>
        /// 注销消息处理器
        /// </summary>
        public void UnregisterHandler(NetworkMessageType type)
        {
            _messageHandlers.Remove(type);
        }
        
        /// <summary>
        /// 入队消息
        /// </summary>
        private void EnqueueMessage(int senderId, NetworkMessage message)
        {
            lock (_queueLock)
            {
                _messageQueue.Enqueue((senderId, message));
            }
        }
        
        /// <summary>
        /// 入队回调
        /// </summary>
        private Queue<Action> _callbackQueue = new Queue<Action>();
        private void EnqueueCallback(Action callback)
        {
            lock (_queueLock)
            {
                _callbackQueue.Enqueue(callback);
            }
        }
        
        /// <summary>
        /// 处理消息队列（主线程）
        /// </summary>
        private void ProcessMessageQueue()
        {
            // 处理回调
            lock (_queueLock)
            {
                while (_callbackQueue.Count > 0)
                {
                    var callback = _callbackQueue.Dequeue();
                    callback?.Invoke();
                }
            }
            
            // 处理消息
            lock (_queueLock)
            {
                while (_messageQueue.Count > 0)
                {
                    var (senderId, message) = _messageQueue.Dequeue();
                    
                    // 调用注册的处理器
                    if (_messageHandlers.TryGetValue(message.MessageType, out var handler))
                    {
                        handler(senderId, message);
                    }
                    
                    // 触发通用事件
                    OnMessageReceived?.Invoke(senderId, message);
                }
            }
        }
        
        /// <summary>
        /// 创建消息对象
        /// </summary>
        private NetworkMessage CreateMessage(NetworkMessageType type, byte[] payload)
        {
            NetworkMessage message = type switch
            {
                NetworkMessageType.RoomCreate => new RoomCreateMessage(),
                NetworkMessageType.RoomCreateResponse => new RoomCreateResponseMessage(),
                NetworkMessageType.RoomJoin => new RoomJoinMessage(),
                NetworkMessageType.RoomJoinResponse => new RoomJoinResponseMessage(),
                NetworkMessageType.RoomListResponse => new RoomListResponseMessage(),
                NetworkMessageType.RoomPlayerJoined => new RoomPlayerJoinedMessage(),
                NetworkMessageType.RoomPlayerLeft => new RoomPlayerLeftMessage(),
                NetworkMessageType.RoomHostChanged => new RoomHostChangedMessage(),
                NetworkMessageType.RoomStartGame => new RoomStartGameMessage(),
                NetworkMessageType.RoomChat => new RoomChatMessage(),
                NetworkMessageType.PlayerSpawn => new PlayerSpawnMessage(),
                NetworkMessageType.PlayerMove => new PlayerMoveMessage(),
                NetworkMessageType.PlayerAim => new PlayerAimMessage(),
                NetworkMessageType.PlayerFire => new PlayerFireMessage(),
                NetworkMessageType.PlayerHit => new PlayerHitMessage(),
                NetworkMessageType.PlayerDeath => new PlayerDeathMessage(),
                NetworkMessageType.PlayerStateSync => new PlayerStateSyncMessage(),
                NetworkMessageType.ActorSpawn => new ActorSpawnMessage(),
                NetworkMessageType.ActorMove => new ActorMoveMessage(),
                NetworkMessageType.ActorDamage => new ActorDamageMessage(),
                NetworkMessageType.ActorDeath => new ActorDeathMessage(),
                NetworkMessageType.ItemSpawn => new ItemSpawnMessage(),
                NetworkMessageType.ItemPickedUp => new ItemPickedUpMessage(),
                _ => null
            };
            
            message?.Deserialize(payload);
            return message;
        }
        
        #endregion
        
        #region 断开连接
        
        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            _isRunning = false;
            
            // 关闭服务器
            if (_server != null)
            {
                try
                {
                    _server.Stop();
                }
                catch { }
                _server = null;
            }
            
            // 关闭所有客户端连接
            lock (_connectedClients)
            {
                foreach (var pair in _connectedClients)
                {
                    try
                    {
                        pair.Value.TcpClient?.Close();
                    }
                    catch { }
                }
                _connectedClients.Clear();
            }
            
            // 关闭客户端连接
            if (_client != null)
            {
                try
                {
                    _client.Close();
                }
                catch { }
                _client = null;
                _clientStream = null;
            }
            
            _role = NetworkRole.None;
            _localPlayerId = -1;
            SetState(NetworkState.Disconnected);
            
            Debug.Log("网络已断开");
        }
        
        #endregion
        
        #region 工具方法
        
        /// <summary>
        /// 设置状态
        /// </summary>
        private void SetState(NetworkState newState)
        {
            if (_state != newState)
            {
                _state = newState;
                OnStateChanged?.Invoke(newState);
            }
        }
        
        /// <summary>
        /// 设置本地玩家ID
        /// </summary>
        public void SetLocalPlayerId(int playerId)
        {
            _localPlayerId = playerId;
        }
        
        /// <summary>
        /// 心跳更新
        /// </summary>
        private void UpdateHeartbeat()
        {
            if (!IsConnected) return;
            
            _heartbeatTimer += Time.deltaTime;
            if (_heartbeatTimer >= _heartbeatInterval)
            {
                _heartbeatTimer = 0;
                // TODO: 发送心跳包
            }
        }
        
        /// <summary>
        /// 获取所有连接的客户端ID
        /// </summary>
        public List<int> GetConnectedClientIds()
        {
            lock (_connectedClients)
            {
                return new List<int>(_connectedClients.Keys);
            }
        }
        
        /// <summary>
        /// 获取本地IP地址
        /// </summary>
        public string GetLocalIP()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return "127.0.0.1";
        }
        
        #endregion
    }
    
    /// <summary>
    /// 客户端连接信息
    /// </summary>
    public class ClientConnection
    {
        public int ClientId;
        public TcpClient TcpClient;
        public NetworkStream Stream;
    }
}

