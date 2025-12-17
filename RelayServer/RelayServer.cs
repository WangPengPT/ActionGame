using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Linq;

namespace RelayServer
{
    /// <summary>
    /// 中继服务器 - 独立运行的公网服务器程序
    /// 负责转发所有客户端之间的消息
    /// </summary>
    class RelayServer
    {
        private TcpListener _listener;
        private Dictionary<int, ClientConnection> _clients = new Dictionary<int, ClientConnection>();
        private Dictionary<string, RoomData> _rooms = new Dictionary<string, RoomData>();
        private int _nextClientId = 1;
        private bool _isRunning;
        private readonly object _lock = new object();
        
        // 心跳配置
        private const int HEARTBEAT_INTERVAL = 5000;  // 心跳检测间隔(毫秒)
        private const int HEARTBEAT_TIMEOUT = 15000;  // 心跳超时时间(毫秒)
        private Timer _heartbeatTimer;

        static void Main(string[] args)
        {
            int port = 7777;
            if (args.Length > 0 && int.TryParse(args[0], out int customPort))
            {
                port = customPort;
            }

            var server = new RelayServer();
            server.Start(port);
        }

        public void Start(int port)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                _isRunning = true;

                Console.WriteLine("========================================");
                Console.WriteLine("       游戏中继服务器 (Relay Server)");
                Console.WriteLine("========================================");
                Console.WriteLine($"服务器启动成功，监听端口: {port}");
                Console.WriteLine($"本机IP: {GetLocalIP()}");
                Console.WriteLine("等待客户端连接...");
                Console.WriteLine("----------------------------------------");

                // 启动接受连接线程
                Thread acceptThread = new Thread(AcceptClients);
                acceptThread.Start();
                
                // 启动心跳检测
                _heartbeatTimer = new Timer(CheckHeartbeats, null, HEARTBEAT_INTERVAL, HEARTBEAT_INTERVAL);
                Console.WriteLine($"心跳检测已启动 (间隔: {HEARTBEAT_INTERVAL/1000}秒, 超时: {HEARTBEAT_TIMEOUT/1000}秒)");

