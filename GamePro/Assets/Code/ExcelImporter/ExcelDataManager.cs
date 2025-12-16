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

        private static Dictionary<int, PlayerData> _playerDict = new Dictionary<int, PlayerData>();
        private static List<PlayerData> _playerList = new List<PlayerData>();

        private static Dictionary<int, WashData> _washDict = new Dictionary<int, WashData>();
        private static List<WashData> _washList = new List<WashData>();

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

            // 加载Player数据
            var playerJson = Resources.Load<TextAsset>("ExcelImporter/Player");
            if (playerJson != null && !string.IsNullOrEmpty(playerJson.text))
            {
                try
                {
                    // Unity JsonUtility要求JSON格式必须严格匹配类结构
                    // 确保JSON字符串格式正确
                    var jsonText = playerJson.text.Trim();
                    if (string.IsNullOrEmpty(jsonText))
                    {
                        Debug.LogError($"加载Player数据失败: JSON内容为空");
                        return;
                    }
                    
                    // Unity JsonUtility对数组反序列化有已知问题，使用包装类方式
                    // 注意：JSON格式必须是 {"items":[...]} 格式
                    var playerArray = JsonUtility.FromJson<PlayerDataArray>(jsonText);
                    
                    if (playerArray == null)
                    {
                        Debug.LogError($"加载Player数据失败: 反序列化结果为null");
                        Debug.LogError($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        return;
                    }
                    
                    if (playerArray.items == null)
                    {
                        Debug.LogWarning($"加载Player数据失败: items数组为null");
                        Debug.LogWarning($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        // 尝试手动解析JSON（备用方案）
                        Debug.LogWarning("尝试备用解析方案...");
                        return;
                    }
                    
                    Debug.Log($"加载Player数据: { playerArray.items.Length } 条记录");
                    
                    int loadedCount = 0;
                    int nullCount = 0;
                    foreach (var item in playerArray.items)
                    {
                        if (item != null)
                        {
                            _playerList.Add(item);
                            loadedCount++;
                            
                            // 尝试使用Id字段，如果没有则使用第一个字段
                            var idValue = GetIdValue(item);
                            if (idValue != null && idValue != 0)
                            {
                                _playerDict[idValue.Value] = item;
                            }
                        }
                        else
                        {
                            nullCount++;
                        }
                    }
                    
                    if (nullCount > 0)
                    {
                        Debug.LogWarning($"加载Player数据时发现 {nullCount} 个null项");
                    }
                    
                    Debug.Log($"成功加载Player数据: {loadedCount} 条记录到列表，{ _playerDict.Count } 条记录到字典");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"加载Player数据时发生错误: {e.Message}");
                    Debug.LogError($"堆栈跟踪: {e.StackTrace}");
                    if (playerJson.text.Length > 0)
                    {
                        Debug.LogError($"JSON前500字符: { playerJson.text.Substring(0, Math.Min(500, playerJson.text.Length)) }");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"未找到Player数据文件或文件为空: ExcelImporter/Player");
            }

            // 加载Wash数据
            var washJson = Resources.Load<TextAsset>("ExcelImporter/Wash");
            if (washJson != null && !string.IsNullOrEmpty(washJson.text))
            {
                try
                {
                    // Unity JsonUtility要求JSON格式必须严格匹配类结构
                    // 确保JSON字符串格式正确
                    var jsonText = washJson.text.Trim();
                    if (string.IsNullOrEmpty(jsonText))
                    {
                        Debug.LogError($"加载Wash数据失败: JSON内容为空");
                        return;
                    }
                    
                    // Unity JsonUtility对数组反序列化有已知问题，使用包装类方式
                    // 注意：JSON格式必须是 {"items":[...]} 格式
                    var washArray = JsonUtility.FromJson<WashDataArray>(jsonText);
                    
                    if (washArray == null)
                    {
                        Debug.LogError($"加载Wash数据失败: 反序列化结果为null");
                        Debug.LogError($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        return;
                    }
                    
                    if (washArray.items == null)
                    {
                        Debug.LogWarning($"加载Wash数据失败: items数组为null");
                        Debug.LogWarning($"JSON前200字符: {jsonText.Substring(0, Math.Min(200, jsonText.Length))}");
                        // 尝试手动解析JSON（备用方案）
                        Debug.LogWarning("尝试备用解析方案...");
                        return;
                    }
                    
                    Debug.Log($"加载Wash数据: { washArray.items.Length } 条记录");
                    
                    int loadedCount = 0;
                    int nullCount = 0;
                    foreach (var item in washArray.items)
                    {
                        if (item != null)
                        {
                            _washList.Add(item);
                            loadedCount++;
                            
                            // 尝试使用Id字段，如果没有则使用第一个字段
                            var idValue = GetIdValue(item);
                            if (idValue != null && idValue != 0)
                            {
                                _washDict[idValue.Value] = item;
                            }
                        }
                        else
                        {
                            nullCount++;
                        }
                    }
                    
                    if (nullCount > 0)
                    {
                        Debug.LogWarning($"加载Wash数据时发现 {nullCount} 个null项");
                    }
                    
                    Debug.Log($"成功加载Wash数据: {loadedCount} 条记录到列表，{ _washDict.Count } 条记录到字典");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"加载Wash数据时发生错误: {e.Message}");
                    Debug.LogError($"堆栈跟踪: {e.StackTrace}");
                    if (washJson.text.Length > 0)
                    {
                        Debug.LogError($"JSON前500字符: { washJson.text.Substring(0, Math.Min(500, washJson.text.Length)) }");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"未找到Wash数据文件或文件为空: ExcelImporter/Wash");
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
        /// 通过ID获取Player数据
        /// </summary>
        public static PlayerData GetPlayerById(int id)
        {
            if (!_isInitialized) Initialize();
            _playerDict.TryGetValue(id, out var data);
            return data;
        }
        
        /// <summary>
        /// 获取所有Player数据
        /// </summary>
        public static List<PlayerData> GetAllPlayer()
        {
            if (!_isInitialized) Initialize();
            return _playerList;
        }
        
        /// <summary>
        /// 查询Player数据（支持字段查询）
        /// </summary>
        public static List<PlayerData> QueryPlayer(Func<PlayerData, bool> predicate)
        {
            if (!_isInitialized) Initialize();
            return _playerList.Where(predicate).ToList();
        }

        /// <summary>
        /// 通过ID获取Wash数据
        /// </summary>
        public static WashData GetWashById(int id)
        {
            if (!_isInitialized) Initialize();
            _washDict.TryGetValue(id, out var data);
            return data;
        }
        
        /// <summary>
        /// 获取所有Wash数据
        /// </summary>
        public static List<WashData> GetAllWash()
        {
            if (!_isInitialized) Initialize();
            return _washList;
        }
        
        /// <summary>
        /// 查询Wash数据（支持字段查询）
        /// </summary>
        public static List<WashData> QueryWash(Func<WashData, bool> predicate)
        {
            if (!_isInitialized) Initialize();
            return _washList.Where(predicate).ToList();
        }
    }
}
