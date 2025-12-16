#!/bin/bash

cd "$(dirname "$0")"

echo "========================================"
echo "Excel数据导出工具"
echo "========================================"
echo ""

python3 excel_to_unity.py

if [ $? -ne 0 ]; then
    echo ""
    echo "错误: 导出失败！"
    echo "请确保已安装Python和openpyxl库"
    echo "安装命令: pip install -r requirements.txt"
    exit 1
fi

echo ""
read -p "按Enter键退出..."

