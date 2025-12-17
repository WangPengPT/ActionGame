using UnityEngine;
using GamePlay.Actor;

namespace GamePlay.Item
{
    /// <summary>
    /// 武器类型
    /// </summary>
    public enum WeaponType
    {
        /// <summary>
        /// 手枪
        /// </summary>
        Pistol = 0,
        
        /// <summary>
        /// 步枪
        /// </summary>
        Rifle = 1,
        
        /// <summary>
        /// 冲锋枪
        /// </summary>
        SMG = 2,
        
        /// <summary>
        /// 霰弹枪
        /// </summary>
        Shotgun = 3,
        
        /// <summary>
        /// 狙击枪
        /// </summary>
        Sniper = 4,
        
        /// <summary>
        /// 机枪
        /// </summary>
        MachineGun = 5,
        
        /// <summary>
        /// 近战武器
        /// </summary>
        Melee = 6
    }
    
    /// <summary>
    /// 元素类型
    /// </summary>
    public enum ElementType
    {
        /// <summary>
        /// 无元素
        /// </summary>
        None = 0,
        
        /// <summary>
        /// 冰元素
        /// </summary>
        Ice = 1,
        
        /// <summary>
        /// 毒元素
        /// </summary>
        Poison = 2,
        
        /// <summary>
        /// 火元素
        /// </summary>
        Fire = 3,
        
        /// <summary>
        /// 雷元素
        /// </summary>
        Lightning = 4
    }
    
    /// <summary>
    /// 武器类
    /// </summary>
    public class Weapon : Equipment
    {
        #region 属性
        
        /// <summary>
        /// 武器类型
        /// </summary>
        [SerializeField] protected WeaponType _weaponType;
        public WeaponType WeaponType => _weaponType;
        
        /// <summary>
        /// 元素类型
        /// </summary>
        [SerializeField] protected ElementType _elementType;
        public ElementType ElementType => _elementType;
        
        /// <summary>
        /// 武器伤害
        /// </summary>
        [SerializeField] protected float _damage = 10f;
        public float Damage => _damage;
        
        /// <summary>
        /// 射速（每秒发射次数）
        /// </summary>
        [SerializeField] protected float _fireRate = 5f;
        public float FireRate => _fireRate;
        
        /// <summary>
        /// 射程
        /// </summary>
        [SerializeField] protected float _range = 30f;
        public float Range => _range;
        
        /// <summary>
        /// 弹匣容量
        /// </summary>
        [SerializeField] protected int _magazineSize = 30;
        public int MagazineSize => _magazineSize;
        
        /// <summary>
        /// 换弹时间（秒）
        /// </summary>
        [SerializeField] protected float _reloadTime = 2f;
        public float ReloadTime => _reloadTime;
        
        /// <summary>
        /// 散射角度（度）
        /// </summary>
        [SerializeField] protected float _spread = 2f;
        public float Spread => _spread;
        
        /// <summary>
        /// 后坐力
        /// </summary>
        [SerializeField] protected float _recoil = 1f;
        public float Recoil => _recoil;
        
        /// <summary>
        /// 穿透力（可穿透的目标数）
        /// </summary>
        [SerializeField] protected int _penetration = 0;
        public int Penetration => _penetration;
        
        /// <summary>
        /// 霰弹枪弹丸数量
        /// </summary>
        [SerializeField] protected int _pelletCount = 1;
        public int PelletCount => _pelletCount;
        
        #endregion
        
        protected override void Awake()
        {
            base.Awake();
            _itemType = ItemType.Weapon;
            _slot = EquipmentSlot.PrimaryWeapon;
        }
        
        /// <summary>
        /// 初始化武器
        /// </summary>
        public void InitializeWeapon(int itemId, int configId, string name, ItemQuality quality,
            WeaponType weaponType, ElementType elementType, float damage, float fireRate, 
            float range, int magazineSize, float reloadTime)
        {
            InitializeEquipment(itemId, configId, name, quality, EquipmentSlot.PrimaryWeapon);
            _itemType = ItemType.Weapon;
            _weaponType = weaponType;
            _elementType = elementType;
            _damage = damage;
            _fireRate = fireRate;
            _range = range;
            _magazineSize = magazineSize;
            _reloadTime = reloadTime;
            
            // 根据武器类型设置默认参数
            SetDefaultsByWeaponType();
        }
        
        /// <summary>
        /// 根据武器类型设置默认参数
        /// </summary>
        private void SetDefaultsByWeaponType()
        {
            switch (_weaponType)
            {
                case WeaponType.Pistol:
                    _spread = 2f;
                    _recoil = 0.5f;
                    break;
                    
                case WeaponType.Rifle:
                    _spread = 1f;
                    _recoil = 1f;
                    break;
                    
                case WeaponType.SMG:
                    _spread = 3f;
                    _recoil = 0.8f;
                    break;
                    
                case WeaponType.Shotgun:
                    _spread = 15f;
                    _recoil = 2f;
                    _pelletCount = 8;
                    break;
                    
                case WeaponType.Sniper:
                    _spread = 0.1f;
                    _recoil = 2.5f;
                    _penetration = 2;
                    break;
                    
                case WeaponType.MachineGun:
                    _spread = 4f;
                    _recoil = 1.5f;
                    break;
                    
                case WeaponType.Melee:
                    _spread = 0;
                    _recoil = 0;
                    _range = 2f;
                    break;
            }
        }
        
        /// <summary>
        /// 设置武器参数
        /// </summary>
        public void SetWeaponParams(float spread, float recoil, int penetration = 0, int pelletCount = 1)
        {
            _spread = spread;
            _recoil = recoil;
            _penetration = penetration;
            _pelletCount = pelletCount;
        }
        
        public override string GetStatsDescription()
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine($"武器类型: {GetWeaponTypeName()}");
            sb.AppendLine($"伤害: {_damage}");
            sb.AppendLine($"射速: {_fireRate}/秒");
            sb.AppendLine($"射程: {_range}m");
            sb.AppendLine($"弹匣: {_magazineSize}发");
            sb.AppendLine($"换弹: {_reloadTime}秒");
            
            if (_elementType != ElementType.None)
            {
                sb.AppendLine($"元素: {GetElementName()}");
            }
            
            sb.AppendLine();
            sb.Append(base.GetStatsDescription());
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 获取武器类型名称
        /// </summary>
        public string GetWeaponTypeName()
        {
            switch (_weaponType)
            {
                case WeaponType.Pistol: return "手枪";
                case WeaponType.Rifle: return "步枪";
                case WeaponType.SMG: return "冲锋枪";
                case WeaponType.Shotgun: return "霰弹枪";
                case WeaponType.Sniper: return "狙击枪";
                case WeaponType.MachineGun: return "机枪";
                case WeaponType.Melee: return "近战武器";
                default: return "未知";
            }
        }
        
        /// <summary>
        /// 获取元素名称
        /// </summary>
        public string GetElementName()
        {
            switch (_elementType)
            {
                case ElementType.None: return "无";
                case ElementType.Ice: return "冰";
                case ElementType.Poison: return "毒";
                case ElementType.Fire: return "火";
                case ElementType.Lightning: return "雷";
                default: return "未知";
            }
        }
    }
}

