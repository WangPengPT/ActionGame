using System;
using System.Collections.Generic;
using UnityEngine;
using GamePlay.Data;

namespace GamePlay.Actor
{
    /// <summary>
    /// Actor基类 - 游戏中所有活动实体的基类（角色、怪物、NPC）
    /// </summary>
    public abstract class ActorBase : MonoBehaviour
    {
        #region 属性和字段
        
        /// <summary>
        /// Actor唯一ID
        /// </summary>
        [SerializeField] protected int _actorId;
        public int ActorId => _actorId;
        
        /// <summary>
        /// 配置表ID（Excel中的ID）
        /// </summary>
        [SerializeField] protected int _configId;
        public int ConfigId => _configId;
        
        /// <summary>
        /// Actor名称
        /// </summary>
        [SerializeField] protected string _actorName;
        public string ActorName => _actorName;
        
        /// <summary>
        /// 阵营
        /// </summary>
        [SerializeField] protected ActorFaction _faction = ActorFaction.Neutral;
        public ActorFaction Faction 
        { 
            get => _faction; 
            set => _faction = value; 
        }
        
        /// <summary>
        /// 当前状态
        /// </summary>
        [SerializeField] protected ActorState _currentState = ActorState.Idle;
        public ActorState CurrentState 
        { 
            get => _currentState; 
            protected set
            {
                if (_currentState != value)
                {
                    var oldState = _currentState;
                    _currentState = value;
                    OnStateChanged?.Invoke(oldState, _currentState);
                }
            }
        }
        
        /// <summary>
        /// 属性系统
        /// </summary>
        [SerializeField] protected ActorAttribute _attribute = new ActorAttribute();
        public ActorAttribute Attribute => _attribute;
        
        /// <summary>
        /// Buff列表
        /// </summary>
        protected List<BuffData> _buffs = new List<BuffData>();
        public IReadOnlyList<BuffData> Buffs => _buffs;
        
        /// <summary>
        /// 是否可以被选中
        /// </summary>
        [SerializeField] protected bool _selectable = true;
        public bool Selectable 
        { 
            get => _selectable; 
            set => _selectable = value; 
        }
        
        /// <summary>
        /// 是否被选中
        /// </summary>
        protected bool _isSelected;
        public bool IsSelected => _isSelected;
        
        /// <summary>
        /// 当前目标
        /// </summary>
        protected ActorBase _currentTarget;
        public ActorBase CurrentTarget => _currentTarget;
        
        /// <summary>
        /// 上次攻击时间
        /// </summary>
        protected float _lastAttackTime;
        
        #endregion
        
        #region 事件
        
        /// <summary>
        /// 状态改变事件
        /// </summary>
        public event Action<ActorState, ActorState> OnStateChanged;
        
        /// <summary>
        /// 受伤事件
        /// </summary>
        public event Action<float, ActorBase> OnDamaged;
        
        /// <summary>
        /// 死亡事件
        /// </summary>
        public event Action<ActorBase> OnDeath;
        
        /// <summary>
        /// 治疗事件
        /// </summary>
        public event Action<float> OnHealed;
        
        /// <summary>
        /// Buff添加事件
        /// </summary>
        public event Action<BuffData> OnBuffAdded;
        
        /// <summary>
        /// Buff移除事件
        /// </summary>
        public event Action<BuffData> OnBuffRemoved;
        
        #endregion
        
        #region Unity生命周期
        
        protected virtual void Awake()
        {
            _attribute.Initialize();
        }
        
        protected virtual void Start()
        {
        }
        
        protected virtual void Update()
        {
            if (!_attribute.IsAlive) return;
            
            UpdateBuffs(Time.deltaTime);
            UpdateState();
        }
        
        protected virtual void OnDestroy()
        {
            // 清理事件订阅
            OnStateChanged = null;
            OnDamaged = null;
            OnDeath = null;
            OnHealed = null;
            OnBuffAdded = null;
            OnBuffRemoved = null;
        }
        
        #endregion
        
        #region 初始化
        
        /// <summary>
        /// 初始化Actor
        /// </summary>
        public virtual void Initialize(int actorId, int configId, string name, ActorFaction faction)
        {
            _actorId = actorId;
            _configId = configId;
            _actorName = name;
            _faction = faction;
            _currentState = ActorState.Idle;
            _attribute.Initialize();
        }
        
