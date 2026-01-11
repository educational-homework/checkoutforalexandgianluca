using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    public static class Vector3Extensions
    {
        public static Vector3 FloorToTenth(this Vector3 vector)
        {
            return new Vector3(
                Mathf.Floor(vector.x * 10) / 10,
                Mathf.Floor(vector.y * 10) / 10,
                Mathf.Floor(vector.z * 10) / 10
            );
        }

        public static Vector3 Flatten(this Vector3 vector)
        {
            return new Vector3(vector.x, 0, vector.z);
        }
    }
}
