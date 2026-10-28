using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;

public class UnityUtils {
    public static bool SphereContant(Vector3 center, float sqrR, Vector3 point)
    {
        // 中心点:  默认物体的原点, 瞄准点;
        float dis = Vector3.SqrMagnitude(point - center);

        if (dis <= sqrR)
        {
            return true;
        }

        return false;
    }

    public static bool BoundContant(Collider c, Vector3 point)
    {
        Bounds b = c.bounds;
        if (b != null && b.Contains(point))
        {
            return true;
        }
        return false;
    }

    public static void DestroyAllChildren(Transform parent)
    {
        int count = parent.childCount;
        for (int i = 0; i < count; i++)
        {
            Transform child = parent.GetChild(0);
            GameObject.DestroyImmediate(child.gameObject);
            count--;
        }
    }

    public static Vector3 StringToVector3(string vecStr, Vector3 defaultValue)
    {
        Vector3 value = defaultValue;
        string[] values = vecStr.Split(',');
        if (values.Length == 3)
        {
            try
            {
                value.x = float.Parse(values[0]);
                value.y = float.Parse(values[1]);
                value.z = float.Parse(values[2]);
            }
            catch (System.Exception e)
            {
                Debug.Log(e.ToString());
            }

        }

        return value;
    }
}
