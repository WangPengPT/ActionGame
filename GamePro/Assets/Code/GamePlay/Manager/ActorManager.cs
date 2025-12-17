using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GamePlay.Actor;
using GamePlay.Data;
using ExcelImporter;

namespace GamePlay.Manager
{
    /// <summary>
    /// Actor管理器 - 管理场景中所有Actor的创建、销毁和查询
    /// </summary>
    public class ActorManager : MonoBehaviour
    {
        #region 单例
        
        private static ActorManager _instance;
        public static ActorManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ActorManager");
                    _instance = go.AddComponent<ActorManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region 属性
        
        /// <summary>
        /// 所有Actor字典（ID -> Actor）
        /// </summary>
        private Dictionary<int, ActorBase> _actors = new Dictionary<int, ActorBase>();
        
        /// <summary>
        /// 按类型分类的Actor列表
        /// </summary>
        private Dictionary<Type, List<ActorBase>> _actorsByType = new Dictionary<Type, List<ActorBase>>();
        
        /// <summary>
        /// 按阵营分类的Actor列表
        /// </summary>
        private Dictionary<ActorFaction, List<ActorBase>> _actorsByFaction = new Dictionary<ActorFaction, List<ActorBase>>();
        
        /// <summary>
        /// ID生成器
        /// </summary>
        private int _nextActorId = 1;
        
        /// <summary>
        /// 玩家角色引用
        /// </summary>
        private Character _playerCharacter;
        public Character PlayerCharacter => _playerCharacter;
        
        /// <summary>
        /// 当前选中的Actor
        /// </summary>
        private ActorBase _selectedActor;
        public ActorBase SelectedActor => _selectedActor;
        
        /// <summary>
        /// Actor预制体缓存（ConfigId -> Prefab）
        /// </summary>
        private Dictionary<int, GameObject> _actorPrefabs = new Dictionary<int, GameObject>();
        
        #endregion
        
        #region 事件
        
        /// <summary>
        /// Actor创建事件
        /// </summary>
        public event Action<ActorBase> OnActorCreated;
        
        /// <summary>
        /// Actor销毁事件
        /// </summary>
        public event Action<ActorBase> OnActorDestroyed;
        
        /// <summary>
        /// Actor选中事件
        /// </summary>
        public event Action<ActorBase> OnActorSelected;
        
        /// <summary>
        /// Actor取消选中事件
        /// </summary>
        public event Action<ActorBase> OnActorDeselected;
        
        /// <summary>
        /// 玩家死亡事件
        /// </summary>
        public event Action OnPlayerDeath;
        
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
            
