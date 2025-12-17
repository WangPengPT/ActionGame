using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ExcelImporter;
using GamePlay.Actor;
using GamePlay.Item;
using GamePlay.Data;

namespace GamePlay.Manager
{
    /// <summary>
    /// 配置数据辅助类 - 将Excel配置转换为游戏对象
    /// </summary>
    public static class ConfigHelper
    {
        #region Actor配置
        
        /// <summary>
        /// 从配置初始化Actor属性
        /// </summary>
        public static void InitActorFromConfig(ActorBase actor, int configId)
        {
            var config = ExcelDataManager.GetActorById(configId);
            if (config == null)
            {
                Debug.LogWarning($"找不到Actor配置: {configId}");
                return;
            }
            
            actor.Attribute.InitFromConfig(
                config.Maxhealth,
                config.Attack,
                config.Defense,
                config.Movespeed,
                config.Critrate,
                config.Critdamage,
                config.Dodgerate,
                config.Attackspeed,
                config.Attackrange
            );
            
            // 设置元素抗性
            actor.Attribute.FreezeResistance = config.Freezeresistance;
            actor.Attribute.PoisonResistance = config.Poisonresistance;
            actor.Attribute.FireResistance = config.Fireresistance;
        }
        
        /// <summary>
        /// 获取Actor名称
        /// </summary>
        public static string GetActorName(int configId)
        {
            var config = ExcelDataManager.GetActorById(configId);
            return config?.Name ?? $"Actor_{configId}";
        }
        
        /// <summary>
        /// 获取Actor预制体路径
        /// </summary>
        public static string GetActorPrefabPath(int configId)
        {
            var config = ExcelDataManager.GetActorById(configId);
            return config?.Prefabpath ?? "";
        }
        
        #endregion
        
        #region 怪物配置
        
        /// <summary>
        /// 获取怪物配置
        /// </summary>
        public static MonsterData GetMonsterConfig(int monsterId)
        {
            return ExcelDataManager.GetMonsterById(monsterId);
        }
        
        /// <summary>
        /// 初始化怪物
        /// </summary>
        public static void InitMonsterFromConfig(Monster monster, int monsterId)
        {
            var monsterConfig = ExcelDataManager.GetMonsterById(monsterId);
            if (monsterConfig == null)
            {
                Debug.LogWarning($"找不到Monster配置: {monsterId}");
                return;
            }
            
            // 从Actor配置初始化基础属性
            InitActorFromConfig(monster, monsterConfig.Actorid);
            
            // 设置怪物特有属性（通过反射或公开方法）
            // 注意：这里需要Monster类提供设置方法
            SetMonsterProperties(monster, monsterConfig);
        }
        
        /// <summary>
        /// 设置怪物属性
        /// </summary>
        private static void SetMonsterProperties(Monster monster, MonsterData config)
        {
            // 解析掉落配置
            var dropItemIds = ParseIntList(config.Dropitemids);
            var dropRates = ParseFloatList(config.Droprates);
            monster.SetDropConfig(dropItemIds, dropRates);
        }
        
        /// <summary>
        /// 获取怪物类型
        /// </summary>
        public static MonsterType GetMonsterType(int monsterId)
        {
            var config = ExcelDataManager.GetMonsterById(monsterId);
            if (config == null) return MonsterType.Normal;
            return (MonsterType)config.Monstertype;
        }
        
        /// <summary>
        /// 获取怪物奖励
        /// </summary>
        public static (int exp, int gold) GetMonsterRewards(int monsterId)
        {
            var config = ExcelDataManager.GetMonsterById(monsterId);
            if (config == null) return (0, 0);
            return (config.Expreward, config.Goldreward);
        }
        
        #endregion
        
        #region NPC配置
        
        /// <summary>
        /// 获取NPC配置
        /// </summary>
        public static NpcData GetNPCConfig(int npcId)
        {
            return ExcelDataManager.GetNPCById(npcId);
        }
        
