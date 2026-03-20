using UnityEngine;

namespace ProjectUtilities
{
    /// <summary>
    /// Global extension methods for Transform objects.
    /// Can be used from any script by importing the ProjectUtilities namespace.
    /// </summary>
    public static class TransformExtensions
    {
        // Resets position, rotation, and scale.
        public static void ResetLocal(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        // Extremely fast way to safely loop through and destroy all children of an object.
        public static void DestroyAllChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

        // Modify only X, Y, or Z without writing "new Vector3(x, transform.position.y...)"
        public static void SetX(this Transform transform, float x)
        {
            Vector3 pos = transform.position;
            pos.x = x;
            transform.position = pos;
        }

        public static void SetY(this Transform transform, float y)
        {
            Vector3 pos = transform.position;
            pos.y = y;
            transform.position = pos;
        }

        public static void SetZ(this Transform transform, float z)
        {
            Vector3 pos = transform.position;
            pos.z = z;
            transform.position = pos;
        }
    }
}

