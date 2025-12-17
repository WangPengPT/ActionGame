using System;
using System.Collections.Generic;
using UnityEngine;
using GamePlay.Data;
using GamePlay.Item;

namespace GamePlay.Actor
{
    /// <summary>
    /// 玩家角色类 - 可由玩家控制
    /// </summary>
    public class Character : ActorBase
    {
        #region 字段
        
        /// <summary>
        /// 角色控制器
        /// </summary>
        [SerializeField] private CharacterController _characterController;
        
        /// <summary>
        /// 移动输入方向
        /// </summary>
        private Vector3 _moveInput;
        
        /// <summary>
        /// 瞄准方向
        /// </summary>
        private Vector3 _aimDirection;
        
        /// <summary>
        /// 背包容量
        /// </summary>
        [SerializeField] private int _inventoryCapacity = 20;
        
        /// <summary>
        /// 背包物品列表
        /// </summary>
        private List<ItemBase> _inventory = new List<ItemBase>();
        public IReadOnlyList<ItemBase> Inventory => _inventory;
        
        /// <summary>
        /// 装备槽
        /// </summary>
        private Dictionary<EquipmentSlot, Equipment> _equipments = new Dictionary<EquipmentSlot, Equipment>();
        
        /// <summary>
        /// 当前武器
        /// </summary>
        private Weapon _currentWeapon;
        public Weapon CurrentWeapon => _currentWeapon;
        
        /// <summary>
        /// 当前弹药数
        /// </summary>
        private int _currentAmmo;
        public int CurrentAmmo => _currentAmmo;
        
        /// <summary>
        /// 备用弹药数
        /// </summary>
        private int _reserveAmmo;
        public int ReserveAmmo => _reserveAmmo;
        
        /// <summary>
        /// 换弹计时器
        /// </summary>
        private float _reloadTimer;
        
        /// <summary>
        /// 经验值
        /// </summary>
        [SerializeField] private int _experience;
        public int Experience => _experience;
        
        /// <summary>
        /// 等级
        /// </summary>
        [SerializeField] private int _level = 1;
        public int Level => _level;
        
        /// <summary>
        /// 金币
        /// </summary>
        [SerializeField] private int _gold;
        public int Gold => _gold;
        
        #endregion
        
        #region 事件
        
        /// <summary>
        /// 拾取物品事件
        /// </summary>
        public event Action<ItemBase> OnItemPickedUp;
        
        /// <summary>
        /// 使用物品事件
        /// </summary>
        public event Action<ItemBase> OnItemUsed;
        
        /// <summary>
        /// 装备变更事件
        /// </summary>
        public event Action<EquipmentSlot, Equipment> OnEquipmentChanged;
        
        /// <summary>
        /// 武器切换事件
        /// </summary>
        public event Action<Weapon> OnWeaponChanged;
        
        /// <summary>
        /// 换弹事件
        /// </summary>
        public event Action OnReloadStart;
        public event Action OnReloadComplete;
        
        /// <summary>
        /// 升级事件
        /// </summary>
        public event Action<int> OnLevelUp;
        
        /// <summary>
        /// 开火事件
        /// </summary>
        public event Action OnFired;
        
        /// <summary>
        /// 换弹完成事件（别名）
        /// </summary>
        public event Action OnReloaded { add { OnReloadComplete += value; } remove { OnReloadComplete -= value; } }
        
        #endregion
        
        #region Unity生命周期
        
        protected override void Awake()
        {
            base.Awake();
            _characterController = GetComponent<CharacterController>();
            _faction = ActorFaction.Player;
            
            // 初始化装备槽
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                _equipments[slot] = null;
            }
        }
        
        protected override void Update()
        {
            base.Update();
            
            if (!_attribute.IsAlive) return;
            
            UpdateMovement();
            UpdateReload();
        }
        
        #endregion
        
        #region 移动控制
        
        /// <summary>
        /// 设置移动输入
        /// </summary>
        public void SetMoveInput(Vector3 input)
        {
            _moveInput = input.normalized;
        }
        