        /// <summary>
        /// 初始化NPC
        /// </summary>
        public static void InitNPCFromConfig(NPC npc, int npcId)
        {
            var npcConfig = ExcelDataManager.GetNPCById(npcId);
            if (npcConfig == null)
            {
                Debug.LogWarning($"找不到NPC配置: {npcId}");
                return;
            }
            
            // 从Actor配置初始化基础属性
            InitActorFromConfig(npc, npcConfig.Actorid);
            
            // 设置对话
            var dialogues = ParseStringList(npcConfig.Dialogues, '|');
            npc.SetDialogues(dialogues);
            
            // 设置商店物品
            var shopItems = ParseIntList(npcConfig.Shopitemids);
            npc.SetShopItems(shopItems);
            
            // 设置任务列表
            var quests = ParseIntList(npcConfig.Questids);
            npc.SetQuests(quests);
        }
        
        /// <summary>
        /// 获取NPC类型
        /// </summary>
        public static NPCType GetNPCType(int npcId)
        {
            var config = ExcelDataManager.GetNPCById(npcId);
            if (config == null) return NPCType.Normal;
            return (NPCType)config.Npctype;
        }
        
        #endregion
        
        #region 物品配置
        
        /// <summary>
        /// 获取物品配置
        /// </summary>
        public static ItemData GetItemConfig(int itemId)
        {
            return ExcelDataManager.GetItemById(itemId);
        }
        
        /// <summary>
        /// 初始化物品
        /// </summary>
        public static void InitItemFromConfig(ItemBase item, int itemId)
        {
            var config = ExcelDataManager.GetItemById(itemId);
            if (config == null)
            {
                Debug.LogWarning($"找不到Item配置: {itemId}");
                return;
            }
            
            item.Initialize(
                item.ItemId,
                config.Id,
                config.Name,
                (ItemType)config.Itemtype,
                (ItemQuality)config.Quality
            );
        }
        
        #endregion
        
        #region 武器配置
        
        /// <summary>
        /// 获取武器配置
        /// </summary>
        public static WeaponData GetWeaponConfig(int weaponId)
        {
            return ExcelDataManager.GetWeaponById(weaponId);
        }
        
        /// <summary>
        /// 初始化武器
        /// </summary>
        public static void InitWeaponFromConfig(Weapon weapon, int weaponId)
        {
            var config = ExcelDataManager.GetWeaponById(weaponId);
            if (config == null)
            {
                Debug.LogWarning($"找不到Weapon配置: {weaponId}");
                return;
            }
            
            weapon.InitializeWeapon(
                weapon.ItemId,
                config.Id,
                config.Name,
                (ItemQuality)config.Quality,
                (WeaponType)config.Weapontype,
                (ElementType)config.Elementtype,
                config.Damage,
                config.Firerate,
                config.Range,
                config.Magazinesize,
                config.Reloadtime
            );
            
            weapon.SetWeaponParams(
                config.Spread,
                config.Recoil,
                config.Penetration,
                config.Pelletcount
            );
            
            weapon.SetStats(
                config.Healthbonus,
                config.Attackbonus,
                config.Defensebonus,
                config.Critratebonus
            );
        }
        
        #endregion
        
        #region 装备配置
        
        /// <summary>
        /// 获取装备配置
        /// </summary>
        public static EquipmentData GetEquipmentConfig(int equipmentId)
        {
            return ExcelDataManager.GetEquipmentById(equipmentId);
        }
        
        /// <summary>
        /// 初始化装备
        /// </summary>
        public static void InitEquipmentFromConfig(Equipment equipment, int equipmentId)
        {
            var config = ExcelDataManager.GetEquipmentById(equipmentId);
            if (config == null)
            {
                Debug.LogWarning($"找不到Equipment配置: {equipmentId}");
                return;
            }
            
            equipment.InitializeEquipment(
                equipment.ItemId,
                config.Id,
                config.Name,
                (ItemQuality)config.Quality,
                (EquipmentSlot)config.Slot,
                config.Requiredlevel
            );
            
            equipment.SetStats(
                config.Healthbonus,
                config.Attackbonus,
                config.Defensebonus,
                config.Critratebonus,
                config.Critdamagebonus,
                config.Dodgeratebonus,
                config.Movespeedbonus,
                config.Attackspeedbonus
            );
            
            equipment.SetResistances(
                config.Freezeresistance,
                config.Poisonresistance,
                config.Fireresistance
            );
        }
        
