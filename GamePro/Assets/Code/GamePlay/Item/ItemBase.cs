using System;
using UnityEngine;
using GamePlay.Actor;

namespace GamePlay.Item
{
    /// <summary>
    /// 物品类型枚举
    /// </summary>
    public enum ItemType
    {
        /// <summary>
        /// 消耗品
        /// </summary>
        Consumable = 0,
        
        /// <summary>
        /// 装备
        /// </summary>
        Equipment = 1,
        
        /// <summary>
        /// 武器
        /// </summary>
        Weapon = 2,
        
        /// <summary>
        /// 弹药
        /// </summary>
        Ammo = 3,
        
        /// <summary>
        /// 材料
        /// </summary>
        Material = 4,
        
        /// <summary>
        /// 任务物品
        /// </summary>
        Quest = 5
    }
    
    /// <summary>
    /// 物品品质枚举
    /// </summary>
    public enum ItemQuality
    {
        Common = 0,      // 普通（白色）
        Uncommon = 1,    // 优秀（绿色）
        Rare = 2,        // 稀有（蓝色）
        Epic = 3,        // 史诗（紫色）
        Legendary = 4    // 传说（橙色）
    }
    
    /// <summary>
    /// 物品基类
    /// </summary>
    public class ItemBase : MonoBehaviour
    {
        #region 属性
        
        /// <summary>
        /// 物品唯一ID
        /// </summary>
        [SerializeField] protected int _itemId;
        public int ItemId => _itemId;
        
        /// <summary>
        /// 配置表ID
        /// </summary>
        [SerializeField] protected int _configId;
        public int ConfigId => _configId;
        
        /// <summary>
        /// 物品名称
        /// </summary>
        [SerializeField] protected string _itemName;
        public string ItemName => _itemName;
        
        /// <summary>
        /// 物品描述
        /// </summary>
        [SerializeField] protected string _description;
        public string Description => _description;
        
        /// <summary>
        /// 物品图标
        /// </summary>
        [SerializeField] protected Sprite _icon;
        public Sprite Icon => _icon;
        
        /// <summary>
        /// 物品类型
        /// </summary>
        [SerializeField] protected ItemType _itemType;
        public ItemType Type => _itemType;
        
        /// <summary>
        /// 物品品质
        /// </summary>
        [SerializeField] protected ItemQuality _quality;
        public ItemQuality Quality => _quality;
        
        /// <summary>
        /// 是否可堆叠
        /// </summary>
        [SerializeField] protected bool _stackable = false;
        public bool Stackable => _stackable;
        
        /// <summary>
        /// 堆叠数量
        /// </summary>
        [SerializeField] protected int _stackCount = 1;
        public int StackCount 
        { 
            get => _stackCount; 
            set => _stackCount = Mathf.Max(1, value); 
        }
        
        /// <summary>
        /// 最大堆叠数量
        /// </summary>
        [SerializeField] protected int _maxStackCount = 99;
        public int MaxStackCount => _maxStackCount;
        
        /// <summary>
        /// 是否可使用
        /// </summary>
        [SerializeField] protected bool _usable = false;
        public virtual bool IsUsable => _usable;
        
        /// <summary>
        /// 是否为消耗品
        /// </summary>
        public virtual bool IsConsumable => _itemType == ItemType.Consumable;
        
        /// <summary>
        /// 购买价格
        /// </summary>
        [SerializeField] protected int _buyPrice;
        public int BuyPrice => _buyPrice;
        
        /// <summary>
        /// 出售价格
        /// </summary>
        [SerializeField] protected int _sellPrice;
        public int SellPrice => _sellPrice;
        
        /// <summary>
        /// 是否在地面上
        /// </summary>
        protected bool _isOnGround = true;
        public bool IsOnGround => _isOnGround;
        
        /// <summary>
        /// 碰撞体（用于拾取检测）
        /// </summary>
        protected Collider _collider;
        
        #endregion
        
