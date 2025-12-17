using System;

namespace ExcelImporter
{
    /// <summary>
    /// SpawnPoint数据表
    /// </summary>
    [Serializable]
    public class SpawnpointData
    {
        public int Id;
        public int Monsterid;
        public int Posx;
        public int Posy;
        public int Posz;
        public int Respawntime;
        public int Maxcount;
        public int Patrolradius;
        public string Description;
    }
}
