using UnityEngine;

namespace tezcat.Framework.Exp
{
    public abstract class SphereArea : MonoBehaviour
    {
        public Transform markObject;

        public abstract Vector3 planetCenter { get; }
        public float planetRadius;
        public Vector2 cloudThickness;

        public float innerRadius => this.planetRadius + this.cloudThickness.x;
        public float outerRadius => this.planetRadius + this.cloudThickness.y;

        public float cloudMin => this.cloudThickness.x;
        public float cloudMax => this.cloudThickness.y;

        public Vector4 planetData
        {
            get
            {
                return new Vector4(planetCenter.x, planetCenter.y, planetCenter.z, planetRadius);
            }
        }
    }
}
