using System.Runtime.CompilerServices;
using UnityEngine;

namespace LMCore.Extensions
{
    public static class VectorExtensions
    {
        /// <summary>
        /// Convert a world vector to the int vector space
        /// </summary>
        /// <param name="scale">Size of each int vector step in world space</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int ToVector2IntXZPlane(this Vector3 vector, int scale = 3) =>
            new Vector2Int(Mathf.RoundToInt(vector.x / scale), Mathf.RoundToInt(vector.z / scale));

        /// <summary>
        /// Convert a world vector to the int vector space
        /// </summary>
        /// <param name="scale">Size of each int vector step in world space</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Int ToVector3Int(this Vector3 vector, int scale = 3) =>
            new Vector3Int(Mathf.RoundToInt(vector.x / scale), Mathf.RoundToInt(vector.y / scale), Mathf.RoundToInt(vector.z / scale));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int FloorToInt(this Vector2 vector) => new Vector2Int(Mathf.FloorToInt(vector.x), Mathf.FloorToInt(vector.y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int CeilToInt(this Vector2 vector) => new Vector2Int(Mathf.CeilToInt(vector.x), Mathf.CeilToInt(vector.y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ClampDimensions(this Vector2 vector, float min, float max) =>
            new Vector2(Mathf.Clamp(vector.x, min, max), Mathf.Clamp(vector.y, min, max));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Scale(this Vector2 vector, float xScale, float yScale) =>
            new Vector2(vector.x * xScale, vector.y * yScale);
    }
}