                // 主线程处理命令
                while (_isRunning)
                {
                    string input = Console.ReadLine();
                    ProcessCommand(input);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"服务器启动失败: {e.Message}");
            }
        }

        private void AcceptClients()
        {
            while (_isRunning)
            {
                try
                {
                    if (_listener.Pending())
                    {
                        TcpClient tcpClient = _listener.AcceptTcpClient();
                        int clientId = _nextClientId++;

                        var client = new ClientConnection
                        {
                            ClientId = clientId,
                            TcpClient = tcpClient,
                            Stream = tcpClient.GetStream(),
                            PlayerName = $"Player_{clientId}"
                        };

                        lock (_lock)
                        {
                            _clients[clientId] = client;
                        }

                        // 发送欢迎消息（分配ID）
                        SendToClient(client, new RelayMessage
                        {
                            Type = RelayMessageType.Welcome,
                            ClientId = clientId
                        });

                        // 启动接收线程
                        Thread receiveThread = new Thread(() => ReceiveFromClient(client));
                        receiveThread.Start();

                        Console.WriteLine($"[连接] 客户端 {clientId} 已连接 ({tcpClient.Client.RemoteEndPoint})");
                    }
                    Thread.Sleep(10);
                }
                catch (Exception e)
                {
                    if (_isRunning)
                    {
                        Console.WriteLine($"接受连接错误: {e.Message}");
                    }
                }
            }
        }

        private void ReceiveFromClient(ClientConnection client)
        {
            byte[] headerBuffer = new byte[8]; // 4字节类型 + 4字节长度

            while (_isRunning && client.TcpClient.Connected)
            {
                try
                {
                    // 读取消息头
                    int bytesRead = client.Stream.Read(headerBuffer, 0, 8);
                    if (bytesRead == 0) break;

                    int msgType = BitConverter.ToInt32(headerBuffer, 0);
                    int length = BitConverter.ToInt32(headerBuffer, 4);

                    // 读取消息体
                    byte[] payload = new byte[length];
                    int totalRead = 0;
                    while (totalRead < length)
                    {
                        bytesRead = client.Stream.Read(payload, totalRead, length - totalRead);
                        if (bytesRead == 0) break;
                        totalRead += bytesRead;
                    }

                    // 处理消息
                    var message = RelayMessage.Deserialize(payload);
                    message.Type = (RelayMessageType)msgType;
                    message.ClientId = client.ClientId;
                    
                    ProcessMessage(client, message);
                }
                catch (Exception e)
                {
                    if (_isRunning)
                    {
                        Console.WriteLine($"[错误] 接收客户端 {client.ClientId} 数据: {e.Message}");
                    }
                    break;
                }
            }

            // 客户端断开
            HandleClientDisconnect(client);
        }

        private void ProcessMessage(ClientConnection client, RelayMessage message)
        {
            switch (message.Type)
            {
                case RelayMessageType.CreateRoom:
                    HandleCreateRoom(client, message);
                    break;

                case RelayMessageType.JoinRoom:
                    HandleJoinRoom(client, message);
                    break;

                case RelayMessageType.LeaveRoom:
                    HandleLeaveRoom(client);
                    break;

                case RelayMessageType.GetRoomList:
                    HandleGetRoomList(client);
                    break;

                case RelayMessageType.StartGame:
                    HandleStartGame(client);
                    break;

                case RelayMessageType.GameData:
                    HandleGameData(client, message);
                    break;

                case RelayMessageType.Chat:
                    HandleChat(client, message);
                    break;

                case RelayMessageType.SetPlayerName:
                    client.PlayerName = message.Data;
                    Console.WriteLine($"[信息] 客户端 {client.ClientId} 设置名称: {message.Data}");
                    break;

                case RelayMessageType.KickPlayer:
                    HandleKickPlayer(client, message);
                    break;
                    
                case RelayMessageType.Ping:
                    // 收到客户端心跳，更新最后活跃时间
                    client.LastHeartbeat = DateTime.UtcNow;
                    // 回复Pong
                    SendToClient(client, new RelayMessage { Type = RelayMessageType.Pong });
                    break;
                    
                case RelayMessageType.Pong:
                    // 收到客户端的Pong响应
                    client.LastHeartbeat = DateTime.UtcNow;
                    break;
            }
        }
        
        /// <summary>
        /// 心跳检测 - 定时检查所有客户端
        /// </summary>
        private void CheckHeartbeats(object state)
        {
            var now = DateTime.UtcNow;
            var disconnectedClients = new List<ClientConnection>();
            
            lock (_lock)
            {
                foreach (var client in _clients.Values)
                {
                    var elapsed = (now - client.LastHeartbeat).TotalMilliseconds;
                    
                    if (elapsed > HEARTBEAT_TIMEOUT)
                    {
                        // 心跳超时，标记断开
                        disconnectedClients.Add(client);
                        Console.WriteLine($"[心跳] 客户端 {client.ClientId} ({client.PlayerName}) 心跳超时");
                    }
                    else if (elapsed > HEARTBEAT_INTERVAL)
                    {
                        // 发送心跳检测
                        try
                        {
                            SendToClient(client, new RelayMessage { Type = RelayMessageType.Ping });
                        }
                        catch
                        {
                            disconnectedClients.Add(client);
                        }
                    }
                }
            }
            
            // 断开超时的客户端
            foreach (var client in disconnectedClients)
            {
                HandleClientDisconnect(client);
            }
        }

        #region 房间管理

        private void HandleCreateRoom(ClientConnection client, RelayMessage message)
        {
            // 如果已经在房间中，先离开
            if (!string.IsNullOrEmpty(client.RoomId))
            {
                HandleLeaveRoom(client);
            }

            string roomId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            string[] parts = message.Data.Split('|');
            string roomName = parts.Length > 0 ? parts[0] : $"Room_{roomId}";
            int maxPlayers = parts.Length > 1 && int.TryParse(parts[1], out int max) ? max : 4;
            string password = parts.Length > 2 ? parts[2] : "";

            var room = new RoomData
            {
                RoomId = roomId,
                RoomName = roomName,
                HostClientId = client.ClientId,
                MaxPlayers = maxPlayers,
                Password = password,
                IsInGame = false
            };
            room.Players.Add(client.ClientId);

            lock (_lock)
            {
                _rooms[roomId] = room;
                client.RoomId = roomId;
                client.IsHost = true;
            }

            // 发送创建成功
            SendToClient(client, new RelayMessage
            {
                Type = RelayMessageType.RoomCreated,
                RoomId = roomId,
                Data = roomName
            });

            Console.WriteLine($"[房间] 客户端 {client.ClientId} 创建房间: {roomName} ({roomId})");
        }

        private void HandleJoinRoom(ClientConnection client, RelayMessage message)
        {
            string[] parts = message.Data.Split('|');
            string roomId = parts[0];
            string password = parts.Length > 1 ? parts[1] : "";

            lock (_lock)
            {
                if (!_rooms.TryGetValue(roomId, out RoomData room))
                {
                    SendToClient(client, new RelayMessage
                    {
                        Type = RelayMessageType.Error,
                        Data = "房间不存在"
                    });
                    return;
                }

                if (room.IsInGame)
                {
                    SendToClient(client, new RelayMessage
                    {
                        Type = RelayMessageType.Error,
                        Data = "游戏已开始"
                    });
                    return;
                }

                if (room.Players.Count >= room.MaxPlayers)
                {
                    SendToClient(client, new RelayMessage
                    {
                        Type = RelayMessageType.Error,
                        Data = "房间已满"
                    });
                    return;
                }

                if (!string.IsNullOrEmpty(room.Password) && room.Password != password)
                {
                    SendToClient(client, new RelayMessage
                    {
                        Type = RelayMessageType.Error,
                        Data = "密码错误"
                    });
                    return;
                }

                // 如果在其他房间，先离开
                if (!string.IsNullOrEmpty(client.RoomId))
                {
                    HandleLeaveRoom(client);
                }

                room.Players.Add(client.ClientId);
                client.RoomId = roomId;
                client.IsHost = false;
            }

            // 发送加入成功
            SendToClient(client, new RelayMessage
            {
                Type = RelayMessageType.RoomJoined,
                RoomId = roomId,
                Data = GetRoomInfoString(roomId)
            });

            // 通知房间内其他玩家
            BroadcastToRoom(roomId, new RelayMessage
            {
                Type = RelayMessageType.PlayerJoined,
                ClientId = client.ClientId,
                Data = client.PlayerName
            }, client.ClientId);

            Console.WriteLine($"[房间] 客户端 {client.ClientId} ({client.PlayerName}) 加入房间 {roomId}");
        }

        private void HandleLeaveRoom(ClientConnection client)
        {
            if (string.IsNullOrEmpty(client.RoomId)) return;

            string roomId = client.RoomId;
            bool wasHost = client.IsHost;

            lock (_lock)
            {
                if (_rooms.TryGetValue(roomId, out RoomData room))
                {
                    room.Players.Remove(client.ClientId);

                    // 通知其他玩家
                    BroadcastToRoom(roomId, new RelayMessage
                    {
                        Type = RelayMessageType.PlayerLeft,
                        ClientId = client.ClientId,
                        Data = "离开房间"
                    });

                    // 如果房间空了，删除房间
                    if (room.Players.Count == 0)
                    {
                        _rooms.Remove(roomId);
                        Console.WriteLine($"[房间] 房间 {roomId} 已关闭（无玩家）");
                    }
                    // 如果房主离开，转移房主
                    else if (wasHost && room.Players.Count > 0)
                    {
                        int newHostId = room.Players[0];
                        room.HostClientId = newHostId;
                        
                        if (_clients.TryGetValue(newHostId, out var newHost))
                        {
                            newHost.IsHost = true;
                        }

                        BroadcastToRoom(roomId, new RelayMessage
                        {
                            Type = RelayMessageType.HostChanged,
                            ClientId = newHostId
                        });

                        Console.WriteLine($"[房间] 房间 {roomId} 房主变更为 {newHostId}");
                    }
                }
            }

            client.RoomId = null;
            client.IsHost = false;
            Console.WriteLine($"[房间] 客户端 {client.ClientId} 离开房间 {roomId}");
        }

        private void HandleGetRoomList(ClientConnection client)
        {
            var roomList = new List<string>();

            lock (_lock)
            {
                foreach (var room in _rooms.Values.Where(r => !r.IsInGame))
                {
                    // 格式: roomId|roomName|currentPlayers|maxPlayers|hasPassword|hostName
                    string hostName = "Unknown";
                    if (_clients.TryGetValue(room.HostClientId, out var host))
                    {
                        hostName = host.PlayerName;
                    }
                    roomList.Add($"{room.RoomId}|{room.RoomName}|{room.Players.Count}|{room.MaxPlayers}|{!string.IsNullOrEmpty(room.Password)}|{hostName}");
                }
            }

            SendToClient(client, new RelayMessage
            {
                Type = RelayMessageType.RoomList,
                Data = string.Join("\n", roomList)
            });
        }

        private void HandleStartGame(ClientConnection client)
        {
            if (!client.IsHost || string.IsNullOrEmpty(client.RoomId)) return;

            lock (_lock)
            {
                if (_rooms.TryGetValue(client.RoomId, out RoomData room))
                {
                    room.IsInGame = true;

                    // 通知所有玩家游戏开始
                    BroadcastToRoom(client.RoomId, new RelayMessage
                    {
                        Type = RelayMessageType.GameStarted,
                        Data = GetRoomInfoString(client.RoomId)
                    });

                    Console.WriteLine($"[游戏] 房间 {client.RoomId} 游戏开始");
                }
            }
        }

        private void HandleGameData(ClientConnection client, RelayMessage message)
        {
            if (string.IsNullOrEmpty(client.RoomId)) return;

            // 转发游戏数据给房间内其他玩家
            message.ClientId = client.ClientId;
            BroadcastToRoom(client.RoomId, message, client.ClientId);
        }

        private void HandleChat(ClientConnection client, RelayMessage message)
        {
            if (string.IsNullOrEmpty(client.RoomId)) return;

            // 转发聊天消息
            var chatMsg = new RelayMessage
            {
                Type = RelayMessageType.Chat,
                ClientId = client.ClientId,
                Data = $"{client.PlayerName}|{message.Data}"
            };
            BroadcastToRoom(client.RoomId, chatMsg);
        }

        private void HandleKickPlayer(ClientConnection client, RelayMessage message)
        {
            if (!client.IsHost || string.IsNullOrEmpty(client.RoomId)) return;

            if (int.TryParse(message.Data, out int targetId))
            {
                lock (_lock)
                {
                    if (_clients.TryGetValue(targetId, out var target) && target.RoomId == client.RoomId)
                    {
                        // 通知被踢玩家
                        SendToClient(target, new RelayMessage
                        {
                            Type = RelayMessageType.Kicked,
                            Data = "被房主踢出"
                        });

                        HandleLeaveRoom(target);
                        Console.WriteLine($"[房间] 玩家 {targetId} 被踢出房间 {client.RoomId}");
                    }
                }
            }
        }

        #endregion

        #region 工具方法

        private void HandleClientDisconnect(ClientConnection client)
        {
            lock (_lock)
            {
                HandleLeaveRoom(client);
                _clients.Remove(client.ClientId);
                client.TcpClient?.Close();
            }
            Console.WriteLine($"[断开] 客户端 {client.ClientId} 已断开连接");
        }

        private void SendToClient(ClientConnection client, RelayMessage message)
        {
            try
            {
                byte[] payload = message.Serialize();
                byte[] header = new byte[8];
                BitConverter.GetBytes((int)message.Type).CopyTo(header, 0);
                BitConverter.GetBytes(payload.Length).CopyTo(header, 4);

                client.Stream.Write(header, 0, 8);
                client.Stream.Write(payload, 0, payload.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[错误] 发送到客户端 {client.ClientId}: {e.Message}");
            }
        }

        private void BroadcastToRoom(string roomId, RelayMessage message, int exceptClientId = -1)
        {
            lock (_lock)
            {
                if (!_rooms.TryGetValue(roomId, out RoomData room)) return;

                foreach (int clientId in room.Players)
                {
                    if (clientId != exceptClientId && _clients.TryGetValue(clientId, out var client))
                    {
                        SendToClient(client, message);
                    }
                }
            }
        }

        private string GetRoomInfoString(string roomId)
        {
            lock (_lock)
            {
                if (!_rooms.TryGetValue(roomId, out RoomData room)) return "";

                var playerInfos = new List<string>();
                foreach (int clientId in room.Players)
                {
                    if (_clients.TryGetValue(clientId, out var client))
                    {
                        playerInfos.Add($"{clientId}:{client.PlayerName}:{(client.IsHost ? 1 : 0)}");
                    }
                }

                return $"{room.RoomId}|{room.RoomName}|{room.HostClientId}|{room.MaxPlayers}|{room.IsInGame}|{string.Join(",", playerInfos)}";
            }
        }

        private void ProcessCommand(string input)
        {
            if (string.IsNullOrEmpty(input)) return;

            string[] parts = input.Split(' ');
            string cmd = parts[0].ToLower();

            switch (cmd)
            {
                case "quit":
                case "exit":
                    _isRunning = false;
                    _listener?.Stop();
                    Console.WriteLine("服务器关闭");
                    break;

                case "list":
                    Console.WriteLine($"在线客户端: {_clients.Count}");
                    foreach (var client in _clients.Values)
                    {
                        Console.WriteLine($"  - {client.ClientId}: {client.PlayerName} (房间: {client.RoomId ?? "无"})");
                    }
                    break;

                case "rooms":
                    Console.WriteLine($"房间数: {_rooms.Count}");
                    foreach (var room in _rooms.Values)
                    {
                        Console.WriteLine($"  - {room.RoomId}: {room.RoomName} ({room.Players.Count}/{room.MaxPlayers}) {(room.IsInGame ? "[游戏中]" : "")}");
                    }
                    break;

                case "help":
                    Console.WriteLine("命令列表:");
                    Console.WriteLine("  list  - 显示在线客户端");
                    Console.WriteLine("  rooms - 显示房间列表");
                    Console.WriteLine("  quit  - 关闭服务器");
                    break;

                default:
                    Console.WriteLine($"未知命令: {cmd}，输入 help 查看帮助");
                    break;
            }
        }

        private string GetLocalIP()
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

    #region 数据类

    class ClientConnection
    {
        public int ClientId;
        public TcpClient TcpClient;
        public DateTime LastHeartbeat = DateTime.UtcNow;
        public NetworkStream Stream;
        public string PlayerName;
        public string RoomId;
        public bool IsHost;
    }

    class RoomData
    {
        public string RoomId;
        public string RoomName;
        public int HostClientId;
        public int MaxPlayers;
        public string Password;
        public bool IsInGame;
        public List<int> Players = new List<int>();
    }

    enum RelayMessageType
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

    class RelayMessage
    {
        public RelayMessageType Type;
        public int ClientId;
        public string RoomId;
        public string Data;
        public byte[] RawData;

        public byte[] Serialize()
        {
            // 简单的序列化: ClientId(4) + RoomIdLen(4) + RoomId + DataLen(4) + Data + RawDataLen(4) + RawData
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

    #endregion
}

