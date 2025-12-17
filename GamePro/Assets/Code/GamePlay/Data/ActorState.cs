namespace GamePlay.Data
{
    /// <summary>
    /// Actor状态枚举
    /// </summary>
    public enum ActorState
    {
        /// <summary>
        /// 空闲状态
        /// </summary>
        Idle = 0,
        
        /// <summary>
        /// 移动中
        /// </summary>
        Moving = 1,
        
        /// <summary>
        /// 攻击中
        /// </summary>
        Attacking = 2,
        
        /// <summary>
        /// 受伤中
        /// </summary>
        TakingDamage = 3,
        
        /// <summary>
        /// 死亡
        /// </summary>
        Dead = 4,
        
        /// <summary>
        /// 眩晕
        /// </summary>
        Stunned = 5,
        
        /// <summary>
        /// 交互中（与NPC对话等）
        /// </summary>
        Interacting = 6,
        
        /// <summary>
        /// 拾取中
        /// </summary>
        Picking = 7,
        
        /// <summary>
        /// 换弹中
        /// </summary>
        Reloading = 8
    }
}

