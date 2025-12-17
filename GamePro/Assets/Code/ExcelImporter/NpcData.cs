using System;

namespace ExcelImporter
{
    /// <summary>
    /// NPC数据表
    /// </summary>
    [Serializable]
    public class NpcData
    {
        public int Id;
        public int Actorid;
        public int Npctype;
        public string Dialogues;
        public string Shopitemids;
        public string Questids;
        public int Interactionrange;
        public bool Isstationary;
    }
}
