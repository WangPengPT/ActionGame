using System;
using System.Collections.Generic;
using UnityEngine;
using GamePlay.Actor;
using GamePlay.Data;

namespace GamePlay.Manager
{
    /// <summary>
    /// 伤害类型
    /// </summary>
    public enum DamageType
    {
        /// <summary>
        /// 物理伤害
        /// </summary>
        Physical = 0,
        
        /// <summary>
        /// 冰冻伤害
        /// </summary>
        Ice = 1,
        
        /// <summary>
        /// 毒素伤害
        /// </summary>
        Poison = 2,
        
        /// <summary>
        /// 火焰伤害
        /// </summary>
        Fire = 3,
        
        /// <summary>
        /// 雷电伤害
        /// </summary>
        Lightning = 4,
        
        /// <summary>
        /// 真实伤害（无视防御）
        /// </summary>
        True = 5
    }
    
    /// <summary>
    /// 伤害信息
    /// </summary>
    public struct DamageInfo
    {
        public float BaseDamage;
        public float FinalDamage;
        public DamageType Type;
        public bool IsCritical;
        public bool IsDodged;
        public bool IsBlocked;
        public ActorBase Source;
        public ActorBase Target;
        public Vector3 HitPoint;
        public Vector3 HitDirection;
    }
    
    /// <summary>
    /// 战斗管理器 - 处理伤害计算、战斗效果等
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        #region 单例
        
        private static CombatManager _instance;
        public static CombatManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("CombatManager");
                    _instance = go.AddComponent<CombatManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region 属性
        
        /// <summary>
        /// 伤害浮动范围 (±)
        /// </summary>
        [SerializeField] private float _damageVariance = 0.1f;
        
        /// <summary>
        /// 基础护甲穿透
        /// </summary>
        [SerializeField] private float _baseArmorPenetration = 0f;
        
        /// <summary>
        /// 伤害数字显示持续时间
        /// </summary>
        [SerializeField] private float _damageNumberDuration = 1f;
        
        /// <summary>
        /// 连击计数
        /// </summary>
        private Dictionary<int, int> _comboCount = new Dictionary<int, int>();
        
        /// <summary>
        /// 连击计时器
        /// </summary>
        private Dictionary<int, float> _comboTimer = new Dictionary<int, float>();
        
        /// <summary>
        /// 连击超时时间
        /// </summary>
        [SerializeField] private float _comboTimeout = 3f;
        
        #endregion
        
        #region 事件
        
        /// <summary>
        /// 伤害事件
        /// </summary>
        public event Action<DamageInfo> OnDamageDealt;
        
        /// <summary>
        /// 暴击事件
        /// </summary>
        public event Action<DamageInfo> OnCriticalHit;
        
        /// <summary>
        /// 击杀事件
        /// </summary>
        public event Action<ActorBase, ActorBase> OnKill;
        
        /// <summary>
        /// 连击事件
        /// </summary>
        public event Action<int, int> OnCombo; // actorId, comboCount
        
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
            UpdateComboTimers();
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion
        
        #region 伤害计算
        
        /// <summary>
        /// 计算并应用伤害
        /// </summary>
        public DamageInfo DealDamage(ActorBase source, ActorBase target, float baseDamage, 
            DamageType damageType = DamageType.Physical, Vector3? hitPoint = null)
        {
            var info = new DamageInfo
            {
                BaseDamage = baseDamage,
                Type = damageType,
                Source = source,
                Target = target,
                HitPoint = hitPoint ?? target.transform.position,
                HitDirection = (target.transform.position - source.transform.position).normalized
            };
            
            // 闪避判定
            if (damageType != DamageType.True && target.Attribute.DodgeRate > 0)
            {
                if (UnityEngine.Random.value < target.Attribute.DodgeRate)
                {
                    info.IsDodged = true;
                    info.FinalDamage = 0;
                    
                    OnDamageDealt?.Invoke(info);
                    return info;
                }
            }
            
            // 暴击判定
            if (source != null && UnityEngine.Random.value < source.Attribute.CritRate)
            {
                info.IsCritical = true;
                baseDamage *= source.Attribute.CritDamage;
            }
            
            // 伤害浮动
            float variance = UnityEngine.Random.Range(-_damageVariance, _damageVariance);
            baseDamage *= (1 + variance);
            
            // 元素伤害加成
            baseDamage = ApplyElementalBonus(baseDamage, damageType, source, target);
            
            // 防御减免
            float finalDamage = baseDamage;
            if (damageType != DamageType.True)
            {
                float defense = target.Attribute.Defense;
                
                // 防御Buff
                var defenseBuff = target.GetBuff(BuffType.DefenseBuff);
                if (defenseBuff != null)
                {
                    defense *= (1 + defenseBuff.GetEffectValue());
                }
                
                // 护盾
                var shieldBuff = target.GetBuff(BuffType.Shield);
                if (shieldBuff != null)
                {
                    float absorbed = Mathf.Min(finalDamage, shieldBuff.Value);
                    shieldBuff.Value -= absorbed;
                    finalDamage -= absorbed;
                    
                    if (shieldBuff.Value <= 0)
                    {
                        target.RemoveBuff(shieldBuff);
                    }
                }
                
                // 防御公式
                float damageReduction = defense / (defense + 100);
                finalDamage *= (1 - damageReduction);
            }
            
            // 确保最小伤害
            info.FinalDamage = Mathf.Max(1, finalDamage);
            
            // 应用伤害
            bool wasAlive = target.Attribute.IsAlive;
            target.TakeDamage(info.FinalDamage, source, damageType == DamageType.True);
            
            // 应用元素效果
            ApplyElementalEffect(damageType, source, target);
            
            // 更新连击
            if (source != null)
            {
                UpdateCombo(source.ActorId);
            }
            
            // 触发事件
            OnDamageDealt?.Invoke(info);
            
            if (info.IsCritical)
            {
                OnCriticalHit?.Invoke(info);
            }
            
            // 击杀判定
            if (wasAlive && !target.Attribute.IsAlive)
            {
                OnKill?.Invoke(source, target);
            }
            
            return info;
        }
        
