using System;
using UnityEngine;
using GamePlay.Actor;
using ExcelImporter;

namespace GamePlay.Manager
{
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
        /// 游戏中
        /// </summary>
        Playing = 2,
        
        /// <summary>
        /// 暂停
        /// </summary>
        Paused = 3,
        
        /// <summary>
        /// 游戏结束
        /// </summary>
        GameOver = 4,
        
        /// <summary>
        /// 胜利
        /// </summary>
        Victory = 5
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
            
            // 直接开始游戏（后续可改为显示主菜单）
            StartGame();
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
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {
            Debug.Log("GameManager: 开始游戏");
            
            // 清理旧数据
            ActorManager.Instance.ClearAll();
            SceneElementManager.Instance.ClearAll();
            
            // 从配置获取玩家名称
            string playerName = ConfigHelper.GetActorName(_playerConfigId);
            
            // 创建玩家（属性会自动从配置加载）
            var player = ActorManager.Instance.CreatePlayer(_playerConfigId, _playerSpawnPoint, playerName);
            
            // 从配置加载刷怪点
            LoadSpawnPointsFromConfig();
            
            // 创建NPC
            SpawnNPCsFromConfig();
            
            // 创建测试物品
            SpawnTestItems();
            
            // 改变状态
            ChangeState(GameState.Playing);
            _gameTime = 0;
            
            OnGameStart?.Invoke();
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
        /// 从配置生成NPC
        /// </summary>
        private void SpawnNPCsFromConfig()
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
            Debug.Log($"GameManager: 从配置生成了 {npcConfigs.Count} 个NPC");
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
            ActorManager.Instance.ClearAll();
            SceneElementManager.Instance.ClearAll();
            ChangeState(GameState.MainMenu);
        }
        
        #endregion
        
        #region 状态管理
        
        /// <summary>
        /// 改变游戏状态
        /// </summary>
        private void ChangeState(GameState newState)
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

