using System;
using UnityEngine;
using GamePlay.Actor;

namespace GamePlay.AI
{
    /// <summary>
    /// AI状态枚举
    /// </summary>
    public enum AIState
    {
        /// <summary>
        /// 空闲
        /// </summary>
        Idle = 0,
        
        /// <summary>
        /// 巡逻
        /// </summary>
        Patrol = 1,
        
        /// <summary>
        /// 追击
        /// </summary>
        Chase = 2,
        
        /// <summary>
        /// 攻击
        /// </summary>
        Attack = 3,
        
        /// <summary>
        /// 逃跑
        /// </summary>
        Flee = 4,
        
        /// <summary>
        /// 返回
        /// </summary>
        Return = 5,
        
        /// <summary>
        /// 徘徊
        /// </summary>
        Wander = 6,
        
        /// <summary>
        /// 等待
        /// </summary>
        Wait = 7
    }
    
    /// <summary>
    /// AI控制器基类
    /// </summary>
    public abstract class AIController : MonoBehaviour
    {
        #region 属性
        
        /// <summary>
        /// 关联的Actor
        /// </summary>
        protected ActorBase _owner;
        public ActorBase Owner => _owner;
        
        /// <summary>
        /// 当前AI状态
        /// </summary>
        [SerializeField] protected AIState _currentState = AIState.Idle;
        public AIState CurrentState => _currentState;
        
        /// <summary>
        /// 上一个状态
        /// </summary>
        protected AIState _previousState;
        
        /// <summary>
        /// 状态计时器
        /// </summary>
        protected float _stateTimer;
        
        /// <summary>
        /// 思考间隔（避免每帧都执行AI逻辑）
        /// </summary>
        [SerializeField] protected float _thinkInterval = 0.2f;
        protected float _thinkTimer;
        
        /// <summary>
        /// 移动目标点
        /// </summary>
        protected Vector3 _moveTarget;
        
        /// <summary>
        /// 是否启用AI
        /// </summary>
        [SerializeField] protected bool _enabled = true;
        public bool AIEnabled 
        { 
            get => _enabled; 
            set => _enabled = value; 
        }
        
        /// <summary>
        /// 移动速度倍率
        /// </summary>
        [SerializeField] protected float _moveSpeedMultiplier = 1f;
        
        /// <summary>
        /// 到达目标的距离阈值
        /// </summary>
        [SerializeField] protected float _arrivalThreshold = 0.5f;
        
        #endregion
        
        #region 事件
        
        /// <summary>
        /// 状态改变事件
        /// </summary>
        public event Action<AIState, AIState> OnStateChanged;
        
        #endregion
        
        #region 初始化
        
        /// <summary>
        /// 初始化AI控制器
        /// </summary>
        public virtual void Initialize(ActorBase owner)
        {
            _owner = owner;
            _currentState = AIState.Idle;
            _thinkTimer = 0;
        }
        
        #endregion
        
        #region AI更新
        
        /// <summary>
        /// 更新AI（每帧调用）
        /// </summary>
        public virtual void UpdateAI()
        {
            if (!_enabled || _owner == null || !_owner.Attribute.IsAlive)
                return;
            
            _stateTimer += Time.deltaTime;
            _thinkTimer += Time.deltaTime;
            
            // 思考间隔控制
            if (_thinkTimer >= _thinkInterval)
            {
                _thinkTimer = 0;
                Think();
            }
            
            // 持续更新当前状态
            UpdateCurrentState();
        }
        
        /// <summary>
        /// AI思考（决策）
        /// </summary>
        protected abstract void Think();
        
        /// <summary>
        /// 更新当前状态
        /// </summary>
        protected virtual void UpdateCurrentState()
        {
            switch (_currentState)
            {
                case AIState.Idle:
                    UpdateIdle();
                    break;
                case AIState.Patrol:
                    UpdatePatrol();
                    break;
                case AIState.Chase:
                    UpdateChase();
                    break;
                case AIState.Attack:
                    UpdateAttack();
                    break;
                case AIState.Flee:
                    UpdateFlee();
                    break;
                case AIState.Return:
                    UpdateReturn();
                    break;
                case AIState.Wander:
                    UpdateWander();
                    break;
                case AIState.Wait:
                    UpdateWait();
                    break;
            }
        }
        
        #endregion
        
        #region 状态更新（子类重写）
        
