using UnityEngine;

namespace Network
{
    /// <summary>
    /// 游戏配置 - 可在Unity中创建和编辑
    /// 使用方式：在Project窗口右键 -> Create -> Game/Game Config
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Game Config", order = 1)]
    public class GameConfig : ScriptableObject
    {
        [Header("=== 游戏模式 ===")]
        [Tooltip("开发时使用Offline，发布时使用Relay")]
        public GameModeType DefaultMode = GameModeType.Offline;
        
        [Header("=== 服务器配置 ===")]
        [Tooltip("中继服务器IP地址")]
        public string RelayServerIP = "127.0.0.1";
        
        [Tooltip("服务器端口")]
        public int ServerPort = 7777;
        
        [Header("=== 默认玩家设置 ===")]
        [Tooltip("默认玩家名称")]
        public string DefaultPlayerName = "Player";
        
        [Tooltip("默认玩家配置ID")]
        public int DefaultPlayerConfigId = 1;
        
        [Header("=== 开发选项 ===")]
        [Tooltip("是否显示调试UI")]
        public bool ShowDebugUI = true;
        
        [Tooltip("是否自动开始游戏")]
        public bool AutoStartGame = true;
        
        [Tooltip("自动开始的房间名")]
        public string AutoStartRoomName = "测试房间";
        
        [Header("=== 网络选项 ===")]
        [Tooltip("是否启用断线重连")]
        public bool EnableReconnect = true;
        
        [Tooltip("最大重连次数")]
        public int MaxReconnectAttempts = 5;
        
        [Tooltip("重连间隔(秒)")]
        public float ReconnectInterval = 3f;
        
        #region 单例
        
        private static GameConfig _instance;
        public static GameConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GameConfig>("GameConfig");
                    if (_instance == null)
                    {
                        Debug.LogWarning("GameConfig not found in Resources. Creating default config.");
                        _instance = CreateInstance<GameConfig>();
                    }
                }
                return _instance;
            }
        }
        
        #endregion
        
        /// <summary>
        /// 是否是发布版本（非Offline模式）
        /// </summary>
        public bool IsReleaseBuild => DefaultMode != GameModeType.Offline;
        
        /// <summary>
        /// 是否是开发版本（Offline模式）
        /// </summary>
        public bool IsDevBuild => DefaultMode == GameModeType.Offline;
    }
}

