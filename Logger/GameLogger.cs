namespace UniCore.Runtime.ProfilerTools
{
    using System;
    using System.Runtime.CompilerServices;
    using Interfaces;
    using UnityEngine;
    using Debug = UnityEngine.Debug;
    using Object = UnityEngine.Object;
    
#if ENABLE_CRASHLYTICS_GAMELOG
    using Firebase.Crashlytics;
#endif

#if ENABLE_ZSTRING
    using Cysharp.Text;
#endif
    
    public class GameLogger : IGameLogger
    {
        private const string NameTemplate = @"[{0}]:";
        private const string LogTemplate  = @"{0} :{1} {2}";

        private int  _counter;
        private bool _addTimeStamp = true;

        public bool   Enabled = true;
        public string Name;

        protected string LogPrefix => GetNamePrefix();

        public GameLogger(string name)
        {
            Name = name;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(string message, Object source = null)
        {
#if UNITY_EDITOR || GAME_LOGS_ENABLED || DEBUG
            LogRuntime(message, source);
#endif
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogImportant(string message)
        {
            LogRuntime(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogFormatWithTrace(string template, params object[] values)
        {
#if UNITY_EDITOR || GAME_LOGS_ENABLED || DEBUG

            LogFormat(template, values);
            LogFormat("Stack Trace {0}", System.Environment.StackTrace);

#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogFormat(string template, Color color, params object[] values)
        {
#if UNITY_EDITOR || GAME_LOGS_ENABLED || DEBUG

            var message = values == null || values.Length == 0 
                ? template 
                : Format(template, values);
            Log(message, color);
#endif
        }

        public string Format(string template, params object[] values)
        {
#if ENABLE_ZSTRING
            return ZString.Format(template, values);
#endif
            return string.Format(template, values);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogFormat(string template, params object[] values)
        {
#if UNITY_EDITOR || GAME_LOGS_ENABLED || DEBUG
            LogFormatRuntime(template, values);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(string message, Color color, Object source = null)
        {
#if UNITY_EDITOR || GAME_LOGS_ENABLED || DEBUG
            LogRuntime(message, color, source);
#endif
        }

        public void LogWarning(string message, Color color, Object source = null)
        {
            if (!Enabled) return;
            var colorMessage = GetColorTemplate(message, color);
            LogWarning(colorMessage, source);
        }

        public void EditorLogFormat(LogType logType, string format, params object[] objects)
        {
            var prefix = GetNamePrefix();
            format = prefix + format;

            switch (logType) {
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    LogErrorFormat(format, objects);
                    break;
                case LogType.Warning:
                    Debug.LogWarningFormat(format, objects);
                    break;
                case LogType.Log:
                    LogFormat(format, objects);
                    break;
            }
        }

        public void LogWarning(string message, Object source = null)
        {
            if (!Enabled) return;
            
            if (source) {
                Debug.LogWarning(GetLogMessageWithPrefix(message), source);
                return;
            }

            Debug.LogWarning(GetLogMessageWithPrefix(message));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogWarningFormat(string template, params object[] values)
        {
            if (!Enabled) return;
            var message = Format(template, values);
            Debug.LogWarning(GetLogMessageWithPrefix(message));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogException(Exception e)
        {
#if ENABLE_CRASHLYTICS_GAMELOG
            if(Crashlytics.IsCrashlyticsCollectionEnabled)
                Crashlytics.LogException(e);
#endif
            Debug.LogException(e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogError(string message, Object source = null)
        {
#if ENABLE_CRASHLYTICS_GAMELOG
            if(Crashlytics.IsCrashlyticsCollectionEnabled)
                Crashlytics.Log($"ERROR: {message}");
#endif
            if (source) 
            {
                Debug.LogError(message, source);
                return;
            }

            Debug.LogError(message);
        }

        public void LogError(Exception message, Object source = null)
        {
            if (source) {
                Debug.LogError(message, source);
                return;
            }

            Debug.LogError(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogErrorFormat(string message, params object[] objects)
        {
            var value = string.Format(message, objects);
            Debug.LogError(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogFormatRuntime(string template, params object[] values)
        {
            var message = values == null || values.Length == 0 ? template : string.Format(template, values);
            LogRuntime(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogRuntime(string message, Color color, Object source = null)
        {
            if (!Enabled || string.IsNullOrEmpty(message)) return;
            var colorMessage = GetColorTemplate(message, color);
            LogRuntime(colorMessage, source);
        }

        public string GetColorTemplate(string message, Color color)
        {
            var colorMessage = Format("<color=#{0:X2}{1:X2}{2:X2}>{3}</color>",
                                             (byte) (color.r * 255f), (byte) (color.g * 255f), (byte) (color.b * 255f),
                                             message);
            return colorMessage;
        }

        public void LogRuntime(string message, Object source = null)
        {
            if (!Enabled) return;

            message = GetLogMessageWithPrefix(message);
            if (source) {
                Debug.Log(message, source);
                return;
            }

            Debug.Log(message);
        }


        private string GetNamePrefix()
        {
            if(string.IsNullOrEmpty(Name)) return string.Empty;
            return Format(NameTemplate, Name);
        }

        private string GetLogMessageWithPrefix(string message)
        {
            return Format(LogTemplate, DateTime.Now.ToLongTimeString(), LogPrefix, message);
        }
    }
}