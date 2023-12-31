using UnityEngine;

namespace tezcat.Framework.Exp
{
    public class BoxCloud : MonoBehaviour
    {
        public Transform markObject;

        public Vector3 position => markObject.position;
        public Vector3 edgeLength => markObject.localScale;

        public Vector3 min => position - edgeLength * 0.5f;
        public Vector3 max => position + edgeLength * 0.5f;

        public float minEdgeLength
        {
            get
            {
                return Mathf.Min(edgeLength.x, Mathf.Min(edgeLength.y, edgeLength.z));
            }
        }
    }
}
