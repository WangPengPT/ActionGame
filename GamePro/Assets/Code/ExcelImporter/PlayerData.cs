using System;

namespace ExcelImporter
{
    /// <summary>
    /// Player数据表
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        public int Id;
        public string Name;
        public string File;
        public int Age;
        public bool Student;
        public float Height;
    }
}
