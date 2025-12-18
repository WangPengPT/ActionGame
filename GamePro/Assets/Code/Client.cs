using UnityEngine;
using GamePlay.Manager;
using Network;

/// <summary>
/// 游戏客户端入口 - 统一管理游戏启动流程
/// 
/// 职责：
/// 1. 初始化游戏逻辑层（GameManager）
/// 2. 初始化网络/模式层（GameModeManager）
/// 3. 根据配置自动启动游戏（可选）
/// 
/// 注意：
/// - 如果场景中有 GameStarter 组件，它会负责启动流程
/// - 如果没有 GameStarter，Client 会根据配置自动启动
/// </summary>
public class Client : MonoBehaviour
{
    [Header("=== 启动配置 ===")]
    [Tooltip("是否自动启动游戏（如果场景中没有 GameStarter）")]
    [SerializeField] private bool _autoStartIfNoStarter = true;
    
    [Tooltip("是否使用 GameConfig 配置")]
    [SerializeField] private bool _useGameConfig = true;
    
    /// <summary>
    /// 游戏管理器引用
    /// </summary>
    private GameManager _gameManager;
    
    /// <summary>
    /// 游戏模式管理器引用
    /// </summary>
    private GameModeManager _gameModeManager;
    
    /// <summary>
    /// 是否已经启动过游戏
    /// </summary>
    private bool _hasStarted = false;

    void Awake()
    {
        // 确保只有一个Client实例
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        InitializeGame();
    }
    
    /// <summary>
    /// 初始化游戏系统
    /// </summary>
    private void InitializeGame()
    {
        Debug.Log("[Client] 开始初始化游戏系统...");
        
        // 1. 初始化游戏逻辑层（GameManager）
        _gameManager = GameManager.Instance;
        Debug.Log("[Client] GameManager 初始化完成");
        
        // 2. 初始化网络/模式层（GameModeManager）
        _gameModeManager = GameModeManager.Instance;
        Debug.Log("[Client] GameModeManager 初始化完成");
        
        // 3. 检查当前场景类型
        var currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        // 判断当前场景类型
        if (currentSceneName == "MainMenu" || currentSceneName.Contains("Menu"))
        {
            Debug.Log("[Client] 当前在主界面场景，等待主界面管理器初始化");
            // 主界面场景由 MainMenuManager 管理，Client 不干预
            return;
        }
        else if (currentSceneName == "Lobby" || currentSceneName.Contains("Lobby"))
        {
            Debug.Log("[Client] 当前在大厅场景，GameManager 会自动初始化（支持独立测试）");
            // 大厅场景由 GameManager 自动检测并初始化，Client 不干预
            return;
        }
        else if (currentSceneName == "Main" || currentSceneName.Contains("Level") || currentSceneName.Contains("Game"))
        {
            Debug.Log("[Client] 当前在游戏关卡场景，GameManager 会自动初始化（支持独立测试）");
            // 游戏关卡场景由 GameManager 自动检测并初始化，Client 不干预
            return;
        }
        
        // 4. 检查场景中是否有 GameStarter（游戏关卡场景）
        var gameStarter = FindObjectOfType<GameStarter>();
        if (gameStarter != null)
        {
            Debug.Log("[Client] 检测到 GameStarter 组件，由它负责启动流程");
            // GameStarter 会自己处理启动，Client 不干预
            return;
        }
        
        // 5. 如果是游戏关卡场景且没有 GameStarter，根据配置自动启动
        if (_autoStartIfNoStarter)
        {
            StartGameByConfig();
        }
        
        Debug.Log("=== 游戏启动完成 ===");
        PrintControls();
    }
    
    /// <summary>
    /// 根据配置启动游戏
    /// </summary>
    private void StartGameByConfig()
    {
        if (_hasStarted)
        {
            Debug.LogWarning("[Client] 游戏已经启动过，跳过重复启动");
            return;
        }
        
        if (_useGameConfig)
        {
            var config = GameConfig.Instance;
            if (config != null && config.AutoStartGame)
            {
                Debug.Log($"[Client] 根据 GameConfig 自动启动游戏，模式: {config.DefaultMode}");
                
                // 应用配置到 GameModeManager
                _gameModeManager.SetMode(config.DefaultMode);
                _gameModeManager.PlayerName = config.DefaultPlayerName;
                _gameModeManager.PlayerConfigId = config.DefaultPlayerConfigId;
                _gameModeManager.RelayServerIP = config.RelayServerIP;
                _gameModeManager.ServerPort = config.ServerPort;
                
                // 根据模式启动
                switch (config.DefaultMode)
                {
                    case GameModeType.Offline:
                        _gameModeManager.QuickStartOffline();
                        break;
                        
                    case GameModeType.LAN:
                        _gameModeManager.CreateRoom(config.AutoStartRoomName);
                        // LAN 模式需要手动开始，等待玩家加入
                        break;
                        
                    case GameModeType.Relay:
                        _gameModeManager.OnConnected += OnRelayConnected;
                        _gameModeManager.Connect();
                        break;
                }
                
                _hasStarted = true;
            }
            else
            {
                Debug.Log("[Client] GameConfig 中 AutoStartGame 为 false，跳过自动启动");
            }
        }
        else
        {
            // 不使用配置，默认单机模式快速启动
            Debug.Log("[Client] 使用默认配置，快速启动单机模式");
            _gameModeManager.QuickStartOffline();
            _hasStarted = true;
        }
    }
    
    /// <summary>
    /// 中继模式连接成功回调
    /// </summary>
    private void OnRelayConnected()
    {
        var config = GameConfig.Instance;
        if (config != null)
        {
            _gameModeManager.OnConnected -= OnRelayConnected;
            _gameModeManager.CreateRoom(config.AutoStartRoomName);
        }
    }
    
    /// <summary>
    /// 打印操作说明
    /// </summary>
    private void PrintControls()
    {
        Debug.Log("操作说明:");
        Debug.Log("  WASD - 移动");
        Debug.Log("  鼠标移动 - 瞄准");
        Debug.Log("  鼠标左键 - 射击");
        Debug.Log("  鼠标右键 - 选中目标");
        Debug.Log("  R - 换弹");
        Debug.Log("  E - 交互");
        Debug.Log("  F - 拾取物品");
        Debug.Log("  Tab/I - 打开背包");
        Debug.Log("  Esc - 暂停游戏");
    }

    void Update()
    {
        // 全局快捷键或调试功能可以放在这里
        #if UNITY_EDITOR
        // 编辑器调试功能
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log("=== 调试信息 ===");
            var player = ActorManager.Instance?.PlayerCharacter;
            if (player != null)
            {
                Debug.Log($"玩家位置: {player.transform.position}");
                Debug.Log($"玩家生命: {player.Attribute.CurrentHealth}/{player.Attribute.MaxHealth}");
                Debug.Log($"当前武器: {player.CurrentWeapon?.ItemName ?? "无"}");
                Debug.Log($"弹药: {player.CurrentAmmo}/{player.ReserveAmmo}");
            }
        }
        
        // 按F2回满血
        if (Input.GetKeyDown(KeyCode.F2))
        {
            var player = ActorManager.Instance?.PlayerCharacter;
            if (player != null)
            {
                player.Heal(player.Attribute.MaxHealth);
                Debug.Log("调试: 已回满血");
            }
        }
        #endif
    }
}
