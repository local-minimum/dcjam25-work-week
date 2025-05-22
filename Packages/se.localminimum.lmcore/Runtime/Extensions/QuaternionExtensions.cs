using UnityEngine;

namespace LMCore.Extensions
{
    public static class QuaternionExtensions 
    {
        public static float GetRoll(this Quaternion rotation) => 
            Mathf.Atan2(2* rotation.y * rotation.w - 2 * rotation.x * rotation.z, 1 - 2 * rotation.y* rotation.y - 2 * rotation.z * rotation.z);

        public static float GetPitch(this Quaternion rotation) =>
            Mathf.Atan2(2 * rotation.x * rotation.w - 2 * rotation.y * rotation.z, 1 - 2 * rotation.x * rotation.x - 2 * rotation.z * rotation.z);

        public static float GetYaw(this Quaternion rotation) =>
            Mathf.Asin(2 * rotation.x * rotation.y + 2 * rotation.z * rotation.w);
    }
}