        #endregion
        
        #region 状态更新
        
        /// <summary>
        /// 更新状态（子类重写）
        /// </summary>
        protected virtual void UpdateState()
        {
            // 子类实现具体状态更新逻辑
        }
        
        /// <summary>
        /// 更新Buff效果
        /// </summary>
        protected virtual void UpdateBuffs(float deltaTime)
        {
            // 临时减速倍率重置
            _attribute.MoveSpeedMultiplier = 1f;
            
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                var buff = _buffs[i];
                buff.Update(deltaTime);
                
                // 应用Buff效果
                ApplyBuffEffect(buff, deltaTime);
                
                // 移除过期Buff
                if (buff.IsExpired)
                {
                    RemoveBuff(buff);
                }
            }
        }
        
        /// <summary>
        /// 应用Buff效果
        /// </summary>
        protected virtual void ApplyBuffEffect(BuffData buff, float deltaTime)
        {
            switch (buff.Type)
            {
                case BuffType.Freeze:
                    // 减速效果
                    float slowPercent = buff.GetEffectValue() * (1 - _attribute.FreezeResistance);
                    _attribute.MoveSpeedMultiplier *= (1 - slowPercent);
                    break;
                    
                case BuffType.Poison:
                    // 持续伤害
                    float poisonDamage = buff.GetEffectValue() * deltaTime * (1 - _attribute.PoisonResistance);
                    TakeDamage(poisonDamage, null, true); // 忽略防御的真实伤害
                    break;
                    
                case BuffType.Burn:
                    // 燃烧持续伤害
                    float burnDamage = buff.GetEffectValue() * deltaTime * (1 - _attribute.FireResistance);
                    TakeDamage(burnDamage, null, true);
                    break;
                    
                case BuffType.Stun:
                    // 眩晕状态
                    if (CurrentState != ActorState.Dead && CurrentState != ActorState.Stunned)
                    {
                        CurrentState = ActorState.Stunned;
                    }
                    break;
                    
                case BuffType.Heal:
                    // 持续治疗
                    Heal(buff.GetEffectValue() * deltaTime);
                    break;
                    
                case BuffType.Speed:
                    // 加速
                    _attribute.MoveSpeedMultiplier *= (1 + buff.GetEffectValue());
                    break;
                    
                case BuffType.AttackBuff:
                    // 攻击力增益（在计算时生效）
                    break;
                    
                case BuffType.DefenseBuff:
                    // 防御力增益（在计算时生效）
                    break;
            }
        }
        
        #endregion
        
        #region 移动
        
        /// <summary>
        /// 移动到目标位置
        /// </summary>
        public virtual void MoveTo(Vector3 targetPosition)
        {
            if (!CanMove()) return;
            
            CurrentState = ActorState.Moving;
        }
        
        /// <summary>
        /// 停止移动
        /// </summary>
        public virtual void StopMoving()
        {
            if (CurrentState == ActorState.Moving)
            {
                CurrentState = ActorState.Idle;
            }
        }
        
        /// <summary>
        /// 是否可以移动
        /// </summary>
        public virtual bool CanMove()
        {
            return _attribute.IsAlive && 
                   CurrentState != ActorState.Dead && 
                   CurrentState != ActorState.Stunned &&
                   CurrentState != ActorState.Attacking;
        }
        
        #endregion
        
        #region 攻击
        
        /// <summary>
        /// 攻击目标
        /// </summary>
        public virtual void Attack(ActorBase target)
        {
            if (!CanAttack(target)) return;
            
            _currentTarget = target;
            CurrentState = ActorState.Attacking;
            _lastAttackTime = Time.time;
            
            // 计算伤害
            float damage = CalculateDamage(target, out bool isCrit);
            target.TakeDamage(damage, this);
            
            // 触发攻击后效果
            OnAttackHit(target, damage, isCrit);
        }
        