        /// <summary>
        /// 应用元素伤害加成
        /// </summary>
        private float ApplyElementalBonus(float damage, DamageType type, ActorBase source, ActorBase target)
        {
            float resistance = 0f;
            float bonus = 0f;
            
            switch (type)
            {
                case DamageType.Ice:
                    resistance = target.Attribute.FreezeResistance;
                    // 冰冻状态下的目标受到额外伤害
                    if (target.HasBuff(BuffType.Freeze))
                    {
                        bonus = 0.2f;
                    }
                    break;
                    
                case DamageType.Poison:
                    resistance = target.Attribute.PoisonResistance;
                    break;
                    
                case DamageType.Fire:
                    resistance = target.Attribute.FireResistance;
                    // 燃烧状态叠加伤害
                    var burnBuff = target.GetBuff(BuffType.Burn);
                    if (burnBuff != null)
                    {
                        bonus = 0.15f * burnBuff.StackCount;
                    }
                    break;
                    
                case DamageType.Lightning:
                    // 雷电伤害对湿润目标（冰冻）有加成
                    if (target.HasBuff(BuffType.Freeze))
                    {
                        bonus = 0.5f;
                    }
                    break;
            }
            
            return damage * (1 - resistance) * (1 + bonus);
        }
        
        /// <summary>
        /// 应用元素效果
        /// </summary>
        private void ApplyElementalEffect(DamageType type, ActorBase source, ActorBase target)
        {
            int sourceId = source?.ActorId ?? 0;
            
            switch (type)
            {
                case DamageType.Ice:
                    // 30%概率施加冰冻减速
                    if (UnityEngine.Random.value < 0.3f)
                    {
                        target.AddBuff(new BuffData(BuffType.Freeze, 0.3f, 3f, 5, sourceId));
                    }
                    break;
                    
                case DamageType.Poison:
                    // 50%概率施加中毒
                    if (UnityEngine.Random.value < 0.5f)
                    {
                        float poisonDamage = source?.Attribute.Attack * 0.1f ?? 5f;
                        target.AddBuff(new BuffData(BuffType.Poison, poisonDamage, 5f, 5, sourceId));
                    }
                    break;
                    
                case DamageType.Fire:
                    // 40%概率施加燃烧
                    if (UnityEngine.Random.value < 0.4f)
                    {
                        float burnDamage = source?.Attribute.Attack * 0.15f ?? 8f;
                        target.AddBuff(new BuffData(BuffType.Burn, burnDamage, 4f, 3, sourceId));
                    }
                    break;
                    
                case DamageType.Lightning:
                    // 15%概率眩晕
                    if (UnityEngine.Random.value < 0.15f)
                    {
                        target.AddBuff(new BuffData(BuffType.Stun, 0, 1f, 1, sourceId));
                    }
                    break;
            }
        }
        
        #endregion
        
        #region 范围伤害
        
        /// <summary>
        /// 范围伤害
        /// </summary>
        public List<DamageInfo> DealAreaDamage(ActorBase source, Vector3 center, float radius, 
            float damage, DamageType damageType = DamageType.Physical, bool friendlyFire = false)
        {
            var results = new List<DamageInfo>();
            var actors = ActorManager.Instance.GetActorsInRange(center, radius);
            
            foreach (var actor in actors)
            {
                if (actor == source) continue;
                if (!friendlyFire && source != null && source.IsFriendlyTo(actor)) continue;
                
                // 距离衰减
                float distance = Vector3.Distance(center, actor.transform.position);
                float falloff = 1 - (distance / radius) * 0.5f; // 边缘50%伤害衰减
                float actualDamage = damage * falloff;
                
                var info = DealDamage(source, actor, actualDamage, damageType, center);
                results.Add(info);
            }
            
            return results;
        }
        
