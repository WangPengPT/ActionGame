using System;

namespace ExcelImporter
{
    /// <summary>
    /// Monster数据表
    /// </summary>
    [Serializable]
    public class MonsterData
    {
        public int Id;
        public int Actorid;
        public int Monstertype;
        public int Expreward;
        public int Goldreward;
        public int Detectionrange;
        public int Chaserange;
        public bool Canflee;
        public float Fleehealthpercent;
        public string Dropitemids;
        public string Droprates;
        public int Patrolradius;
    }
}
