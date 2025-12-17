using UnityEngine;
using GamePlay.Actor;
using GamePlay.Data;

namespace GamePlay.Item
{
    /// <summary>
    /// 消耗品效果类型
    /// </summary>
    public enum ConsumableEffectType
    {
        /// <summary>
        /// 恢复生命
        /// </summary>
        HealHealth = 0,
        
        /// <summary>
        /// 恢复生命百分比
        /// </summary>
        HealHealthPercent = 1,
        
        /// <summary>
        /// 添加Buff
        /// </summary>
        AddBuff = 2,
        
        /// <summary>
        /// 移除Debuff
        /// </summary>
        RemoveDebuff = 3,
        
        /// <summary>
        /// 增加弹药
        /// </summary>
        AddAmmo = 4,
        
        /// <summary>
        /// 增加经验
        /// </summary>
        AddExperience = 5
    }
    
    /// <summary>
    /// 消耗品类
    /// </summary>
    public class Consumable : ItemBase
    {
        #region 属性
        
        /// <summary>
        /// 效果类型
        /// </summary>
        [SerializeField] protected ConsumableEffectType _effectType;
        public ConsumableEffectType EffectType => _effectType;
        
        /// <summary>
        /// 效果数值
        /// </summary>
        [SerializeField] protected float _effectValue;
        public float EffectValue => _effectValue;
        
        /// <summary>
        /// 效果持续时间（用于Buff）
        /// </summary>
        [SerializeField] protected float _effectDuration;
        public float EffectDuration => _effectDuration;
        
        /// <summary>
        /// Buff类型（如果效果是添加Buff）
        /// </summary>
        [SerializeField] protected BuffType _buffType;
        public BuffType BuffType => _buffType;
        
        /// <summary>
        /// 使用冷却时间
        /// </summary>
        [SerializeField] protected float _cooldown = 1f;
        public float Cooldown => _cooldown;
        
        /// <summary>
        /// 上次使用时间
        /// </summary>
        protected float _lastUseTime;
        
        #endregion
        
        protected override void Awake()
        {
            base.Awake();
            _itemType = ItemType.Consumable;
            _usable = true;
            _stackable = true;
        }
        
        /// <summary>
        /// 初始化消耗品
        /// </summary>
        public void InitializeConsumable(int itemId, int configId, string name, ItemQuality quality,
            ConsumableEffectType effectType, float effectValue, float duration = 0, BuffType buffType = BuffType.None)
        {
            Initialize(itemId, configId, name, ItemType.Consumable, quality);
            _effectType = effectType;
            _effectValue = effectValue;
            _effectDuration = duration;
            _buffType = buffType;
        }
        
        public override bool IsUsable
        {
            get
            {
                if (Time.time - _lastUseTime < _cooldown)
                    return false;
                return base.IsUsable;
            }
        }
        
        protected override void OnUse(Character user)
        {
            base.OnUse(user);
            _lastUseTime = Time.time;
            
            switch (_effectType)
            {
                case ConsumableEffectType.HealHealth:
                    user.Heal(_effectValue);
                    Debug.Log($"{user.ActorName} 恢复了 {_effectValue} 点生命");
                    break;
                    
                case ConsumableEffectType.HealHealthPercent:
                    float healAmount = user.Attribute.MaxHealth * _effectValue;
                    user.Heal(healAmount);
                    Debug.Log($"{user.ActorName} 恢复了 {healAmount} 点生命 ({_effectValue * 100}%)");
                    break;
                    
                case ConsumableEffectType.AddBuff:
                    var buff = new BuffData(_buffType, _effectValue, _effectDuration);
                    user.AddBuff(buff);
                    Debug.Log($"{user.ActorName} 获得了 {_buffType} 效果");
                    break;
                    
                case ConsumableEffectType.RemoveDebuff:
                    user.ClearDebuffs();
                    Debug.Log($"{user.ActorName} 清除了所有负面效果");
                    break;
                    
                case ConsumableEffectType.AddAmmo:
                    user.AddAmmo((int)_effectValue);
                    Debug.Log($"{user.ActorName} 获得了 {_effectValue} 发弹药");
                    break;
                    
                case ConsumableEffectType.AddExperience:
                    user.AddExperience((int)_effectValue);
                    Debug.Log($"{user.ActorName} 获得了 {_effectValue} 点经验");
                    break;
            }
        }
    }
}

