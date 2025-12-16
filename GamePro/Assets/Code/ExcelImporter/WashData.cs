using System;

namespace ExcelImporter
{
    /// <summary>
    /// Wash数据表
    /// </summary>
    [Serializable]
    public class WashData
    {
        public int Id;
        public string Name;
        public string File;
        public int Age;
        public bool Student;
        public float Height;
    }
}
