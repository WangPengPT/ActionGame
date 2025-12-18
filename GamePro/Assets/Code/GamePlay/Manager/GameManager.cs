using System;
using System.Collections.Generic;
using UnityEngine;
using GamePlay.Actor;
using ExcelImporter;
using Network;

namespace GamePlay.Manager
{
    /// <summary>
    /// NPC 生成配置（大厅场景使用）
    /// </summary>
    [System.Serializable]
    public class LobbyNPCConfig
    {
        [Tooltip("NPC配置ID（从Excel配置）")]
        public int NPCConfigId;
        
        [Tooltip("NPC生成位置")]
        public Vector3 SpawnPosition;
        
        [Tooltip("NPC类型（如果为None则从配置读取）")]
        public Actor.NPCType OverrideType = Actor.NPCType.Normal;
    }
    
    /// <summary>
    /// 关卡信息
    /// </summary>
    [System.Serializable]
    public class LevelInfo
    {
        public int LevelId;
        public string LevelName;
        public string Description;
        public Sprite PreviewImage;
        public bool IsUnlocked = true;
        public bool SupportMultiplayer = false;
    }
    
    /// <summary>
    /// 游戏状态
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// 初始化中
        /// </summary>
        Initializing = 0,
        
        /// <summary>
        /// 主菜单
        /// </summary>
        MainMenu = 1,
        
        /// <summary>
        /// 玩家大厅
        /// </summary>
        Lobby = 2,
        
        /// <summary>
        /// 游戏中
        /// </summary>
        Playing = 3,
        
        /// <summary>
        /// 暂停
        /// </summary>
        Paused = 4,
        
        /// <summary>
        /// 游戏结束
        /// </summary>
        GameOver = 5,
        
