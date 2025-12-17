using UnityEngine;
using GamePlay.Actor;
using GamePlay.Data;

namespace GamePlay.AI
{
    /// <summary>
    /// 怪物AI控制器
    /// </summary>
    public class MonsterAIController : AIController
    {
        #region 属性
        
        /// <summary>
        /// 怪物引用
        /// </summary>
        protected Monster _monster;
        
        /// <summary>
        /// 巡逻等待时间
        /// </summary>
        [SerializeField] protected float _patrolWaitTime = 2f;
        protected float _patrolWaitTimer;
        
        /// <summary>
        /// 攻击冷却时间
        /// </summary>
        [SerializeField] protected float _attackCooldown = 1f;
        protected float _attackCooldownTimer;
        
        /// <summary>
        /// 追击超时时间
        /// </summary>
        [SerializeField] protected float _chaseTimeout = 10f;
        
        /// <summary>
        /// 逃跑生命阈值（百分比）
        /// </summary>
        [SerializeField] protected float _fleeHealthThreshold = 0.2f;
        
        /// <summary>
        /// 是否会逃跑
        /// </summary>
        [SerializeField] protected bool _canFlee = false;
        
        /// <summary>
        /// 当前巡逻目标点
        /// </summary>
        protected Vector3 _currentPatrolTarget;
        
        #endregion
        
        public override void Initialize(ActorBase owner)
        {
            base.Initialize(owner);
            _monster = owner as Monster;
            
            if (_monster == null)
            {
                Debug.LogError("MonsterAIController 必须挂载在 Monster 上");
                return;
            }
            
            // Boss不会逃跑
            if (_monster.MonsterType == MonsterType.Boss)
            {
                _canFlee = false;
            }
            
            // 如果有巡逻点，开始巡逻；否则进入空闲
            if (_monster.PatrolPoints.Count > 0)
            {
                ChangeState(AIState.Patrol);
            }
            else
            {
                ChangeState(AIState.Idle);
            }
        }
        
        #region AI决策
        
        protected override void Think()
        {
            if (_monster == null) return;
            
            // 死亡检测
            if (!_monster.Attribute.IsAlive)
            {
                _enabled = false;
                return;
            }
            
            // 眩晕状态不执行AI
            if (_monster.HasBuff(BuffType.Stun))
            {
                return;
            }
            
            // 检测敌人
            var target = _monster.DetectEnemy();
            
            // 根据当前状态和条件进行决策
            switch (_currentState)
            {
                case AIState.Idle:
                case AIState.Patrol:
                case AIState.Wander:
                    // 发现敌人则追击
                    if (target != null)
                    {
                        ChangeState(AIState.Chase);
                    }
                    break;
                    
                case AIState.Chase:
                    // 目标丢失或超出追击范围
                    if (target == null || _monster.IsOutOfChaseRange())
                    {
                        ChangeState(AIState.Return);
                    }
                    // 进入攻击范围
                    else if (IsInAttackRange(target))
                    {
                        ChangeState(AIState.Attack);
                    }
                    // 检查是否需要逃跑
                    else if (ShouldFlee())
                    {
                        ChangeState(AIState.Flee);
                    }
                    // 追击超时
                    else if (_stateTimer > _chaseTimeout)
                    {
                        ChangeState(AIState.Return);
                    }
                    break;
                    
                case AIState.Attack:
                    // 目标死亡或丢失
                    if (target == null || !target.Attribute.IsAlive)
                    {
                        ChangeState(AIState.Idle);
                    }
                    // 目标离开攻击范围
                    else if (!IsInAttackRange(target))
                    {
                        ChangeState(AIState.Chase);
                    }
                    // 检查是否需要逃跑
                    else if (ShouldFlee())
                    {
                        ChangeState(AIState.Flee);
                    }
                    break;
                    
                case AIState.Flee:
                    // 逃跑一段时间后尝试返回
                    if (_stateTimer > 3f)
                    {
                        ChangeState(AIState.Return);
                    }
                    break;
                    
                case AIState.Return:
                    // 返回途中发现敌人（如果生命足够）
                    if (target != null && !ShouldFlee())
                    {
                        ChangeState(AIState.Chase);
                    }
                    // 到达出生点
                    else if (HasReachedTarget(_monster.SpawnPosition))
                    {
                        // 恢复生命
                        _monster.Heal(_monster.Attribute.MaxHealth * 0.5f);
                        _monster.ClearThreat();
                        
                        if (_monster.PatrolPoints.Count > 0)
                        {
                            ChangeState(AIState.Patrol);
                        }
                        else
                        {
                            ChangeState(AIState.Idle);
                        }
                    }
                    break;
            }
        }
        
