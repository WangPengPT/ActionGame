using System;

namespace ExcelImporter
{
    /// <summary>
    /// Consumable数据表
    /// </summary>
    [Serializable]
    public class ConsumableData
    {
        public int Id;
        public int Itemid;
        public int Effecttype;
        public float Effectvalue;
        public int Duration;
        public int Bufftype;
        public float Cooldown;
        public string Description;
    }
}
