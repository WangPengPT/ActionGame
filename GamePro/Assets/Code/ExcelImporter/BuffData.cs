using System;

namespace ExcelImporter
{
    /// <summary>
    /// Buff数据表
    /// </summary>
    [Serializable]
    public class BuffData
    {
        public int Id;
        public string Name;
        public int Bufftype;
        public float Defaultvalue;
        public int Defaultduration;
        public int Maxstack;
        public string Iconpath;
        public string Effectpath;
        public string Description;
    }
}
