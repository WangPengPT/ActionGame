using System;
using System.Collections.Generic;
using UnityEngine;
using GamePlay.Data;
using GamePlay.AI;
using GamePlay.Item;

namespace GamePlay.Actor
{
    /// <summary>
    /// 怪物类型
    /// </summary>
    public enum MonsterType
    {
        /// <summary>
        /// 普通怪物
        /// </summary>
        Normal = 0,
        
        /// <summary>
        /// 精英怪物
        /// </summary>
        Elite = 1,
        
        /// <summary>
        /// Boss
        /// </summary>
        Boss = 2,
        
        /// <summary>
        /// 小怪/召唤物
        /// </summary>
        Minion = 3
    }
    
    /// <summary>
    /// 怪物类 - AI控制的敌对单位
    /// </summary>
    public class Monster : ActorBase
    {
        #region 属性
        
        /// <summary>
        /// 怪物类型
        /// </summary>
        [SerializeField] protected MonsterType _monsterType = MonsterType.Normal;
        public MonsterType MonsterType => _monsterType;
        
        /// <summary>
        /// AI控制器
        /// </summary>
        protected AIController _aiController;
        public AIController AIController => _aiController;
        
        /// <summary>
        /// 巡逻路径点
        /// </summary>
        [SerializeField] protected List<Vector3> _patrolPoints = new List<Vector3>();
        public IReadOnlyList<Vector3> PatrolPoints => _patrolPoints;
        
        /// <summary>
        /// 当前巡逻点索引
        /// </summary>
        protected int _currentPatrolIndex;
        
        /// <summary>
        /// 索敌范围
        /// </summary>
        [SerializeField] protected float _detectionRange = 15f;
        public float DetectionRange => _detectionRange;
        
        /// <summary>
        /// 追击范围（超过此范围返回）
        /// </summary>
        [SerializeField] protected float _chaseRange = 25f;
        public float ChaseRange => _chaseRange;
        
        /// <summary>
        /// 出生点
        /// </summary>
        protected Vector3 _spawnPosition;
        public Vector3 SpawnPosition => _spawnPosition;
        
        /// <summary>
        /// 经验奖励
        /// </summary>
        [SerializeField] protected int _expReward = 50;
        public int ExpReward => _expReward;
        
        /// <summary>
        /// 金币奖励
        /// </summary>
        [SerializeField] protected int _goldReward = 10;
        public int GoldReward => _goldReward;
        
        /// <summary>
        /// 掉落物品ID列表（配置表ID）
        /// </summary>
        [SerializeField] protected List<int> _dropItemIds = new List<int>();
        public IReadOnlyList<int> DropItemIds => _dropItemIds;
        
        /// <summary>
        /// 掉落概率（对应dropItemIds）
        /// </summary>
        [SerializeField] protected List<float> _dropRates = new List<float>();
        
        /// <summary>
        /// 仇恨列表
        /// </summary>
        protected Dictionary<ActorBase, float> _threatTable = new Dictionary<ActorBase, float>();
        
        /// <summary>
        /// 是否可以被击退
        /// </summary>
        [SerializeField] protected bool _canBeKnockedBack = true;
        public bool CanBeKnockedBack => _canBeKnockedBack;
        
        #endregion
        
        #region 事件
        
        /// <summary>
        /// 发现敌人事件
        /// </summary>
        public event Action<ActorBase> OnEnemyDetected;
        
        /// <summary>
        /// 丢失目标事件
        /// </summary>
        public event Action OnTargetLost;
        
        /// <summary>
        /// 返回出生点事件
        /// </summary>
        public event Action OnReturnToSpawn;
        
        #endregion
        
        #region Unity生命周期
        
        protected override void Awake()
        {
            base.Awake();
            _faction = ActorFaction.Enemy;
        }
        
        protected override void Start()
        {
            base.Start();
            _spawnPosition = transform.position;
            
            // 初始化AI控制器
            _aiController = GetComponent<AIController>();
            if (_aiController == null)
            {
                _aiController = gameObject.AddComponent<MonsterAIController>();
            }
            _aiController.Initialize(this);
        }
        
        protected override void Update()
        {
            base.Update();
            
            if (!_attribute.IsAlive) return;
            
            // AI控制器更新
            _aiController?.UpdateAI();
            
            // 清理死亡目标的仇恨
            CleanupThreatTable();
        }
        
        #endregion
        
        #region 初始化
        
        /// <summary>
        /// 初始化怪物
        /// </summary>
        public void InitializeMonster(int actorId, int configId, string name, MonsterType type,
            int expReward = 50, int goldReward = 10)
        {
            Initialize(actorId, configId, name, ActorFaction.Enemy);
            _monsterType = type;
            _expReward = expReward;
            _goldReward = goldReward;
            
            // 根据类型调整属性
            AdjustStatsByType();
        }
        
        /// <summary>
        /// 根据类型调整属性
        /// </summary>
        private void AdjustStatsByType()
        {
            switch (_monsterType)
            {
                case MonsterType.Elite:
                    _attribute.MaxHealth *= 2f;
                    _attribute.Attack *= 1.5f;
                    _attribute.Defense *= 1.5f;
                    _expReward *= 3;
                    _goldReward *= 3;
                    break;
                    
                case MonsterType.Boss:
                    _attribute.MaxHealth *= 10f;
                    _attribute.Attack *= 2f;
                    _attribute.Defense *= 2f;
                    _expReward *= 10;
                    _goldReward *= 10;
                    _canBeKnockedBack = false;
                    break;
                    
                case MonsterType.Minion:
                    _attribute.MaxHealth *= 0.5f;
                    _expReward /= 2;
                    _goldReward /= 2;
                    break;
            }
            
            _attribute.CurrentHealth = _attribute.MaxHealth;
        }
        