        /// <summary>
        /// 胜利
        /// </summary>
        Victory = 6
    }
    
    /// <summary>
    /// 游戏管理器 - 游戏的核心入口和状态管理
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region 单例
        
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region 属性
        
        /// <summary>
        /// 当前游戏状态
        /// </summary>
        [SerializeField] private GameState _currentState = GameState.Initializing;
        public GameState CurrentState => _currentState;
        
        /// <summary>
        /// 游戏是否正在运行
        /// </summary>
        public bool IsPlaying => _currentState == GameState.Playing;
        
        /// <summary>
        /// 游戏是否暂停
        /// </summary>
        public bool IsPaused => _currentState == GameState.Paused;
        
        /// <summary>
        /// 游戏时间
        /// </summary>
        private float _gameTime;
        public float GameTime => _gameTime;
        
        /// <summary>
        /// 玩家配置ID
        /// </summary>
        [SerializeField] private int _playerConfigId = 1;
        
        /// <summary>
        /// 玩家出生点
        /// </summary>
        [SerializeField] private Vector3 _playerSpawnPoint = Vector3.zero;
        
        [Header("=== 独立测试配置 ===")]
        [Tooltip("是否自动检测场景并初始化（支持独立测试）")]
        [SerializeField] private bool _autoInitializeScene = true;
        
        #endregion
        
        #region 大厅场景配置
        
        [Header("=== 大厅场景配置 ===")]
        [Tooltip("大厅NPC列表（从配置生成）")]
        [SerializeField] private List<LobbyNPCConfig> _lobbyNPCs = new List<LobbyNPCConfig>();
        
        [Header("=== 大厅UI引用 ===")]
        [Tooltip("返回主菜单按钮")]
        [SerializeField] private UnityEngine.UI.Button _lobbyBackButton;
        
        [Tooltip("关卡选择面板")]
        [SerializeField] private GameObject _levelSelectPanel;
        
        [Tooltip("关卡选择按钮容器")]
        [SerializeField] private Transform _levelButtonContainer;
        
        [Tooltip("关卡按钮预制体")]
        [SerializeField] private GameObject _levelButtonPrefab;
        
        [Tooltip("单机模式按钮")]
        [SerializeField] private UnityEngine.UI.Button _offlineButton;
        
        [Tooltip("联网模式按钮")]
        [SerializeField] private UnityEngine.UI.Button _onlineButton;
        
        [Tooltip("开始游戏按钮")]
        [SerializeField] private UnityEngine.UI.Button _startButton;
        
        [Tooltip("打开关卡选择按钮")]
        [SerializeField] private UnityEngine.UI.Button _openLevelSelectButton;
        
        [Header("=== 关卡配置 ===")]
        [Tooltip("关卡列表")]
        [SerializeField] private List<LevelInfo> _levels = new List<LevelInfo>();
        
        [Header("=== 当前选择 ===")]
        [Tooltip("当前选中的关卡ID")]
        [SerializeField] private int _selectedLevelId = -1;
        
        [Tooltip("当前选择的游戏模式")]
        [SerializeField] private Network.GameModeType _selectedGameMode = Network.GameModeType.Offline;
        
        /// <summary>
        /// 大厅中的NPC列表
        /// </summary>
        private List<NPC> _lobbyNPCsList = new List<NPC>();
        
        /// <summary>
        /// 关卡按钮列表
        /// </summary>
        private List<GameObject> _levelButtons = new List<GameObject>();
        
        #endregion
        
        #region 事件
        
        /// <summary>
        /// 游戏状态改变事件
        /// </summary>
        public event Action<GameState, GameState> OnGameStateChanged;
        
        /// <summary>
        /// 游戏开始事件
        /// </summary>
        public event Action OnGameStart;
        
        /// <summary>
        /// 游戏暂停事件
        /// </summary>
        public event Action OnGamePaused;
        
        /// <summary>
        /// 游戏继续事件
        /// </summary>
        public event Action OnGameResumed;
        
        /// <summary>
        /// 游戏结束事件
        /// </summary>
        public event Action<bool> OnGameEnd; // true = victory, false = defeat
        
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
        }
        
        private void Start()
        {
            Initialize();
            
            // 自动检测场景类型并初始化（支持独立测试）
            if (_autoInitializeScene)
            {
                AutoInitializeScene();
            }
        }
        
        /// <summary>
        /// 自动检测场景类型并初始化（支持独立测试）
        /// 允许直接打开场景进行测试，无需依赖其他场景
        /// 
        /// 注意：如果场景是通过 SceneManager 加载的，SceneManager 会负责初始化
        /// 这个方法只在直接打开场景时使用（独立测试模式）
        /// </summary>
        private void AutoInitializeScene()
        {
            // 检查是否是通过 SceneManager 加载的场景
            // 如果是，SceneManager 会负责初始化，这里跳过
            var sceneManager = SceneManager.Instance;
            if (sceneManager != null && sceneManager.IsTransitioning)
            {
                Debug.Log("[GameManager] 场景正在通过 SceneManager 切换，跳过自动初始化");
                return;
            }
            
            var currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            
            Debug.Log($"[GameManager] 自动检测场景: {currentSceneName}（独立测试模式）");
            
            // 检测场景类型并自动初始化
            if (currentSceneName == "Lobby" || currentSceneName.Contains("Lobby"))
            {
                // 检查是否已经初始化（可能是从其他场景切换过来的）
                if (_currentState != GameState.Lobby)
                {
                    Debug.Log("[GameManager] ✓ 检测到大厅场景，自动初始化（独立测试模式）");
                    InitializeLobby();
                }
                else
                {
                    Debug.Log("[GameManager] 大厅场景已经初始化，跳过");
                }
            }
            else if (currentSceneName == "Main" || currentSceneName.Contains("Level") || currentSceneName.Contains("Game"))
            {
                // 检查是否已经初始化（可能是从其他场景切换过来的）
                if (_currentState != GameState.Playing)
                {
                    Debug.Log("[GameManager] ✓ 检测到游戏关卡场景，自动初始化（独立测试模式）");
                    // 使用默认配置启动游戏（独立测试模式）
                    StartGame();
                }
                else
                {
                    Debug.Log("[GameManager] 游戏关卡场景已经初始化，跳过");
                }
            }
            else
            {
                Debug.Log("[GameManager] 当前场景不是大厅或关卡，跳过自动初始化");
            }
            // 主菜单场景由 MainMenuManager 管理，不在这里处理
        }
        
        private void Update()
        {
            if (_currentState == GameState.Playing)
            {
                _gameTime += Time.deltaTime;
            }
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion
        
        #region 初始化
        
        /// <summary>
        /// 初始化游戏
        /// </summary>
        public void Initialize()
        {
            Debug.Log("GameManager: 开始初始化...");
            
            // 初始化数据管理器
            ExcelDataManager.Initialize();
            
            // 确保各管理器实例存在
            var actorManager = ActorManager.Instance;
            var sceneManager = SceneElementManager.Instance;
            var combatManager = CombatManager.Instance;
            var inputManager = InputManager.Instance;
            
            // 订阅事件
            SubscribeEvents();
            
            Debug.Log("GameManager: 初始化完成");
            
            // 注意：游戏启动由 GameModeManager 统一管理，不再自动调用 StartGame()
            // 如果需要自动开始，请通过 GameModeManager 的 QuickStartOffline() 等方法
        }
        
        /// <summary>
        /// 订阅各种事件
        /// </summary>
        private void SubscribeEvents()
        {
            // 订阅玩家死亡事件
            ActorManager.Instance.OnPlayerDeath += OnPlayerDeath;
            
            // 订阅输入事件
            InputManager.Instance.OnPausePressed += TogglePause;
        }
        
        #endregion
        
        #region 游戏流程
        
        /// <summary>
        /// 初始化大厅场景
        /// </summary>
        public void InitializeLobby()
        {
            // 如果已经是大厅状态，避免重复初始化
            if (_currentState == GameState.Lobby)
            {
                Debug.Log("[GameManager] 大厅场景已经初始化，跳过重复初始化");
                return;
            }
            
            Debug.Log("[GameManager] 初始化大厅场景");
            ChangeState(GameState.Lobby);
            
            // 清理旧数据（保留玩家数据）
            CleanupScene(false);
            
            // 创建或移动玩家到大厅位置
            CreateOrMovePlayer(_playerSpawnPoint);
            
            // 生成大厅NPC（功能型NPC：商人、铁匠、训练师等）
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
            
            Debug.Log("[GameManager] ✓ 大厅场景初始化完成（可独立测试）");
        }
        
        /// <summary>
        /// 开始游戏（游戏关卡场景）
        /// </summary>
        /// <param name="playerConfigId">玩家配置ID（如果为0则使用默认值）</param>
        /// <param name="playerName">玩家名称（如果为空则从配置获取）</param>
        /// <param name="spawnPoint">玩家出生点（如果为null则使用默认值）</param>
        public void StartGame(int playerConfigId = 0, string playerName = null, Vector3? spawnPoint = null)
        {
            // 如果已经在游戏中，避免重复初始化
            if (_currentState == GameState.Playing)
            {
                Debug.Log("[GameManager] 游戏已经在运行，跳过重复初始化");
                return;
            }
            
            Debug.Log("GameManager: 开始游戏（独立测试模式）");
            
            // 使用传入的参数，如果没有则使用默认值
            int configId = playerConfigId != 0 ? playerConfigId : _playerConfigId;
            string name = !string.IsNullOrEmpty(playerName) ? playerName : ConfigHelper.GetActorName(configId);
            Vector3 spawn = spawnPoint ?? _playerSpawnPoint;
            
            // 清理旧数据（保留玩家数据）
            CleanupScene(false);
            
            // 创建或移动玩家到关卡位置
            CreateOrMovePlayer(spawn);
            
            // 从配置加载刷怪点（关卡专用）
            LoadSpawnPointsFromConfig();
            
            // 创建关卡NPC（关卡中的NPC，不同于大厅NPC）
            SpawnGameLevelNPCs();
            
            // 创建测试物品（关卡专用）
            SpawnTestItems();
            
            // 改变状态
            ChangeState(GameState.Playing);
            _gameTime = 0;
            
            OnGameStart?.Invoke();
            
            Debug.Log("[GameManager] ✓ 游戏关卡场景初始化完成（可独立测试）");
        }
        
        /// <summary>
        /// 从配置加载刷怪点
        /// </summary>
        private void LoadSpawnPointsFromConfig()
        {
            var spawnConfigs = ConfigHelper.GetAllSpawnPointConfigs();
            foreach (var config in spawnConfigs)
            {
                SceneElementManager.Instance.AddSpawnPoint(config);
            }
            Debug.Log($"GameManager: 从配置加载了 {spawnConfigs.Count} 个刷怪点");
        }
        
        /// <summary>
        /// 生成游戏关卡NPC（关卡中的NPC，不同于大厅NPC）
        /// </summary>
        private void SpawnGameLevelNPCs()
        {
            var npcConfigs = ExcelDataManager.GetAllNPC();
            foreach (var npcConfig in npcConfigs)
            {
                var actorConfig = ExcelDataManager.GetActorById(npcConfig.Actorid);
                if (actorConfig == null) continue;
                
                // NPC位置可以从配置或关卡数据获取，这里使用测试位置
                Vector3 position = GetNPCSpawnPosition(npcConfig.Id);
                
                var npc = ActorManager.Instance.CreateNPC(
                    npcConfig.Actorid, 
                    position, 
                    (NPCType)npcConfig.Npctype, 
                    actorConfig.Name
                );
                
                // 从配置初始化NPC
                ConfigHelper.InitNPCFromConfig(npc, npcConfig.Id);
            }
            Debug.Log($"GameManager: 从配置生成了 {npcConfigs.Count} 个关卡NPC");
        }
        
        /// <summary>
        /// 获取NPC出生位置（可以从关卡配置读取）
        /// </summary>
        private Vector3 GetNPCSpawnPosition(int npcId)
        {
            // 临时位置，实际应该从关卡配置读取
            switch (npcId)
            {
                case 1: return new Vector3(-5, 0, 0);   // 商人
                case 2: return new Vector3(5, 0, 0);    // 任务NPC
                case 3: return new Vector3(0, 0, -5);   // 铁匠
                case 4: return new Vector3(-3, 0, -3);  // 医疗兵
                default: return Vector3.zero;
            }
        }
        
        /// <summary>
        /// 生成测试物品
        /// </summary>
        private void SpawnTestItems()
        {
            // 从配置生成武器
            var weaponConfig = ExcelDataManager.GetWeaponById(2101); // M4A1突击步枪
            if (weaponConfig != null)
            {
                SceneElementManager.Instance.SpawnWeapon(
                    weaponConfig.Id, 
                    new Vector3(3, 0, 3),
                    (Item.WeaponType)weaponConfig.Weapontype,
                    weaponConfig.Damage,
                    weaponConfig.Firerate,
                    weaponConfig.Magazinesize
                );
            }
            
            // 从配置生成消耗品
            var healConfig = ExcelDataManager.GetConsumableById(1); // 小型治疗包
            if (healConfig != null)
            {
                SceneElementManager.Instance.SpawnConsumable(
                    healConfig.Itemid,
                    new Vector3(-3, 0, 3),
                    (Item.ConsumableEffectType)healConfig.Effecttype,
                    healConfig.Effectvalue,
                    healConfig.Duration
                );
            }
            
            var ammoConfig = ExcelDataManager.GetConsumableById(102); // 步枪弹药
            if (ammoConfig != null)
            {
                SceneElementManager.Instance.SpawnConsumable(
                    ammoConfig.Itemid,
                    new Vector3(-3, 0, 5),
                    (Item.ConsumableEffectType)ammoConfig.Effecttype,
                    ammoConfig.Effectvalue
                );
            }
            
            Debug.Log("GameManager: 生成测试物品完成");
        }
        
        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            if (_currentState != GameState.Playing) return;
            
            ChangeState(GameState.Paused);
            Time.timeScale = 0;
            InputManager.Instance.InputEnabled = false;
            
            OnGamePaused?.Invoke();
            Debug.Log("游戏已暂停");
        }
        
        /// <summary>
        /// 继续游戏
        /// </summary>
        public void ResumeGame()
        {
            if (_currentState != GameState.Paused) return;
            
            ChangeState(GameState.Playing);
            Time.timeScale = 1;
            InputManager.Instance.InputEnabled = true;
            
            OnGameResumed?.Invoke();
            Debug.Log("游戏已继续");
        }
        
        /// <summary>
        /// 切换暂停状态
        /// </summary>
        public void TogglePause()
        {
            if (_currentState == GameState.Playing)
            {
                PauseGame();
            }
            else if (_currentState == GameState.Paused)
            {
                ResumeGame();
            }
        }
        
        /// <summary>
        /// 游戏结束
        /// </summary>
        public void EndGame(bool victory)
        {
            ChangeState(victory ? GameState.Victory : GameState.GameOver);
            InputManager.Instance.InputEnabled = false;
            
            OnGameEnd?.Invoke(victory);
            
            Debug.Log(victory ? "游戏胜利！" : "游戏失败！");
        }
        
        /// <summary>
        /// 重新开始游戏
        /// </summary>
        public void RestartGame()
        {
            Time.timeScale = 1;
            StartGame();
        }
        
        /// <summary>
        /// 返回主菜单
        /// </summary>
        public void ReturnToMainMenu()
        {
            Time.timeScale = 1;
            CleanupScene(true);
            ChangeState(GameState.MainMenu);
            SceneManager.Instance.LoadMainMenu();
        }
        
        /// <summary>
        /// 从关卡返回大厅
        /// </summary>
        public void ReturnToLobby()
        {
            Debug.Log("[GameManager] 从关卡返回大厅");
            Time.timeScale = 1;
            ChangeState(GameState.Lobby);
            SceneManager.Instance.LoadLobby();
        }
        
        #endregion
        
        #region 状态管理
        
        /// <summary>
        /// 改变游戏状态
        /// </summary>
        public void ChangeState(GameState newState)
        {
            if (_currentState == newState) return;
            
            var oldState = _currentState;
            _currentState = newState;
            
            OnGameStateChanged?.Invoke(oldState, newState);
        }
        
        #endregion
        
        #region 事件处理
        
        /// <summary>
        /// 玩家死亡处理
        /// </summary>
        private void OnPlayerDeath()
        {
            // 延迟显示游戏结束界面
            Invoke(nameof(ShowGameOver), 2f);
        }
        
        private void ShowGameOver()
        {
            EndGame(false);
        }
        
        #endregion
        
        #region 大厅场景功能
        
        /// <summary>
        /// 生成大厅NPC（功能型NPC：商人、铁匠、训练师等）
        /// </summary>
        private void SpawnLobbyNPCs()
        {
            if (_lobbyNPCs.Count > 0)
            {
                foreach (var npcConfig in _lobbyNPCs)
                {
                    SpawnLobbyNPC(npcConfig);
                }
            }
            else
            {
                SpawnDefaultLobbyNPCs();
            }
        }
        
        /// <summary>
        /// 生成单个大厅NPC
        /// </summary>
        private void SpawnLobbyNPC(LobbyNPCConfig config)
        {
            var npcData = ExcelDataManager.GetNPCById(config.NPCConfigId);
            if (npcData == null)
            {
                Debug.LogWarning($"[GameManager] NPC配置不存在: {config.NPCConfigId}");
                return;
            }
            
            var actorData = ExcelDataManager.GetActorById(npcData.Actorid);
            if (actorData == null)
            {
                Debug.LogWarning($"[GameManager] Actor配置不存在: {npcData.Actorid}");
                return;
            }
            
            var npcType = config.OverrideType != NPCType.Normal 
                ? config.OverrideType 
                : (NPCType)npcData.Npctype;
            
            var npc = ActorManager.Instance.CreateNPC(
                npcData.Actorid,
                config.SpawnPosition,
                npcType,
                actorData.Name
            );
            
            ConfigHelper.InitNPCFromConfig(npc, config.NPCConfigId);
            _lobbyNPCsList.Add(npc);
            Debug.Log($"[GameManager] 生成大厅NPC: {actorData.Name} ({npcType})");
        }
        
        /// <summary>
        /// 生成默认大厅NPC
        /// </summary>
        private void SpawnDefaultLobbyNPCs()
        {
            var defaultNPCs = new List<LobbyNPCConfig>
            {
                new LobbyNPCConfig { NPCConfigId = 1, SpawnPosition = new Vector3(-5, 0, 0), OverrideType = NPCType.Merchant },
                new LobbyNPCConfig { NPCConfigId = 3, SpawnPosition = new Vector3(5, 0, 0), OverrideType = NPCType.Blacksmith },
                new LobbyNPCConfig { NPCConfigId = 2, SpawnPosition = new Vector3(0, 0, -5), OverrideType = NPCType.QuestGiver }
            };
            
            foreach (var config in defaultNPCs)
            {
                SpawnLobbyNPC(config);
            }
        }
        
        /// <summary>
        /// 初始化默认关卡列表
        /// </summary>
        private void InitializeDefaultLevels()
        {
            _levels.Add(new LevelInfo { LevelId = 1, LevelName = "训练场", Description = "新手训练关卡", IsUnlocked = true, SupportMultiplayer = false });
            _levels.Add(new LevelInfo { LevelId = 2, LevelName = "废弃工厂", Description = "单人/多人关卡", IsUnlocked = true, SupportMultiplayer = true });
            _levels.Add(new LevelInfo { LevelId = 3, LevelName = "城市废墟", Description = "多人合作关卡", IsUnlocked = false, SupportMultiplayer = true });
        }
        
        /// <summary>
        /// 设置大厅UI
        /// </summary>
        private void SetupLobbyUI()
        {
            if (_lobbyBackButton != null)
            {
                _lobbyBackButton.onClick.RemoveAllListeners();
                _lobbyBackButton.onClick.AddListener(() => ReturnToMainMenu());
            }
            
            if (_openLevelSelectButton != null)
            {
                _openLevelSelectButton.onClick.RemoveAllListeners();
                _openLevelSelectButton.onClick.AddListener(() => {
                    if (_levelSelectPanel != null) _levelSelectPanel.SetActive(!_levelSelectPanel.activeSelf);
                });
            }
            
            if (_offlineButton != null)
            {
                _offlineButton.onClick.RemoveAllListeners();
                _offlineButton.onClick.AddListener(() => SelectGameMode(Network.GameModeType.Offline));
            }
            
            if (_onlineButton != null)
            {
                _onlineButton.onClick.RemoveAllListeners();
                _onlineButton.onClick.AddListener(() => SelectGameMode(Network.GameModeType.Relay));
            }
            
            if (_startButton != null)
            {
                _startButton.onClick.RemoveAllListeners();
                _startButton.onClick.AddListener(OnStartGameFromLobby);
            }
            
            if (_levelSelectPanel != null) _levelSelectPanel.SetActive(false);
        }
        
        /// <summary>
        /// 创建关卡按钮
        /// </summary>
        private void CreateLevelButtons()
        {
            if (_levelButtonContainer == null || _levelButtonPrefab == null) return;
            
            foreach (var btn in _levelButtons)
            {
                if (btn != null) Destroy(btn);
            }
            _levelButtons.Clear();
            
            foreach (var level in _levels)
            {
                var buttonObj = Instantiate(_levelButtonPrefab, _levelButtonContainer);
                var button = buttonObj.GetComponent<UnityEngine.UI.Button>();
                
                if (button != null)
                {
                    var text = buttonObj.GetComponentInChildren<UnityEngine.UI.Text>();
                    if (text != null) text.text = level.LevelName;
                    
                    int levelId = level.LevelId;
                    button.onClick.AddListener(() => SelectLevel(levelId));
                    if (!level.IsUnlocked) button.interactable = false;
                }
                
                _levelButtons.Add(buttonObj);
            }
        }
        
        /// <summary>
        /// 选择关卡
        /// </summary>
        private void SelectLevel(int levelId)
        {
            var level = _levels.Find(l => l.LevelId == levelId);
            if (level == null || !level.IsUnlocked) return;
            
            _selectedLevelId = levelId;
            Debug.Log($"[GameManager] 选择关卡: {level.LevelName}");
            
            if (!level.SupportMultiplayer && _selectedGameMode != Network.GameModeType.Offline)
            {
                SelectGameMode(Network.GameModeType.Offline);
            }
        }
        
        /// <summary>
        /// 选择游戏模式
        /// </summary>
        private void SelectGameMode(Network.GameModeType mode)
        {
            var level = _levels.Find(l => l.LevelId == _selectedLevelId);
            if (level != null && !level.SupportMultiplayer && mode != Network.GameModeType.Offline)
            {
                Debug.LogWarning("[GameManager] 当前关卡不支持多人模式");
                return;
            }
            
            _selectedGameMode = mode;
            Debug.Log($"[GameManager] 选择游戏模式: {mode}");
        }
        
        /// <summary>
        /// 从大厅开始游戏
        /// </summary>
        private void OnStartGameFromLobby()
        {
            if (_selectedLevelId < 0)
            {
                Debug.LogWarning("[GameManager] 请先选择关卡");
                return;
            }
            
            Debug.Log($"[GameManager] 从大厅开始游戏 - 关卡: {_selectedLevelId}, 模式: {_selectedGameMode}");
            
            var gameMode = GameModeManager.Instance;
            gameMode.SetMode(_selectedGameMode);
            SceneManager.Instance.LoadGameLevel(_selectedLevelId, _selectedGameMode);
        }
        
        /// <summary>
        /// 处理NPC交互（大厅功能）
        /// </summary>
        public void OnNPCInteracted(NPC npc)
        {
            if (npc == null || _currentState != GameState.Lobby) return;
            
            Debug.Log($"[GameManager] 与NPC交互: {npc.Name} ({npc.NPCType})");
            
            if (npc.NPCType == NPCType.QuestGiver && _levelSelectPanel != null)
            {
                _levelSelectPanel.SetActive(true);
            }
        }
        
        /// <summary>
        /// 解锁关卡
        /// </summary>
        public void UnlockLevel(int levelId)
        {
            var level = _levels.Find(l => l.LevelId == levelId);
            if (level != null)
            {
                level.IsUnlocked = true;
                CreateLevelButtons();
            }
        }
        
        /// <summary>
        /// 获取关卡信息
        /// </summary>
        public LevelInfo GetLevelInfo(int levelId)
        {
            return _levels.Find(l => l.LevelId == levelId);
        }
        
        /// <summary>
        /// 显示关卡选择面板
        /// </summary>
        public void ShowLevelSelectPanel()
        {
            if (_levelSelectPanel != null) _levelSelectPanel.SetActive(true);
        }
        
        #endregion
        
        #region 存档系统（预留）
        
        /// <summary>
        /// 保存游戏
        /// </summary>
        public void SaveGame(int slotIndex = 0)
        {
            // TODO: 实现存档逻辑
            Debug.Log($"保存游戏到槽位 {slotIndex}");
        }
        
        /// <summary>
        /// 加载游戏
        /// </summary>
        public void LoadGame(int slotIndex = 0)
        {
            // TODO: 实现读档逻辑
            Debug.Log($"从槽位 {slotIndex} 加载游戏");
        }
        
        #endregion
    }
}

