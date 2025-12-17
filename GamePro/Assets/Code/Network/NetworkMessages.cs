using System;
using System.Collections.Generic;
using UnityEngine;

namespace Network
{
    /// <summary>
    /// 网络消息类型
    /// </summary>
    public enum NetworkMessageType : ushort
    {
        // 连接相关 (0-99)
        None = 0,
        Ping = 1,
        Pong = 2,
        Disconnect = 3,
        
        // 房间相关 (100-199)
        RoomCreate = 100,
        RoomCreateResponse = 101,
        RoomJoin = 102,
        RoomJoinResponse = 103,
        RoomLeave = 104,
        RoomLeaveResponse = 105,
        RoomList = 106,
        RoomListResponse = 107,
        RoomPlayerJoined = 108,
        RoomPlayerLeft = 109,
        RoomHostChanged = 110,
        RoomStartGame = 111,
        RoomGameStarted = 112,
        RoomKickPlayer = 113,
        RoomChat = 114,
        
        // 玩家同步 (200-299)
        PlayerSpawn = 200,
        PlayerDespawn = 201,
        PlayerMove = 202,
        PlayerAim = 203,
        PlayerFire = 204,
        PlayerReload = 205,
        PlayerHit = 206,
        PlayerDeath = 207,
        PlayerRespawn = 208,
        PlayerPickup = 209,
        PlayerUseItem = 210,
        PlayerEquip = 211,
        PlayerInteract = 212,
        PlayerStateSync = 213,
        
        // Actor同步 (300-399)
        ActorSpawn = 300,
        ActorDespawn = 301,
        ActorMove = 302,
        ActorAttack = 303,
        ActorDamage = 304,
        ActorDeath = 305,
        ActorStateChange = 306,
        ActorBuffAdd = 307,
        ActorBuffRemove = 308,
        ActorFullSync = 309,
        
        // 游戏事件 (400-499)
        ItemSpawn = 400,
        ItemDespawn = 401,
        ItemPickedUp = 402,
        
        // RPC调用 (500-599)
        RpcCall = 500,
        RpcResponse = 501,
    }
    
    /// <summary>
    /// 网络消息基类
    /// </summary>
    [Serializable]
    public abstract class NetworkMessage
    {
        public NetworkMessageType MessageType { get; protected set; }
        public int SenderId { get; set; }
        public long Timestamp { get; set; }
        
        protected NetworkMessage(NetworkMessageType type)
        {
            MessageType = type;
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
        
        /// <summary>
        /// 序列化消息
        /// </summary>
        public abstract byte[] Serialize();
        
        /// <summary>
        /// 反序列化消息
        /// </summary>
        public abstract void Deserialize(byte[] data);
    }
    
    #region 房间消息
    
    /// <summary>
    /// 创建房间请求
    /// </summary>
    [Serializable]
    public class RoomCreateMessage : NetworkMessage
    {
        public string RoomName;
        public string Password;
        public int MaxPlayers;
        public string HostName;
        
        public RoomCreateMessage() : base(NetworkMessageType.RoomCreate) { }
        
        public override byte[] Serialize()
        {
            return NetworkSerializer.Serialize(this);
        }
        
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<RoomCreateMessage>(data);
            RoomName = msg.RoomName;
            Password = msg.Password;
            MaxPlayers = msg.MaxPlayers;
            HostName = msg.HostName;
        }
    }
    
    /// <summary>
    /// 创建房间响应
    /// </summary>
    [Serializable]
    public class RoomCreateResponseMessage : NetworkMessage
    {
        public bool Success;
        public string RoomId;
        public string ErrorMessage;
        
