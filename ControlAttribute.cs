using UnityEngine;

[System.AttributeUsageAttribute(System.AttributeTargets.Field)]
public class ControlAttribute : System.Attribute
{
    public string Name;
    public string FullPath;

    public static void Process(MonoBehaviour obj)
    {
        var type = obj.GetType();
        var fileds = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic);

        foreach (var filed in fileds)
        {
            var attribute = filed.GetCustomAttributes(typeof(ControlAttribute), true);
            if (attribute.Length == 0)
                continue;

            var controlAttribute = attribute[0] as ControlAttribute;
            if (controlAttribute == null)
                continue;

            if (!string.IsNullOrEmpty(controlAttribute.Name))
            {
                var control = GetControl(obj.gameObject, controlAttribute.Name, filed.FieldType);
                filed.SetValue(obj, control);
                continue;
            }

            if (!string.IsNullOrEmpty(controlAttribute.FullPath))
            {
                var control = GetControlByFullPath(obj.gameObject, controlAttribute.FullPath, filed.FieldType);
                filed.SetValue(obj, control);
                continue;
            }
        }
    }

    public static Object GetControl(GameObject gameObject, string name, System.Type type)
    {
        var go = gameObject.GetChildByName(name);
        if (go == null)
            return null;

        if (type == typeof(GameObject))
            return go;
        return go.GetComponent(type);
    }

    public static Object GetControlByFullPath(GameObject gameObject, string name, System.Type type)
    {
        var go = gameObject.transform.Find(name);
        if (go == null)
            return null;

        if (type == typeof(GameObject))
            return go.gameObject;
        return go.GetComponent(type);
    }
}