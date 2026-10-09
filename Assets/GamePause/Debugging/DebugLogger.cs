using UnityEngine;
using TMPro;
using System;
using System.Text;

namespace GamePause.Debugging
{
    /// <summary>
    /// Singleton debug logger that outputs to both console and TextMeshPro component
    /// </summary>
    public class DebugLogger : MonoBehaviour
    {
        private static DebugLogger _instance;
        private static bool _isApplicationQuitting;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static DebugLogger Instance
        {
            get
            {
                if (_isApplicationQuitting)
                {
                    return null;
                }

                if (_instance == null)
                {
                    // Use the new non-deprecated API
                    #if UNITY_2023_1_OR_NEWER
                        _instance = FindAnyObjectByType<DebugLogger>();
                    #elif UNITY_2020_1_OR_NEWER
                        _instance = FindObjectOfType<DebugLogger>(true);
                    #else
                        _instance = FindObjectOfType<DebugLogger>();
                    #endif

                    if (_instance == null)
                    {
                        GameObject loggerObject = new GameObject("DebugLogger");
                        _instance = loggerObject.AddComponent<DebugLogger>();
                        Debug.LogWarning("[DebugLogger] Instance was created automatically. Consider adding it to your scene.");
                    }
                }
                return _instance;
            }
        }

        [Header("UI Reference")]
        [SerializeField]
        [Tooltip("TextMeshPro component to display logs on screen")]
        private TextMeshProUGUI _logDisplayText;

        [Header("Settings")]
        [SerializeField]
        [Tooltip("Maximum number of log lines to keep in the display")]
        private int _maxLogLines = 30;

        [SerializeField]
        [Tooltip("Show timestamp with each log entry")]
        private bool _showTimestamp = true;

        [SerializeField]
        [Tooltip("Format for the timestamp")]
        private string _timestampFormat = "HH:mm:ss";

        [SerializeField]
        [Tooltip("Auto-scroll to bottom when new log is added")]
        private bool _autoScroll = true;

        [SerializeField]
        [Tooltip("Persist across scene loads")]
        private bool _dontDestroyOnLoad = true;

        private StringBuilder _logStringBuilder;
        private int _currentLineCount;

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            
            if (_dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            _logStringBuilder = new StringBuilder();
            _currentLineCount = 0;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            _isApplicationQuitting = true;
        }

        #endregion

        #region Public Static Log Methods

        /// <summary>
        /// Log a standard message
        /// </summary>
        public static void Log(string message)
        {
            Debug.Log(message);
            Instance?.AppendToDisplay(message, LogType.Log);
        }

        /// <summary>
        /// Log a standard message with context
        /// </summary>
        public static void Log(string message, UnityEngine.Object context)
        {
            Debug.Log(message, context);
            Instance?.AppendToDisplay(message, LogType.Log);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public static void LogWarning(string message)
        {
            Debug.LogWarning(message);
            Instance?.AppendToDisplay(message, LogType.Warning);
        }

        /// <summary>
        /// Log a warning message with context
        /// </summary>
        public static void LogWarning(string message, UnityEngine.Object context)
        {
            Debug.LogWarning(message, context);
            Instance?.AppendToDisplay(message, LogType.Warning);
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public static void LogError(string message)
        {
            Debug.LogError(message);
            Instance?.AppendToDisplay(message, LogType.Error);
        }

        /// <summary>
        /// Log an error message with context
        /// </summary>
        public static void LogError(string message, UnityEngine.Object context)
        {
            Debug.LogError(message, context);
            Instance?.AppendToDisplay(message, LogType.Error);
        }

        /// <summary>
        /// Log a formatted message
        /// </summary>
        public static void LogFormat(string format, params object[] args)
        {
            string message = string.Format(format, args);
            Log(message);
        }

        /// <summary>
        /// Log a formatted warning message
        /// </summary>
        public static void LogWarningFormat(string format, params object[] args)
        {
            string message = string.Format(format, args);
            LogWarning(message);
        }

        /// <summary>
        /// Log a formatted error message
        /// </summary>
        public static void LogErrorFormat(string format, params object[] args)
        {
            string message = string.Format(format, args);
            LogError(message);
        }

        /// <summary>
        /// Log an exception
        /// </summary>
        public static void LogException(Exception exception)
        {
            Debug.LogException(exception);
            Instance?.AppendToDisplay($"Exception: {exception.Message}\n{exception.StackTrace}", LogType.Exception);
        }

        /// <summary>
        /// Clear all displayed logs
        /// </summary>
        public static void ClearLogs()
        {
            Instance?.ClearDisplay();
        }

        /// <summary>
        /// Check if instance exists without creating one
        /// </summary>
        public static bool HasInstance => _instance != null;

        #endregion

        #region Private Methods

        private void AppendToDisplay(string message, LogType logType)
        {
            if (_logDisplayText == null)
                return;

            string formattedMessage = FormatLogMessage(message, logType);

            _logStringBuilder.AppendLine(formattedMessage);
            _currentLineCount++;

            while (_currentLineCount > _maxLogLines)
            {
                RemoveOldestLine();
            }

            UpdateDisplayText();
        }

        private string FormatLogMessage(string message, LogType logType)
        {
            StringBuilder formattedBuilder = new StringBuilder();

            if (_showTimestamp)
            {
                formattedBuilder.Append($"<color=#888888>[{DateTime.Now.ToString(_timestampFormat)}]</color> ");
            }

            string colorHex = GetLogTypeColor(logType);
            string prefix = GetLogTypePrefix(logType);
            
            formattedBuilder.Append($"<color={colorHex}>{prefix}{message}</color>");

            return formattedBuilder.ToString();
        }

        private string GetLogTypeColor(LogType logType)
        {
            switch (logType)
            {
                case LogType.Error:
                case LogType.Exception:
                    return "#FF5555";

                case LogType.Warning:
                    return "#FFFF55";

                case LogType.Log:
                default:
                    return "#FFFFFF";
            }
        }

        private string GetLogTypePrefix(LogType logType)
        {
            switch (logType)
            {
                case LogType.Error:
                    return "[ERROR] ";
                case LogType.Exception:
                    return "[EXCEPTION] ";
                case LogType.Warning:
                    return "[WARNING] ";
                default:
                    return "";
            }
        }

        private void RemoveOldestLine()
        {
            string content = _logStringBuilder.ToString();
            int firstNewlineIndex = content.IndexOf('\n');

            if (firstNewlineIndex >= 0 && firstNewlineIndex < content.Length - 1)
            {
                _logStringBuilder.Remove(0, firstNewlineIndex + 1);
                _currentLineCount--;
            }
            else
            {
                _logStringBuilder.Clear();
                _currentLineCount = 0;
            }
        }

        private void UpdateDisplayText()
        {
            if (_logDisplayText != null)
            {
                _logDisplayText.text = _logStringBuilder.ToString();
            }
        }

        private void ClearDisplay()
        {
            _logStringBuilder.Clear();
            _currentLineCount = 0;

            if (_logDisplayText != null)
            {
                _logDisplayText.text = string.Empty;
            }
        }

        #endregion

        #region Editor Utilities

        #if UNITY_EDITOR
        [ContextMenu("Test Log")]
        private void TestLog()
        {
            Log("This is a test log message.");
        }

        [ContextMenu("Test Warning")]
        private void TestWarning()
        {
            LogWarning("This is a test warning message.");
        }

        [ContextMenu("Test Error")]
        private void TestError()
        {
            LogError("This is a test error message.");
        }
        #endif

        #endregion
    }
}