using System;

namespace ExcelImporter
{
    /// <summary>
    /// Actor数据表
    /// </summary>
    [Serializable]
    public class ActorData
    {
        public int Id;
        public string Name;
        public int Actortype;
        public int Maxhealth;
        public int Attack;
        public int Defense;
        public float Movespeed;
        public float Critrate;
        public float Critdamage;
        public float Dodgerate;
        public float Attackspeed;
        public float Attackrange;
        public float Freezeresistance;
        public float Poisonresistance;
        public float Fireresistance;
        public string Prefabpath;
        public string Description;
    }
}
