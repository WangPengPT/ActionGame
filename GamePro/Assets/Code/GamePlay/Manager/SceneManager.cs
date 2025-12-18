using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamePlay.Manager
{
    /// <summary>
    /// 场景类型
    /// </summary>
    public enum SceneType
    {
        /// <summary>
        /// 主界面场景
        /// </summary>
        MainMenu,
        
        /// <summary>
        /// 玩家大厅场景
        /// </summary>
        Lobby,
        
        /// <summary>
        /// 游戏关卡场景
        /// </summary>
        GameLevel
    }
    
    /// <summary>
    /// 场景管理器 - 统一管理场景切换
    /// </summary>
    public class SceneManager : MonoBehaviour
    {
        #region 单例
        
        private static SceneManager _instance;
        public static SceneManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SceneManager");
                    _instance = go.AddComponent<SceneManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region 事件
        
        /// <summary>
        /// 场景切换开始事件
        /// </summary>
        public event Action<SceneType, SceneType> OnSceneTransitionStart;
        
        /// <summary>
        /// 场景切换完成事件
        /// </summary>
        public event Action<SceneType> OnSceneTransitionComplete;
        
        /// <summary>
        /// 场景加载进度事件 (0-1)
        /// </summary>
        public event Action<float> OnSceneLoadProgress;
        
        #endregion
        
        #region 属性
        
        /// <summary>
        /// 当前场景类型
        /// </summary>
        public SceneType CurrentSceneType { get; private set; } = SceneType.MainMenu;
        
        /// <summary>
        /// 是否正在切换场景
        /// </summary>
        public bool IsTransitioning { get; private set; }
        
        /// <summary>
        /// 目标关卡ID（用于加载关卡）
        /// </summary>
        public int TargetLevelId { get; private set; }
        
        /// <summary>
        /// 目标游戏模式（单机/联网）
        /// </summary>
        public Network.GameModeType TargetGameMode { get; private set; }
        
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
            
            // 监听Unity场景加载事件
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        #endregion
        
        #region 场景切换
        
        /// <summary>
        /// 切换到主界面
        /// </summary>
        public void LoadMainMenu()
        {
            LoadScene(SceneType.MainMenu);
        }
        
        /// <summary>
        /// 切换到玩家大厅
        /// </summary>
        public void LoadLobby()
        {
            LoadScene(SceneType.Lobby);
        }
        
        /// <summary>
        /// 加载游戏关卡
        /// </summary>
        /// <param name="levelId">关卡ID</param>
        /// <param name="gameMode">游戏模式（单机/联网）</param>
        public void LoadGameLevel(int levelId, Network.GameModeType gameMode = Network.GameModeType.Offline)
        {
            TargetLevelId = levelId;
            TargetGameMode = gameMode;
            LoadScene(SceneType.GameLevel);
        }
        
        /// <summary>
        /// 加载场景
        /// </summary>
        private void LoadScene(SceneType targetScene)
        {
            if (IsTransitioning)
            {
                Debug.LogWarning($"[SceneManager] 正在切换场景，忽略请求: {targetScene}");
                return;
            }
            
            if (CurrentSceneType == targetScene)
            {
                Debug.LogWarning($"[SceneManager] 已经在目标场景: {targetScene}");
                return;
            }
            
            StartCoroutine(TransitionToScene(targetScene));
        }
        
        /// <summary>
        /// 场景切换协程
        /// </summary>
        private IEnumerator TransitionToScene(SceneType targetScene)
        {
            IsTransitioning = true;
            var oldScene = CurrentSceneType;
            
            Debug.Log($"[SceneManager] 开始切换场景: {oldScene} -> {targetScene}");
            OnSceneTransitionStart?.Invoke(oldScene, targetScene);
            
            // 卸载当前场景（如果需要）
            // 这里可以根据需要添加淡出效果等
            
            // 加载新场景
            string sceneName = GetSceneName(targetScene);
            AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;
            
            // 等待加载进度
            while (asyncLoad.progress < 0.9f)
            {
                OnSceneLoadProgress?.Invoke(asyncLoad.progress);
                yield return null;
            }
            
            // 等待一帧确保所有准备工作完成
            yield return null;
            
            // 激活场景
            asyncLoad.allowSceneActivation = true;
            
            // 等待场景完全加载
            while (!asyncLoad.isDone)
            {
                OnSceneLoadProgress?.Invoke(1f);
                yield return null;
            }
            
            CurrentSceneType = targetScene;
            IsTransitioning = false;
            
            Debug.Log($"[SceneManager] 场景切换完成: {targetScene}");
            OnSceneTransitionComplete?.Invoke(targetScene);
        }
        
        /// <summary>
        /// 获取场景名称
        /// </summary>
        private string GetSceneName(SceneType sceneType)
        {
            switch (sceneType)
            {
                case SceneType.MainMenu:
                    return "MainMenu";
                case SceneType.Lobby:
                    return "Lobby";
                case SceneType.GameLevel:
                    return "Main"; // 当前游戏场景
                default:
                    return "MainMenu";
            }
        }
        
        /// <summary>
        /// Unity场景加载完成回调
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[SceneManager] Unity场景加载完成: {scene.name}");
            
            // 根据场景类型初始化对应的管理器
            switch (CurrentSceneType)
            {
                case SceneType.MainMenu:
                    // 主界面场景初始化
                    var mainMenu = FindObjectOfType<MainMenuManager>();
                    if (mainMenu != null)
                    {
                        mainMenu.Initialize();
                    }
                    break;
                    
                case SceneType.Lobby:
                    // 大厅场景初始化 - 使用统一的 GameManager
                    // 如果 GameManager 已经自动初始化，这里不会重复初始化
                    if (GameManager.Instance.CurrentState != GameState.Lobby)
                    {
                        GameManager.Instance.InitializeLobby();
                    }
                    break;
                    
                case SceneType.GameLevel:
                    // 游戏关卡场景初始化
                    // GameManager 会自动检测并初始化（如果还未初始化）
                    // 但为了确保通过 SceneManager 加载时也能正确初始化，这里也检查一下
                    if (GameManager.Instance.CurrentState != GameState.Playing)
                    {
                        Debug.Log("[SceneManager] 初始化游戏关卡场景");
                        // 从 SceneManager 获取关卡信息（虽然 StartGame 暂时不使用，但保留以备将来扩展）
                        var levelId = TargetLevelId > 0 ? TargetLevelId : 1;
                        var gameMode = TargetGameMode;
                        
                        // 设置游戏模式（如果需要）
                        if (gameMode != Network.GameModeType.Offline)
                        {
                            var gameModeManager = Network.GameModeManager.Instance;
                            gameModeManager.SetMode(gameMode);
                        }
                        
                        // 调用 GameManager 初始化游戏关卡
                        GameManager.Instance.StartGame();
                    }
                    else
                    {
                        Debug.Log("[SceneManager] 游戏关卡场景已经初始化，跳过");
                    }
                    break;
            }
        }
        
        #endregion
        
        #region 工具方法
        
        /// <summary>
        /// 重新加载当前场景
        /// </summary>
        public void ReloadCurrentScene()
        {
            LoadScene(CurrentSceneType);
        }
        
        #endregion
    }
}

