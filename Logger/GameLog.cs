namespace UniCore.Runtime.ProfilerTools
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Interfaces;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public static class GameLog
    {

        private static IGameLogger _logger;
        public static IGameLogger Logger
        {
            get
            {
                if(_logger == null)
                    _logger = new GameLogger(string.Empty);
                return _logger;
            }

        }
        
        private static IGameLogger _messageLogger;
        public static IGameLogger MessageLogger
        {
            get
            {
                if(_messageLogger == null)
                    _messageLogger = new GameLogger("MESSAGE");
                return _messageLogger;
            }

        }
        
        [Conditional("UNITY_EDITOR"), 
         Conditional("GAME_LOGS_ENABLED"),
         Conditional("GAME_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log(string message, Object source = null)
        {
            Logger.Log(message, source);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogImportant(string message)
        {
            Logger.LogImportant(message);
        }
        
        [Conditional("UNITY_EDITOR"), 
         Conditional("GAME_LOGS_ENABLED"),
         Conditional("GAME_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogFormatWithTrace(string template, params object[] values)
        {
            Logger.LogFormatWithTrace(template, values);
        }

        [Conditional("UNITY_EDITOR"), 
         Conditional("GAME_LOGS_ENABLED"),
         Conditional("GAME_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogFormat(string template, Color color, params object[] values)
        {
            Logger.LogFormat(template,color,values);
        }

        [Conditional("UNITY_EDITOR"), 
         Conditional("GAME_LOGS_ENABLED"),
         Conditional("GAME_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log(string message, Color color, Object source = null) {
            Logger.Log(message, color, source);
        }

        [Conditional("UNITY_EDITOR"), 
         Conditional("GAME_LOGS_ENABLED"),
         Conditional("GAME_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogWarning(string message, Color color, Object source = null)
        {
            Logger.LogWarning(message,color, source);
        }
  
        [Conditional("LOG_GAME_STATE")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogGameState(string message)
        {
            Logger.Log(message);
        }

        [Conditional("UNITY_EDITOR")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EditorLogFormat(LogType logType,string format, params object[] objects)
        {
            Logger.EditorLogFormat(logType, format, objects);
        }
        
        
        [Conditional("UNITY_EDITOR"), 
         Conditional("GAME_LOGS_ENABLED"),
         Conditional("GAME_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogWarning(string message, Object source = null)
        {
            Logger.LogWarning(message, source);
        }

        [Conditional("UNITY_EDITOR"), 
         Conditional("GAME_LOGS_ENABLED"),
         Conditional("GAME_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogWarningFormat(string template, params object[] values)
        {
            Logger.LogWarningFormat(template,values);
        }

        [Conditional("UNITY_EDITOR"), 
         Conditional("GAME_LOGS_ENABLED"),
         Conditional("GAME_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogFormat(string template, params object[] values)
        {
            Logger.LogFormat(template, values);
        }

        [Conditional("UNITY_EDITOR"), 
         Conditional("GAME_LOGS_ENABLED"),
         Conditional("GAME_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogMessage(string template, params object[] values)
        {
            MessageLogger.LogFormat(template,values);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogError(string message, Object source = null)
        {
            Logger.LogError(message, source);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogException(Exception e)
        {
            Logger.LogException(e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogError(Exception message, Object source = null)
        {
            Logger.LogError(message, source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogErrorFormat(string message, params object[] objects)
        {
            Logger.LogErrorFormat(message, objects);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogFormatRuntime(string template, params object[] values)
        {
            Logger.LogFormatRuntime(template,values);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogRuntime(string message, Color color, Object source = null)
        {
            Logger.LogRuntime(message,color, source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetColorTemplate(string message, Color color)
        {
            return Logger.GetColorTemplate(message, color);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogRuntime(string message, Object source = null)
        {
            Logger.LogRuntime(message,source);
        }

    }
}
