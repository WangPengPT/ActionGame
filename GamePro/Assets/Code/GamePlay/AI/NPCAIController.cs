using UnityEngine;
using GamePlay.Actor;

namespace GamePlay.AI
{
    /// <summary>
    /// NPC AI控制器（用于非固定位置的NPC）
    /// </summary>
    public class NPCAIController : AIController
    {
        #region 属性
        
        /// <summary>
        /// NPC引用
        /// </summary>
        protected NPC _npc;
        
        /// <summary>
        /// 徘徊半径
        /// </summary>
        [SerializeField] protected float _wanderRadius = 5f;
        
        /// <summary>
        /// 空闲时间范围
        /// </summary>
        [SerializeField] protected float _idleTimeMin = 2f;
        [SerializeField] protected float _idleTimeMax = 5f;
        protected float _idleDuration;
        
        /// <summary>
        /// 初始位置
        /// </summary>
        protected Vector3 _homePosition;
        
        /// <summary>
        /// 最大远离距离
        /// </summary>
        [SerializeField] protected float _maxWanderDistance = 10f;
        
        #endregion
        
        public override void Initialize(ActorBase owner)
        {
            base.Initialize(owner);
            _npc = owner as NPC;
            
            if (_npc == null)
            {
                Debug.LogError("NPCAIController 必须挂载在 NPC 上");
                return;
            }
            
            _homePosition = _npc.transform.position;
            ChangeState(AIState.Idle);
        }
        
        #region AI决策
        
        protected override void Think()
        {
            if (_npc == null) return;
            
            // NPC正在交互时不执行AI
            if (_npc.IsInteracting)
            {
                return;
            }
            
            switch (_currentState)
            {
                case AIState.Idle:
                    // 空闲一段时间后开始徘徊
                    if (_stateTimer > _idleDuration)
                    {
                        if (UnityEngine.Random.value < 0.5f)
                        {
                            ChangeState(AIState.Wander);
                        }
                        else
                        {
                            // 重置空闲时间
                            _idleDuration = UnityEngine.Random.Range(_idleTimeMin, _idleTimeMax);
                            _stateTimer = 0;
                        }
                    }
                    break;
                    
                case AIState.Wander:
                    // 到达目标点或超时
                    if (HasReachedTarget(_moveTarget) || _stateTimer > 10f)
                    {
                        ChangeState(AIState.Idle);
                    }
                    // 走太远了，返回
                    else if (Vector3.Distance(_npc.transform.position, _homePosition) > _maxWanderDistance)
                    {
                        ChangeState(AIState.Return);
                    }
                    break;
                    
                case AIState.Return:
                    // 到达家的位置
                    if (HasReachedTarget(_homePosition))
                    {
                        ChangeState(AIState.Idle);
                    }
                    break;
            }
        }
        
        #endregion
        
        #region 状态更新
        
        protected override void UpdateIdle()
        {
            // NPC空闲时可以做一些动画或行为
        }
        
        protected override void UpdateWander()
        {
            MoveToTarget(_moveTarget);
        }
        
        protected override void UpdateReturn()
        {
            MoveToTarget(_homePosition);
        }
        
        #endregion
        
        #region 状态进入
        
        protected override void OnEnterIdle()
        {
            _idleDuration = UnityEngine.Random.Range(_idleTimeMin, _idleTimeMax);
            StopMoving();
        }
        
        protected override void OnEnterWander()
        {
            // 生成一个在家附近的随机点
            _moveTarget = GetRandomWanderPoint(_wanderRadius);
            
            // 确保不会太远
            Vector3 direction = (_moveTarget - _homePosition).normalized;
            float distance = Vector3.Distance(_moveTarget, _homePosition);
            if (distance > _maxWanderDistance)
            {
                _moveTarget = _homePosition + direction * _maxWanderDistance;
            }
        }
        
        protected override void OnEnterReturn()
        {
            _moveTarget = _homePosition;
        }
        
        #endregion
    }
}

