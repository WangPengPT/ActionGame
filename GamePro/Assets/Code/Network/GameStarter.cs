using UnityEngine;

namespace Network
{
    /// <summary>
    /// 游戏启动器 - 可挂载到场景中，方便配置启动模式
    /// 
    /// 使用方法：
    /// 1. 将此脚本挂载到场景中的任意GameObject上
    /// 2. 在Inspector中设置参数，或使用配置文件
    /// 3. 开发时选择Offline模式，可一键测试
    /// 4. 发布时切换到Relay模式
    /// </summary>
    public class GameStarter : MonoBehaviour
    {
        [Header("=== 配置来源 ===")]
        [Tooltip("是否使用GameConfig配置文件（Resources/GameConfig）")]
        [SerializeField] private bool _useConfig = true;
        
        [Header("=== 游戏模式（不使用配置时生效）===")]
        [Tooltip("选择游戏模式：\n- Offline: 单机模式(开发测试)\n- LAN: 局域网模式\n- Relay: 中继服务器模式(公网联机)")]
        [SerializeField] private GameModeType _gameMode = GameModeType.Offline;
        
        [Header("=== 玩家设置 ===")]
        [SerializeField] private string _playerName = "TestPlayer";
        [SerializeField] private int _playerConfigId = 1;
        
        [Header("=== 服务器设置 ===")]
        [Tooltip("中继服务器IP（Relay模式使用）")]
        [SerializeField] private string _relayServerIP = "127.0.0.1";
        [SerializeField] private int _serverPort = 7777;
        
        [Header("=== 快速启动 ===")]
        [Tooltip("启动时自动开始游戏")]
        [SerializeField] private bool _autoStart = true;
        
        [Tooltip("单机模式下的房间名")]
        [SerializeField] private string _roomName = "测试房间";
        
        [Header("=== 调试 ===")]
        [SerializeField] private bool _showDebugUI = true;
        
        private void Start()
        {
            var gm = GameModeManager.Instance;
            
            // 根据配置来源设置参数
            if (_useConfig)
            {
                var config = GameConfig.Instance;
                if (config != null)
                {
                    _gameMode = config.DefaultMode;
                    _playerName = config.DefaultPlayerName;
                    _playerConfigId = config.DefaultPlayerConfigId;
                    _relayServerIP = config.RelayServerIP;
                    _serverPort = config.ServerPort;
                    _showDebugUI = config.ShowDebugUI;
                    _autoStart = config.AutoStartGame;
                    _roomName = config.AutoStartRoomName;
                }
            }
            
            // 应用配置
            gm.PlayerName = _playerName;
            gm.PlayerConfigId = _playerConfigId;
            gm.RelayServerIP = _relayServerIP;
            gm.ServerPort = _serverPort;
            
            // 设置模式
            gm.SetMode(_gameMode);
            
            // 订阅事件
            gm.OnConnected += () => Debug.Log("[GameStarter] 连接成功");
            gm.OnDisconnected += (reason) => Debug.Log($"[GameStarter] 断开连接: {reason}");
            gm.OnRoomCreated += (room) => Debug.Log($"[GameStarter] 房间创建: {room.RoomName}");
            gm.OnGameStarted += () => Debug.Log("[GameStarter] 游戏开始!");
            gm.OnError += (error) => Debug.LogError($"[GameStarter] 错误: {error}");
            
            // 自动启动
            if (_autoStart)
            {
                StartGameByMode();
            }
        }
        
        /// <summary>
        /// 根据模式启动游戏
        /// </summary>
        private void StartGameByMode()
        {
            var gm = GameModeManager.Instance;
            
            switch (_gameMode)
            {
                case GameModeType.Offline:
                    // 单机模式一键启动
                    gm.CreateRoom(_roomName);
                    gm.StartGame();
                    break;
                    
                case GameModeType.LAN:
                    // 局域网模式创建房间
                    gm.CreateRoom(_roomName);
                    // 等待玩家加入后手动开始
                    break;
                    
                case GameModeType.Relay:
                    // 中继模式先连接服务器
                    gm.OnConnected += OnRelayConnected;
                    gm.Connect();
                    break;
            }
        }
        
        /// <summary>
        /// 中继模式连接成功后
        /// </summary>
        private void OnRelayConnected()
        {
            GameModeManager.Instance.OnConnected -= OnRelayConnected;
            // 创建房间
            GameModeManager.Instance.CreateRoom(_roomName);
        }
        
        #region 调试UI
        
        private void OnGUI()
        {
            if (!_showDebugUI) return;
            
            var gm = GameModeManager.Instance;
            
            GUILayout.BeginArea(new Rect(10, 10, 250, 300));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label($"=== 游戏模式: {gm.CurrentMode} ===");
            GUILayout.Label($"连接状态: {(gm.IsConnected ? "已连接" : "未连接")}");
            GUILayout.Label($"是否房主: {gm.IsHost}");
            GUILayout.Label($"玩家ID: {gm.LocalPlayerId}");
            
            var room = gm.GetCurrentRoom();
            if (room != null)
            {
                GUILayout.Label($"房间: {room.RoomName}");
                GUILayout.Label($"玩家数: {room.CurrentPlayers}/{room.MaxPlayers}");
                GUILayout.Label($"游戏中: {room.IsInGame}");
            }
            
            GUILayout.Space(10);
            
            // 模式切换按钮
            GUILayout.Label("--- 模式切换 ---");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("单机"))
            {
                gm.SetOfflineMode();
            }
            if (GUILayout.Button("局域网"))
            {
                gm.SetLANMode();
            }
            if (GUILayout.Button("中继"))
            {
                gm.SetRelayMode();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // 操作按钮
            GUILayout.Label("--- 操作 ---");
            
            if (!gm.IsConnected && gm.CurrentMode == GameModeType.Relay)
            {
                if (GUILayout.Button("连接服务器"))
                {
                    gm.Connect();
                }
            }
            
            if (GUILayout.Button("创建房间"))
            {
                gm.CreateRoom(_roomName);
            }
            
            if (GUILayout.Button("开始游戏"))
            {
                gm.StartGame();
            }
            
            if (GUILayout.Button("离开房间"))
            {
                gm.LeaveRoom();
            }
            
            GUILayout.Space(5);
            
            // 快捷按钮
            GUI.color = Color.green;
            if (GUILayout.Button("一键单机测试", GUILayout.Height(30)))
            {
                gm.QuickStartOffline();
            }
            GUI.color = Color.white;
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        #endregion
    }
}

