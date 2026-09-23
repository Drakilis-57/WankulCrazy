using System;
using UnityEngine;

namespace WankulCrazyPlugin.patch
{
    public static class DebugFilterPatch
    {
        public static bool LogWarningPrefix(object message)
        {
            if (message != null && message.ToString().Contains("The character used for Underline is not available in font asset"))
            {
                return false;
            }
            return true;
        }

        public static bool LogWarningContextPrefix(object message, UnityEngine.Object context)
        {
            if (message != null && message.ToString().Contains("The character used for Underline is not available in font asset"))
            {
                return false;
            }
            return true;
        }

        public static bool LogWarningFormatPrefix(string format, params object[] args)
        {
            if (format != null && format.Contains("The character used for Underline is not available in font asset"))
            {
                return false;
            }
            return true;
        }
    }
}