            // 初始化阵营字典
            foreach (ActorFaction faction in Enum.GetValues(typeof(ActorFaction)))
            {
                _actorsByFaction[faction] = new List<ActorBase>();
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
        
        #region Actor创建
        
        /// <summary>
        /// 生成Actor的唯一ID
        /// </summary>
        public int GenerateActorId()
        {
            return _nextActorId++;
        }
        
        /// <summary>
        /// 创建玩家角色
        /// </summary>
        public Character CreatePlayer(int configId, Vector3 position, string name = "Player")
        {
            var prefab = GetActorPrefab(configId);
            GameObject go;
            
            if (prefab != null)
            {
                go = Instantiate(prefab, position, Quaternion.identity);
            }
            else
            {
                go = new GameObject(name);
                go.transform.position = position;
            }
            
            var character = go.GetComponent<Character>();
            if (character == null)
            {
                character = go.AddComponent<Character>();
            }
            
            int actorId = GenerateActorId();
            character.Initialize(actorId, configId, name, ActorFaction.Player);
            
            // 从配置初始化属性
            InitializeActorFromConfig(character, configId);
            
            RegisterActor(character);
            _playerCharacter = character;
            
            // 订阅玩家死亡事件
            character.OnDeath += OnPlayerCharacterDeath;
            
            OnActorCreated?.Invoke(character);
            Debug.Log($"创建玩家角色: {name} (ID: {actorId})");
            
            return character;
        }
        
        /// <summary>
        /// 创建怪物
        /// </summary>
        public Monster CreateMonster(int configId, Vector3 position, MonsterType type = MonsterType.Normal, string name = null)
        {
            var prefab = GetActorPrefab(configId);
            GameObject go;
            
            if (prefab != null)
            {
                go = Instantiate(prefab, position, Quaternion.identity);
            }
            else
            {
                go = new GameObject(name ?? $"Monster_{configId}");
                go.transform.position = position;
            }
            
            var monster = go.GetComponent<Monster>();
            if (monster == null)
            {
                monster = go.AddComponent<Monster>();
            }
            
            int actorId = GenerateActorId();
            monster.InitializeMonster(actorId, configId, name ?? $"怪物{configId}", type);
            
            // 从配置初始化属性
            InitializeActorFromConfig(monster, configId);
            
            RegisterActor(monster);
            
            OnActorCreated?.Invoke(monster);
            Debug.Log($"创建怪物: {monster.ActorName} (ID: {actorId}, Type: {type})");
            
            return monster;
        }
        
        /// <summary>
        /// 创建NPC
        /// </summary>
        public NPC CreateNPC(int configId, Vector3 position, NPCType type = NPCType.Normal, string name = null)
        {
            var prefab = GetActorPrefab(configId);
            GameObject go;
            
            if (prefab != null)
            {
                go = Instantiate(prefab, position, Quaternion.identity);
            }
            else
            {
                go = new GameObject(name ?? $"NPC_{configId}");
                go.transform.position = position;
            }
            
            var npc = go.GetComponent<NPC>();
            if (npc == null)
            {
                npc = go.AddComponent<NPC>();
            }
            
            int actorId = GenerateActorId();
            npc.InitializeNPC(actorId, configId, name ?? $"NPC{configId}", type);
            
            // 从配置初始化属性
            InitializeActorFromConfig(npc, configId);
            
            RegisterActor(npc);
            
            OnActorCreated?.Invoke(npc);
            Debug.Log($"创建NPC: {npc.ActorName} (ID: {actorId}, Type: {type})");
            
            return npc;
        }
        
        /// <summary>
        /// 从配置初始化Actor属性
        /// </summary>
        private void InitializeActorFromConfig(ActorBase actor, int configId)
        {
            ConfigHelper.InitActorFromConfig(actor, configId);
        }
        
        /// <summary>
        /// 获取Actor预制体
        /// </summary>
        private GameObject GetActorPrefab(int configId)
        {
            if (_actorPrefabs.TryGetValue(configId, out var prefab))
            {
                return prefab;
            }
            
            // TODO: 从Resources或AssetBundle加载预制体
            // var prefab = Resources.Load<GameObject>($"Prefabs/Actors/{configId}");
            // if (prefab != null)
            // {
            //     _actorPrefabs[configId] = prefab;
            // }
            
            return null;
        }
        
        #endregion
        
        #region Actor注册和注销
        
        /// <summary>
        /// 注册Actor
        /// </summary>
        public void RegisterActor(ActorBase actor)
        {
            if (actor == null || _actors.ContainsKey(actor.ActorId)) return;
            
            _actors[actor.ActorId] = actor;
            
            // 按类型分类
            var type = actor.GetType();
            if (!_actorsByType.ContainsKey(type))
            {
                _actorsByType[type] = new List<ActorBase>();
            }
            _actorsByType[type].Add(actor);
            
            // 按阵营分类
            _actorsByFaction[actor.Faction].Add(actor);
            
            // 订阅死亡事件
            actor.OnDeath += OnActorDeath;
        }
        
        /// <summary>
        /// 注销Actor
        /// </summary>
        public void UnregisterActor(ActorBase actor)
        {
            if (actor == null || !_actors.ContainsKey(actor.ActorId)) return;
            
            _actors.Remove(actor.ActorId);
            
            // 从类型列表移除
            var type = actor.GetType();
            if (_actorsByType.ContainsKey(type))
            {
                _actorsByType[type].Remove(actor);
            }
            
            // 从阵营列表移除
            _actorsByFaction[actor.Faction].Remove(actor);
            
            // 取消订阅
            actor.OnDeath -= OnActorDeath;
            
            // 如果是选中的Actor，取消选中
            if (_selectedActor == actor)
            {
                DeselectActor();
            }
        }
        
        /// <summary>
        /// 销毁Actor
        /// </summary>
        public void DestroyActor(ActorBase actor)
        {
            if (actor == null) return;
            
            UnregisterActor(actor);
            OnActorDestroyed?.Invoke(actor);
            
            if (actor.gameObject != null)
            {
                Destroy(actor.gameObject);
            }
        }
        
        /// <summary>
        /// 销毁指定ID的Actor
        /// </summary>
        public void DestroyActor(int actorId)
        {
            if (_actors.TryGetValue(actorId, out var actor))
            {
                DestroyActor(actor);
            }
        }
        
        #endregion
        
        #region Actor查询
        
        /// <summary>
        /// 通过ID获取Actor
        /// </summary>
        public ActorBase GetActorById(int actorId)
        {
            _actors.TryGetValue(actorId, out var actor);
            return actor;
        }
        
        /// <summary>
        /// 获取指定类型的所有Actor
        /// </summary>
        public List<T> GetActorsByType<T>() where T : ActorBase
        {
            var type = typeof(T);
            if (_actorsByType.TryGetValue(type, out var list))
            {
                return list.Cast<T>().ToList();
            }
            return new List<T>();
        }
        
        /// <summary>
        /// 获取指定阵营的所有Actor
        /// </summary>
        public List<ActorBase> GetActorsByFaction(ActorFaction faction)
        {
            return _actorsByFaction.TryGetValue(faction, out var list) 
                ? new List<ActorBase>(list) 
                : new List<ActorBase>();
        }
        
        /// <summary>
        /// 获取所有存活的Actor
        /// </summary>
        public List<ActorBase> GetAllAliveActors()
        {
            return _actors.Values.Where(a => a.Attribute.IsAlive).ToList();
        }
        
        /// <summary>
        /// 获取范围内的Actor
        /// </summary>
        public List<ActorBase> GetActorsInRange(Vector3 center, float radius, ActorFaction? factionFilter = null)
        {
            var result = new List<ActorBase>();
            float radiusSqr = radius * radius;
            
            foreach (var actor in _actors.Values)
            {
                if (!actor.Attribute.IsAlive) continue;
                if (factionFilter.HasValue && actor.Faction != factionFilter.Value) continue;
                
                float distanceSqr = (actor.transform.position - center).sqrMagnitude;
                if (distanceSqr <= radiusSqr)
                {
                    result.Add(actor);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 获取最近的敌对Actor
        /// </summary>
        public ActorBase GetNearestHostileActor(ActorBase actor, float maxRange = float.MaxValue)
        {
            ActorBase nearest = null;
            float nearestDistance = maxRange;
            
            foreach (var other in _actors.Values)
            {
                if (!other.Attribute.IsAlive) continue;
                if (!actor.IsHostileTo(other)) continue;
                
                float distance = Vector3.Distance(actor.transform.position, other.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = other;
                }
            }
            
            return nearest;
        }
        
        /// <summary>
        /// 获取所有怪物
        /// </summary>
        public List<Monster> GetAllMonsters()
        {
            return GetActorsByType<Monster>();
        }
        
        /// <summary>
        /// 获取所有NPC
        /// </summary>
        public List<NPC> GetAllNPCs()
        {
            return GetActorsByType<NPC>();
        }
        
        #endregion
        
        #region 选中系统
        
        /// <summary>
        /// 选中Actor
        /// </summary>
        public void SelectActor(ActorBase actor)
        {
            if (actor == null || !actor.Selectable) return;
            
            // 取消之前的选中
            if (_selectedActor != null && _selectedActor != actor)
            {
                _selectedActor.Deselect();
                OnActorDeselected?.Invoke(_selectedActor);
            }
            
            _selectedActor = actor;
            actor.Select();
            OnActorSelected?.Invoke(actor);
            
            Debug.Log($"选中Actor: {actor.ActorName}");
        }
        
        /// <summary>
        /// 取消选中
        /// </summary>
        public void DeselectActor()
        {
            if (_selectedActor == null) return;
            
            var actor = _selectedActor;
            _selectedActor.Deselect();
            _selectedActor = null;
            
            OnActorDeselected?.Invoke(actor);
        }
        
        /// <summary>
        /// 通过屏幕点击选中Actor
        /// </summary>
        public ActorBase SelectActorByScreenPoint(Vector2 screenPoint, Camera camera)
        {
            Ray ray = camera.ScreenPointToRay(screenPoint);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var actor = hit.collider.GetComponent<ActorBase>();
                if (actor != null)
                {
                    SelectActor(actor);
                    return actor;
                }
            }
            
            DeselectActor();
            return null;
        }
        
        #endregion
        
        #region 事件处理
        
        /// <summary>
        /// Actor死亡处理
        /// </summary>
        private void OnActorDeath(ActorBase actor)
        {
            // 延迟销毁，给死亡动画时间
            StartCoroutine(DelayedDestroy(actor, 3f));
        }
        
        /// <summary>
        /// 玩家角色死亡处理
        /// </summary>
        private void OnPlayerCharacterDeath(ActorBase actor)
        {
            OnPlayerDeath?.Invoke();
            Debug.Log("玩家死亡！");
        }
        
        /// <summary>
        /// 延迟销毁
        /// </summary>
        private System.Collections.IEnumerator DelayedDestroy(ActorBase actor, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (actor != null && actor != _playerCharacter)
            {
                DestroyActor(actor);
            }
        }
        
        #endregion
        
        #region 清理
        
        /// <summary>
        /// 清除所有Actor（保留玩家）
        /// </summary>
        public void ClearAllExceptPlayer()
        {
            var toRemove = _actors.Values.Where(a => a != _playerCharacter).ToList();
            foreach (var actor in toRemove)
            {
                DestroyActor(actor);
            }
        }
        
        /// <summary>
        /// 清除所有Actor
        /// </summary>
        public void ClearAll()
        {
            var toRemove = _actors.Values.ToList();
            foreach (var actor in toRemove)
            {
                DestroyActor(actor);
            }
            
            _playerCharacter = null;
            _nextActorId = 1;
        }
        
        #endregion
    }
}
