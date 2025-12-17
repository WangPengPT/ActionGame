using System;

namespace ExcelImporter
{
    /// <summary>
    /// Weapon数据表
    /// </summary>
    [Serializable]
    public class WeaponData
    {
        public int Id;
        public string Name;
        public int Weapontype;
        public int Elementtype;
        public int Quality;
        public int Damage;
        public float Firerate;
        public float Range;
        public int Magazinesize;
        public float Reloadtime;
        public float Spread;
        public float Recoil;
        public int Penetration;
        public int Pelletcount;
        public int Healthbonus;
        public int Attackbonus;
        public int Defensebonus;
        public float Critratebonus;
        public int Requiredlevel;
        public int Buyprice;
        public int Sellprice;
        public string Prefabpath;
        public string Description;
    }
}