        /// <summary>
        /// 扇形范围伤害
        /// </summary>
        public List<DamageInfo> DealConeDamage(ActorBase source, Vector3 origin, Vector3 direction, 
            float angle, float range, float damage, DamageType damageType = DamageType.Physical)
        {
            var results = new List<DamageInfo>();
            var actors = ActorManager.Instance.GetActorsInRange(origin, range);
            
            foreach (var actor in actors)
            {
                if (actor == source) continue;
                if (source != null && source.IsFriendlyTo(actor)) continue;
                
                Vector3 toTarget = (actor.transform.position - origin).normalized;
                float targetAngle = Vector3.Angle(direction, toTarget);
                
                if (targetAngle <= angle / 2)
                {
                    var info = DealDamage(source, actor, damage, damageType, origin);
                    results.Add(info);
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// 直线穿透伤害
        /// </summary>
        public List<DamageInfo> DealLineDamage(ActorBase source, Vector3 origin, Vector3 direction, 
            float range, float width, float damage, int maxTargets = 0, DamageType damageType = DamageType.Physical)
        {
            var results = new List<DamageInfo>();
            
            // 射线检测所有命中的Actor
            var hits = Physics.SphereCastAll(origin, width, direction, range);
            var hitActors = new List<ActorBase>();
            
            foreach (var hit in hits)
            {
                var actor = hit.collider.GetComponent<ActorBase>();
                if (actor != null && actor != source && !hitActors.Contains(actor))
                {
                    if (source == null || source.IsHostileTo(actor))
                    {
                        hitActors.Add(actor);
                    }
                }
            }
            
            // 按距离排序
            hitActors.Sort((a, b) => 
                Vector3.Distance(origin, a.transform.position).CompareTo(
                Vector3.Distance(origin, b.transform.position)));
            
            // 应用伤害
            int count = 0;
            float currentDamage = damage;
            
            foreach (var actor in hitActors)
            {
                if (maxTargets > 0 && count >= maxTargets) break;
                
                var info = DealDamage(source, actor, currentDamage, damageType, actor.transform.position);
                results.Add(info);
                
                count++;
                currentDamage *= 0.8f; // 穿透衰减
            }
            
            return results;
        }
        
        #endregion
        
        #region 连击系统
        
        /// <summary>
        /// 更新连击
        /// </summary>
        private void UpdateCombo(int actorId)
        {
            if (!_comboCount.ContainsKey(actorId))
            {
                _comboCount[actorId] = 0;
            }
            
            _comboCount[actorId]++;
            _comboTimer[actorId] = _comboTimeout;
            
            OnCombo?.Invoke(actorId, _comboCount[actorId]);
        }
        
        /// <summary>
        /// 更新连击计时器
        /// </summary>
        private void UpdateComboTimers()
        {
            var expiredActors = new List<int>();
            
            foreach (var pair in _comboTimer)
            {
                _comboTimer[pair.Key] = pair.Value - Time.deltaTime;
                
                if (_comboTimer[pair.Key] <= 0)
                {
                    expiredActors.Add(pair.Key);
                }
            }
            
            foreach (var actorId in expiredActors)
            {
                _comboCount.Remove(actorId);
                _comboTimer.Remove(actorId);
            }
        }
        
        /// <summary>
        /// 获取连击数
        /// </summary>
        public int GetComboCount(int actorId)
        {
            return _comboCount.TryGetValue(actorId, out var count) ? count : 0;
        }
        
        /// <summary>
        /// 重置连击
        /// </summary>
        public void ResetCombo(int actorId)
        {
            _comboCount.Remove(actorId);
            _comboTimer.Remove(actorId);
        }
        
        #endregion
        
        #region 特殊效果
        
        /// <summary>
        /// 击退效果
        /// </summary>
        public void ApplyKnockback(ActorBase target, Vector3 direction, float force)
        {
            if (target == null) return;
            
            // 检查是否可被击退
            if (target is Monster monster && !monster.CanBeKnockedBack)
            {
                return;
            }
            
            // 应用击退力
            var rb = target.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(direction.normalized * force, ForceMode.Impulse);
            }
            else
            {
                // 没有刚体时直接移动
                target.transform.position += direction.normalized * force * 0.1f;
            }
        }
        
        /// <summary>
        /// 治疗
        /// </summary>
        public void Heal(ActorBase target, float amount, ActorBase source = null)
        {
            if (target == null || !target.Attribute.IsAlive) return;
            
            target.Heal(amount);
            
            Debug.Log($"{target.ActorName} 被治疗了 {amount} 点生命");
        }
        
        /// <summary>
        /// 范围治疗
        /// </summary>
        public void HealArea(Vector3 center, float radius, float amount, ActorFaction faction)
        {
            var actors = ActorManager.Instance.GetActorsInRange(center, radius, faction);
            
            foreach (var actor in actors)
            {
                Heal(actor, amount);
            }
        }
        
        #endregion
    }
}

