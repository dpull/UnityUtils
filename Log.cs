using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Net;

namespace X
{
    public class Log : MonoBehaviour
    {
        private System.IO.StreamWriter LogWriter;

        private Thread WebThread;
        private Queue<string> LogQueue = new Queue<string>();
        private string FileName;
        private string Identifier;
        private string WebUrl;

        void Start()
        {
            CleanOldLog();
            Application.RegisterLogCallback(HandleLog);
            Identifier = SystemInfo.deviceUniqueIdentifier;
        }

        void OnDestroy()
        {
            Application.RegisterLogCallback(null);
        }

        string GetLogDirectory()
        {
            var path = System.IO.Path.Combine(Application.persistentDataPath, "logs");
            if (!System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);
            return path;
        }

        void CleanOldLog()
        {
            var dir = GetLogDirectory();
            var files = System.IO.Directory.GetFiles(dir, "*.log", System.IO.SearchOption.AllDirectories);
            var now = System.DateTime.Now;
            var expirationTime = new System.TimeSpan(24 * 3, 0, 0);

            foreach (var file in files)
            {
                var fileInfo = new System.IO.FileInfo(file);
                var subTime = now - fileInfo.LastWriteTime;
                if (subTime < expirationTime)
                    continue;

                try
                {
                    System.IO.File.Delete(file);
                }
                catch
                {
                }
            }
        }

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            try
            {
                LogToFile(logString, stackTrace, type);
                LogToWeb(logString, stackTrace, type);
            }
            catch
            {
            }
        }

        void LogToFile(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Log && !logString.StartsWith("Lua:"))
                return;

            if (LogWriter == null)
            {
                lock (this)
                {
                    if (LogWriter == null)
                    {
                        try
                        {
                            var logfile = GetLogDirectory();
                            FileName = string.Format("{0}.log", System.DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"));
                            logfile = System.IO.Path.Combine(logfile, FileName);
                            LogWriter = new System.IO.StreamWriter(logfile);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            if (LogWriter != null)
            {
                LogWriter.Write(System.DateTime.Now.ToString("MM-dd HH:mm:ss"));
                LogWriter.Write(type.ToString());
                LogWriter.WriteLine(logString);

                if (!string.IsNullOrEmpty(stackTrace))
                {
                    LogWriter.WriteLine();
                    LogWriter.WriteLine(stackTrace);
                    LogWriter.WriteLine();
                    LogWriter.WriteLine();
                }

                LogWriter.Flush();
            }
        }

        void LogToWeb(string logString, string stackTrace, LogType type)
        {
            if (WebThread == null)
                return;

            if (type == LogType.Log && !logString.StartsWith("Lua:stack traceback:"))
                return;

            var log = string.Format("{0}\n{1}", logString ?? string.Empty, stackTrace ?? string.Empty);

            lock (LogQueue)
            {
                if (LogQueue.Count >= 512)
                    return;

                LogQueue.Enqueue(log);
            }
        }

        void StartWebLogClient(string url)
        {
            if (WebThread != null)
                return;

            if (string.IsNullOrEmpty(url))
            {
                Debug.LogError("failed to start web client of log, illegal url");
                return;
            }

#if UNITY_EDITOR
            WebUrl = null;
#else
			WebUrl = url;
			WebThread = new Thread(ProcessLogQueue);
			WebThread.IsBackground = true;
			WebThread.Start();
#endif
        }

        void StopWebLogClient()
        {
            if (WebThread != null)
            {
                lock (LogQueue)
                {
                    LogQueue.Clear();
                }

                WebThread.Abort();
                WebThread = null;
            }
        }

        void ProcessLogQueue()
        {
            while (true)
            {
                if (string.IsNullOrEmpty(FileName))
                    continue;

                string log = null;

                lock (LogQueue)
                {
                    if (LogQueue.Count > 0)
                        log = LogQueue.Dequeue();
                }

                if (string.IsNullOrEmpty(log))
                {
                    Thread.Sleep(300);
                    continue;
                }

                try
                {
                    var web = new WebClient();
                    var nc = new System.Collections.Specialized.NameValueCollection();

                    nc.Add("ClientIdentifier", Identifier);
                    nc.Add("FileName", FileName);
                    nc.Add("Log", log);
                    web.UploadValues(WebUrl, nc);
                }
                catch
                {
                }
            }
        }
    }
}