        /// <summary>
        /// 设置瞄准方向
        /// </summary>
        public void SetAimDirection(Vector3 direction)
        {
            _aimDirection = direction.normalized;
            
            // 角色面向瞄准方向
            if (_aimDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(_aimDirection);
            }
        }
        
        /// <summary>
        /// 更新移动
        /// </summary>
        private void UpdateMovement()
        {
            if (!CanMove()) return;
            
            if (_moveInput.sqrMagnitude > 0.01f)
            {
                Vector3 movement = _moveInput * _attribute.MoveSpeed * Time.deltaTime;
                
                if (_characterController != null)
                {
                    _characterController.Move(movement);
                }
                else
                {
                    transform.position += movement;
                }
                
                if (CurrentState != ActorState.Moving)
                {
                    CurrentState = ActorState.Moving;
                }
            }
            else if (CurrentState == ActorState.Moving)
            {
                CurrentState = ActorState.Idle;
            }
        }
        
        #endregion
        
        #region 射击和换弹
        
        /// <summary>
        /// 开火
        /// </summary>
        public void Fire()
        {
            if (!CanFire()) return;
            
            if (_currentWeapon == null)
            {
                Debug.LogWarning("没有装备武器");
                return;
            }
            
            if (_currentAmmo <= 0)
            {
                // 自动换弹
                StartReload();
                return;
            }
            
            // 执行射击
            _currentAmmo--;
            _lastAttackTime = Time.time;
            CurrentState = ActorState.Attacking;
            
            // 发射弹道
            FireProjectile();
            
            // 触发开火事件
            OnFired?.Invoke();
        }
        
        /// <summary>
        /// 发射弹道
        /// </summary>
        private void FireProjectile()
        {
            if (_currentWeapon == null) return;
            
            // 计算射击方向（带散射）
            Vector3 fireDirection = _aimDirection;
            if (_currentWeapon.Spread > 0)
            {
                float spreadAngle = UnityEngine.Random.Range(-_currentWeapon.Spread, _currentWeapon.Spread);
                fireDirection = Quaternion.Euler(0, spreadAngle, 0) * fireDirection;
            }
            
            // 射线检测命中
            RaycastHit hit;
            Vector3 fireOrigin = transform.position + Vector3.up * 1f; // 从腰部位置发射
            
            if (Physics.Raycast(fireOrigin, fireDirection, out hit, _currentWeapon.Range))
            {
                // 检测是否命中Actor
                var hitActor = hit.collider.GetComponent<ActorBase>();
                if (hitActor != null && IsHostileTo(hitActor))
                {
                    // 计算伤害
                    float damage = CalculateWeaponDamage(hitActor, out bool isCrit);
                    hitActor.TakeDamage(damage, this);
                    
                    // 应用武器特殊效果
                    ApplyWeaponEffects(hitActor);
                }
            }
        }
        
        /// <summary>
        /// 计算武器伤害
        /// </summary>
        private float CalculateWeaponDamage(ActorBase target, out bool isCrit)
        {
            float baseDamage = _currentWeapon != null ? _currentWeapon.Damage : _attribute.Attack;
            baseDamage += _attribute.Attack * 0.1f; // 属性加成
            
            // 暴击判定
            isCrit = UnityEngine.Random.value < _attribute.CritRate;
            if (isCrit)
            {
                baseDamage *= _attribute.CritDamage;
            }
            
            // 考虑目标防御
            float defense = target.Attribute.Defense;
            float damageReduction = defense / (defense + 100);
            float finalDamage = baseDamage * (1 - damageReduction);
            
            return Mathf.Max(1, finalDamage);
        }
        
        /// <summary>
        /// 应用武器特殊效果
        /// </summary>
        private void ApplyWeaponEffects(ActorBase target)
        {
            if (_currentWeapon == null) return;
            
            // 根据武器元素类型应用Buff
            switch (_currentWeapon.ElementType)
            {
                case ElementType.Ice:
                    target.AddBuff(new BuffData(BuffType.Freeze, 0.3f, 3f, 5, _actorId));
                    break;
                case ElementType.Poison:
                    target.AddBuff(new BuffData(BuffType.Poison, _currentWeapon.Damage * 0.2f, 5f, 5, _actorId));
                    break;
                case ElementType.Fire:
                    target.AddBuff(new BuffData(BuffType.Burn, _currentWeapon.Damage * 0.15f, 4f, 3, _actorId));
                    break;
            }
        }
        
