using System;
using UnityEngine;
using GamePlay.Actor;

namespace GamePlay.Item
{
    /// <summary>
    /// 装备槽位枚举
    /// </summary>
    public enum EquipmentSlot
    {
        /// <summary>
        /// 头盔
        /// </summary>
        Head = 0,
        
        /// <summary>
        /// 护甲/胸甲
        /// </summary>
        Chest = 1,
        
        /// <summary>
        /// 手套
        /// </summary>
        Gloves = 2,
        
        /// <summary>
        /// 腿甲
        /// </summary>
        Legs = 3,
        
        /// <summary>
        /// 鞋子
        /// </summary>
        Boots = 4,
        
        /// <summary>
        /// 主武器
        /// </summary>
        PrimaryWeapon = 5,
        
        /// <summary>
        /// 副武器
        /// </summary>
        SecondaryWeapon = 6,
        
        /// <summary>
        /// 背包
        /// </summary>
        Backpack = 7,
        
        /// <summary>
        /// 饰品1
        /// </summary>
        Accessory1 = 8,
        
        /// <summary>
        /// 饰品2
        /// </summary>
        Accessory2 = 9
    }
    
    /// <summary>
    /// 装备类
    /// </summary>
    public class Equipment : ItemBase
    {
        #region 属性
        
        /// <summary>
        /// 装备槽位
        /// </summary>
        [SerializeField] protected EquipmentSlot _slot;
        public EquipmentSlot Slot => _slot;
        
        /// <summary>
        /// 装备等级要求
        /// </summary>
        [SerializeField] protected int _requiredLevel = 1;
        public int RequiredLevel => _requiredLevel;
        
        /// <summary>
        /// 是否已装备
        /// </summary>
        protected bool _isEquipped;
        public bool IsEquipped => _isEquipped;
        
        #region 属性加成
        
        /// <summary>
        /// 生命值加成
        /// </summary>
        [SerializeField] protected float _healthBonus;
        public float HealthBonus => _healthBonus;
        
        /// <summary>
        /// 攻击力加成
        /// </summary>
        [SerializeField] protected float _attackBonus;
        public float AttackBonus => _attackBonus;
        
        /// <summary>
        /// 防御力加成
        /// </summary>
        [SerializeField] protected float _defenseBonus;
        public float DefenseBonus => _defenseBonus;
        
        /// <summary>
        /// 暴击率加成
        /// </summary>
        [SerializeField] protected float _critRateBonus;
        public float CritRateBonus => _critRateBonus;
        
        /// <summary>
        /// 暴击伤害加成
        /// </summary>
        [SerializeField] protected float _critDamageBonus;
        public float CritDamageBonus => _critDamageBonus;
        
        /// <summary>
        /// 闪避率加成
        /// </summary>
        [SerializeField] protected float _dodgeRateBonus;
        public float DodgeRateBonus => _dodgeRateBonus;
        
        /// <summary>
        /// 移动速度加成
        /// </summary>
        [SerializeField] protected float _moveSpeedBonus;
        public float MoveSpeedBonus => _moveSpeedBonus;
        
        /// <summary>
        /// 攻击速度加成
        /// </summary>
        [SerializeField] protected float _attackSpeedBonus;
        public float AttackSpeedBonus => _attackSpeedBonus;
        
        #region 元素抗性
        
        /// <summary>
        /// 冰冻抗性加成
        /// </summary>
        [SerializeField] protected float _freezeResistanceBonus;
        public float FreezeResistanceBonus => _freezeResistanceBonus;
        
        /// <summary>
        /// 毒素抗性加成
        /// </summary>
        [SerializeField] protected float _poisonResistanceBonus;
        public float PoisonResistanceBonus => _poisonResistanceBonus;
        
        /// <summary>
        /// 火焰抗性加成
        /// </summary>
        [SerializeField] protected float _fireResistanceBonus;
        public float FireResistanceBonus => _fireResistanceBonus;
        
        #endregion
        
        #endregion
        
        #endregion
        
        #region 事件
        
