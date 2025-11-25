using UnityEditor;
using UnityEngine;
using System.Reflection;

[CanEditMultipleObjects]
[CustomEditor(typeof(MonoBehaviour), true)]
public class EditorButtonDrawer : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var targetType = target.GetType();
        var methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<EditorButtonAttribute>();
            if (attr != null)
            {
                string buttonName = string.IsNullOrEmpty(attr.ButtonName) ? method.Name : attr.ButtonName;
                if (GUILayout.Button(buttonName))
                {
                    foreach (var t in targets)
                    {
                        method.Invoke(t, null);
                    }
                }
            }
        }
    }
}
