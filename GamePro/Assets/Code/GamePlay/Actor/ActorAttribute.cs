using System;
using UnityEngine;

namespace GamePlay.Actor
{
    /// <summary>
    /// Actor属性系统
    /// </summary>
    [Serializable]
    public class ActorAttribute
    {
        #region 基础属性
        
        /// <summary>
        /// 最大生命值
        /// </summary>
        [SerializeField] private float _maxHealth = 100f;
        public float MaxHealth 
        { 
            get => _maxHealth + MaxHealthBonus; 
            set => _maxHealth = value; 
        }
        public float MaxHealthBonus { get; set; }
        
        /// <summary>
        /// 当前生命值
        /// </summary>
        [SerializeField] private float _currentHealth;
        public float CurrentHealth 
        { 
            get => _currentHealth; 
            set => _currentHealth = Mathf.Clamp(value, 0, MaxHealth); 
        }
        
        /// <summary>
        /// 移动速度
        /// </summary>
        [SerializeField] private float _moveSpeed = 5f;
        public float MoveSpeed 
        { 
            get => (_moveSpeed + MoveSpeedBonus) * MoveSpeedMultiplier; 
            set => _moveSpeed = value; 
        }
        public float MoveSpeedBonus { get; set; }
        public float MoveSpeedMultiplier { get; set; } = 1f;
        
        #endregion
        
        #region 战斗属性
        
        /// <summary>
        /// 攻击力
        /// </summary>
        [SerializeField] private float _attack = 10f;
        public float Attack 
        { 
            get => (_attack + AttackBonus) * AttackMultiplier; 
            set => _attack = value; 
        }
        public float AttackBonus { get; set; }
        public float AttackMultiplier { get; set; } = 1f;
        
        /// <summary>
        /// 防御力
        /// </summary>
        [SerializeField] private float _defense = 5f;
        public float Defense 
        { 
            get => (_defense + DefenseBonus) * DefenseMultiplier; 
            set => _defense = value; 
        }
        public float DefenseBonus { get; set; }
        public float DefenseMultiplier { get; set; } = 1f;
        
        /// <summary>
        /// 暴击率 (0-1)
        /// </summary>
        [SerializeField] private float _critRate = 0.05f;
        public float CritRate 
        { 
            get => Mathf.Clamp01(_critRate + CritRateBonus); 
            set => _critRate = Mathf.Clamp01(value); 
        }
        public float CritRateBonus { get; set; }
        
        /// <summary>
        /// 暴击伤害倍率
        /// </summary>
        [SerializeField] private float _critDamage = 1.5f;
        public float CritDamage 
        { 
            get => _critDamage + CritDamageBonus; 
            set => _critDamage = value; 
        }
        public float CritDamageBonus { get; set; }
        
        /// <summary>
        /// 闪避率 (0-1)
        /// </summary>
        [SerializeField] private float _dodgeRate = 0.05f;
        public float DodgeRate 
        { 
            get => Mathf.Clamp01(_dodgeRate + DodgeRateBonus); 
            set => _dodgeRate = Mathf.Clamp01(value); 
        }
        public float DodgeRateBonus { get; set; }
        
        /// <summary>
        /// 攻击速度
        /// </summary>
        [SerializeField] private float _attackSpeed = 1f;
        public float AttackSpeed 
        { 
            get => (_attackSpeed + AttackSpeedBonus) * AttackSpeedMultiplier; 
            set => _attackSpeed = value; 
        }
        public float AttackSpeedBonus { get; set; }
        public float AttackSpeedMultiplier { get; set; } = 1f;
        
        /// <summary>
        /// 攻击范围
        /// </summary>
        [SerializeField] private float _attackRange = 10f;
        public float AttackRange 
        { 
            get => _attackRange + AttackRangeBonus; 
            set => _attackRange = value; 
        }
        public float AttackRangeBonus { get; set; }
        
        #endregion
        
        #region 元素抗性
        
        /// <summary>
        /// 冰冻抗性 (0-1，1为完全免疫)
        /// </summary>
        [SerializeField] private float _freezeResistance = 0f;
        public float FreezeResistance 
        { 
            get => Mathf.Clamp01(_freezeResistance); 
            set => _freezeResistance = Mathf.Clamp01(value); 
        }
        
        /// <summary>
        /// 毒素抗性 (0-1)
        /// </summary>
        [SerializeField] private float _poisonResistance = 0f;
        public float PoisonResistance 
        { 
            get => Mathf.Clamp01(_poisonResistance); 
            set => _poisonResistance = Mathf.Clamp01(value); 
        }
        
        /// <summary>
        /// 火焰抗性 (0-1)
        /// </summary>
        [SerializeField] private float _fireResistance = 0f;
        public float FireResistance 
        { 
            get => Mathf.Clamp01(_fireResistance); 
            set => _fireResistance = Mathf.Clamp01(value); 
        }
        
        #endregion
        
        /// <summary>
        /// 是否存活
        /// </summary>
        public bool IsAlive => CurrentHealth > 0;
        
        /// <summary>
        /// 生命值百分比
        /// </summary>
        public float HealthPercent => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0;
        
        /// <summary>
        /// 初始化属性
        /// </summary>
        public void Initialize()
        {
            CurrentHealth = MaxHealth;
            ResetBonuses();
        }
        
        /// <summary>
        /// 重置所有加成
        /// </summary>
        public void ResetBonuses()
        {
            MaxHealthBonus = 0;
            MoveSpeedBonus = 0;
            MoveSpeedMultiplier = 1f;
            AttackBonus = 0;
            AttackMultiplier = 1f;
            DefenseBonus = 0;
            DefenseMultiplier = 1f;
            CritRateBonus = 0;
            CritDamageBonus = 0;
            DodgeRateBonus = 0;
            AttackSpeedBonus = 0;
            AttackSpeedMultiplier = 1f;
            AttackRangeBonus = 0;
        }
        
        /// <summary>
        /// 从配置数据初始化属性
        /// </summary>
        public void InitFromConfig(float maxHealth, float attack, float defense, float moveSpeed, 
            float critRate = 0.05f, float critDamage = 1.5f, float dodgeRate = 0.05f, 
            float attackSpeed = 1f, float attackRange = 10f)
        {
            _maxHealth = maxHealth;
            _attack = attack;
            _defense = defense;
            _moveSpeed = moveSpeed;
            _critRate = critRate;
            _critDamage = critDamage;
            _dodgeRate = dodgeRate;
            _attackSpeed = attackSpeed;
            _attackRange = attackRange;
            Initialize();
        }
    }
}

