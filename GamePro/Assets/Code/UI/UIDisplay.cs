using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UIDataBinding
{
    /// <summary>
    /// UI显示静态类，包含所有自定义显示方法
    /// 所有显示方法签名: public static void MethodName(Component uiComponent, object value)
    /// </summary>
    public static class UIDisplay
    {
        /// <summary>
        /// 默认显示方法 - 根据UI组件类型自动选择显示方式
        /// </summary>
        public static void Default(Component uiComponent, object value)
        {
            if (uiComponent == null || value == null) return;

            string strValue = value.ToString();

            // 检查Text组件
            if (uiComponent is Text text)
            {
                text.text = strValue;
                return;
            }

            // 检查TextMeshProUGUI组件
            if (uiComponent is TextMeshProUGUI tmpText)
            {
                tmpText.text = strValue;
                return;
            }

            // 检查TextMeshPro组件
            if (uiComponent is TextMeshPro tmp3D)
            {
                tmp3D.text = strValue;
                return;
            }

            // 检查Image组件 - 如果value是Sprite则设置sprite
            if (uiComponent is Image image)
            {
                if (value is Sprite sprite)
                {
                    image.sprite = sprite;
                }
                else if (value is float fillAmount)
                {
                    image.fillAmount = Mathf.Clamp01(fillAmount);
                }
                return;
            }

            // 检查Slider组件
            if (uiComponent is Slider slider)
            {
                if (value is float floatVal)
                {
                    slider.value = floatVal;
                }
                else if (value is int intVal)
                {
                    slider.value = intVal;
                }
                else if (float.TryParse(strValue, out float parsed))
                {
                    slider.value = parsed;
                }
                return;
            }

            // 检查Toggle组件
            if (uiComponent is Toggle toggle)
            {
                if (value is bool boolVal)
                {
                    toggle.isOn = boolVal;
                }
                return;
            }

            // 检查InputField组件
            if (uiComponent is InputField inputField)
            {
                inputField.text = strValue;
                return;
            }

            // 检查TMP_InputField组件
            if (uiComponent is TMP_InputField tmpInputField)
            {
                tmpInputField.text = strValue;
                return;
            }
        }

        /// <summary>
        /// 显示为百分比格式
        /// </summary>
        public static void Percentage(Component uiComponent, object value)
        {
            if (uiComponent == null || value == null) return;

            float floatValue = 0f;
            if (value is float f) floatValue = f;
            else if (value is double d) floatValue = (float)d;
            else if (value is int i) floatValue = i;
            else if (float.TryParse(value.ToString(), out float parsed)) floatValue = parsed;

            string percentStr = $"{floatValue * 100:F1}%";
            SetText(uiComponent, percentStr);
        }

        /// <summary>
        /// 显示为货币格式
        /// </summary>
        public static void Currency(Component uiComponent, object value)
        {
            if (uiComponent == null || value == null) return;

            decimal decValue = 0m;
            if (value is int i) decValue = i;
            else if (value is float f) decValue = (decimal)f;
            else if (value is double d) decValue = (decimal)d;
            else if (value is decimal dec) decValue = dec;
            else if (decimal.TryParse(value.ToString(), out decimal parsed)) decValue = parsed;

            string currencyStr = $"${decValue:N0}";
            SetText(uiComponent, currencyStr);
        }

        /// <summary>
        /// 显示为整数格式（四舍五入）
        /// </summary>
        public static void Integer(Component uiComponent, object value)
        {
            if (uiComponent == null || value == null) return;

            int intValue = 0;
            if (value is int i) intValue = i;
            else if (value is float f) intValue = Mathf.RoundToInt(f);
            else if (value is double d) intValue = (int)Math.Round(d);
            else if (int.TryParse(value.ToString(), out int parsed)) intValue = parsed;

            SetText(uiComponent, intValue.ToString());
        }

        /// <summary>
        /// 显示为保留两位小数的浮点数
        /// </summary>
        public static void Float2(Component uiComponent, object value)
        {
            if (uiComponent == null || value == null) return;

            float floatValue = 0f;
            if (value is float f) floatValue = f;
            else if (value is double d) floatValue = (float)d;
            else if (value is int i) floatValue = i;
            else if (float.TryParse(value.ToString(), out float parsed)) floatValue = parsed;

            SetText(uiComponent, floatValue.ToString("F2"));
        }

        /// <summary>
        /// 显示为时间格式 (mm:ss)
        /// </summary>
        public static void TimeMinuteSecond(Component uiComponent, object value)
        {
            if (uiComponent == null || value == null) return;

            float seconds = 0f;
            if (value is float f) seconds = f;
            else if (value is double d) seconds = (float)d;
            else if (value is int i) seconds = i;
            else if (float.TryParse(value.ToString(), out float parsed)) seconds = parsed;

            int minutes = (int)(seconds / 60);
            int secs = (int)(seconds % 60);
            SetText(uiComponent, $"{minutes:D2}:{secs:D2}");
        }

        /// <summary>
        /// 辅助方法：设置文本内容
        /// </summary>
        private static void SetText(Component uiComponent, string text)
        {
            if (uiComponent is Text uiText)
            {
                uiText.text = text;
            }
            else if (uiComponent is TextMeshProUGUI tmpText)
            {
                tmpText.text = text;
            }
            else if (uiComponent is TextMeshPro tmp3D)
            {
                tmp3D.text = text;
            }
            else if (uiComponent is InputField inputField)
            {
                inputField.text = text;
            }
            else if (uiComponent is TMP_InputField tmpInputField)
            {
                tmpInputField.text = text;
            }
        }

        /// <summary>
        /// 获取所有可用的显示方法名称
        /// </summary>
        public static string[] GetDisplayMethodNames()
        {
            var methods = typeof(UIDisplay).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var methodNames = new System.Collections.Generic.List<string>();

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                // 只选择签名为 (Component, object) 的方法
                if (parameters.Length == 2 &&
                    typeof(Component).IsAssignableFrom(parameters[0].ParameterType) &&
                    parameters[1].ParameterType == typeof(object))
                {
                    methodNames.Add(method.Name);
                }
            }

            return methodNames.ToArray();
        }

        /// <summary>
        /// 根据方法名获取显示方法
        /// </summary>
        public static MethodInfo GetDisplayMethod(string methodName)
        {
            if (string.IsNullOrEmpty(methodName)) return null;

            return typeof(UIDisplay).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static,
                null, new Type[] { typeof(Component), typeof(object) }, null);
        }
    }
}