        /// <summary>
        /// 计算对目标的伤害
        /// </summary>
        protected virtual float CalculateDamage(ActorBase target, out bool isCrit)
        {
            float baseDamage = _attribute.Attack;
            
            // 计算攻击力Buff
            var attackBuff = _buffs.Find(b => b.Type == BuffType.AttackBuff);
            if (attackBuff != null)
            {
                baseDamage *= (1 + attackBuff.GetEffectValue());
            }
            
            // 计算燃烧加成
            var burnBuff = target.GetBuff(BuffType.Burn);
            if (burnBuff != null)
            {
                baseDamage *= (1 + 0.2f * burnBuff.StackCount); // 每层燃烧增加20%伤害
            }
            
            // 暴击判定
            isCrit = UnityEngine.Random.value < _attribute.CritRate;
            if (isCrit)
            {
                baseDamage *= _attribute.CritDamage;
            }
            
            // 计算目标防御
            float defense = target.Attribute.Defense;
            var defenseBuff = target.GetBuff(BuffType.DefenseBuff);
            if (defenseBuff != null)
            {
                defense *= (1 + defenseBuff.GetEffectValue());
            }
            
            // 伤害减免公式
            float damageReduction = defense / (defense + 100);
            float finalDamage = baseDamage * (1 - damageReduction);
            
            return Mathf.Max(1, finalDamage); // 最小伤害为1
        }
        
        /// <summary>
        /// 攻击命中回调
        /// </summary>
        protected virtual void OnAttackHit(ActorBase target, float damage, bool isCrit)
        {
            // 子类实现特殊效果
        }
        
        /// <summary>
        /// 是否可以攻击
        /// </summary>
        public virtual bool CanAttack(ActorBase target)
        {
            if (target == null || !target.Attribute.IsAlive) return false;
            if (!_attribute.IsAlive) return false;
            if (CurrentState == ActorState.Dead || CurrentState == ActorState.Stunned) return false;
            
            // 攻击间隔检测
            float attackInterval = 1f / _attribute.AttackSpeed;
            if (Time.time - _lastAttackTime < attackInterval) return false;
            
            // 攻击范围检测
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance > _attribute.AttackRange) return false;
            
