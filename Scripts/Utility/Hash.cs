using UnityEngine;

namespace tezcat.Framework.Utility
{
    public static class Hash
    {
        static Vector3 dotFactors2 = new Vector3(2097152.0f, 262144.0f, 32768.0f);
        static Vector3 dotFactors = new Vector3(7.0f, 157.0f, 113.0f);

        private static float fract(float x)
        {
            return x - Mathf.Floor(x);
        }

        private static Vector3 fract(Vector3 v)
        {
            return new Vector3(fract(v.x), fract(v.y), fract(v.z));
        }

        public static Vector3 hash33(Vector3 position)
        {
            float n = Mathf.Sin(Vector3.Dot(position, dotFactors));
            return fract(dotFactors2 * n);
        }
    }
}