        /// <summary>
        /// 是否可以开火
        /// </summary>
        public bool CanFire()
        {
            if (!_attribute.IsAlive) return false;
            if (CurrentState == ActorState.Dead || CurrentState == ActorState.Stunned) return false;
            if (CurrentState == ActorState.Reloading) return false;
            if (_currentWeapon == null) return false;
            
            // 射击间隔检测
            float fireInterval = 1f / _currentWeapon.FireRate;
            if (Time.time - _lastAttackTime < fireInterval) return false;
            
            return true;
        }
        
        /// <summary>
        /// 开始换弹
        /// </summary>
        public void StartReload()
        {
            if (CurrentState == ActorState.Reloading) return;
            if (_currentWeapon == null) return;
            if (_reserveAmmo <= 0) return;
            if (_currentAmmo >= _currentWeapon.MagazineSize) return;
            
            CurrentState = ActorState.Reloading;
            _reloadTimer = _currentWeapon.ReloadTime;
            OnReloadStart?.Invoke();
        }
        
        /// <summary>
        /// 更新换弹
        /// </summary>
        private void UpdateReload()
        {
            if (CurrentState != ActorState.Reloading) return;
            
            _reloadTimer -= Time.deltaTime;
            if (_reloadTimer <= 0)
            {
                CompleteReload();
            }
        }
        
        /// <summary>
        /// 完成换弹
        /// </summary>
        private void CompleteReload()
        {
            if (_currentWeapon == null) return;
            
            int needed = _currentWeapon.MagazineSize - _currentAmmo;
            int toReload = Mathf.Min(needed, _reserveAmmo);
            
            _currentAmmo += toReload;
            _reserveAmmo -= toReload;
            
            CurrentState = ActorState.Idle;
            OnReloadComplete?.Invoke();
        }
        
        #endregion
        
        #region 物品和装备
        
        /// <summary>
        /// 拾取物品
        /// </summary>
        public bool PickupItem(ItemBase item)
        {
            if (item == null) return false;
            
            if (_inventory.Count >= _inventoryCapacity)
            {
                Debug.LogWarning("背包已满");
                return false;
            }
            
            _inventory.Add(item);
            item.OnPickup(this);
            OnItemPickedUp?.Invoke(item);
            
            return true;
        }
        
        /// <summary>
        /// 丢弃物品
        /// </summary>
        public bool DropItem(ItemBase item)
        {
            if (!_inventory.Contains(item)) return false;
            
            _inventory.Remove(item);
            item.OnDrop(this);
            
            return true;
        }
        
