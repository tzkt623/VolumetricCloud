using UnityEditor;
using UnityEngine;

namespace tezcat.Framework.Exp
{
    [System.Serializable]
    public class AreaData : BaseData
    {
        public enum CloudArea
        {
            Box,
            Planet,
            HorizonLine
        }

        public enum CameraPos
        {
            UnderGround = -1,
            UnderCloud = 0,
            InCloud = 1,
            OutCloud = 2
        }

        public CloudArea mDrawArea;
        public BoxCloud mBoxCloud;
        public PlanetCloud mPlanetCloud;
        public HorizonLineCloud mHorizonLineCloud;

        public override void initIDs()
        {
            this.generateIDs("_DrawAreaIndex"
                , "_BoxMin"
                , "_BoxMax"
                , "_PlanetData"
                , "_CloudData"
                , "_CameraUp"
                , "_ViewPosition");
        }

        public override void sendToGPU(Material material)
        {
            material.SetInt(mIDs[0], (int)mDrawArea);
            switch (this.mDrawArea)
            {
                case CloudArea.Box:
                    {
                        material.SetVector(mIDs[1], mBoxCloud.min);
                        material.SetVector(mIDs[2], mBoxCloud.max);
                    }
                    break;
                case CloudArea.Planet:
                    {
                        this.sendSphereAreaData(material, mPlanetCloud);
                    }
                    break;
                case CloudArea.HorizonLine:
                    {
                        this.sendSphereAreaData(material, mHorizonLineCloud);
                    }
                    break;
                default:
                    break;
            }
        }

        private void sendSphereAreaData(Material material, SphereArea sphereArea)
        {
            material.SetVector(mIDs[3], sphereArea.planetData);
            material.SetVector(mIDs[4], sphereArea.cloudData);

            Camera cam;
#if UNITY_EDITOR
            cam = SceneView.GetAllSceneCameras()[0];
            //cam = Camera.main;
#else
            cam = Camera.main;
#endif
            material.SetVector(mIDs[5], cam.transform.up);
            var camera_height = (cam.transform.position - sphereArea.planetCenter).magnitude;

            ///在云层上
            if (camera_height > sphereArea.outerRadius)
            {
                material.SetInt(mIDs[6], (int)CameraPos.OutCloud);
            }
            ///在云层中
            else if (camera_height > sphereArea.innerRadius)
            {
                material.SetInt(mIDs[6], (int)CameraPos.InCloud);
            }
            ///在云层下
            else if (camera_height > sphereArea.planetRadius)
            {
                material.SetInt(mIDs[6], (int)CameraPos.UnderCloud);
            }
            else
            {
                //GG
                material.SetInt(mIDs[6], (int)CameraPos.UnderGround);
            }
        }
    }
}
