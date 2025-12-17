namespace GamePlay.Data
{
    /// <summary>
    /// Buff类型枚举
    /// </summary>
    public enum BuffType
    {
        /// <summary>
        /// 无效果
        /// </summary>
        None = 0,
        
        /// <summary>
        /// 冰冻 - 减速效果，可叠加
        /// </summary>
        Freeze = 1,
        
        /// <summary>
        /// 中毒 - 持续伤害，可叠加
        /// </summary>
        Poison = 2,
        
        /// <summary>
        /// 燃烧 - 每次伤害加成
        /// </summary>
        Burn = 3,
        
        /// <summary>
        /// 眩晕 - 无法行动
        /// </summary>
        Stun = 4,
        
        /// <summary>
        /// 加速
        /// </summary>
        Speed = 5,
        
        /// <summary>
        /// 护盾
        /// </summary>
        Shield = 6,
        
        /// <summary>
        /// 治疗 - 持续回血
        /// </summary>
        Heal = 7,
        
        /// <summary>
        /// 攻击力增益
        /// </summary>
        AttackBuff = 8,
        
        /// <summary>
        /// 防御力增益
        /// </summary>
        DefenseBuff = 9
    }
}

