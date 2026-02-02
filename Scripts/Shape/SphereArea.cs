using UnityEngine;

namespace tezcat.Framework.Exp
{
    public abstract class SphereArea : MonoBehaviour
    {
        public Transform markObject;

        public abstract Vector3 planetCenter { get; }
        public float planetRadius;
        public Vector2 cloudMinMax;

        public float innerRadius => this.planetRadius + this.cloudMinMax.x;
        public float outerRadius => this.planetRadius + this.cloudMinMax.y;

        public float cloudMin => this.cloudMinMax.x;
        public float cloudMax => this.cloudMinMax.y;
        public float cloudThickness => this.cloudMinMax.y - this.cloudMinMax.x;

        public Vector4 planetData
        {
            get
            {
                return new Vector4(planetCenter.x, planetCenter.y, planetCenter.z, planetRadius);
            }
        }

        public Vector4 cloudData
        {
            get
            {
                return new Vector4(cloudMinMax.x, cloudMinMax.y, cloudMinMax.y - cloudMinMax.x, 0);
            }
        }
    }
}
