# 合并 LobbyManager 到 GameManager 说明

## 目标

将 `LobbyManager` 的功能整合到 `GameManager` 中，统一管理大厅和游戏关卡场景。

## 原因

两个管理器功能相似：
- 都创建玩家
- 都生成NPC
- 都管理场景状态
- 只是场景类型不同（Lobby vs GameLevel）

## 合并方案

### 1. 在 GameManager 中添加类型定义

已在 `GameManager.cs` 中添加：
- `LobbyNPCConfig` - NPC生成配置
- `LevelInfo` - 关卡信息

### 2. 添加大厅场景配置字段

需要在 `GameManager` 的 `#region 属性` 部分后添加：

```csharp
#region 大厅场景配置

[Header("=== 大厅场景配置 ===")]
[SerializeField] private List<LobbyNPCConfig> _lobbyNPCs = new List<LobbyNPCConfig>();

[Header("=== 大厅UI引用 ===")]
[SerializeField] private UnityEngine.UI.Button _lobbyBackButton;
[SerializeField] private GameObject _levelSelectPanel;
[SerializeField] private Transform _levelButtonContainer;
[SerializeField] private GameObject _levelButtonPrefab;
[SerializeField] private UnityEngine.UI.Button _offlineButton;
[SerializeField] private UnityEngine.UI.Button _onlineButton;
[SerializeField] private UnityEngine.UI.Button _startButton;
[SerializeField] private UnityEngine.UI.Button _openLevelSelectButton;

[Header("=== 关卡配置 ===")]
[SerializeField] private List<LevelInfo> _levels = new List<LevelInfo>();

[Header("=== 当前选择 ===")]
[SerializeField] private int _selectedLevelId = -1;
[SerializeField] private Network.GameModeType _selectedGameMode = Network.GameModeType.Offline;

private List<NPC> _lobbyNPCsList = new List<NPC>();
private List<GameObject> _levelButtons = new List<GameObject>();

#endregion
```

### 3. 添加 InitializeLobby 方法

在 `StartGame` 方法前添加：

```csharp
/// <summary>
/// 初始化大厅场景
/// </summary>
public void InitializeLobby()
{
    Debug.Log("[GameManager] 初始化大厅场景");
    ChangeState(GameState.Lobby);
    
    // 清理旧数据
    ActorManager.Instance.ClearAll();
    SceneElementManager.Instance.ClearAll();
    
    // 创建玩家
    CreatePlayerForScene(_playerSpawnPoint);
    
    // 生成大厅NPC
    SpawnLobbyNPCs();
    
    // 初始化关卡列表
    if (_levels.Count == 0)
    {
        InitializeDefaultLevels();
    }
    
    // 设置UI
    SetupLobbyUI();
    
    // 创建关卡按钮
    CreateLevelButtons();
    
    // 默认选择第一个关卡
    if (_levels.Count > 0)
    {
        _selectedLevelId = _levels[0].LevelId;
    }
    
    Debug.Log("[GameManager] 大厅场景初始化完成");
}
```

### 4. 添加辅助方法

需要从 `LobbyManager.cs` 复制以下方法到 `GameManager`：
- `CreatePlayerForScene()` - 为场景创建玩家
- `SpawnLobbyNPCs()` - 生成大厅NPC
- `SpawnLobbyNPC()` - 生成单个NPC
- `SpawnDefaultLobbyNPCs()` - 生成默认NPC
- `InitializeDefaultLevels()` - 初始化默认关卡
- `SetupLobbyUI()` - 设置大厅UI
- `CreateLevelButtons()` - 创建关卡按钮
- `SelectLevel()` - 选择关卡
- `SelectGameMode()` - 选择游戏模式
- `OnStartGameFromLobby()` - 从大厅开始游戏
- `OnNPCInteracted()` - 处理NPC交互

### 5. 修改 SceneManager

修改 `SceneManager.cs` 的 `OnSceneLoaded` 方法：

```csharp
case SceneType.Lobby:
    // 大厅场景初始化 - 使用 GameManager
    GameManager.Instance.InitializeLobby();
    break;
```

### 6. 删除 LobbyManager

合并完成后，删除 `LobbyManager.cs` 文件。

## 使用方式

### 大厅场景

1. 在 Unity 中创建 `Lobby` 场景
2. 添加 `GameManager` 组件（如果还没有）
3. 在 Inspector 中配置：
   - 大厅NPC列表（`_lobbyNPCs`）
   - UI引用（按钮、面板等）
   - 关卡列表（`_levels`）
4. `SceneManager` 会自动调用 `GameManager.InitializeLobby()`

### 游戏关卡场景

1. 使用现有的 `Main` 场景
2. `GameManager.StartGame()` 会自动初始化游戏关卡

## 优势

1. **统一管理**: 一个管理器处理所有场景
2. **减少重复**: 共享玩家创建、NPC生成等逻辑
3. **易于维护**: 代码集中，修改更方便
4. **清晰职责**: GameManager 负责所有游戏场景逻辑

## 注意事项

1. 确保 `GameManager` 是单例且 `DontDestroyOnLoad`
2. 场景切换时正确清理旧数据
3. UI引用需要在每个场景的 `GameManager` 实例上配置
4. 保持向后兼容，不影响现有功能