        /// <summary>
        /// 装备事件
        /// </summary>
        public event Action<Character> OnEquipped;
        
        /// <summary>
        /// 卸下事件
        /// </summary>
        public event Action<Character> OnUnequipped;
        
        #endregion
        
        protected override void Awake()
        {
            base.Awake();
            _itemType = ItemType.Equipment;
            _stackable = false;
        }
        
        /// <summary>
        /// 初始化装备
        /// </summary>
        public virtual void InitializeEquipment(int itemId, int configId, string name, ItemQuality quality,
            EquipmentSlot slot, int requiredLevel = 1)
        {
            Initialize(itemId, configId, name, ItemType.Equipment, quality);
            _slot = slot;
            _requiredLevel = requiredLevel;
        }
        
        /// <summary>
        /// 设置属性加成
        /// </summary>
        public void SetStats(float health = 0, float attack = 0, float defense = 0, 
            float critRate = 0, float critDamage = 0, float dodge = 0, 
            float moveSpeed = 0, float attackSpeed = 0)
        {
            _healthBonus = health;
            _attackBonus = attack;
            _defenseBonus = defense;
            _critRateBonus = critRate;
            _critDamageBonus = critDamage;
            _dodgeRateBonus = dodge;
            _moveSpeedBonus = moveSpeed;
            _attackSpeedBonus = attackSpeed;
        }
        
        /// <summary>
        /// 设置元素抗性
        /// </summary>
        public void SetResistances(float freeze = 0, float poison = 0, float fire = 0)
        {
            _freezeResistanceBonus = freeze;
            _poisonResistanceBonus = poison;
            _fireResistanceBonus = fire;
        }
        
        /// <summary>
        /// 是否可以装备
        /// </summary>
        public virtual bool CanEquip(Character character)
        {
            if (character == null) return false;
            if (character.Level < _requiredLevel)
            {
                Debug.LogWarning($"等级不足，需要 {_requiredLevel} 级");
                return false;
            }
            return true;
        }
        
        /// <summary>
        /// 装备
        /// </summary>
        public virtual void OnEquip(Character character)
        {
            _isEquipped = true;
            _isOnGround = false;
            OnEquipped?.Invoke(character);
            Debug.Log($"{character.ActorName} 装备了 {_itemName}");
        }
        
        /// <summary>
        /// 卸下
        /// </summary>
        public virtual void OnUnequip(Character character)
        {
            _isEquipped = false;
            OnUnequipped?.Invoke(character);
            Debug.Log($"{character.ActorName} 卸下了 {_itemName}");
        }
        
        /// <summary>
        /// 获取属性描述
        /// </summary>
        public virtual string GetStatsDescription()
        {
            var sb = new System.Text.StringBuilder();
            
            if (_healthBonus != 0) sb.AppendLine($"生命值 +{_healthBonus}");
            if (_attackBonus != 0) sb.AppendLine($"攻击力 +{_attackBonus}");
            if (_defenseBonus != 0) sb.AppendLine($"防御力 +{_defenseBonus}");
            if (_critRateBonus != 0) sb.AppendLine($"暴击率 +{_critRateBonus * 100}%");
            if (_critDamageBonus != 0) sb.AppendLine($"暴击伤害 +{_critDamageBonus * 100}%");
            if (_dodgeRateBonus != 0) sb.AppendLine($"闪避率 +{_dodgeRateBonus * 100}%");
            if (_moveSpeedBonus != 0) sb.AppendLine($"移动速度 +{_moveSpeedBonus}");
            if (_attackSpeedBonus != 0) sb.AppendLine($"攻击速度 +{_attackSpeedBonus}");
            
            if (_freezeResistanceBonus != 0) sb.AppendLine($"冰冻抗性 +{_freezeResistanceBonus * 100}%");
            if (_poisonResistanceBonus != 0) sb.AppendLine($"毒素抗性 +{_poisonResistanceBonus * 100}%");
            if (_fireResistanceBonus != 0) sb.AppendLine($"火焰抗性 +{_fireResistanceBonus * 100}%");
            
            return sb.ToString();
        }
    }
}