            return true;
        }
        
        #endregion
        
        #region 受伤和死亡
        
        /// <summary>
        /// 受到伤害
        /// </summary>
        public virtual void TakeDamage(float damage, ActorBase attacker, bool ignoreDefense = false)
        {
            if (!_attribute.IsAlive) return;
            
            // 闪避判定
            if (attacker != null && UnityEngine.Random.value < _attribute.DodgeRate)
            {
                OnDodge(attacker);
                return;
            }
            
            // 护盾吸收
            var shieldBuff = GetBuff(BuffType.Shield);
            if (shieldBuff != null)
            {
                float absorbed = Mathf.Min(damage, shieldBuff.Value);
                shieldBuff.Value -= absorbed;
                damage -= absorbed;
                
                if (shieldBuff.Value <= 0)
                {
                    RemoveBuff(shieldBuff);
                }
            }
            
            // 应用伤害
            _attribute.CurrentHealth -= damage;
            CurrentState = ActorState.TakingDamage;
            
            OnDamaged?.Invoke(damage, attacker);
            
            // 死亡检测
            if (!_attribute.IsAlive)
            {
                Die(attacker);
            }
        }
        
        /// <summary>
        /// 闪避回调
        /// </summary>
        protected virtual void OnDodge(ActorBase attacker)
        {
            // 显示闪避效果
            Debug.Log($"{_actorName} 闪避了 {attacker?.ActorName} 的攻击");
        }
        
        /// <summary>
        /// 死亡
        /// </summary>
        public virtual void Die(ActorBase killer = null)
        {
            CurrentState = ActorState.Dead;
            _buffs.Clear();
            
            OnDeath?.Invoke(this);
            
            Debug.Log($"{_actorName} 被 {killer?.ActorName ?? "未知"} 击杀");
        }
        
        /// <summary>
        /// 治疗
        /// </summary>
        public virtual void Heal(float amount)
        {
            if (!_attribute.IsAlive) return;
            
            float oldHealth = _attribute.CurrentHealth;
            _attribute.CurrentHealth += amount;
            float actualHeal = _attribute.CurrentHealth - oldHealth;
            
            if (actualHeal > 0)
            {
                OnHealed?.Invoke(actualHeal);
            }
        }
        
        /// <summary>
        /// 复活
        /// </summary>
        public virtual void Revive(float healthPercent = 1f)
        {
            _attribute.CurrentHealth = _attribute.MaxHealth * healthPercent;
            CurrentState = ActorState.Idle;
            _buffs.Clear();
        }
        
        #endregion
        
        #region Buff管理
        
        /// <summary>
        /// 添加Buff
        /// </summary>
        public virtual void AddBuff(BuffData buff)
        {
            // 检查是否已有同类型Buff
            var existingBuff = _buffs.Find(b => b.Type == buff.Type);
            
            if (existingBuff != null && existingBuff.CanStack)
            {
                // 叠加
                existingBuff.AddStack();
            }
            else if (existingBuff != null)
            {
                // 刷新持续时间
                existingBuff.Refresh();
                if (buff.Value > existingBuff.Value)
                {
                    existingBuff.Value = buff.Value;
                }
            }
            else
            {
                // 添加新Buff
                _buffs.Add(buff);
                OnBuffAdded?.Invoke(buff);
            }
        }
        
        /// <summary>
        /// 移除Buff
        /// </summary>
        public virtual void RemoveBuff(BuffData buff)
        {
            if (_buffs.Remove(buff))
            {
                // 如果移除的是眩晕Buff，恢复到空闲状态
                if (buff.Type == BuffType.Stun && CurrentState == ActorState.Stunned)
                {
                    CurrentState = ActorState.Idle;
                }
                
                OnBuffRemoved?.Invoke(buff);
            }
        }
        
        /// <summary>
        /// 移除指定类型的Buff
        /// </summary>
        public virtual void RemoveBuffByType(BuffType type)
        {
            var buff = _buffs.Find(b => b.Type == type);
            if (buff != null)
            {
                RemoveBuff(buff);
            }
        }
        
        /// <summary>
        /// 获取指定类型的Buff
        /// </summary>
        public BuffData GetBuff(BuffType type)
        {
            return _buffs.Find(b => b.Type == type);
        }
        
        /// <summary>
        /// 是否有指定类型的Buff
        /// </summary>
        public bool HasBuff(BuffType type)
        {
            return _buffs.Exists(b => b.Type == type);
        }
        
        /// <summary>
        /// 清除所有Buff
        /// </summary>
        public virtual void ClearAllBuffs()
        {
            foreach (var buff in _buffs.ToArray())
            {
                RemoveBuff(buff);
            }
        }
        
        /// <summary>
        /// 清除所有负面Buff
        /// </summary>
        public virtual void ClearDebuffs()
        {
            var debuffs = _buffs.FindAll(b => 
                b.Type == BuffType.Freeze || 
                b.Type == BuffType.Poison || 
                b.Type == BuffType.Burn || 
                b.Type == BuffType.Stun);
            
            foreach (var buff in debuffs)
            {
                RemoveBuff(buff);
            }
        }
        
        #endregion
        
        #region 选中和交互
        
        /// <summary>
        /// 选中
        /// </summary>
        public virtual void Select()
        {
            if (!_selectable) return;
            _isSelected = true;
            OnSelected();
        }
        
        /// <summary>
        /// 取消选中
        /// </summary>
        public virtual void Deselect()
        {
            _isSelected = false;
            OnDeselected();
        }
        
        /// <summary>
        /// 被选中回调
        /// </summary>
        protected virtual void OnSelected()
        {
            // 显示选中效果
        }
        
        /// <summary>
        /// 取消选中回调
        /// </summary>
        protected virtual void OnDeselected()
        {
            // 隐藏选中效果
        }
        
        /// <summary>
        /// 交互（子类实现）
        /// </summary>
        public virtual void Interact(ActorBase interactor)
        {
            // 子类实现具体交互逻辑
        }
        
        #endregion
        
        #region 阵营关系
        
        /// <summary>
        /// 是否是敌对阵营
        /// </summary>
        public virtual bool IsHostileTo(ActorBase other)
        {
            if (other == null) return false;
            
            // 中立对所有阵营都不敌对
            if (_faction == ActorFaction.Neutral || other.Faction == ActorFaction.Neutral)
                return false;
            
            // 不同阵营视为敌对
            return _faction != other.Faction;
        }
        
        /// <summary>
        /// 是否是友好阵营
        /// </summary>
        public virtual bool IsFriendlyTo(ActorBase other)
        {
            if (other == null) return false;
            return _faction == other.Faction;
        }
        
        /// <summary>
        /// 设置目标
        /// </summary>
        public virtual void SetTarget(ActorBase target)
        {
            _currentTarget = target;
        }
        
        #endregion
    }
}

