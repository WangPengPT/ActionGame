# Excel数据导出工具

将DataTables目录下的.xlsx文件导出为Unity可用的格式。

## 功能特性

- 自动读取DataTables目录下的所有.xlsx文件
- 导出数据为Unity可用的JSON格式到 `GamePro/Assets/Resources/ExcelImporter`
- 自动生成C#数据类代码到 `GamePro/Assets/Code/ExcelImporter`
- 生成数据管理器，支持一次性加载所有数据
- 支持通过ID直接访问记录
- 支持字段查询功能

## 安装依赖

```bash
pip install -r requirements.txt
```

## 使用方法

1. 将Excel文件（.xlsx格式）放入 `DataTables` 目录
2. 运行导出工具：
   ```bash
   python Tools/ExcelExporter/excel_to_unity.py
   ```
   或使用便捷脚本：
   ```bash
   # Windows
   Tools\ExcelExporter\export.bat
   
   # Linux/Mac
   Tools/ExcelExporter/export.sh
   ```
3. 工具会自动：
   - 解析所有Excel文件
   - 生成JSON数据文件到 `GamePro/Assets/Resources/ExcelImporter`
   - 生成C#代码到 `GamePro/Assets/Code/ExcelImporter`

## Unity中使用

### 初始化数据（只需调用一次）

```csharp
using ExcelImporter;

// 在游戏启动时调用一次
ExcelDataManager.Initialize();
```

### 通过ID获取数据

```csharp
// 假设有一个名为"角色"的表
var characterData = ExcelDataManager.Get角色ById(1);
if (characterData != null)
{
    Debug.Log($"角色名称: {characterData.Name}");
}
```

### 查询数据

```csharp
// 查询所有等级大于10的角色
var highLevelCharacters = ExcelDataManager.Query角色(c => c.Level > 10);

// 查询所有名称包含"战士"的角色
var warriors = ExcelDataManager.Query角色(c => c.Name.Contains("战士"));
```

### 获取所有数据

```csharp
// 获取所有角色数据
var allCharacters = ExcelDataManager.GetAll角色();
```

## Excel文件格式要求

- 文件格式：.xlsx（Excel文件）
- 第一行：表头（字段名）
- 第二行开始：数据行
- 建议包含一个名为"Id"、"ID"或"id"的字段作为主键（用于快速查找）

## 注意事项

- 工具会自动推断数据类型（int, float, bool, string）
- 表名和字段名会自动转换为PascalCase（C#命名规范）
- 如果Excel文件有多个工作表，会为每个工作表生成单独的数据文件
- 数据加载是懒加载的，第一次访问时自动初始化

