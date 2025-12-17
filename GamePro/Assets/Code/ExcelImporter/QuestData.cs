using System;

namespace ExcelImporter
{
    /// <summary>
    /// Quest数据表
    /// </summary>
    [Serializable]
    public class QuestData
    {
        public int Id;
        public string Name;
        public int Npcid;
        public int Type;
        public int Targettype;
        public int Targetid;
        public int Targetcount;
        public int Rewardexp;
        public int Rewardgold;
        public string Rewarditemids;
        public string Rewarditemcounts;
        public int Prequestid;
        public string Description;
    }
}