        /// <summary>
        /// 是否应该逃跑
        /// </summary>
        private bool ShouldFlee()
        {
            if (!_canFlee) return false;
            return _monster.Attribute.HealthPercent < _fleeHealthThreshold;
        }
        
        #endregion
        
        #region 状态更新
        
        protected override void UpdateIdle()
        {
            // 空闲时随机徘徊
            if (_stateTimer > 3f)
            {
                if (UnityEngine.Random.value < 0.3f)
                {
                    ChangeState(AIState.Wander);
                }
                _stateTimer = 0;
            }
        }
        
        protected override void UpdatePatrol()
        {
            // 等待中
            if (_patrolWaitTimer > 0)
            {
                _patrolWaitTimer -= Time.deltaTime;
                return;
            }
            
            // 移动到巡逻点
            MoveToTarget(_currentPatrolTarget);
            
            // 到达巡逻点
            if (HasReachedTarget(_currentPatrolTarget))
            {
                _patrolWaitTimer = _patrolWaitTime;
                _currentPatrolTarget = _monster.GetNextPatrolPoint();
            }
        }
        
        protected override void UpdateChase()
        {
            var target = _monster.CurrentTarget;
            if (target == null) return;
            
            MoveTowardsTarget(target);
        }
        
        protected override void UpdateAttack()
        {
            var target = _monster.CurrentTarget;
            if (target == null) return;
            
            // 面向目标
            Vector3 lookDir = target.transform.position - _monster.transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                _monster.transform.rotation = Quaternion.LookRotation(lookDir);
            }
            
            // 攻击冷却
            _attackCooldownTimer -= Time.deltaTime;
            if (_attackCooldownTimer <= 0)
            {
                _monster.Attack(target);
                _attackCooldownTimer = _attackCooldown;
            }
        }
        
        protected override void UpdateFlee()
        {
            var target = _monster.CurrentTarget;
            if (target != null)
            {
                MoveAwayFromTarget(target, 10f);
            }
            else
            {
                // 没有目标时向出生点逃跑
                MoveToTarget(_monster.SpawnPosition);
            }
        }
        
        protected override void UpdateReturn()
        {
            MoveToTarget(_monster.SpawnPosition);
        }
        
        protected override void UpdateWander()
        {
            MoveToTarget(_moveTarget);
            
            // 到达徘徊点或超时
            if (HasReachedTarget(_moveTarget) || _stateTimer > 5f)
            {
                ChangeState(AIState.Idle);
            }
        }
        
        #endregion
        
        #region 状态进入
        
        protected override void OnEnterPatrol()
        {
            _currentPatrolTarget = _monster.GetNextPatrolPoint();
            _patrolWaitTimer = 0;
        }
        
        protected override void OnEnterChase()
        {
            Debug.Log($"{_monster.ActorName} 开始追击目标");
        }
        
        protected override void OnEnterAttack()
        {
            _attackCooldownTimer = 0; // 立即可以攻击
            StopMoving();
        }
        
        protected override void OnEnterFlee()
        {
            Debug.Log($"{_monster.ActorName} 开始逃跑");
        }
        
        protected override void OnEnterReturn()
        {
            Debug.Log($"{_monster.ActorName} 返回出生点");
        }
        
        protected override void OnEnterWander()
        {
            _moveTarget = GetRandomWanderPoint(5f);
        }
        
        #endregion
    }
}