        public RoomCreateResponseMessage() : base(NetworkMessageType.RoomCreateResponse) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<RoomCreateResponseMessage>(data);
            Success = msg.Success;
            RoomId = msg.RoomId;
            ErrorMessage = msg.ErrorMessage;
        }
    }
    
    /// <summary>
    /// 加入房间请求
    /// </summary>
    [Serializable]
    public class RoomJoinMessage : NetworkMessage
    {
        public string RoomId;
        public string Password;
        public string PlayerName;
        
        public RoomJoinMessage() : base(NetworkMessageType.RoomJoin) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<RoomJoinMessage>(data);
            RoomId = msg.RoomId;
            Password = msg.Password;
            PlayerName = msg.PlayerName;
        }
    }
    
    /// <summary>
    /// 加入房间响应
    /// </summary>
    [Serializable]
    public class RoomJoinResponseMessage : NetworkMessage
    {
        public bool Success;
        public string ErrorMessage;
        public RoomInfo RoomInfo;
        public int AssignedPlayerId;
        
        public RoomJoinResponseMessage() : base(NetworkMessageType.RoomJoinResponse) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<RoomJoinResponseMessage>(data);
            Success = msg.Success;
            ErrorMessage = msg.ErrorMessage;
            RoomInfo = msg.RoomInfo;
            AssignedPlayerId = msg.AssignedPlayerId;
        }
    }
    
    /// <summary>
    /// 房间信息
    /// </summary>
    [Serializable]
    public class RoomInfo
    {
        public string RoomId;
        public string RoomName;
        public int HostPlayerId;
        public string HostName;
        public int CurrentPlayers;
        public int MaxPlayers;
        public bool HasPassword;
        public bool IsInGame;
        public List<PlayerInfo> Players = new List<PlayerInfo>();
    }
    
    /// <summary>
    /// 玩家信息
    /// </summary>
    [Serializable]
    public class PlayerInfo
    {
        public int PlayerId;
        public string PlayerName;
        public bool IsHost;
        public bool IsReady;
        public int ConfigId;
    }
    
    /// <summary>
    /// 请求房间列表
    /// </summary>
    [Serializable]
    public class RoomListRequestMessage : NetworkMessage
    {
        public RoomListRequestMessage() : base(NetworkMessageType.RoomList) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            // 请求消息无需反序列化数据
        }
    }
    
    /// <summary>
    /// 房间列表响应
    /// </summary>
    [Serializable]
    public class RoomListResponseMessage : NetworkMessage
    {
        public List<RoomInfo> Rooms = new List<RoomInfo>();
        
        public RoomListResponseMessage() : base(NetworkMessageType.RoomListResponse) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<RoomListResponseMessage>(data);
            Rooms = msg.Rooms;
        }
    }
    
    /// <summary>
    /// 玩家加入房间通知
    /// </summary>
    [Serializable]
    public class RoomPlayerJoinedMessage : NetworkMessage
    {
        public PlayerInfo Player;
        
        public RoomPlayerJoinedMessage() : base(NetworkMessageType.RoomPlayerJoined) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<RoomPlayerJoinedMessage>(data);
            Player = msg.Player;
        }
    }
    
    /// <summary>
    /// 玩家离开房间通知
    /// </summary>
    [Serializable]
    public class RoomPlayerLeftMessage : NetworkMessage
    {
        public int PlayerId;
        public string Reason;
        
        public RoomPlayerLeftMessage() : base(NetworkMessageType.RoomPlayerLeft) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<RoomPlayerLeftMessage>(data);
            PlayerId = msg.PlayerId;
            Reason = msg.Reason;
        }
    }
    
    /// <summary>
    /// 房主变更通知
    /// </summary>
    [Serializable]
    public class RoomHostChangedMessage : NetworkMessage
    {
        public int NewHostPlayerId;
        
        public RoomHostChangedMessage() : base(NetworkMessageType.RoomHostChanged) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<RoomHostChangedMessage>(data);
            NewHostPlayerId = msg.NewHostPlayerId;
        }
    }
    
    /// <summary>
    /// 开始游戏请求（房主发起）
    /// </summary>
    [Serializable]
    public class RoomStartGameMessage : NetworkMessage
    {
        public int MapId;
        public int GameMode;
        
        public RoomStartGameMessage() : base(NetworkMessageType.RoomStartGame) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<RoomStartGameMessage>(data);
            MapId = msg.MapId;
            GameMode = msg.GameMode;
        }
    }
    
    /// <summary>
    /// 房间聊天消息
    /// </summary>
    [Serializable]
    public class RoomChatMessage : NetworkMessage
    {
        public string Content;
        public string SenderName;
        
        public RoomChatMessage() : base(NetworkMessageType.RoomChat) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<RoomChatMessage>(data);
            Content = msg.Content;
            SenderName = msg.SenderName;
        }
    }
    
    #endregion
    
    #region 玩家同步消息
    
    /// <summary>
    /// 玩家生成消息
    /// </summary>
    [Serializable]
    public class PlayerSpawnMessage : NetworkMessage
    {
        public int PlayerId;
        public int ConfigId;
        public string PlayerName;
        public float PosX, PosY, PosZ;
        public float RotY;
        
        public PlayerSpawnMessage() : base(NetworkMessageType.PlayerSpawn) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<PlayerSpawnMessage>(data);
            PlayerId = msg.PlayerId;
            ConfigId = msg.ConfigId;
            PlayerName = msg.PlayerName;
            PosX = msg.PosX; PosY = msg.PosY; PosZ = msg.PosZ;
            RotY = msg.RotY;
        }
    }
    
    /// <summary>
    /// 玩家移动消息
    /// </summary>
    [Serializable]
    public class PlayerMoveMessage : NetworkMessage
    {
        public int PlayerId;
        public float PosX, PosY, PosZ;
        public float VelX, VelY, VelZ;
        public float RotY;
        public int State;
        
        public PlayerMoveMessage() : base(NetworkMessageType.PlayerMove) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<PlayerMoveMessage>(data);
            PlayerId = msg.PlayerId;
            PosX = msg.PosX; PosY = msg.PosY; PosZ = msg.PosZ;
            VelX = msg.VelX; VelY = msg.VelY; VelZ = msg.VelZ;
            RotY = msg.RotY;
            State = msg.State;
        }
    }
    
    /// <summary>
    /// 玩家瞄准消息
    /// </summary>
    [Serializable]
    public class PlayerAimMessage : NetworkMessage
    {
        public int PlayerId;
        public float AimX, AimY, AimZ;
        
        public PlayerAimMessage() : base(NetworkMessageType.PlayerAim) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<PlayerAimMessage>(data);
            PlayerId = msg.PlayerId;
            AimX = msg.AimX; AimY = msg.AimY; AimZ = msg.AimZ;
        }
    }
    
    /// <summary>
    /// 玩家开火消息
    /// </summary>
    [Serializable]
    public class PlayerFireMessage : NetworkMessage
    {
        public int PlayerId;
        public float OriginX, OriginY, OriginZ;
        public float DirX, DirY, DirZ;
        public int WeaponId;
        
        public PlayerFireMessage() : base(NetworkMessageType.PlayerFire) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<PlayerFireMessage>(data);
            PlayerId = msg.PlayerId;
            OriginX = msg.OriginX; OriginY = msg.OriginY; OriginZ = msg.OriginZ;
            DirX = msg.DirX; DirY = msg.DirY; DirZ = msg.DirZ;
            WeaponId = msg.WeaponId;
        }
    }
    
    /// <summary>
    /// 玩家受击消息
    /// </summary>
    [Serializable]
    public class PlayerHitMessage : NetworkMessage
    {
        public int TargetPlayerId;
        public int AttackerPlayerId;
        public float Damage;
        public bool IsCritical;
        public float HitX, HitY, HitZ;
        
        public PlayerHitMessage() : base(NetworkMessageType.PlayerHit) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<PlayerHitMessage>(data);
            TargetPlayerId = msg.TargetPlayerId;
            AttackerPlayerId = msg.AttackerPlayerId;
            Damage = msg.Damage;
            IsCritical = msg.IsCritical;
            HitX = msg.HitX; HitY = msg.HitY; HitZ = msg.HitZ;
        }
    }
    
    /// <summary>
    /// 玩家死亡消息
    /// </summary>
    [Serializable]
    public class PlayerDeathMessage : NetworkMessage
    {
        public int PlayerId;
        public int KillerId;
        
        public PlayerDeathMessage() : base(NetworkMessageType.PlayerDeath) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<PlayerDeathMessage>(data);
            PlayerId = msg.PlayerId;
            KillerId = msg.KillerId;
        }
    }
    
    /// <summary>
    /// 玩家状态完整同步
    /// </summary>
    [Serializable]
    public class PlayerStateSyncMessage : NetworkMessage
    {
        public int PlayerId;
        public float Health;
        public float MaxHealth;
        public int CurrentAmmo;
        public int ReserveAmmo;
        public int WeaponId;
        public int State;
        public List<int> BuffIds = new List<int>();
        
        public PlayerStateSyncMessage() : base(NetworkMessageType.PlayerStateSync) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<PlayerStateSyncMessage>(data);
            PlayerId = msg.PlayerId;
            Health = msg.Health;
            MaxHealth = msg.MaxHealth;
            CurrentAmmo = msg.CurrentAmmo;
            ReserveAmmo = msg.ReserveAmmo;
            WeaponId = msg.WeaponId;
            State = msg.State;
            BuffIds = msg.BuffIds;
        }
    }
    
    #endregion
    
    #region Actor同步消息
    
    /// <summary>
    /// Actor生成消息
    /// </summary>
    [Serializable]
    public class ActorSpawnMessage : NetworkMessage
    {
        public int ActorId;
        public int ConfigId;
        public int ActorType; // 0=Monster, 1=NPC
        public float PosX, PosY, PosZ;
        public float RotY;
        
        public ActorSpawnMessage() : base(NetworkMessageType.ActorSpawn) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<ActorSpawnMessage>(data);
            ActorId = msg.ActorId;
            ConfigId = msg.ConfigId;
            ActorType = msg.ActorType;
            PosX = msg.PosX; PosY = msg.PosY; PosZ = msg.PosZ;
            RotY = msg.RotY;
        }
    }
    
    /// <summary>
    /// Actor移动消息
    /// </summary>
    [Serializable]
    public class ActorMoveMessage : NetworkMessage
    {
        public int ActorId;
        public float PosX, PosY, PosZ;
        public float RotY;
        public int State;
        
        public ActorMoveMessage() : base(NetworkMessageType.ActorMove) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<ActorMoveMessage>(data);
            ActorId = msg.ActorId;
            PosX = msg.PosX; PosY = msg.PosY; PosZ = msg.PosZ;
            RotY = msg.RotY;
            State = msg.State;
        }
    }
    
    /// <summary>
    /// Actor受伤消息
    /// </summary>
    [Serializable]
    public class ActorDamageMessage : NetworkMessage
    {
        public int ActorId;
        public int AttackerId;
        public float Damage;
        public float RemainingHealth;
        public bool IsCritical;
        
        public ActorDamageMessage() : base(NetworkMessageType.ActorDamage) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<ActorDamageMessage>(data);
            ActorId = msg.ActorId;
            AttackerId = msg.AttackerId;
            Damage = msg.Damage;
            RemainingHealth = msg.RemainingHealth;
            IsCritical = msg.IsCritical;
        }
    }
    
    /// <summary>
    /// Actor死亡消息
    /// </summary>
    [Serializable]
    public class ActorDeathMessage : NetworkMessage
    {
        public int ActorId;
        public int KillerId;
        public int ExpReward;
        public int GoldReward;
        
        public ActorDeathMessage() : base(NetworkMessageType.ActorDeath) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<ActorDeathMessage>(data);
            ActorId = msg.ActorId;
            KillerId = msg.KillerId;
            ExpReward = msg.ExpReward;
            GoldReward = msg.GoldReward;
        }
    }
    
    #endregion
    
    #region 物品消息
    
    /// <summary>
    /// 物品生成消息
    /// </summary>
    [Serializable]
    public class ItemSpawnMessage : NetworkMessage
    {
        public int ItemId;
        public int ConfigId;
        public int ItemType;
        public float PosX, PosY, PosZ;
        public int Count;
        
        public ItemSpawnMessage() : base(NetworkMessageType.ItemSpawn) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<ItemSpawnMessage>(data);
            ItemId = msg.ItemId;
            ConfigId = msg.ConfigId;
            ItemType = msg.ItemType;
            PosX = msg.PosX; PosY = msg.PosY; PosZ = msg.PosZ;
            Count = msg.Count;
        }
    }
    
    /// <summary>
    /// 物品被拾取消息
    /// </summary>
    [Serializable]
    public class ItemPickedUpMessage : NetworkMessage
    {
        public int ItemId;
        public int PlayerId;
        
        public ItemPickedUpMessage() : base(NetworkMessageType.ItemPickedUp) { }
        
        public override byte[] Serialize() => NetworkSerializer.Serialize(this);
        public override void Deserialize(byte[] data)
        {
            var msg = NetworkSerializer.Deserialize<ItemPickedUpMessage>(data);
            ItemId = msg.ItemId;
            PlayerId = msg.PlayerId;
        }
    }
    
    #endregion
    
    /// <summary>
    /// 简单的JSON序列化器
    /// </summary>
    public static class NetworkSerializer
    {
        public static byte[] Serialize<T>(T obj)
        {
            string json = JsonUtility.ToJson(obj);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }
        
        public static T Deserialize<T>(byte[] data)
        {
            string json = System.Text.Encoding.UTF8.GetString(data);
            return JsonUtility.FromJson<T>(json);
        }
        
        /// <summary>
        /// 打包消息（添加消息头）
        /// </summary>
        public static byte[] Pack(NetworkMessage message)
        {
            byte[] payload = message.Serialize();
            byte[] packet = new byte[payload.Length + 6]; // 2字节类型 + 4字节长度 + 数据
            
            // 消息类型
            ushort type = (ushort)message.MessageType;
            packet[0] = (byte)(type >> 8);
            packet[1] = (byte)(type & 0xFF);
            
            // 数据长度
            int length = payload.Length;
            packet[2] = (byte)(length >> 24);
            packet[3] = (byte)(length >> 16);
            packet[4] = (byte)(length >> 8);
            packet[5] = (byte)(length & 0xFF);
            
            // 数据
            Array.Copy(payload, 0, packet, 6, payload.Length);
            
            return packet;
        }
        
        /// <summary>
        /// 解包消息头
        /// </summary>
        public static (NetworkMessageType type, int length) UnpackHeader(byte[] header)
        {
            ushort type = (ushort)((header[0] << 8) | header[1]);
            int length = (header[2] << 24) | (header[3] << 16) | (header[4] << 8) | header[5];
            return ((NetworkMessageType)type, length);
        }
    }
}

