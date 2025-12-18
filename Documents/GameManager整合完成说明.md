# GameManager 整合完成说明

## ✅ 已完成

已成功将 `LobbyManager` 的所有功能整合到 `GameManager` 中，并删除了 `LobbyManager.cs`。

## 主要改动

### 1. 统一场景管理

`GameManager` 现在统一管理两种场景：
- **大厅场景 (Lobby)**: 功能型NPC、关卡选择、商店、升级等
- **游戏关卡场景 (GameLevel)**: 战斗NPC、刷怪点、测试物品等

### 2. 场景切换功能

- `InitializeLobby()` - 初始化大厅场景
- `StartGame()` - 初始化游戏关卡场景
- `ReturnToLobby()` - 从关卡返回大厅
- `ReturnToMainMenu()` - 返回主菜单

### 3. NPC 生成区分

- **大厅NPC**: `SpawnLobbyNPCs()` - 生成功能型NPC（商人、铁匠、训练师、任务NPC）
- **关卡NPC**: `SpawnGameLevelNPCs()` - 生成关卡中的NPC（战斗相关）

### 4. 玩家管理

- `CreateOrMovePlayer()` - 创建或移动玩家到指定位置
- 玩家数据在场景切换时保留，只移动位置

### 5. 场景清理

- `CleanupScene()` - 统一清理场景数据
- 支持选择是否清理玩家

## 使用方式

### 大厅场景

1. `SceneManager.Instance.LoadLobby()` 会自动调用 `GameManager.Instance.InitializeLobby()`
2. 在 Unity Inspector 中配置：
   - 大厅NPC列表（`_lobbyNPCs`）
   - UI引用（按钮、面板等）
   - 关卡列表（`_levels`）

### 游戏关卡场景

1. `SceneManager.Instance.LoadGameLevel()` 后调用 `GameManager.Instance.StartGame()`
2. 自动生成关卡NPC、刷怪点、测试物品

### 场景切换

```csharp
// 从大厅到关卡
GameManager.Instance.OnStartGameFromLobby(); // 内部调用

// 从关卡返回大厅
GameManager.Instance.ReturnToLobby();

// 返回主菜单
GameManager.Instance.ReturnToMainMenu();
```

## 关键区别

| 功能 | 大厅场景 | 游戏关卡场景 |
|------|---------|-------------|
| NPC类型 | 功能型（商人、铁匠、训练师） | 战斗型（任务NPC、普通NPC） |
| 刷怪点 | ❌ 无 | ✅ 有 |
| 测试物品 | ❌ 无 | ✅ 有 |
| 关卡选择 | ✅ 有 | ❌ 无 |
| 商店/升级 | ✅ 有 | ❌ 无 |

## 注意事项

1. **玩家数据保留**: 场景切换时玩家数据会保留，只移动位置
2. **NPC区分**: 大厅和关卡的NPC功能不同，但都使用相同的NPC系统
3. **UI配置**: 大厅UI需要在Unity Inspector中配置到 `GameManager` 组件
4. **场景切换**: 使用 `SceneManager` 统一管理场景切换

## 已删除文件

- ✅ `GamePro/Assets/Code/GamePlay/Manager/LobbyManager.cs` - 已删除

## 更新的文件

- ✅ `GameManager.cs` - 整合了所有大厅功能
- ✅ `SceneManager.cs` - 使用 `GameManager.InitializeLobby()`
- ✅ `Client.cs` - 更新了注释

现在所有场景管理都统一在 `GameManager` 中，代码更简洁、易维护！

