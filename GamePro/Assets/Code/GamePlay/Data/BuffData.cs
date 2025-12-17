using System;
using UnityEngine;

namespace GamePlay.Data
{
    /// <summary>
    /// Buff数据结构
    /// </summary>
    [Serializable]
    public class BuffData
    {
        /// <summary>
        /// Buff类型
        /// </summary>
        public BuffType Type;
        
        /// <summary>
        /// Buff数值（减速百分比、伤害值等）
        /// </summary>
        public float Value;
        
        /// <summary>
        /// 持续时间（秒）
        /// </summary>
        public float Duration;
        
        /// <summary>
        /// 剩余时间
        /// </summary>
        public float RemainingTime;
        
        /// <summary>
        /// 叠加层数
        /// </summary>
        public int StackCount;
        
        /// <summary>
        /// 最大叠加层数
        /// </summary>
        public int MaxStack;
        
        /// <summary>
        /// 来源Actor的ID
        /// </summary>
        public int SourceActorId;
        
        /// <summary>
        /// 是否可以叠加
        /// </summary>
        public bool CanStack => MaxStack > 1;
        
        /// <summary>
        /// 是否已过期
        /// </summary>
        public bool IsExpired => RemainingTime <= 0;
        
        public BuffData(BuffType type, float value, float duration, int maxStack = 1, int sourceActorId = 0)
        {
            Type = type;
            Value = value;
            Duration = duration;
            RemainingTime = duration;
            StackCount = 1;
            MaxStack = maxStack;
            SourceActorId = sourceActorId;
        }
        
        /// <summary>
        /// 更新Buff
        /// </summary>
        public void Update(float deltaTime)
        {
            RemainingTime -= deltaTime;
        }
        
        /// <summary>
        /// 刷新持续时间
        /// </summary>
        public void Refresh()
        {
            RemainingTime = Duration;
        }
        
        /// <summary>
        /// 增加叠加层数
        /// </summary>
        public bool AddStack()
        {
            if (StackCount < MaxStack)
            {
                StackCount++;
                Refresh();
                return true;
            }
            Refresh();
            return false;
        }
        
        /// <summary>
        /// 获取当前效果值（考虑叠加）
        /// </summary>
        public float GetEffectValue()
        {
            return Value * StackCount;
        }
    }
}

