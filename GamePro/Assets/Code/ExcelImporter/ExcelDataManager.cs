using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ExcelImporter
{
    /// <summary>
    /// Excel数据管理器 - 一次性加载所有数据
    /// </summary>
    public static class ExcelDataManager
    {
        private static bool _isInitialized = false;
        private const BindingFlags FieldFlags = BindingFlags.Public | BindingFlags.Instance;

        private static Dictionary<int, ActorData> _actorDict = new Dictionary<int, ActorData>();
        private static List<ActorData> _actorList = new List<ActorData>();

        private static Dictionary<int, BuffData> _buffDict = new Dictionary<int, BuffData>();
        private static List<BuffData> _buffList = new List<BuffData>();

        private static Dictionary<int, ConsumableData> _consumableDict = new Dictionary<int, ConsumableData>();
        private static List<ConsumableData> _consumableList = new List<ConsumableData>();

        private static Dictionary<int, EquipmentData> _equipmentDict = new Dictionary<int, EquipmentData>();
        private static List<EquipmentData> _equipmentList = new List<EquipmentData>();

        private static Dictionary<int, ItemData> _itemDict = new Dictionary<int, ItemData>();
        private static List<ItemData> _itemList = new List<ItemData>();

        private static Dictionary<int, MonsterData> _monsterDict = new Dictionary<int, MonsterData>();
        private static List<MonsterData> _monsterList = new List<MonsterData>();

        private static Dictionary<int, NpcData> _npcDict = new Dictionary<int, NpcData>();
        private static List<NpcData> _npcList = new List<NpcData>();

        private static Dictionary<int, QuestData> _questDict = new Dictionary<int, QuestData>();
        private static List<QuestData> _questList = new List<QuestData>();

        private static Dictionary<int, SpawnpointData> _spawnpointDict = new Dictionary<int, SpawnpointData>();
        private static List<SpawnpointData> _spawnpointList = new List<SpawnpointData>();

        private static Dictionary<int, WeaponData> _weaponDict = new Dictionary<int, WeaponData>();
        private static List<WeaponData> _weaponList = new List<WeaponData>();

        /// <summary>
        /// 初始化并加载所有数据（只需调用一次）
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("ExcelDataManager已经初始化过了");
                return;
            }

            // 加载Actor数据
            var actorJson = Resources.Load<TextAsset>("ExcelImporter/Actor");
            if (actorJson != null && !string.IsNullOrEmpty(actorJson.text))
            {
                try
                {
                    // Unity JsonUtility要求JSON格式必须严格匹配类结构
                    // 确保JSON字符串格式正确
                    var jsonText = actorJson.text.Trim();
                    if (string.IsNullOrEmpty(jsonText))
                    {
                        Debug.LogError($"加载Actor数据失败: JSON内容为空");
                        return;
                    }
                    
                    // Unity JsonUtility对数组反序列化有已知问题，使用包装类方式
                    // 注意：JSON格式必须是 {"items":[...]} 格式
                    var actorArray = JsonUtility.FromJson<ActorDataArray>(jsonText);
                    
                    if (actorArray == null)
                    {
                        Debug.LogError($"加载Actor数据失败: 反序列化结果为null");
                        Debug.LogError($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        return;
                    }
                    
                    if (actorArray.items == null)
                    {
                        Debug.LogWarning($"加载Actor数据失败: items数组为null");
                        Debug.LogWarning($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        // 尝试手动解析JSON（备用方案）
                        Debug.LogWarning("尝试备用解析方案...");
                        return;
                    }
                    
                    Debug.Log($"加载Actor数据: { actorArray.items.Length } 条记录");
                    
                    int loadedCount = 0;
                    int nullCount = 0;
                    foreach (var item in actorArray.items)
                    {
                        if (item != null)
                        {
                            _actorList.Add(item);
                            loadedCount++;
                            
                            // 尝试使用Id字段，如果没有则使用第一个字段
                            var idValue = GetIdValue(item);
                            if (idValue != null && idValue != 0)
                            {
                                _actorDict[idValue.Value] = item;
                            }
                        }
                        else
                        {
                            nullCount++;
                        }
                    }
                    
                    if (nullCount > 0)
                    {
                        Debug.LogWarning($"加载Actor数据时发现 {nullCount} 个null项");
                    }
                    
                    Debug.Log($"成功加载Actor数据: {loadedCount} 条记录到列表，{ _actorDict.Count } 条记录到字典");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"加载Actor数据时发生错误: {e.Message}");
                    Debug.LogError($"堆栈跟踪: {e.StackTrace}");
                    if (actorJson.text.Length > 0)
                    {
                        Debug.LogError($"JSON前500字符: { actorJson.text.Substring(0, Math.Min(500, actorJson.text.Length)) }");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"未找到Actor数据文件或文件为空: ExcelImporter/Actor");
            }

            // 加载Buff数据
            var buffJson = Resources.Load<TextAsset>("ExcelImporter/Buff");
            if (buffJson != null && !string.IsNullOrEmpty(buffJson.text))
            {
                try
                {
                    // Unity JsonUtility要求JSON格式必须严格匹配类结构
                    // 确保JSON字符串格式正确
                    var jsonText = buffJson.text.Trim();
                    if (string.IsNullOrEmpty(jsonText))
                    {
                        Debug.LogError($"加载Buff数据失败: JSON内容为空");
                        return;
                    }
                    
                    // Unity JsonUtility对数组反序列化有已知问题，使用包装类方式
                    // 注意：JSON格式必须是 {"items":[...]} 格式
                    var buffArray = JsonUtility.FromJson<BuffDataArray>(jsonText);
                    
                    if (buffArray == null)
                    {
                        Debug.LogError($"加载Buff数据失败: 反序列化结果为null");
                        Debug.LogError($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        return;
                    }
                    
                    if (buffArray.items == null)
                    {
                        Debug.LogWarning($"加载Buff数据失败: items数组为null");
                        Debug.LogWarning($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        // 尝试手动解析JSON（备用方案）
                        Debug.LogWarning("尝试备用解析方案...");
                        return;
                    }
                    
                    Debug.Log($"加载Buff数据: { buffArray.items.Length } 条记录");
                    
                    int loadedCount = 0;
                    int nullCount = 0;
                    foreach (var item in buffArray.items)
                    {
                        if (item != null)
                        {
                            _buffList.Add(item);
                            loadedCount++;
                            
                            // 尝试使用Id字段，如果没有则使用第一个字段
                            var idValue = GetIdValue(item);
                            if (idValue != null && idValue != 0)
                            {
                                _buffDict[idValue.Value] = item;
                            }
                        }
                        else
                        {
                            nullCount++;
                        }
                    }
                    
                    if (nullCount > 0)
                    {
                        Debug.LogWarning($"加载Buff数据时发现 {nullCount} 个null项");
                    }
                    
                    Debug.Log($"成功加载Buff数据: {loadedCount} 条记录到列表，{ _buffDict.Count } 条记录到字典");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"加载Buff数据时发生错误: {e.Message}");
                    Debug.LogError($"堆栈跟踪: {e.StackTrace}");
                    if (buffJson.text.Length > 0)
                    {
                        Debug.LogError($"JSON前500字符: { buffJson.text.Substring(0, Math.Min(500, buffJson.text.Length)) }");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"未找到Buff数据文件或文件为空: ExcelImporter/Buff");
            }

            // 加载Consumable数据
            var consumableJson = Resources.Load<TextAsset>("ExcelImporter/Consumable");
            if (consumableJson != null && !string.IsNullOrEmpty(consumableJson.text))
            {
                try
                {
                    // Unity JsonUtility要求JSON格式必须严格匹配类结构
                    // 确保JSON字符串格式正确
                    var jsonText = consumableJson.text.Trim();
                    if (string.IsNullOrEmpty(jsonText))
                    {
                        Debug.LogError($"加载Consumable数据失败: JSON内容为空");
                        return;
                    }
                    
                    // Unity JsonUtility对数组反序列化有已知问题，使用包装类方式
                    // 注意：JSON格式必须是 {"items":[...]} 格式
                    var consumableArray = JsonUtility.FromJson<ConsumableDataArray>(jsonText);
                    
                    if (consumableArray == null)
                    {
                        Debug.LogError($"加载Consumable数据失败: 反序列化结果为null");
                        Debug.LogError($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        return;
                    }
                    
                    if (consumableArray.items == null)
                    {
                        Debug.LogWarning($"加载Consumable数据失败: items数组为null");
                        Debug.LogWarning($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        // 尝试手动解析JSON（备用方案）
                        Debug.LogWarning("尝试备用解析方案...");
                        return;
                    }
                    
                    Debug.Log($"加载Consumable数据: { consumableArray.items.Length } 条记录");
                    
                    int loadedCount = 0;
                    int nullCount = 0;
                    foreach (var item in consumableArray.items)
                    {
                        if (item != null)
                        {
                            _consumableList.Add(item);
                            loadedCount++;
                            
                            // 尝试使用Id字段，如果没有则使用第一个字段
                            var idValue = GetIdValue(item);
                            if (idValue != null && idValue != 0)
                            {
                                _consumableDict[idValue.Value] = item;
                            }
                        }
                        else
                        {
                            nullCount++;
                        }
                    }
                    
                    if (nullCount > 0)
                    {
                        Debug.LogWarning($"加载Consumable数据时发现 {nullCount} 个null项");
                    }
                    
                    Debug.Log($"成功加载Consumable数据: {loadedCount} 条记录到列表，{ _consumableDict.Count } 条记录到字典");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"加载Consumable数据时发生错误: {e.Message}");
                    Debug.LogError($"堆栈跟踪: {e.StackTrace}");
                    if (consumableJson.text.Length > 0)
                    {
                        Debug.LogError($"JSON前500字符: { consumableJson.text.Substring(0, Math.Min(500, consumableJson.text.Length)) }");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"未找到Consumable数据文件或文件为空: ExcelImporter/Consumable");
            }

            // 加载Equipment数据
            var equipmentJson = Resources.Load<TextAsset>("ExcelImporter/Equipment");
            if (equipmentJson != null && !string.IsNullOrEmpty(equipmentJson.text))
            {
                try
                {
                    // Unity JsonUtility要求JSON格式必须严格匹配类结构
                    // 确保JSON字符串格式正确
                    var jsonText = equipmentJson.text.Trim();
                    if (string.IsNullOrEmpty(jsonText))
                    {
                        Debug.LogError($"加载Equipment数据失败: JSON内容为空");
                        return;
                    }
                    
                    // Unity JsonUtility对数组反序列化有已知问题，使用包装类方式
                    // 注意：JSON格式必须是 {"items":[...]} 格式
                    var equipmentArray = JsonUtility.FromJson<EquipmentDataArray>(jsonText);
                    
                    if (equipmentArray == null)
                    {
                        Debug.LogError($"加载Equipment数据失败: 反序列化结果为null");
                        Debug.LogError($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        return;
                    }
                    
                    if (equipmentArray.items == null)
                    {
                        Debug.LogWarning($"加载Equipment数据失败: items数组为null");
                        Debug.LogWarning($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        // 尝试手动解析JSON（备用方案）
                        Debug.LogWarning("尝试备用解析方案...");
                        return;
                    }
                    
                    Debug.Log($"加载Equipment数据: { equipmentArray.items.Length } 条记录");
                    
                    int loadedCount = 0;
                    int nullCount = 0;
                    foreach (var item in equipmentArray.items)
                    {
                        if (item != null)
                        {
                            _equipmentList.Add(item);
                            loadedCount++;
                            
                            // 尝试使用Id字段，如果没有则使用第一个字段
                            var idValue = GetIdValue(item);
                            if (idValue != null && idValue != 0)
                            {
                                _equipmentDict[idValue.Value] = item;
                            }
                        }
                        else
                        {
                            nullCount++;
                        }
                    }
                    
                    if (nullCount > 0)
                    {
                        Debug.LogWarning($"加载Equipment数据时发现 {nullCount} 个null项");
                    }
                    
                    Debug.Log($"成功加载Equipment数据: {loadedCount} 条记录到列表，{ _equipmentDict.Count } 条记录到字典");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"加载Equipment数据时发生错误: {e.Message}");
                    Debug.LogError($"堆栈跟踪: {e.StackTrace}");
                    if (equipmentJson.text.Length > 0)
                    {
                        Debug.LogError($"JSON前500字符: { equipmentJson.text.Substring(0, Math.Min(500, equipmentJson.text.Length)) }");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"未找到Equipment数据文件或文件为空: ExcelImporter/Equipment");
            }

            // 加载Item数据
            var itemJson = Resources.Load<TextAsset>("ExcelImporter/Item");
            if (itemJson != null && !string.IsNullOrEmpty(itemJson.text))
            {
                try
                {
                    // Unity JsonUtility要求JSON格式必须严格匹配类结构
                    // 确保JSON字符串格式正确
                    var jsonText = itemJson.text.Trim();
                    if (string.IsNullOrEmpty(jsonText))
                    {
                        Debug.LogError($"加载Item数据失败: JSON内容为空");
                        return;
                    }
                    
                    // Unity JsonUtility对数组反序列化有已知问题，使用包装类方式
                    // 注意：JSON格式必须是 {"items":[...]} 格式
                    var itemArray = JsonUtility.FromJson<ItemDataArray>(jsonText);
                    
                    if (itemArray == null)
                    {
                        Debug.LogError($"加载Item数据失败: 反序列化结果为null");
                        Debug.LogError($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        return;
                    }
                    
                    if (itemArray.items == null)
                    {
                        Debug.LogWarning($"加载Item数据失败: items数组为null");
                        Debug.LogWarning($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        // 尝试手动解析JSON（备用方案）
                        Debug.LogWarning("尝试备用解析方案...");
                        return;
                    }
                    
                    Debug.Log($"加载Item数据: { itemArray.items.Length } 条记录");
                    
                    int loadedCount = 0;
                    int nullCount = 0;
                    foreach (var item in itemArray.items)
                    {
                        if (item != null)
                        {
                            _itemList.Add(item);
                            loadedCount++;
                            
                            // 尝试使用Id字段，如果没有则使用第一个字段
                            var idValue = GetIdValue(item);
                            if (idValue != null && idValue != 0)
                            {
                                _itemDict[idValue.Value] = item;
                            }
                        }
                        else
                        {
                            nullCount++;
                        }
                    }
                    
                    if (nullCount > 0)
                    {
                        Debug.LogWarning($"加载Item数据时发现 {nullCount} 个null项");
                    }
                    
                    Debug.Log($"成功加载Item数据: {loadedCount} 条记录到列表，{ _itemDict.Count } 条记录到字典");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"加载Item数据时发生错误: {e.Message}");
                    Debug.LogError($"堆栈跟踪: {e.StackTrace}");
                    if (itemJson.text.Length > 0)
                    {
                        Debug.LogError($"JSON前500字符: { itemJson.text.Substring(0, Math.Min(500, itemJson.text.Length)) }");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"未找到Item数据文件或文件为空: ExcelImporter/Item");
            }

            // 加载Monster数据
            var monsterJson = Resources.Load<TextAsset>("ExcelImporter/Monster");
            if (monsterJson != null && !string.IsNullOrEmpty(monsterJson.text))
            {
                try
                {
                    // Unity JsonUtility要求JSON格式必须严格匹配类结构
                    // 确保JSON字符串格式正确
                    var jsonText = monsterJson.text.Trim();
                    if (string.IsNullOrEmpty(jsonText))
                    {
                        Debug.LogError($"加载Monster数据失败: JSON内容为空");
                        return;
                    }
                    
                    // Unity JsonUtility对数组反序列化有已知问题，使用包装类方式
                    // 注意：JSON格式必须是 {"items":[...]} 格式
                    var monsterArray = JsonUtility.FromJson<MonsterDataArray>(jsonText);
                    
                    if (monsterArray == null)
                    {
                        Debug.LogError($"加载Monster数据失败: 反序列化结果为null");
                        Debug.LogError($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        return;
                    }
                    
                    if (monsterArray.items == null)
                    {
                        Debug.LogWarning($"加载Monster数据失败: items数组为null");
                        Debug.LogWarning($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        // 尝试手动解析JSON（备用方案）
                        Debug.LogWarning("尝试备用解析方案...");
                        return;
                    }
                    
                    Debug.Log($"加载Monster数据: { monsterArray.items.Length } 条记录");
                    
                    int loadedCount = 0;
                    int nullCount = 0;
                    foreach (var item in monsterArray.items)
                    {
                        if (item != null)
                        {
                            _monsterList.Add(item);
                            loadedCount++;
                            
                            // 尝试使用Id字段，如果没有则使用第一个字段
                            var idValue = GetIdValue(item);
                            if (idValue != null && idValue != 0)
                            {
                                _monsterDict[idValue.Value] = item;
                            }
                        }
                        else
                        {
                            nullCount++;
                        }
                    }
                    
                    if (nullCount > 0)
                    {
                        Debug.LogWarning($"加载Monster数据时发现 {nullCount} 个null项");
                    }
                    
                    Debug.Log($"成功加载Monster数据: {loadedCount} 条记录到列表，{ _monsterDict.Count } 条记录到字典");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"加载Monster数据时发生错误: {e.Message}");
                    Debug.LogError($"堆栈跟踪: {e.StackTrace}");
                    if (monsterJson.text.Length > 0)
                    {
                        Debug.LogError($"JSON前500字符: { monsterJson.text.Substring(0, Math.Min(500, monsterJson.text.Length)) }");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"未找到Monster数据文件或文件为空: ExcelImporter/Monster");
            }

            // 加载NPC数据
            var npcJson = Resources.Load<TextAsset>("ExcelImporter/NPC");
            if (npcJson != null && !string.IsNullOrEmpty(npcJson.text))
            {
                try
                {
                    // Unity JsonUtility要求JSON格式必须严格匹配类结构
                    // 确保JSON字符串格式正确
                    var jsonText = npcJson.text.Trim();
                    if (string.IsNullOrEmpty(jsonText))
                    {
                        Debug.LogError($"加载NPC数据失败: JSON内容为空");
                        return;
                    }
                    
                    // Unity JsonUtility对数组反序列化有已知问题，使用包装类方式
                    // 注意：JSON格式必须是 {"items":[...]} 格式
                    var npcArray = JsonUtility.FromJson<NpcDataArray>(jsonText);
                    
                    if (npcArray == null)
                    {
                        Debug.LogError($"加载NPC数据失败: 反序列化结果为null");
                        Debug.LogError($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        return;
                    }
                    
                    if (npcArray.items == null)
                    {
                        Debug.LogWarning($"加载NPC数据失败: items数组为null");
                        Debug.LogWarning($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        // 尝试手动解析JSON（备用方案）
                        Debug.LogWarning("尝试备用解析方案...");
                        return;
                    }
                    
                    Debug.Log($"加载NPC数据: { npcArray.items.Length } 条记录");
                    
                    int loadedCount = 0;
                    int nullCount = 0;
                    foreach (var item in npcArray.items)
                    {
                        if (item != null)
                        {
                            _npcList.Add(item);
                            loadedCount++;
                            
                            // 尝试使用Id字段，如果没有则使用第一个字段
                            var idValue = GetIdValue(item);
                            if (idValue != null && idValue != 0)
                            {
                                _npcDict[idValue.Value] = item;
                            }
                        }
                        else
                        {
                            nullCount++;
                        }
                    }
                    
                    if (nullCount > 0)
                    {
                        Debug.LogWarning($"加载NPC数据时发现 {nullCount} 个null项");
                    }
                    
                    Debug.Log($"成功加载NPC数据: {loadedCount} 条记录到列表，{ _npcDict.Count } 条记录到字典");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"加载NPC数据时发生错误: {e.Message}");
                    Debug.LogError($"堆栈跟踪: {e.StackTrace}");
                    if (npcJson.text.Length > 0)
                    {
                        Debug.LogError($"JSON前500字符: { npcJson.text.Substring(0, Math.Min(500, npcJson.text.Length)) }");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"未找到NPC数据文件或文件为空: ExcelImporter/NPC");
            }

            // 加载Quest数据
            var questJson = Resources.Load<TextAsset>("ExcelImporter/Quest");
            if (questJson != null && !string.IsNullOrEmpty(questJson.text))
            {
                try
                {
                    // Unity JsonUtility要求JSON格式必须严格匹配类结构
                    // 确保JSON字符串格式正确
                    var jsonText = questJson.text.Trim();
                    if (string.IsNullOrEmpty(jsonText))
                    {
                        Debug.LogError($"加载Quest数据失败: JSON内容为空");
                        return;
                    }
                    
                    // Unity JsonUtility对数组反序列化有已知问题，使用包装类方式
                    // 注意：JSON格式必须是 {"items":[...]} 格式
                    var questArray = JsonUtility.FromJson<QuestDataArray>(jsonText);
                    
                    if (questArray == null)
                    {
                        Debug.LogError($"加载Quest数据失败: 反序列化结果为null");
                        Debug.LogError($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        return;
                    }
                    
                    if (questArray.items == null)
                    {
                        Debug.LogWarning($"加载Quest数据失败: items数组为null");
                        Debug.LogWarning($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        // 尝试手动解析JSON（备用方案）
                        Debug.LogWarning("尝试备用解析方案...");
                        return;
                    }
                    
                    Debug.Log($"加载Quest数据: { questArray.items.Length } 条记录");
                    
                    int loadedCount = 0;
                    int nullCount = 0;
                    foreach (var item in questArray.items)
                    {
                        if (item != null)
                        {
                            _questList.Add(item);
                            loadedCount++;
                            
                            // 尝试使用Id字段，如果没有则使用第一个字段
                            var idValue = GetIdValue(item);
                            if (idValue != null && idValue != 0)
                            {
                                _questDict[idValue.Value] = item;
                            }
                        }
                        else
                        {
                            nullCount++;
                        }
                    }
                    
                    if (nullCount > 0)
                    {
                        Debug.LogWarning($"加载Quest数据时发现 {nullCount} 个null项");
                    }
                    
                    Debug.Log($"成功加载Quest数据: {loadedCount} 条记录到列表，{ _questDict.Count } 条记录到字典");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"加载Quest数据时发生错误: {e.Message}");
                    Debug.LogError($"堆栈跟踪: {e.StackTrace}");
                    if (questJson.text.Length > 0)
                    {
                        Debug.LogError($"JSON前500字符: { questJson.text.Substring(0, Math.Min(500, questJson.text.Length)) }");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"未找到Quest数据文件或文件为空: ExcelImporter/Quest");
            }

            // 加载SpawnPoint数据
            var spawnpointJson = Resources.Load<TextAsset>("ExcelImporter/SpawnPoint");
            if (spawnpointJson != null && !string.IsNullOrEmpty(spawnpointJson.text))
            {
                try
                {
                    // Unity JsonUtility要求JSON格式必须严格匹配类结构
                    // 确保JSON字符串格式正确
                    var jsonText = spawnpointJson.text.Trim();
                    if (string.IsNullOrEmpty(jsonText))
                    {
                        Debug.LogError($"加载SpawnPoint数据失败: JSON内容为空");
                        return;
                    }
                    
                    // Unity JsonUtility对数组反序列化有已知问题，使用包装类方式
                    // 注意：JSON格式必须是 {"items":[...]} 格式
                    var spawnpointArray = JsonUtility.FromJson<SpawnpointDataArray>(jsonText);
                    
                    if (spawnpointArray == null)
                    {
                        Debug.LogError($"加载SpawnPoint数据失败: 反序列化结果为null");
                        Debug.LogError($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        return;
                    }
                    
                    if (spawnpointArray.items == null)
                    {
                        Debug.LogWarning($"加载SpawnPoint数据失败: items数组为null");
                        Debug.LogWarning($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        // 尝试手动解析JSON（备用方案）
                        Debug.LogWarning("尝试备用解析方案...");
                        return;
                    }
                    
                    Debug.Log($"加载SpawnPoint数据: { spawnpointArray.items.Length } 条记录");
                    
                    int loadedCount = 0;
                    int nullCount = 0;
                    foreach (var item in spawnpointArray.items)
                    {
                        if (item != null)
                        {
                            _spawnpointList.Add(item);
                            loadedCount++;
                            
                            // 尝试使用Id字段，如果没有则使用第一个字段
                            var idValue = GetIdValue(item);
                            if (idValue != null && idValue != 0)
                            {
                                _spawnpointDict[idValue.Value] = item;
                            }
                        }
                        else
                        {
                            nullCount++;
                        }
                    }
                    
                    if (nullCount > 0)
                    {
                        Debug.LogWarning($"加载SpawnPoint数据时发现 {nullCount} 个null项");
                    }
                    
                    Debug.Log($"成功加载SpawnPoint数据: {loadedCount} 条记录到列表，{ _spawnpointDict.Count } 条记录到字典");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"加载SpawnPoint数据时发生错误: {e.Message}");
                    Debug.LogError($"堆栈跟踪: {e.StackTrace}");
                    if (spawnpointJson.text.Length > 0)
                    {
                        Debug.LogError($"JSON前500字符: { spawnpointJson.text.Substring(0, Math.Min(500, spawnpointJson.text.Length)) }");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"未找到SpawnPoint数据文件或文件为空: ExcelImporter/SpawnPoint");
            }

            // 加载Weapon数据
            var weaponJson = Resources.Load<TextAsset>("ExcelImporter/Weapon");
            if (weaponJson != null && !string.IsNullOrEmpty(weaponJson.text))
            {
                try
                {
                    // Unity JsonUtility要求JSON格式必须严格匹配类结构
                    // 确保JSON字符串格式正确
                    var jsonText = weaponJson.text.Trim();
                    if (string.IsNullOrEmpty(jsonText))
                    {
                        Debug.LogError($"加载Weapon数据失败: JSON内容为空");
                        return;
                    }
                    
                    // Unity JsonUtility对数组反序列化有已知问题，使用包装类方式
                    // 注意：JSON格式必须是 {"items":[...]} 格式
                    var weaponArray = JsonUtility.FromJson<WeaponDataArray>(jsonText);
                    
                    if (weaponArray == null)
                    {
                        Debug.LogError($"加载Weapon数据失败: 反序列化结果为null");
                        Debug.LogError($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        return;
                    }
                    
                    if (weaponArray.items == null)
                    {
                        Debug.LogWarning($"加载Weapon数据失败: items数组为null");
                        Debug.LogWarning($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        // 尝试手动解析JSON（备用方案）
                        Debug.LogWarning("尝试备用解析方案...");
                        return;
                    }
                    
                    Debug.Log($"加载Weapon数据: { weaponArray.items.Length } 条记录");
                    
                    int loadedCount = 0;
                    int nullCount = 0;
                    foreach (var item in weaponArray.items)
                    {
                        if (item != null)
                        {
                            _weaponList.Add(item);
                            loadedCount++;
                            
                            // 尝试使用Id字段，如果没有则使用第一个字段
                            var idValue = GetIdValue(item);
                            if (idValue != null && idValue != 0)
                            {
                                _weaponDict[idValue.Value] = item;
                            }
                        }
                        else
                        {
                            nullCount++;
                        }
                    }
                    
                    if (nullCount > 0)
                    {
                        Debug.LogWarning($"加载Weapon数据时发现 {nullCount} 个null项");
                    }
                    
                    Debug.Log($"成功加载Weapon数据: {loadedCount} 条记录到列表，{ _weaponDict.Count } 条记录到字典");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"加载Weapon数据时发生错误: {e.Message}");
                    Debug.LogError($"堆栈跟踪: {e.StackTrace}");
                    if (weaponJson.text.Length > 0)
                    {
                        Debug.LogError($"JSON前500字符: { weaponJson.text.Substring(0, Math.Min(500, weaponJson.text.Length)) }");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"未找到Weapon数据文件或文件为空: ExcelImporter/Weapon");
            }

            _isInitialized = true;
            Debug.Log("ExcelDataManager初始化完成");
        }
        
        /// <summary>
        /// 获取对象的ID值（支持Id、ID、id字段，以及所有可能的变体）
        /// </summary>
        private static int? GetIdValue(object obj)
        {
            if (obj == null) return null;
            
            var type = obj.GetType();
            // Unity JsonUtility 反序列化后是字段，不是属性，所以使用 GetField
            // 尝试多种可能的ID字段名
            var idFieldNames = new[] { "Id", "ID", "id", "iD", "ID_", "Id_", "id_" };
            
            foreach (var fieldName in idFieldNames)
            {
                var idField = type.GetField(fieldName, FieldFlags);
                if (idField != null)
                {
                    var value = idField.GetValue(obj);
                    if (value is int intValue && intValue != 0)
                        return intValue;
                    if (value != null && int.TryParse(value.ToString(), out int parsedValue) && parsedValue != 0)
                        return parsedValue;
                }
            }
            
            // 如果标准ID字段都没找到，尝试查找所有字段，看是否有包含"id"的字段
            var allFields = type.GetFields(FieldFlags);
            foreach (var field in allFields)
            {
                var fieldName = field.Name;
                if (fieldName.Equals("Id", StringComparison.OrdinalIgnoreCase) || 
                    fieldName.ToLower().Contains("id"))
                {
                    var value = field.GetValue(obj);
                    if (value is int intValue && intValue != 0)
                        return intValue;
                    if (value != null && int.TryParse(value.ToString(), out int parsedValue) && parsedValue != 0)
                        return parsedValue;
                }
            }
            
            return null;
        }

        /// <summary>
        /// 通过ID获取Actor数据
        /// </summary>
        public static ActorData GetActorById(int id)
        {
            if (!_isInitialized) Initialize();
            _actorDict.TryGetValue(id, out var data);
            return data;
        }
        
        /// <summary>
        /// 获取所有Actor数据
        /// </summary>
        public static List<ActorData> GetAllActor()
        {
            if (!_isInitialized) Initialize();
            return _actorList;
        }
        
        /// <summary>
        /// 查询Actor数据（支持字段查询）
        /// </summary>
        public static List<ActorData> QueryActor(Func<ActorData, bool> predicate)
        {
            if (!_isInitialized) Initialize();
            return _actorList.Where(predicate).ToList();
        }

        /// <summary>
        /// 通过ID获取Buff数据
        /// </summary>
        public static BuffData GetBuffById(int id)
        {
            if (!_isInitialized) Initialize();
            _buffDict.TryGetValue(id, out var data);
            return data;
        }
        
        /// <summary>
        /// 获取所有Buff数据
        /// </summary>
        public static List<BuffData> GetAllBuff()
        {
            if (!_isInitialized) Initialize();
            return _buffList;
        }
        
        /// <summary>
        /// 查询Buff数据（支持字段查询）
        /// </summary>
        public static List<BuffData> QueryBuff(Func<BuffData, bool> predicate)
        {
            if (!_isInitialized) Initialize();
            return _buffList.Where(predicate).ToList();
        }

        /// <summary>
        /// 通过ID获取Consumable数据
        /// </summary>
        public static ConsumableData GetConsumableById(int id)
        {
            if (!_isInitialized) Initialize();
            _consumableDict.TryGetValue(id, out var data);
            return data;
        }
        
        /// <summary>
        /// 获取所有Consumable数据
        /// </summary>
        public static List<ConsumableData> GetAllConsumable()
        {
            if (!_isInitialized) Initialize();
            return _consumableList;
        }
        
        /// <summary>
        /// 查询Consumable数据（支持字段查询）
        /// </summary>
        public static List<ConsumableData> QueryConsumable(Func<ConsumableData, bool> predicate)
        {
            if (!_isInitialized) Initialize();
            return _consumableList.Where(predicate).ToList();
        }

        /// <summary>
        /// 通过ID获取Equipment数据
        /// </summary>
        public static EquipmentData GetEquipmentById(int id)
        {
            if (!_isInitialized) Initialize();
            _equipmentDict.TryGetValue(id, out var data);
            return data;
        }
        
        /// <summary>
        /// 获取所有Equipment数据
        /// </summary>
        public static List<EquipmentData> GetAllEquipment()
        {
            if (!_isInitialized) Initialize();
            return _equipmentList;
        }
        
        /// <summary>
        /// 查询Equipment数据（支持字段查询）
        /// </summary>
        public static List<EquipmentData> QueryEquipment(Func<EquipmentData, bool> predicate)
        {
            if (!_isInitialized) Initialize();
            return _equipmentList.Where(predicate).ToList();
        }

        /// <summary>
        /// 通过ID获取Item数据
        /// </summary>
        public static ItemData GetItemById(int id)
        {
            if (!_isInitialized) Initialize();
            _itemDict.TryGetValue(id, out var data);
            return data;
        }
        
        /// <summary>
        /// 获取所有Item数据
        /// </summary>
        public static List<ItemData> GetAllItem()
        {
            if (!_isInitialized) Initialize();
            return _itemList;
        }
        
        /// <summary>
        /// 查询Item数据（支持字段查询）
        /// </summary>
        public static List<ItemData> QueryItem(Func<ItemData, bool> predicate)
        {
            if (!_isInitialized) Initialize();
            return _itemList.Where(predicate).ToList();
        }

        /// <summary>
        /// 通过ID获取Monster数据
        /// </summary>
        public static MonsterData GetMonsterById(int id)
        {
            if (!_isInitialized) Initialize();
            _monsterDict.TryGetValue(id, out var data);
            return data;
        }
        
        /// <summary>
        /// 获取所有Monster数据
        /// </summary>
        public static List<MonsterData> GetAllMonster()
        {
            if (!_isInitialized) Initialize();
            return _monsterList;
        }
        
        /// <summary>
        /// 查询Monster数据（支持字段查询）
        /// </summary>
        public static List<MonsterData> QueryMonster(Func<MonsterData, bool> predicate)
        {
            if (!_isInitialized) Initialize();
            return _monsterList.Where(predicate).ToList();
        }

        /// <summary>
        /// 通过ID获取NPC数据
        /// </summary>
        public static NpcData GetNPCById(int id)
        {
            if (!_isInitialized) Initialize();
            _npcDict.TryGetValue(id, out var data);
            return data;
        }
        
        /// <summary>
        /// 获取所有NPC数据
        /// </summary>
        public static List<NpcData> GetAllNPC()
        {
            if (!_isInitialized) Initialize();
            return _npcList;
        }
        
        /// <summary>
        /// 查询NPC数据（支持字段查询）
        /// </summary>
        public static List<NpcData> QueryNPC(Func<NpcData, bool> predicate)
        {
            if (!_isInitialized) Initialize();
            return _npcList.Where(predicate).ToList();
        }

        /// <summary>
        /// 通过ID获取Quest数据
        /// </summary>
        public static QuestData GetQuestById(int id)
        {
            if (!_isInitialized) Initialize();
            _questDict.TryGetValue(id, out var data);
            return data;
        }
        
        /// <summary>
        /// 获取所有Quest数据
        /// </summary>
        public static List<QuestData> GetAllQuest()
        {
            if (!_isInitialized) Initialize();
            return _questList;
        }
        
        /// <summary>
        /// 查询Quest数据（支持字段查询）
        /// </summary>
        public static List<QuestData> QueryQuest(Func<QuestData, bool> predicate)
        {
            if (!_isInitialized) Initialize();
            return _questList.Where(predicate).ToList();
        }

        /// <summary>
        /// 通过ID获取SpawnPoint数据
        /// </summary>
        public static SpawnpointData GetSpawnPointById(int id)
        {
            if (!_isInitialized) Initialize();
            _spawnpointDict.TryGetValue(id, out var data);
            return data;
        }
        
        /// <summary>
        /// 获取所有SpawnPoint数据
        /// </summary>
        public static List<SpawnpointData> GetAllSpawnPoint()
        {
            if (!_isInitialized) Initialize();
            return _spawnpointList;
        }
        
        /// <summary>
        /// 查询SpawnPoint数据（支持字段查询）
        /// </summary>
        public static List<SpawnpointData> QuerySpawnPoint(Func<SpawnpointData, bool> predicate)
        {
            if (!_isInitialized) Initialize();
            return _spawnpointList.Where(predicate).ToList();
        }

        /// <summary>
        /// 通过ID获取Weapon数据
        /// </summary>
        public static WeaponData GetWeaponById(int id)
        {
            if (!_isInitialized) Initialize();
            _weaponDict.TryGetValue(id, out var data);
            return data;
        }
        
        /// <summary>
        /// 获取所有Weapon数据
        /// </summary>
        public static List<WeaponData> GetAllWeapon()
        {
            if (!_isInitialized) Initialize();
            return _weaponList;
        }
        
        /// <summary>
        /// 查询Weapon数据（支持字段查询）
        /// </summary>
        public static List<WeaponData> QueryWeapon(Func<WeaponData, bool> predicate)
        {
            if (!_isInitialized) Initialize();
            return _weaponList.Where(predicate).ToList();
        }
    }
}
