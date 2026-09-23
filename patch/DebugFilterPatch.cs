using System;
using UnityEngine;

namespace WankulCrazyPlugin.patch
{
    public static class DebugFilterPatch
    {
        private static bool ShouldIgnore(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.Contains("The character used for Underline is not available in font asset")
                || text.Contains("DontDestroyOnLoad only works for root GameObjects")
                || text.Contains("Parent of RectTransform is being set with parent property")
                || text.Contains("BoxCollider does not support negative scale or size");
        }

        public static bool LogWarningPrefix(object message)
        {
            if (message != null && ShouldIgnore(message.ToString()))
            {
                return false;
            }
            return true;
        }

        public static bool LogWarningContextPrefix(object message, UnityEngine.Object context)
        {
            if (message != null && ShouldIgnore(message.ToString()))
            {
                return false;
            }
            return true;
        }

        public static bool LogWarningFormatPrefix(string format, params object[] args)
        {
            if (format != null && ShouldIgnore(format))
            {
                return false;
            }
            return true;
        }
    }
}