        #endregion
        
        #region 消耗品配置
        
        /// <summary>
        /// 获取消耗品配置
        /// </summary>
        public static ConsumableData GetConsumableConfig(int consumableId)
        {
            return ExcelDataManager.GetConsumableById(consumableId);
        }
        
        /// <summary>
        /// 初始化消耗品
        /// </summary>
        public static void InitConsumableFromConfig(Consumable consumable, int consumableId)
        {
            var config = ExcelDataManager.GetConsumableById(consumableId);
            if (config == null)
            {
                Debug.LogWarning($"找不到Consumable配置: {consumableId}");
                return;
            }
            
            // 获取对应的物品配置
            var itemConfig = ExcelDataManager.GetItemById(config.Itemid);
            string name = itemConfig?.Name ?? $"Consumable_{consumableId}";
            ItemQuality quality = itemConfig != null ? (ItemQuality)itemConfig.Quality : ItemQuality.Common;
            
            consumable.InitializeConsumable(
                consumable.ItemId,
                config.Itemid,
                name,
                quality,
                (ConsumableEffectType)config.Effecttype,
                config.Effectvalue,
                config.Duration,
                (BuffType)config.Bufftype
            );
        }
        
        #endregion
        
        #region Buff配置
        
        /// <summary>
        /// 获取Buff配置
        /// </summary>
        public static ExcelImporter.BuffData GetBuffConfig(int buffId)
        {
            return ExcelDataManager.GetBuffById(buffId);
        }
        
        /// <summary>
        /// 创建Buff数据
        /// </summary>
        public static Data.BuffData CreateBuffFromConfig(int buffId, int sourceActorId = 0)
        {
            var config = ExcelDataManager.GetBuffById(buffId);
            if (config == null)
            {
                Debug.LogWarning($"找不到Buff配置: {buffId}");
                return null;
            }
            
            return new Data.BuffData(
                (BuffType)config.Bufftype,
                config.Defaultvalue,
                config.Defaultduration,
                config.Maxstack,
                sourceActorId
            );
        }
        
        #endregion
        
        #region 刷怪点配置
        
        /// <summary>
        /// 获取所有刷怪点配置
        /// </summary>
        public static List<SpawnPointConfig> GetAllSpawnPointConfigs()
        {
            var configs = ExcelDataManager.GetAllSpawnPoint();
            var result = new List<SpawnPointConfig>();
            
            foreach (var config in configs)
            {
                result.Add(new SpawnPointConfig
                {
                    ConfigId = config.Monsterid,
                    Position = new Vector3(config.Posx, config.Posy, config.Posz),
                    RespawnTime = (float)config.Respawntime,
                    MaxCount = config.Maxcount,
                    MonsterType = GetMonsterType(config.Monsterid)
                });
            }
            
            return result;
        }
        
        #endregion
        
        #region 工具方法
        
        /// <summary>
        /// 解析逗号分隔的整数列表
        /// </summary>
        public static List<int> ParseIntList(string input, char separator = ',')
        {
            var result = new List<int>();
            if (string.IsNullOrEmpty(input)) return result;
            
            var parts = input.Split(separator);
            foreach (var part in parts)
            {
                if (int.TryParse(part.Trim(), out int value))
                {
                    result.Add(value);
                }
            }
            return result;
        }
        
        /// <summary>
        /// 解析逗号分隔的浮点数列表
        /// </summary>
        public static List<float> ParseFloatList(string input, char separator = ',')
        {
            var result = new List<float>();
            if (string.IsNullOrEmpty(input)) return result;
            
            var parts = input.Split(separator);
            foreach (var part in parts)
            {
                if (float.TryParse(part.Trim(), out float value))
                {
                    result.Add(value);
                }
            }
            return result;
        }
        
        /// <summary>
        /// 解析分隔的字符串列表
        /// </summary>
        public static List<string> ParseStringList(string input, char separator = ',')
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(input)) return result;
            
            var parts = input.Split(separator);
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    result.Add(trimmed);
                }
            }
            return result;
        }
        
        #endregion
    }
}

