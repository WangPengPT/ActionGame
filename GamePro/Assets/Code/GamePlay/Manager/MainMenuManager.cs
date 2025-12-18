using UnityEngine;
using GamePlay.Manager;

/// <summary>
/// 主界面管理器 - 管理主菜单UI和功能
/// 
/// 功能：
/// - 开始游戏（进入大厅）
/// - 设置
/// - 存档/读档
/// - 退出游戏
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("=== UI引用 ===")]
    [Tooltip("开始游戏按钮")]
    [SerializeField] private UnityEngine.UI.Button _startButton;
    
    [Tooltip("设置按钮")]
    [SerializeField] private UnityEngine.UI.Button _settingsButton;
    
    [Tooltip("存档按钮")]
    [SerializeField] private UnityEngine.UI.Button _loadButton;
    
    [Tooltip("退出游戏按钮")]
    [SerializeField] private UnityEngine.UI.Button _quitButton;
    
    [Header("=== UI面板 ===")]
    [Tooltip("设置面板")]
    [SerializeField] private GameObject _settingsPanel;
    
    [Tooltip("存档面板")]
    [SerializeField] private GameObject _saveLoadPanel;
    
    /// <summary>
    /// 是否已初始化
    /// </summary>
    private bool _isInitialized = false;
    
    #region Unity生命周期
    
    private void Awake()
    {
        // 确保场景中只有一个主界面管理器
        var existing = FindObjectOfType<MainMenuManager>();
        if (existing != null && existing != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 初始化主界面
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized) return;
        
        Debug.Log("[MainMenu] 初始化主界面");
        
        // 设置游戏状态为主菜单
        GameManager.Instance.ChangeState(GameState.MainMenu);
        
        // 绑定UI事件
        SetupUI();
        
        // 隐藏子面板
        if (_settingsPanel != null) _settingsPanel.SetActive(false);
        if (_saveLoadPanel != null) _saveLoadPanel.SetActive(false);
        
        _isInitialized = true;
        Debug.Log("[MainMenu] 主界面初始化完成");
    }
    
    /// <summary>
    /// 设置UI事件
    /// </summary>
    private void SetupUI()
    {
        // 开始游戏按钮
        if (_startButton != null)
        {
            _startButton.onClick.RemoveAllListeners();
            _startButton.onClick.AddListener(OnStartGameClicked);
        }
        
        // 设置按钮
        if (_settingsButton != null)
        {
            _settingsButton.onClick.RemoveAllListeners();
            _settingsButton.onClick.AddListener(OnSettingsClicked);
        }
        
        // 存档按钮
        if (_loadButton != null)
        {
            _loadButton.onClick.RemoveAllListeners();
            _loadButton.onClick.AddListener(OnLoadGameClicked);
        }
        
        // 退出游戏按钮
        if (_quitButton != null)
        {
            _quitButton.onClick.RemoveAllListeners();
            _quitButton.onClick.AddListener(OnQuitClicked);
        }
    }
    
    #endregion
    
    #region UI事件处理
    
    /// <summary>
    /// 开始游戏按钮点击
    /// </summary>
    private void OnStartGameClicked()
    {
        Debug.Log("[MainMenu] 开始游戏 -> 进入大厅");
        
        // 切换到大厅场景
        SceneManager.Instance.LoadLobby();
    }
    
    /// <summary>
    /// 设置按钮点击
    /// </summary>
    private void OnSettingsClicked()
    {
        Debug.Log("[MainMenu] 打开设置");
        
        if (_settingsPanel != null)
        {
            _settingsPanel.SetActive(!_settingsPanel.activeSelf);
        }
        
        // TODO: 实现设置功能
    }
    
    /// <summary>
    /// 读档按钮点击
    /// </summary>
    private void OnLoadGameClicked()
    {
        Debug.Log("[MainMenu] 打开存档面板");
        
        if (_saveLoadPanel != null)
        {
            _saveLoadPanel.SetActive(!_saveLoadPanel.activeSelf);
        }
        
        // TODO: 实现存档/读档功能
    }
    
    /// <summary>
    /// 退出游戏按钮点击
    /// </summary>
    private void OnQuitClicked()
    {
        Debug.Log("[MainMenu] 退出游戏");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 显示主界面
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 隐藏主界面
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    #endregion
}

