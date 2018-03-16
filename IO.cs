using System;
using System.IO;
using UnityEngine;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

namespace X
{
    public class DirectoryEx
    {
        public static void CreateDirectory(string path)
        {
            path = Path.GetFullPath(path);
            CreateDirectory(new DirectoryInfo(path));
        }

        public static void CreateDirectory(DirectoryInfo path)
        {
            if (path.Exists)
                return;

            while (true)
            {
                var parent = path.Parent;
                if (parent == null || parent.Exists)
                    break;
                CreateDirectory(parent);
            }

            path.Create();
        }

        public static void DeleteDirectory(string path)
        {
            path = Path.GetFullPath(path);
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    public class FileEx
    {
        public static Stream OpenRead(string sourceFileName)
        {
#if UNITY_ANDROID
            if (sourceFileName.StartsWith(Application.streamingAssetsPath))
            {
                var startIndex = Application.streamingAssetsPath.Length;
                if (!PathEx.EndWithDirectorySeparatorChar(Application.streamingAssetsPath))
                    startIndex++;

                sourceFileName = sourceFileName.Substring(startIndex);
                return new AndroidAssetStream(sourceFileName);
            }
#endif
            return File.OpenRead(sourceFileName);
        }

        public static bool SetFileLength(string filepath, long length)
        {
            try
            {
                using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fs.SetLength(length);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                File.Delete(filepath);
                return false;
            }

            if (!File.Exists(filepath) || FileEx.GetFileLength(filepath) != length)
            {
                File.Delete(filepath);
                return false;
            }

            return true;
        }

        public static long GetFileLength(string filepath)
        {
            FileInfo fileInfo = new FileInfo(filepath);
            return fileInfo.Length;
        }
    }

    public class PathEx
    {
        public static bool EndWithDirectorySeparatorChar(string path)
        {
            return path.EndsWith("\\") || path.EndsWith("/");
        }

        // ParsePath("%LOCALAPPDATA%/Unity/Editor/Editor.log");
        public static string ParsePath(string path) //function by dr4yyee
        {
            var newPath = new StringBuilder();
            var folders = path.Split(Path.DirectorySeparatorChar);
            foreach (var folder in folders)
                newPath.Append((Regex.IsMatch(folder, "%.+%"))
                    ? Environment.GetEnvironmentVariable(Regex.Match(folder, "(?:%)(.+)(?:%)").Groups[1].Value)
                    : folder).Append((folders[folders.Length - 1] == folder)
                    ? string.Empty
                    : new string(Path.DirectorySeparatorChar, 1));
            return newPath.ToString();
        }

        /// <summary>
        /// 绝对路径转相对路径
        /// </summary>
        /// <param name="basePath">基本路径</param>
        /// <param name="fullPath">绝对路径</param>
        /// <returns>strFullPath相对于strBasePath的相对路径</returns>
        public static string GetRelativePath(string basePath, string fullPath)
        {
            if (basePath == null)
                throw new ArgumentNullException("strBasePath");

            if (fullPath == null)
                throw new ArgumentNullException("strFullPath");

            basePath = Path.GetFullPath(basePath);
            fullPath = Path.GetFullPath(fullPath);

            var directoryPos = new int[basePath.Length];
            int posCount = 0;

            directoryPos[posCount] = -1;
            ++posCount;

            int directoryPosIndex = 0;
            while (true)
            {
                directoryPosIndex = basePath.IndexOf('\\', directoryPosIndex);
                if (directoryPosIndex == -1)
                    break;

                directoryPos[posCount] = directoryPosIndex;
                ++posCount;
                ++directoryPosIndex;
            }

            if (!basePath.EndsWith("\\"))
            {
                directoryPos[posCount] = basePath.Length;
                ++posCount;
            }

            int common = -1;
            for (int i = 1; i < posCount; ++i)
            {
                int nStart = directoryPos[i - 1] + 1;
                int nLength = directoryPos[i] - nStart;

                if (string.Compare(basePath, nStart, fullPath, nStart, nLength, true) != 0)
                    break;

                common = i;
            }

            if (common == -1)
                return fullPath;

            var strBuilder = new StringBuilder();
            for (int i = common + 1; i < posCount; ++i)
                strBuilder.Append("..\\");

            int nSubStartPos = directoryPos[common] + 1;
            if (nSubStartPos < fullPath.Length)
                strBuilder.Append(fullPath.Substring(nSubStartPos));

            string strResult = strBuilder.ToString();
            return strResult == string.Empty ? ".\\" : strResult;
        }
    }
}