using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GamePlay.Item;
using GamePlay.Actor;

namespace GamePlay.Manager
{
    /// <summary>
    /// 场景元素类型
    /// </summary>
    public enum SceneElementType
    {
        /// <summary>
        /// 物品
        /// </summary>
        Item = 0,
        
        /// <summary>
        /// 掩体
        /// </summary>
        Cover = 1,
        
        /// <summary>
        /// 触发器
        /// </summary>
        Trigger = 2,
        
        /// <summary>
        /// 可交互物
        /// </summary>
        Interactable = 3,
        
        /// <summary>
        /// 刷怪点
        /// </summary>
        SpawnPoint = 4
    }
    
    /// <summary>
    /// 刷怪点配置
    /// </summary>
    [Serializable]
    public class SpawnPointConfig
    {
        public int ConfigId;
        public Vector3 Position;
        public float RespawnTime;
        public int MaxCount;
        public MonsterType MonsterType;
    }
    
    /// <summary>
    /// 场景元素管理器 - 管理场景中的物品、掩体等非Actor元素
    /// </summary>
    public class SceneElementManager : MonoBehaviour
    {
        #region 单例
        
        private static SceneElementManager _instance;
        public static SceneElementManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SceneElementManager");
                    _instance = go.AddComponent<SceneElementManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region 属性
        
        /// <summary>
        /// 地面物品列表
        /// </summary>
        private Dictionary<int, ItemBase> _groundItems = new Dictionary<int, ItemBase>();
        
        /// <summary>
        /// 物品ID生成器
        /// </summary>
        private int _nextItemId = 1;
        
        /// <summary>
        /// 掩体列表
        /// </summary>
        private List<Cover> _covers = new List<Cover>();
        
        /// <summary>
        /// 刷怪点配置
        /// </summary>
        private List<SpawnPointConfig> _spawnPoints = new List<SpawnPointConfig>();
        
        /// <summary>
        /// 刷怪计时器
        /// </summary>
        private Dictionary<int, float> _spawnTimers = new Dictionary<int, float>();
        
        /// <summary>
        /// 当前刷出的怪物数量
        /// </summary>
        private Dictionary<int, int> _spawnCounts = new Dictionary<int, int>();
        
        /// <summary>
        /// 交互提示范围
        /// </summary>
        [SerializeField] private float _interactionRange = 3f;
        
        /// <summary>
        /// 当前可交互的物品
        /// </summary>
        private ItemBase _nearestInteractableItem;
        public ItemBase NearestInteractableItem => _nearestInteractableItem;
        
        /// <summary>
        /// 物品预制体缓存
        /// </summary>
        private Dictionary<int, GameObject> _itemPrefabs = new Dictionary<int, GameObject>();
        
        #endregion
        
        #region 事件
        
        /// <summary>
        /// 物品生成事件
        /// </summary>
        public event Action<ItemBase> OnItemSpawned;
        
        /// <summary>
        /// 物品被拾取事件
        /// </summary>
        public event Action<ItemBase, Character> OnItemPickedUp;
        
        /// <summary>
        /// 进入掩体事件
        /// </summary>
        public event Action<Cover> OnCoverEntered;
        
        /// <summary>
        /// 离开掩体事件
        /// </summary>
        public event Action<Cover> OnCoverExited;
        
        /// <summary>
        /// 可交互物品变化事件
        /// </summary>
        public event Action<ItemBase> OnNearestInteractableChanged;
        
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
        }
        
