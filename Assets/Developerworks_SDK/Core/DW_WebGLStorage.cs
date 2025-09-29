using System.Runtime.InteropServices;
using UnityEngine;

namespace Developerworks_SDK.Core
{
    public static class DW_WebGLStorage
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void DW_SetLocalStorage(string key, string value);

        [DllImport("__Internal")]
        private static extern string DW_GetLocalStorage(string key);

        [DllImport("__Internal")]
        private static extern void DW_RemoveLocalStorage(string key);

        [DllImport("__Internal")]
        private static extern bool DW_HasLocalStorageKey(string key);
#endif

        /// <summary>
        /// 设置localStorage值
        /// </summary>
        public static void SetItem(string key, string value)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            DW_SetLocalStorage(key, value);
#else
            // 非WebGL平台使用PlayerPrefs作为备用
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
#endif
        }

        /// <summary>
        /// 获取localStorage值
        /// </summary>
        public static string GetItem(string key, string defaultValue = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string value = DW_GetLocalStorage(key);
            return string.IsNullOrEmpty(value) ? defaultValue : value;
#else
            // 非WebGL平台使用PlayerPrefs作为备用
            return PlayerPrefs.GetString(key, defaultValue);
#endif
        }

        /// <summary>
        /// 删除localStorage项
        /// </summary>
        public static void RemoveItem(string key)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            DW_RemoveLocalStorage(key);
#else
            // 非WebGL平台使用PlayerPrefs作为备用
            PlayerPrefs.DeleteKey(key);
#endif
        }

        /// <summary>
        /// 检查localStorage是否有指定键
        /// </summary>
        public static bool HasKey(string key)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return DW_HasLocalStorageKey(key);
#else
            // 非WebGL平台使用PlayerPrefs作为备用
            return PlayerPrefs.HasKey(key);
#endif
        }
    }
}