        protected virtual void UpdateIdle() { }
        protected virtual void UpdatePatrol() { }
        protected virtual void UpdateChase() { }
        protected virtual void UpdateAttack() { }
        protected virtual void UpdateFlee() { }
        protected virtual void UpdateReturn() { }
        protected virtual void UpdateWander() { }
        protected virtual void UpdateWait() { }
        
        #endregion
        
        #region 状态切换
        
        /// <summary>
        /// 切换状态
        /// </summary>
        public virtual void ChangeState(AIState newState)
        {
            if (_currentState == newState) return;
            
            // 退出当前状态
            OnExitState(_currentState);
            
            _previousState = _currentState;
            _currentState = newState;
            _stateTimer = 0;
            
            // 进入新状态
            OnEnterState(newState);
            
            OnStateChanged?.Invoke(_previousState, _currentState);
        }
        
        /// <summary>
        /// 进入状态回调
        /// </summary>
        protected virtual void OnEnterState(AIState state)
        {
            switch (state)
            {
                case AIState.Idle:
                    OnEnterIdle();
                    break;
                case AIState.Patrol:
                    OnEnterPatrol();
                    break;
                case AIState.Chase:
                    OnEnterChase();
                    break;
                case AIState.Attack:
                    OnEnterAttack();
                    break;
                case AIState.Flee:
                    OnEnterFlee();
                    break;
                case AIState.Return:
                    OnEnterReturn();
                    break;
                case AIState.Wander:
                    OnEnterWander();
                    break;
                case AIState.Wait:
                    OnEnterWait();
                    break;
            }
        }
        
        /// <summary>
        /// 退出状态回调
        /// </summary>
        protected virtual void OnExitState(AIState state)
        {
            // 子类实现退出状态逻辑
        }
        
        protected virtual void OnEnterIdle() { }
        protected virtual void OnEnterPatrol() { }
        protected virtual void OnEnterChase() { }
        protected virtual void OnEnterAttack() { }
        protected virtual void OnEnterFlee() { }
        protected virtual void OnEnterReturn() { }
        protected virtual void OnEnterWander() { }
        protected virtual void OnEnterWait() { }
        
        #endregion
        
        #region 移动
        
        /// <summary>
        /// 移动到目标点
        /// </summary>
        protected virtual void MoveToTarget(Vector3 target)
        {
            _moveTarget = target;
            
            Vector3 direction = (target - _owner.transform.position).normalized;
            direction.y = 0; // 保持水平移动
            
            if (direction.sqrMagnitude > 0.01f)
            {
                // 旋转朝向
                _owner.transform.rotation = Quaternion.Slerp(
                    _owner.transform.rotation,
                    Quaternion.LookRotation(direction),
                    Time.deltaTime * 10f);
                
                // 移动
                float speed = _owner.Attribute.MoveSpeed * _moveSpeedMultiplier;
                _owner.transform.position += direction * speed * Time.deltaTime;
            }
        }
        
        /// <summary>
        /// 朝目标移动
        /// </summary>
        protected virtual void MoveTowardsTarget(ActorBase target)
        {
            if (target == null) return;
            MoveToTarget(target.transform.position);
        }
        
        /// <summary>
        /// 远离目标
        /// </summary>
        protected virtual void MoveAwayFromTarget(ActorBase target, float distance)
        {
            if (target == null) return;
            
            Vector3 direction = (_owner.transform.position - target.transform.position).normalized;
            Vector3 fleeTarget = _owner.transform.position + direction * distance;
            MoveToTarget(fleeTarget);
        }
        
        /// <summary>
        /// 是否到达目标点
        /// </summary>
        protected virtual bool HasReachedTarget(Vector3 target)
        {
            float distance = Vector3.Distance(_owner.transform.position, target);
            return distance <= _arrivalThreshold;
        }
        
        /// <summary>
        /// 是否在攻击范围内
        /// </summary>
        protected virtual bool IsInAttackRange(ActorBase target)
        {
            if (target == null) return false;
            float distance = Vector3.Distance(_owner.transform.position, target.transform.position);
            return distance <= _owner.Attribute.AttackRange;
        }
        
        #endregion
        
        #region 工具方法
        
        /// <summary>
        /// 获取随机徘徊点
        /// </summary>
        protected virtual Vector3 GetRandomWanderPoint(float radius)
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * radius;
            return _owner.transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        }
        
        /// <summary>
        /// 停止移动
        /// </summary>
        protected virtual void StopMoving()
        {
            _owner.StopMoving();
        }
        
        #endregion
    }
}

