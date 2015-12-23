using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Threading;

#if UNITY_EDITOR
using UnityEditor;

namespace X
{
    public class ProfilerLoggerWindow : EditorWindow
    {
		[MenuItem("XStudio/Tools/Profiler To Log")]
        public static void LoadProfileData()
        {   
            EditorWindow.GetWindow<ProfilerLoggerWindow>(false, "Profiler logger", true).Show();
        }
        
        string LogProfilerDir = null;
        string CurLogProfilerDir = null;
        string[] CurFiles = null;
        int CurSelectFileIndex = -1;
        int LastSelectFileIndex = -1;
        
        string[] GetFiles()
        {
            if (CurFiles == null || CurLogProfilerDir != LogProfilerDir)
            {
                CurLogProfilerDir = LogProfilerDir;
                
                var path = Path.GetFullPath(CurLogProfilerDir);
                var files = Directory.GetFiles(path, "*.txt");
                
                CurFiles = new string[files.Length];
                
                var length = path.Length;
                if (!PathEx.EndWithDirectorySeparatorChar(path))
                    length++;
                
                for (var i = 0; i < files.Length; ++i)
                {
                    CurFiles[i] = files[i].Substring(length);
                }
                
                System.Array.Sort<string>(CurFiles, (s1, s2) => {return s1.CompareTo(s2);});
                
                if (CurFiles.Length == 0)
                    CurFiles = null;
            }
            return CurFiles;
        }
        
        void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(LogProfilerDir);
            if (GUILayout.Button("Change dir"))
            {
                LogProfilerDir = EditorUtility.OpenFolderPanel("Load Profile Data", "", "");
                CurLogProfilerDir = null;
            }
            GUILayout.EndHorizontal();
            
            if (string.IsNullOrEmpty(LogProfilerDir) || !Directory.Exists(LogProfilerDir))
                return;
            
            var files = GetFiles();
            if (files == null)
                return;
            
            for (var i = 0; i < files.Length; ++i)
            {
                var toggle = EditorGUILayout.Toggle(i.ToString(), CurSelectFileIndex == i);
                if (toggle)
                    CurSelectFileIndex = i;
            }
            
            if (LastSelectFileIndex != CurSelectFileIndex)
            {
                LastSelectFileIndex = CurSelectFileIndex;
                
                var path = Path.Combine(CurLogProfilerDir, files[LastSelectFileIndex]);
                Debug.Log(path);
                Profiler.AddFramesFromFile(path);
            }
        }
    }
}
#endif

namespace X
{
    public class ProfilerLogger : MonoBehaviour 
    {
        void Start ()
        {
            Profiler.enableBinaryLog = true;                                                 
            Profiler.enabled = true;
        }
        
        void OnDestroy()
        {
            Profiler.enabled = false;
        }

        void LateUpdate()
        {
            if (Time.frameCount % 256 == 1)
            {
                var file = string.Format("ProfilerLog_{0}_{1}.txt", System.DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"), Application.loadedLevelName);    
                Profiler.logFile = Path.Combine(Application.persistentDataPath, file);
            }
        }
    }
}
