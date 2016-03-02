using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public static class Util
{ 
    public static T GetChildByName<T>(this GameObject go, string name) where T:Component
    {
        var child = GetChildByName(go, name);
        if (child == null)
            return default(T);

        return child.gameObject.GetComponent<T>();
    }

    public static GameObject GetChildByName(this GameObject go, string name) 
    {
        var child = GetChildByName(go.transform, name);
        return child == null ? null : child.gameObject;
    }

    public static Transform GetChildByName(Transform tr, string name)
    {
        // 广度优先
        foreach (Transform child in tr)
        {
            if (child.name == name)
                return child;
        }

        foreach (Transform child in tr)
        {
            Transform c = GetChildByName(child, name);
            if (c != null)
                return c;
        }

        return null;
    }

    public delegate bool TraversalCallback(Transform go);
    public static bool Traversal(this Transform transform, TraversalCallback callback)
    {
        // 深度优先
        foreach (Transform child in transform)
        {
            if (!callback(child))
                return false;

            if (!child.Traversal(callback))
                return false;
        }
        return true;
    }

    public static void ChangeLayer(GameObject go, int layer)
    {
        go.layer = layer;
        go.transform.Traversal(child=>{child.gameObject.layer = layer; return true; });
    }
    
    public static string Md5File(string file)
    {
        using (var stream = System.IO.File.OpenRead(file))
        {
            var md5 = System.Security.Cryptography.MD5.Create();
            var data = md5.ComputeHash(stream);
            var sb = new System.Text.StringBuilder();

            foreach(var b in data)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString().ToLower();
        }
    }

    public static string GetFileSizeString(double size)
    {
        // http://en.wikipedia.org/wiki/File_size
        var units = new string[]{"B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"};
        foreach (var unit in units)
        {
            if (size < 1024d)
                return string.Format("{0:F}{1}", size, unit);
            size /= 1024;
        }
        return string.Format("{0:F}{1}", size, units[units.Length - 1]);
    }

    public static string GetFullName(this GameObject go, Transform root = null)
    {
        var sb = new System.Text.StringBuilder();
        var transform = go.transform;
        while (transform != null && transform != root)
        {
            sb.Insert(0, string.Format("/{0}", transform.name));
            transform = transform.parent;
        }
        return sb.ToString();
    }

	public static void RandomArray<T>(T[] array, int startIndex)
	{
		RandomArray(array, startIndex, array.Length - 1);
	}

	public static void RandomArray<T>(T[] array, int startIndex, int endIndex)
	{
		if (startIndex < 0 || endIndex >= array.Length || startIndex > endIndex)
			throw new UnityException(string.Format("failed to random array({0}), illegal random index: ({1}, {2})", array.Length, startIndex, endIndex));

		var randCount = endIndex - startIndex;
		while (randCount > 0)
		{
			var i = UnityEngine.Random.Range(0, randCount);

			var temp = array[startIndex + randCount];
			array[startIndex + randCount] = array[startIndex + i];
			array[startIndex + i] = temp;

			randCount--;
		}
	}

	public static string TranslateToString(this Vector3 vector)
	{
		return string.Format("{0},{1},{2}", vector.x, vector.y, vector.z);
	}

	public static Vector3 TranslateToVector3(this string strValue)
	{
		if (string.IsNullOrEmpty(strValue))
			return Vector3.zero;

		var values = strValue.Split(",".ToCharArray());
		if (values.Length != 3)
		{
			return Vector3.zero;
		}

		return new Vector3(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]), Convert.ToSingle(values[2]));
	}

	public static string TranslateToString(this Quaternion quaternion)
	{
		return string.Format("{0},{1},{2},{3}", quaternion.x, quaternion.y, quaternion.z, quaternion.w);
	}

	public static Quaternion TranslateToQuaternion(this string strValue)
	{
		if (string.IsNullOrEmpty(strValue))
			return Quaternion.identity;
		
		var values = strValue.Split(",".ToCharArray());
		if (values.Length != 4)
		{
			return Quaternion.identity;
		}
		
		return new Quaternion(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]), Convert.ToSingle(values[2]), Convert.ToSingle(values[3]));
	}

//	static public T AddMissingComponent<T>(this GameObject go) where T : Component
//	{
//		T comp = go.GetComponent<T>();
//		if (comp == null)
//			comp = go.AddComponent<T>();
//		return comp;
//	}
}