        /// <summary>
        /// 设置巡逻点
        /// </summary>
        public void SetPatrolPoints(List<Vector3> points)
        {
            _patrolPoints = points ?? new List<Vector3>();
            _currentPatrolIndex = 0;
        }
        
        /// <summary>
        /// 设置掉落配置
        /// </summary>
        public void SetDropConfig(List<int> itemIds, List<float> rates)
        {
            _dropItemIds = itemIds ?? new List<int>();
            _dropRates = rates ?? new List<float>();
        }
        
        #endregion
        
        #region 索敌和仇恨
        
        /// <summary>
        /// 检测周围敌人
        /// </summary>
        public ActorBase DetectEnemy()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, _detectionRange);
            ActorBase nearestEnemy = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var col in colliders)
            {
                var actor = col.GetComponent<ActorBase>();
                if (actor != null && IsHostileTo(actor) && actor.Attribute.IsAlive)
                {
                    float distance = Vector3.Distance(transform.position, actor.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = actor;
                    }
                }
            }
            
            if (nearestEnemy != null && _currentTarget != nearestEnemy)
            {
                SetTarget(nearestEnemy);
                OnEnemyDetected?.Invoke(nearestEnemy);
            }
            
            return nearestEnemy;
        }
        
        /// <summary>
        /// 添加仇恨
        /// </summary>
        public void AddThreat(ActorBase actor, float amount)
        {
            if (actor == null || !IsHostileTo(actor)) return;
            
            if (_threatTable.ContainsKey(actor))
            {
                _threatTable[actor] += amount;
            }
            else
            {
                _threatTable[actor] = amount;
            }
            
            // 更新目标为仇恨最高的
            UpdateTargetByThreat();
        }
        
        /// <summary>
        /// 根据仇恨更新目标
        /// </summary>
        private void UpdateTargetByThreat()
        {
            float maxThreat = 0;
            ActorBase maxThreatTarget = null;
            
            foreach (var pair in _threatTable)
            {
                if (pair.Key != null && pair.Key.Attribute.IsAlive && pair.Value > maxThreat)
                {
                    maxThreat = pair.Value;
                    maxThreatTarget = pair.Key;
                }
            }
            
            if (maxThreatTarget != null)
            {
                SetTarget(maxThreatTarget);
            }
        }
        
        /// <summary>
        /// 清理仇恨表
        /// </summary>
        private void CleanupThreatTable()
        {
            var toRemove = new List<ActorBase>();
            foreach (var pair in _threatTable)
            {
                if (pair.Key == null || !pair.Key.Attribute.IsAlive)
                {
                    toRemove.Add(pair.Key);
                }
            }
            
            foreach (var actor in toRemove)
            {
                _threatTable.Remove(actor);
            }
            
            // 如果当前目标死亡，切换目标
            if (_currentTarget != null && !_currentTarget.Attribute.IsAlive)
            {
                UpdateTargetByThreat();
                if (_currentTarget == null || !_currentTarget.Attribute.IsAlive)
                {
                    OnTargetLost?.Invoke();
                }
            }
        }
        
        /// <summary>
        /// 清空仇恨
        /// </summary>
        public void ClearThreat()
        {
            _threatTable.Clear();
            _currentTarget = null;
        }
        
        #endregion
        
        #region 巡逻
        
        /// <summary>
        /// 获取下一个巡逻点
        /// </summary>
        public Vector3 GetNextPatrolPoint()
        {
            if (_patrolPoints.Count == 0)
                return _spawnPosition;
            
            var point = _patrolPoints[_currentPatrolIndex];
            _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPoints.Count;
            return point;
        }
        
        /// <summary>
        /// 返回出生点
        /// </summary>
        public void ReturnToSpawn()
        {
            MoveTo(_spawnPosition);
            ClearThreat();
            OnReturnToSpawn?.Invoke();
        }
        
        /// <summary>
        /// 是否超出追击范围
        /// </summary>
        public bool IsOutOfChaseRange()
        {
            return Vector3.Distance(transform.position, _spawnPosition) > _chaseRange;
        }
        
        #endregion
        
        #region 战斗
        
        public override void TakeDamage(float damage, ActorBase attacker, bool ignoreDefense = false)
        {
            base.TakeDamage(damage, attacker, ignoreDefense);
            
            // 受到伤害时增加仇恨
            if (attacker != null)
            {
                AddThreat(attacker, damage);
            }
        }
        
        public override void Die(ActorBase killer = null)
        {
            base.Die(killer);
            
            // 给予奖励
            if (killer is Character character)
            {
                character.AddExperience(_expReward);
                character.AddGold(_goldReward);
            }
            
            // 掉落物品
            DropLoot();
        }
        
        /// <summary>
        /// 掉落物品
        /// </summary>
        protected virtual void DropLoot()
        {
            for (int i = 0; i < _dropItemIds.Count; i++)
            {
                float rate = i < _dropRates.Count ? _dropRates[i] : 0.1f;
                if (UnityEngine.Random.value < rate)
                {
                    // 通过ItemManager创建掉落物品
                    Debug.Log($"掉落物品ID: {_dropItemIds[i]}");
                }
            }
        }
        
        #endregion
        
        #region 特殊能力
        
        /// <summary>
        /// 使用技能（子类重写）
        /// </summary>
        public virtual void UseSkill(int skillId)
        {
            // 子类实现具体技能逻辑
        }
        
        /// <summary>
        /// 召唤小怪（Boss专用）
        /// </summary>
        public virtual void SummonMinions(int count, int minionConfigId)
        {
            if (_monsterType != MonsterType.Boss) return;
            
            Debug.Log($"Boss召唤了 {count} 个小怪");
            // 通过ActorManager创建小怪
        }
        
        #endregion
    }
}
