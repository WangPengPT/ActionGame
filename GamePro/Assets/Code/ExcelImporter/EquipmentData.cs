using System;

namespace ExcelImporter
{
    /// <summary>
    /// Equipment数据表
    /// </summary>
    [Serializable]
    public class EquipmentData
    {
        public int Id;
        public string Name;
        public int Slot;
        public int Quality;
        public int Requiredlevel;
        public int Healthbonus;
        public int Attackbonus;
        public int Defensebonus;
        public float Critratebonus;
        public float Critdamagebonus;
        public float Dodgeratebonus;
        public float Movespeedbonus;
        public float Attackspeedbonus;
        public float Freezeresistance;
        public float Poisonresistance;
        public float Fireresistance;
        public int Buyprice;
        public int Sellprice;
        public string Prefabpath;
        public string Description;
    }
}
