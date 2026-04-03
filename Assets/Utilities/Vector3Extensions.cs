using UnityEngine;

namespace ProjectUtilities
{
    /// <summary>
    /// Global extension methods for Vector3 mathematical operations.
    /// </summary>
    public static class Vector3Extensions
    {
        // Beautiful one-liner to snap any exact position to a grid mathematically.
        // Instead of writing the math locally in GridPlacement, you can now do: vector.SnapToGrid(1f, 0.1f)
        public static Vector3 SnapToGrid(this Vector3 vector, float cellSize, float defaultY = 0)
        {
            return new Vector3(
                Mathf.Round(vector.x / cellSize) * cellSize,
                defaultY,
                Mathf.Round(vector.z / cellSize) * cellSize
            );
        }

        // Easily change only a single axis of a Vector struct
        public static Vector3 WithX(this Vector3 vector, float x)
        {
            return new Vector3(x, vector.y, vector.z);
        }

        public static Vector3 WithY(this Vector3 vector, float y)
        {
            return new Vector3(vector.x, y, vector.z);
        }

        public static Vector3 WithZ(this Vector3 vector, float z)
        {
            return new Vector3(vector.x, vector.y, z);
        }
    }
}
