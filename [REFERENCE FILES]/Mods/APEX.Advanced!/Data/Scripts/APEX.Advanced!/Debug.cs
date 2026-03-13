using VRage.Utils;

namespace APEX.Advanced
{
    public enum DebugLevel
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Debug = 4
    }

    public static class Debug
    {
        /// <summary>
        /// 
        /// This is not switchable via config. We need a const here to achieve best performance!!!
        /// 
        /// </summary>
        public const bool IS_DEBUG = false;
        public static DebugLevel Level { get; set; } = DebugLevel.Debug;
        private const string PREFIX = "[APEX.Advanced!]";

        public static void LogError(string message) => Log(message, DebugLevel.Error);
        public static void LogWarning(string message) => Log(message, DebugLevel.Warning);
        public static void LogInfo(string message) => Log(message, DebugLevel.Info);
        public static void LogDebug(string message) => Log(message, DebugLevel.Debug);        

        /// <summary>
        /// The core logging method, now updated to use specific MyLog functions.
        /// </summary>
        public static void Log(string message, DebugLevel level = DebugLevel.Debug)
        {
            if (IS_DEBUG && level != DebugLevel.None && level <= Level)
            {
                string formattedMessage = $"{PREFIX} {message}";
                
                switch (level)
                {
                    case DebugLevel.Error:
                        MyLog.Default.Error(formattedMessage);
                        break;

                    case DebugLevel.Warning:
                        MyLog.Default.Warning(formattedMessage);
                        break;

                    case DebugLevel.Info:
                        MyLog.Default.Info(formattedMessage);
                        break;

                    case DebugLevel.Debug:
                        MyLog.Default.Info(formattedMessage);
                        break;

                    default:
                        MyLog.Default.WriteLineAndConsole(formattedMessage);
                        break;
                }
            }
        }
    }
}