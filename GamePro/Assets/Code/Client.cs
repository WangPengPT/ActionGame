using UnityEngine;
using GamePlay.Manager;

/// <summary>
/// 游戏客户端入口
/// </summary>
public class Client : MonoBehaviour
{
    /// <summary>
    /// 游戏管理器引用
    /// </summary>
    private GameManager _gameManager;

    void Awake()
    {
        // 确保只有一个Client实例
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // 初始化游戏管理器（会自动初始化所有子系统）
        _gameManager = GameManager.Instance;
        
        Debug.Log("=== 游戏启动完成 ===");
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
