using System;

namespace ExcelImporter
{
    /// <summary>
    /// Item数据表
    /// </summary>
    [Serializable]
    public class ItemData
    {
        public int Id;
        public string Name;
        public int Itemtype;
        public int Quality;
        public bool Stackable;
        public int Maxstack;
        public int Buyprice;
        public int Sellprice;
        public string Iconpath;
        public string Description;
    }
}
