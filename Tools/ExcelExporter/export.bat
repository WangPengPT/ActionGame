@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo ========================================
echo Excel数据导出工具
echo ========================================
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

