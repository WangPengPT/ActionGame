@echo off
chcp 65001 >nul
echo ========================================
echo     启动游戏中继服务器
echo ========================================
echo.

REM 检查是否已编译
if not exist "bin\Debug\net6.0\RelayServer.exe" (
    echo 正在编译服务器...
    dotnet build
    if %ERRORLEVEL% NEQ 0 (
        echo 编译失败！请确保已安装 .NET 6.0 SDK
        pause
        exit /b 1
    )
)

echo 启动服务器（默认端口 7777）...
echo 使用方法: RelayServer.exe [端口号]
echo.
bin\Debug\net6.0\RelayServer.exe %*

pause