        private void Update()
        {
            UpdateSpawnPoints();
            UpdateNearestInteractable();
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion
        
        #region 物品管理
        
        /// <summary>
        /// 生成物品ID
        /// </summary>
        public int GenerateItemId()
        {
            return _nextItemId++;
        }
        
        /// <summary>
        /// 在地面生成物品
        /// </summary>
        public ItemBase SpawnItem(int configId, Vector3 position, int count = 1)
        {
            var prefab = GetItemPrefab(configId);
            GameObject go;
            
            if (prefab != null)
            {
                go = Instantiate(prefab, position, Quaternion.identity);
            }
            else
            {
                go = new GameObject($"Item_{configId}");
                go.transform.position = position;
                
                // 添加基础组件
                var collider = go.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = 0.5f;
            }
            
            var item = go.GetComponent<ItemBase>();
            if (item == null)
            {
                // 根据配置创建对应类型的物品
                item = CreateItemComponent(go, configId);
            }
            
            int itemId = GenerateItemId();
            InitializeItemFromConfig(item, itemId, configId);
            item.StackCount = count;
            
            _groundItems[itemId] = item;
            
            OnItemSpawned?.Invoke(item);
            Debug.Log($"生成物品: {item.ItemName} x{count} 在 {position}");
            
            return item;
        }
        
        /// <summary>
        /// 创建物品组件
        /// </summary>
        private ItemBase CreateItemComponent(GameObject go, int configId)
        {
            // TODO: 根据配置确定物品类型
            // 临时：返回基础物品
            return go.AddComponent<ItemBase>();
        }
        
        /// <summary>
        /// 从配置初始化物品
        /// </summary>
        private void InitializeItemFromConfig(ItemBase item, int itemId, int configId)
        {
            // TODO: 从Excel配置读取物品数据
            // var config = ExcelDataManager.GetItemById(configId);
            
            // 临时默认配置
            item.Initialize(itemId, configId, $"物品{configId}", ItemType.Material, ItemQuality.Common);
        }
        
        /// <summary>
        /// 获取物品预制体
        /// </summary>
        private GameObject GetItemPrefab(int configId)
        {
            if (_itemPrefabs.TryGetValue(configId, out var prefab))
            {
                return prefab;
            }
            
            // TODO: 从Resources加载
            return null;
        }
        
        /// <summary>
        /// 生成消耗品
        /// </summary>
        public Consumable SpawnConsumable(int configId, Vector3 position, 
            ConsumableEffectType effectType, float effectValue, float duration = 0)
        {
            var go = new GameObject($"Consumable_{configId}");
            go.transform.position = position;
            
            var collider = go.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
            
            var item = go.AddComponent<Consumable>();
            int itemId = GenerateItemId();
            item.InitializeConsumable(itemId, configId, $"消耗品{configId}", ItemQuality.Common,
                effectType, effectValue, duration);
            
            _groundItems[itemId] = item;
            OnItemSpawned?.Invoke(item);
            
            return item;
        }
        
        /// <summary>
        /// 生成武器
        /// </summary>
        public Weapon SpawnWeapon(int configId, Vector3 position, 
            WeaponType weaponType, float damage, float fireRate, int magazineSize)
        {
            var go = new GameObject($"Weapon_{configId}");
            go.transform.position = position;
            
            var collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(0.5f, 0.2f, 1f);
            
            var weapon = go.AddComponent<Weapon>();
            int itemId = GenerateItemId();
            weapon.InitializeWeapon(itemId, configId, $"武器{configId}", ItemQuality.Rare,
                weaponType, ElementType.None, damage, fireRate, 30f, magazineSize, 2f);
            
            _groundItems[itemId] = weapon;
            OnItemSpawned?.Invoke(weapon);
            
            return weapon;
        }
        
        /// <summary>
        /// 移除地面物品
        /// </summary>
        public void RemoveGroundItem(ItemBase item)
        {
            if (item == null) return;
            
            _groundItems.Remove(item.ItemId);
            
            if (_nearestInteractableItem == item)
            {
                _nearestInteractableItem = null;
                OnNearestInteractableChanged?.Invoke(null);
            }
        }
        
        /// <summary>
        /// 玩家拾取物品
        /// </summary>
        public bool PickupItem(Character character, ItemBase item)
        {
            if (character == null || item == null) return false;
            if (!item.IsOnGround) return false;
            
            float distance = Vector3.Distance(character.transform.position, item.transform.position);
            if (distance > _interactionRange)
            {
                Debug.LogWarning("距离太远，无法拾取");
                return false;
            }
            
            if (character.PickupItem(item))
            {
                RemoveGroundItem(item);
                OnItemPickedUp?.Invoke(item, character);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 拾取最近的物品
        /// </summary>
        public bool PickupNearestItem(Character character)
        {
            if (_nearestInteractableItem == null) return false;
            return PickupItem(character, _nearestInteractableItem);
        }
        
        /// <summary>
        /// 更新最近可交互物品
        /// </summary>
        private void UpdateNearestInteractable()
        {
            var player = ActorManager.Instance?.PlayerCharacter;
            if (player == null) return;
            
            ItemBase nearest = null;
            float nearestDistance = _interactionRange;
            
            foreach (var item in _groundItems.Values)
            {
                if (!item.IsOnGround) continue;
                
                float distance = Vector3.Distance(player.transform.position, item.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = item;
                }
            }
            
            if (nearest != _nearestInteractableItem)
            {
                _nearestInteractableItem = nearest;
                OnNearestInteractableChanged?.Invoke(nearest);
            }
        }
        
        /// <summary>
        /// 获取范围内的物品
        /// </summary>
        public List<ItemBase> GetItemsInRange(Vector3 center, float radius)
        {
            var result = new List<ItemBase>();
            float radiusSqr = radius * radius;
            
            foreach (var item in _groundItems.Values)
            {
                if (!item.IsOnGround) continue;
                
                float distanceSqr = (item.transform.position - center).sqrMagnitude;
                if (distanceSqr <= radiusSqr)
                {
                    result.Add(item);
                }
            }
            
            return result;
        }
        
        #endregion
        
        #region 掩体管理
        
        /// <summary>
        /// 注册掩体
        /// </summary>
        public void RegisterCover(Cover cover)
        {
            if (!_covers.Contains(cover))
            {
                _covers.Add(cover);
            }
        }
        
        /// <summary>
        /// 注销掩体
        /// </summary>
        public void UnregisterCover(Cover cover)
        {
            _covers.Remove(cover);
        }
        
        /// <summary>
        /// 获取最近的可用掩体
        /// </summary>
        public Cover GetNearestAvailableCover(Vector3 position, Vector3 threatDirection)
        {
            Cover bestCover = null;
            float bestScore = float.MinValue;
            
            foreach (var cover in _covers)
            {
                if (!cover.IsAvailable) continue;
                
                float distance = Vector3.Distance(position, cover.transform.position);
                
                // 计算掩体相对威胁方向的角度（越接近背对威胁越好）
                Vector3 coverDir = (cover.transform.position - position).normalized;
                float angle = Vector3.Angle(coverDir, -threatDirection);
                
                // 评分：距离近且方向好的掩体分数高
                float score = -distance + (180 - angle) * 0.1f;
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCover = cover;
                }
            }
            
            return bestCover;
        }
        
        /// <summary>
        /// 获取范围内的掩体
        /// </summary>
        public List<Cover> GetCoversInRange(Vector3 center, float radius)
        {
            return _covers.Where(c => 
                Vector3.Distance(c.transform.position, center) <= radius).ToList();
        }
        
        #endregion
        
        #region 刷怪点管理
        
        /// <summary>
        /// 添加刷怪点
        /// </summary>
        public void AddSpawnPoint(SpawnPointConfig config)
        {
            int index = _spawnPoints.Count;
            _spawnPoints.Add(config);
            _spawnTimers[index] = 0;
            _spawnCounts[index] = 0;
        }
        
        /// <summary>
        /// 更新刷怪点
        /// </summary>
        private void UpdateSpawnPoints()
        {
            for (int i = 0; i < _spawnPoints.Count; i++)
            {
                var config = _spawnPoints[i];
                
                // 检查是否达到最大数量
                if (_spawnCounts[i] >= config.MaxCount) continue;
                
                // 更新计时器
                _spawnTimers[i] += Time.deltaTime;
                
                if (_spawnTimers[i] >= config.RespawnTime)
                {
                    _spawnTimers[i] = 0;
                    SpawnMonsterAtPoint(i, config);
                }
            }
        }
        
        /// <summary>
        /// 在刷怪点生成怪物
        /// </summary>
        private void SpawnMonsterAtPoint(int pointIndex, SpawnPointConfig config)
        {
            var monster = ActorManager.Instance.CreateMonster(
                config.ConfigId, 
                config.Position, 
                config.MonsterType);
            
            if (monster != null)
            {
                _spawnCounts[pointIndex]++;
                
                // 怪物死亡时减少计数
                monster.OnDeath += (actor) =>
                {
                    if (_spawnCounts.ContainsKey(pointIndex))
                    {
                        _spawnCounts[pointIndex]--;
                    }
                };
            }
        }
        
        /// <summary>
        /// 清除所有刷怪点
        /// </summary>
        public void ClearSpawnPoints()
        {
            _spawnPoints.Clear();
            _spawnTimers.Clear();
            _spawnCounts.Clear();
        }
        
        #endregion
        
        #region 清理
        
        /// <summary>
        /// 清除所有地面物品
        /// </summary>
        public void ClearAllGroundItems()
        {
            foreach (var item in _groundItems.Values.ToList())
            {
                if (item != null && item.gameObject != null)
                {
                    Destroy(item.gameObject);
                }
            }
            _groundItems.Clear();
            _nearestInteractableItem = null;
        }
        
        /// <summary>
        /// 清除所有场景元素
        /// </summary>
        public void ClearAll()
        {
            ClearAllGroundItems();
            ClearSpawnPoints();
            _covers.Clear();
        }
        
        #endregion
    }
    
    /// <summary>
    /// 掩体组件
    /// </summary>
    public class Cover : MonoBehaviour
    {
        /// <summary>
        /// 掩体高度
        /// </summary>
        [SerializeField] private float _height = 1f;
        public float Height => _height;
        
        /// <summary>
        /// 是否可用
        /// </summary>
        [SerializeField] private bool _isAvailable = true;
        public bool IsAvailable 
        { 
            get => _isAvailable; 
            set => _isAvailable = value; 
        }
        
        /// <summary>
        /// 可提供的防护值（0-1）
        /// </summary>
        [SerializeField] private float _coverValue = 0.5f;
        public float CoverValue => _coverValue;
        
        /// <summary>
        /// 当前使用者
        /// </summary>
        private ActorBase _currentUser;
        public ActorBase CurrentUser => _currentUser;
        
        private void Start()
        {
            SceneElementManager.Instance?.RegisterCover(this);
        }
        
        private void OnDestroy()
        {
            SceneElementManager.Instance?.UnregisterCover(this);
        }
        
        /// <summary>
        /// 使用掩体
        /// </summary>
        public bool Use(ActorBase user)
        {
            if (!_isAvailable || _currentUser != null) return false;
            
            _currentUser = user;
            _isAvailable = false;
            return true;
        }
        
        /// <summary>
        /// 离开掩体
        /// </summary>
        public void Release()
        {
            _currentUser = null;
            _isAvailable = true;
        }
    }
}