        /// <summary>
        /// 使用物品
        /// </summary>
        public bool UseItem(ItemBase item)
        {
            if (item == null || !item.IsUsable) return false;
            
            if (item.Use(this))
            {
                OnItemUsed?.Invoke(item);
                
                // 消耗品用完后移除
                if (item.IsConsumable)
                {
                    _inventory.Remove(item);
                }
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 装备物品
        /// </summary>
        public bool Equip(Equipment equipment)
        {
            if (equipment == null) return false;
            
            EquipmentSlot slot = equipment.Slot;
            
            // 先卸下当前装备
            if (_equipments[slot] != null)
            {
                Unequip(slot);
            }
            
            // 装上新装备
            _equipments[slot] = equipment;
            equipment.OnEquip(this);
            ApplyEquipmentStats(equipment, true);
            
            // 如果是武器，设为当前武器
            if (equipment is Weapon weapon)
            {
                SetCurrentWeapon(weapon);
            }
            
            OnEquipmentChanged?.Invoke(slot, equipment);
            return true;
        }
        
        /// <summary>
        /// 卸下装备
        /// </summary>
        public Equipment Unequip(EquipmentSlot slot)
        {
            var equipment = _equipments[slot];
            if (equipment == null) return null;
            
            ApplyEquipmentStats(equipment, false);
            equipment.OnUnequip(this);
            _equipments[slot] = null;
            
            // 如果卸下的是当前武器
            if (equipment == _currentWeapon)
            {
                _currentWeapon = null;
                OnWeaponChanged?.Invoke(null);
            }
            
            OnEquipmentChanged?.Invoke(slot, null);
            
            // 放回背包
            if (_inventory.Count < _inventoryCapacity)
            {
                _inventory.Add(equipment);
            }
            
            return equipment;
        }
        
        /// <summary>
        /// 应用装备属性
        /// </summary>
        private void ApplyEquipmentStats(Equipment equipment, bool apply)
        {
            float multiplier = apply ? 1 : -1;
            
            _attribute.MaxHealthBonus += equipment.HealthBonus * multiplier;
            _attribute.AttackBonus += equipment.AttackBonus * multiplier;
            _attribute.DefenseBonus += equipment.DefenseBonus * multiplier;
            _attribute.CritRateBonus += equipment.CritRateBonus * multiplier;
            _attribute.DodgeRateBonus += equipment.DodgeRateBonus * multiplier;
            _attribute.MoveSpeedBonus += equipment.MoveSpeedBonus * multiplier;
        }
        
        /// <summary>
        /// 设置当前武器
        /// </summary>
        private void SetCurrentWeapon(Weapon weapon)
        {
            _currentWeapon = weapon;
            if (weapon != null)
            {
                _currentAmmo = weapon.MagazineSize;
                _reserveAmmo = weapon.MagazineSize * 3; // 初始3个弹匣
            }
            OnWeaponChanged?.Invoke(weapon);
        }
        
        /// <summary>
        /// 获取指定槽位的装备
        /// </summary>
        public Equipment GetEquipment(EquipmentSlot slot)
        {
            return _equipments.TryGetValue(slot, out var equipment) ? equipment : null;
        }
        
        /// <summary>
        /// 添加弹药
        /// </summary>
        public void AddAmmo(int amount)
        {
            _reserveAmmo += amount;
        }
        
        #endregion
        
        #region 经济和等级
        
        /// <summary>
        /// 添加金币
        /// </summary>
        public void AddGold(int amount)
        {
            _gold += amount;
        }
        
        /// <summary>
        /// 消费金币
        /// </summary>
        public bool SpendGold(int amount)
        {
            if (_gold < amount) return false;
            _gold -= amount;
            return true;
        }
        
        /// <summary>
        /// 添加经验
        /// </summary>
        public void AddExperience(int exp)
        {
            _experience += exp;
            CheckLevelUp();
        }
        
        /// <summary>
        /// 检查升级
        /// </summary>
        private void CheckLevelUp()
        {
            int expNeeded = GetExpForLevel(_level + 1);
            while (_experience >= expNeeded && _level < 100)
            {
                _level++;
                OnLevelUp?.Invoke(_level);
                LevelUp();
                expNeeded = GetExpForLevel(_level + 1);
            }
        }
        
        /// <summary>
        /// 升级
        /// </summary>
        private void LevelUp()
        {
            // 属性提升
            _attribute.MaxHealth *= 1.1f;
            _attribute.Attack *= 1.05f;
            _attribute.Defense *= 1.05f;
            _attribute.CurrentHealth = _attribute.MaxHealth;
            
            Debug.Log($"{_actorName} 升级到 {_level} 级！");
        }
        
        /// <summary>
        /// 获取升级所需经验
        /// </summary>
        private int GetExpForLevel(int level)
        {
            return level * level * 100;
        }
        
        #endregion
        
        #region 交互
        
        /// <summary>
        /// 与目标交互
        /// </summary>
        public void InteractWith(ActorBase target)
        {
            if (target == null) return;
            
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance > 3f) // 交互距离
            {
                Debug.LogWarning("目标太远，无法交互");
                return;
            }
            
            CurrentState = ActorState.Interacting;
            target.Interact(this);
        }
        
        /// <summary>
        /// 结束交互
        /// </summary>
        public void EndInteraction()
        {
            if (CurrentState == ActorState.Interacting)
            {
                CurrentState = ActorState.Idle;
            }
        }
        
        #endregion
    }
}