        #region Unity生命周期
        
        protected virtual void Awake()
        {
            _collider = GetComponent<Collider>();
        }
        
        protected virtual void Start()
        {
        }
        
        #endregion
        
        #region 初始化
        
        /// <summary>
        /// 初始化物品
        /// </summary>
        public virtual void Initialize(int itemId, int configId, string name, ItemType type, ItemQuality quality)
        {
            _itemId = itemId;
            _configId = configId;
            _itemName = name;
            _itemType = type;
            _quality = quality;
        }
        
        #endregion
        
        #region 物品操作
        
        /// <summary>
        /// 使用物品
        /// </summary>
        public virtual bool Use(Character user)
        {
            if (!IsUsable || user == null) return false;
            
            // 子类实现具体使用逻辑
            OnUse(user);
            
            if (IsConsumable && _stackable)
            {
                _stackCount--;
                return _stackCount <= 0;
            }
            
            return true;
        }
        
        /// <summary>
        /// 使用回调
        /// </summary>
        protected virtual void OnUse(Character user)
        {
            Debug.Log($"{user.ActorName} 使用了 {_itemName}");
        }
        
        /// <summary>
        /// 被拾取
        /// </summary>
        public virtual void OnPickup(Character picker)
        {
            _isOnGround = false;
            
            // 隐藏物品显示
            if (_collider != null)
            {
                _collider.enabled = false;
            }
            
            // 隐藏渲染
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
            
            Debug.Log($"{picker.ActorName} 拾取了 {_itemName}");
        }
        
        /// <summary>
        /// 被丢弃
        /// </summary>
        public virtual void OnDrop(Character dropper)
        {
            _isOnGround = true;
            
            // 设置位置到丢弃者附近
            transform.position = dropper.transform.position + dropper.transform.forward * 1f;
            
            // 显示物品
            if (_collider != null)
            {
                _collider.enabled = true;
            }
            
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
            }
            
            Debug.Log($"{dropper.ActorName} 丢弃了 {_itemName}");
        }
        
        /// <summary>
        /// 堆叠物品
        /// </summary>
        public virtual int Stack(int amount)
        {
            if (!_stackable) return amount;
            
            int canAdd = _maxStackCount - _stackCount;
            int toAdd = Mathf.Min(amount, canAdd);
            
            _stackCount += toAdd;
            return amount - toAdd; // 返回剩余数量
        }
        
        /// <summary>
        /// 分割物品
        /// </summary>
        public virtual ItemBase Split(int amount)
        {
            if (!_stackable || amount >= _stackCount) return null;
            
            _stackCount -= amount;
            
            // 创建新物品（需要通过工厂创建）
            // 这里简化处理
            return null;
        }
        
        #endregion
        
        #region 交互
        
        /// <summary>
        /// 触发器进入（用于自动拾取或提示）
        /// </summary>
        protected virtual void OnTriggerEnter(Collider other)
        {
            if (!_isOnGround) return;
            
            var character = other.GetComponent<Character>();
            if (character != null)
            {
                OnCharacterNearby(character);
            }
        }
        
        /// <summary>
        /// 角色靠近
        /// </summary>
        protected virtual void OnCharacterNearby(Character character)
        {
            // 可以在这里显示拾取提示或自动拾取
            Debug.Log($"可以拾取: {_itemName}");
        }
        
        #endregion
        
        #region 工具方法
        
        /// <summary>
        /// 获取品质颜色
        /// </summary>
        public Color GetQualityColor()
        {
            switch (_quality)
            {
                case ItemQuality.Common: return Color.white;
                case ItemQuality.Uncommon: return Color.green;
                case ItemQuality.Rare: return Color.blue;
                case ItemQuality.Epic: return new Color(0.5f, 0, 0.5f); // 紫色
                case ItemQuality.Legendary: return new Color(1f, 0.5f, 0); // 橙色
                default: return Color.white;
            }
        }
        
        #endregion
    }
}

