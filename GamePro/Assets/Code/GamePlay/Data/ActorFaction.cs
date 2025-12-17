namespace GamePlay.Data
{
    /// <summary>
    /// Actor阵营类型
    /// </summary>
    public enum ActorFaction
    {
        /// <summary>
        /// 中立
        /// </summary>
        Neutral = 0,
        
        /// <summary>
        /// 玩家阵营
        /// </summary>
        Player = 1,
        
        /// <summary>
        /// 敌方阵营
        /// </summary>
        Enemy = 2,
        
        /// <summary>
        /// 友方阵营（NPC等）
        /// </summary>
        Friendly = 3
    }
}

