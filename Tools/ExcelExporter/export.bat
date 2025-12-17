@echo off
chcp 65001 >nul
cd /d "%~dp0"
set PYTHONIOENCODING=utf-8

echo ========================================
echo Excel数据导出工具
echo ========================================
echo.

REM 检查是否需要生成配置表
if not exist "..\..\DataTables\Actor.xlsx" (
    echo 检测到配置表不存在，正在生成...
    echo.
    python generate_game_tables.py
    if %ERRORLEVEL% NEQ 0 (
        echo.
        echo 错误: 配置表生成失败！
        pause
        exit /b %ERRORLEVEL%
    )
    echo.
)

echo 正在导出Excel数据到Unity...
echo.
python excel_to_unity.py

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo 错误: 导出失败！
    echo 请确保已安装Python和openpyxl库
    echo 安装命令: pip install -r requirements.txt
    pause
    exit /b %ERRORLEVEL%
)

echo.
pause

