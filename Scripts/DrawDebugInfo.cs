using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tezcat.Framework.Exp
{
    public class DrawDebugInfo : MonoBehaviour
    {
        public enum DectetRayMode
        {
            BaseOnInsideLength,
            BaseOnFixedStepLength
        }

        [Header("Data")]
        public PlanetCloud mPlanetCloud;
        public BoxCloud mBoxCloud;
        public CloudRenderer mRenderer;

        [Header("Base")]
        public Light mLight;
        public Camera mCamera;

        [Header("Detect Info")]
        public bool mDrawGizmos = false;
        public DectetRayMode mDectetRayMode = DectetRayMode.BaseOnInsideLength;

        [Min(0.1f)]
        public float mStepSize = 10;
        [Range(2, 10)]
        public int mStepCount = 10;
        public float mDetectSphereRadius = 1;
        public bool mDrawAllRay = false;
        public bool mDrawCameraRay = false;
        public bool mDrawDetectRay = false;
        public bool mDrawLight = false;

        void Start()
        {

        }

        private void OnDrawGizmos()
        {
            if (mDrawGizmos)
            {
                this.drawArea();
                this.drawRay();
            }
        }

        private void drawArea()
        {
            switch (mRenderer.drawArea)
            {
                case CloudRenderer.CloudArea.Box:
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireCube(mBoxCloud.position, mBoxCloud.edgeLength);
                        Gizmos.color = Color.white;

                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(mBoxCloud.position, mBoxCloud.minEdgeLength * 0.1f);
                        Gizmos.DrawLine(mBoxCloud.position, mBoxCloud.min);
                        Gizmos.DrawLine(mBoxCloud.position, mBoxCloud.max);
                        Gizmos.color = Color.white;
                    }
                    break;
                case CloudRenderer.CloudArea.Planet:
                    {
                        Gizmos.DrawWireSphere(mPlanetCloud.position, mPlanetCloud.planetRadius);
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireSphere(mPlanetCloud.position, mPlanetCloud.innerRadius);
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawWireSphere(mPlanetCloud.position, mPlanetCloud.outerRadius);
                    }
                    break;
            }
        }

        static Vector3 divide(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
        }

        private Vector2 rayBoxDst(Vector3 boxMin, Vector3 boxMax, Vector3 pos, Vector3 rayDir)
        {
            Vector3 t0 = divide(boxMin - pos, rayDir);
            Vector3 t1 = divide(boxMax - pos, rayDir);

            Vector3 tmin = Vector3.Min(t0, t1);
            Vector3 tmax = Vector3.Max(t0, t1);

            //射线到box两个相交点的距离, dstA最近距离， dstB最远距离
            float dstA = Mathf.Max(Mathf.Max(tmin.x, tmin.y), tmin.z);
            float dstB = Mathf.Min(Mathf.Min(tmax.x, tmax.y), tmax.z);

            float dstToBox = Mathf.Max(0, dstA);
            float dstInBox = Mathf.Max(0, dstB - dstToBox);

            return new Vector2(dstToBox, dstInBox);
        }

        private Vector2 raySphereDst(Vector3 sphereCenter, float sphereRadius, Vector3 pos, Vector3 rayDir)
        {
            Vector3 oc = pos - sphereCenter;
            float b = Vector3.Dot(rayDir, oc);
            float c = Vector3.Dot(oc, oc) - sphereRadius * sphereRadius;
            float t = b * b - c;//t > 0有两个交点, = 0 相切， < 0 不相交

            float delta = Mathf.Sqrt(Mathf.Max(t, 0));
            float dstToSphere = Mathf.Max(-b - delta, 0);
            float dstInSphere = Mathf.Max(-b + delta - dstToSphere, 0);
            return new Vector2(dstToSphere, dstInSphere);
        }

        private void drawRay()
        {
            var resolution = Screen.currentResolution;
            var camera_pos = mCamera.transform.position;
            var plane_length = mCamera.nearClipPlane;
            var rate_fDn = mCamera.farClipPlane / mCamera.nearClipPlane;

            var near_height = plane_length * Mathf.Tan(Mathf.Deg2Rad * mCamera.fieldOfView * 0.5f);
            var near_width = near_height * mCamera.aspect;

            var near_center_pos = camera_pos + mCamera.transform.forward * plane_length;
            var right_offset = mCamera.transform.right * near_width;
            var up_offset = mCamera.transform.up * near_height;
            var lb = near_center_pos - right_offset - up_offset;
            var rt = near_center_pos + right_offset + up_offset;
            var rb = near_center_pos + right_offset - up_offset;

            var lengthH = rt - rb;

            if (mDrawAllRay)
            {
                var screen_width = resolution.width / 512;
                var screen_height = resolution.height / 512;
                for (int sh = 0; sh <= screen_height; sh++)
                {
                    for (int sw = 0; sw <= screen_width; sw++)
                    {
                        var camera_ray_end_pos = Vector3.Lerp(lb, rb, sw / (float)screen_width);
                        camera_ray_end_pos += lengthH * sh / screen_height;

                        if (mDrawCameraRay)
                        {
                            Gizmos.DrawLine(camera_pos, camera_ray_end_pos);
                        }

                        if (mDrawDetectRay)
                        {
                            var ray_dir = Vector3.Normalize(camera_ray_end_pos - camera_pos);

                            switch (mRenderer.drawArea)
                            {
                                case CloudRenderer.CloudArea.Box:
                                    this.drawBox(ref camera_pos, ref ray_dir);
                                    break;
                                case CloudRenderer.CloudArea.Planet:
                                    this.drawPlanet(ref camera_pos, ref ray_dir);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }

            {
                Gizmos.DrawWireSphere(camera_pos, 0.02f);

                //draw near plane
                Gizmos.DrawWireSphere(near_center_pos, 0.02f);
                Gizmos.DrawWireSphere(lb, 0.02f);
                Gizmos.DrawWireSphere(rt, 0.02f);
                Gizmos.DrawLine(near_center_pos - right_offset, near_center_pos + right_offset);
                Gizmos.DrawLine(near_center_pos - up_offset, near_center_pos + up_offset);

                var near_up_sp = near_center_pos + mCamera.transform.up * near_height;
                Gizmos.DrawWireSphere(near_up_sp, 0.02f);

                //draw far plane
                var far_sp = camera_pos + mCamera.transform.forward * mCamera.farClipPlane;
                Gizmos.DrawWireSphere(far_sp, 0.02f);
                Gizmos.DrawLine(far_sp - right_offset * rate_fDn, far_sp + right_offset * rate_fDn);
                Gizmos.DrawLine(far_sp - up_offset * rate_fDn, far_sp + up_offset * rate_fDn);

                var dir = near_up_sp - camera_pos;
                var far_up_sp = camera_pos + dir * rate_fDn;
                Gizmos.DrawWireSphere(far_up_sp, 0.02f);
            }
        }

        private void drawPlanet(ref Vector3 camera_pos, ref Vector3 ray_dir)
        {
            Vector3 begin_pos = Vector3.zero;
            Vector3 end_pos = Vector3.zero;

            var view_height = (camera_pos - mPlanetCloud.position).magnitude;
            if (view_height > mPlanetCloud.outerRadius)
            {
                var cloud_max = this.raySphereDst(mPlanetCloud.position, mPlanetCloud.planetRadius + mPlanetCloud.cloudThickness.y, camera_pos, ray_dir);
                if (cloud_max.y > 0)
                {
                    var cloud_min = this.raySphereDst(mPlanetCloud.position, mPlanetCloud.planetRadius + mPlanetCloud.cloudThickness.x, camera_pos, ray_dir);

                    begin_pos = camera_pos + ray_dir * cloud_max.x;
                    if (cloud_min.y > 0)
                    {
                        end_pos = camera_pos + ray_dir * cloud_min.x;
                    }
                    else
                    {
                        end_pos = begin_pos + ray_dir * cloud_max.y;
                    }
                }
            }
            else if (view_height > mPlanetCloud.innerRadius)
            {
                begin_pos = camera_pos;

                var cloud_min = this.raySphereDst(mPlanetCloud.position, mPlanetCloud.planetRadius + mPlanetCloud.cloudThickness.x, camera_pos, ray_dir);

                if (cloud_min.y > 0)
                {
                    end_pos = camera_pos + ray_dir * cloud_min.x;
                }
                else
                {
                    var cloud_max = this.raySphereDst(mPlanetCloud.position, mPlanetCloud.planetRadius + mPlanetCloud.cloudThickness.y, camera_pos, ray_dir);
                    end_pos = camera_pos + ray_dir * cloud_max.y;
                }

            }
            else if (view_height > mPlanetCloud.planetRadius)
            {
                var cloud_min = this.raySphereDst(mPlanetCloud.position, mPlanetCloud.planetRadius + mPlanetCloud.cloudThickness.x, camera_pos, ray_dir);
                if (cloud_min.y > 0)
                {
                    var cloud_max = this.raySphereDst(mPlanetCloud.position, mPlanetCloud.planetRadius + mPlanetCloud.cloudThickness.y, camera_pos, ray_dir);

                    begin_pos = camera_pos + ray_dir * cloud_min.y;
                    end_pos = camera_pos + ray_dir * cloud_max.y;
                }
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(begin_pos, 100);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(end_pos, 100);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(begin_pos, end_pos);


            var sphere_info = this.raySphereDst(mPlanetCloud.position, mPlanetCloud.planetRadius, camera_pos, ray_dir);

            float dst_to_sphere = sphere_info.x;
            float dst_inside_sphere = sphere_info.y;

            float threshold = dst_inside_sphere / mStepSize;
            if (dst_inside_sphere > 0 && threshold <= mStepCount)
            {
                float total_size = 0;
                float step_size = 0;

                switch (mDectetRayMode)
                {
                    case DectetRayMode.BaseOnInsideLength:
                        step_size = dst_inside_sphere / (mStepCount - 1);
                        break;
                    case DectetRayMode.BaseOnFixedStepLength:
                        step_size = mStepSize;
                        break;
                    default:
                        break;
                }

                Gizmos.color = Color.cyan;
                while (total_size <= dst_inside_sphere)
                {
                    Vector3 p = camera_pos + ray_dir * (dst_to_sphere + total_size);
                    total_size += step_size;
                    Gizmos.DrawWireSphere(p, mDetectSphereRadius);
                }
            }
        }

        private void drawBox(ref Vector3 camera_pos, ref Vector3 ray_dir)
        {
            var box_info = rayBoxDst(mBoxCloud.min, mBoxCloud.max, camera_pos, ray_dir);
            float dst_to_box = box_info.x;
            float dst_inside_box = box_info.y;

            float threshold = dst_inside_box / mStepSize;
            if (dst_inside_box > 0 && threshold <= mStepCount)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(camera_pos, camera_pos + ray_dir * dst_to_box);

                float step_size = 0;
                float total_size = 0;

                switch (mDectetRayMode)
                {
                    case DectetRayMode.BaseOnInsideLength:
                        step_size = dst_inside_box / (mStepCount - 1);
                        break;
                    case DectetRayMode.BaseOnFixedStepLength:
                        step_size = mStepSize;
                        break;
                    default:
                        break;
                }

                while (total_size <= dst_inside_box)
                {
                    Vector3 p = camera_pos + ray_dir * (dst_to_box + total_size);
                    total_size += step_size;
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(p, mDetectSphereRadius);

                    if (mDrawLight)
                    {
                        this.lightRayMatch(p, step_size, mBoxCloud.min, mBoxCloud.max);
                    }
                }

                Gizmos.color = Color.white;
            }
        }

        private void lightRayMatch(Vector3 pos, float stepSize, Vector3 boxMin, Vector3 boxMax)
        {
            Gizmos.color = Color.yellow;
            var light_dir = -mLight.transform.forward;

            var box_info = rayBoxDst(boxMin, boxMax, pos, light_dir);
            float dst_to_box = box_info.x;
            float dst_inside_box = box_info.y;
            if (dst_inside_box <= 0)
            {
                return;
            }

            Gizmos.DrawLine(pos, pos + light_dir * dst_inside_box);

            int step = 0;
            float step_size = 0;

            switch (mDectetRayMode)
            {
                case DectetRayMode.BaseOnInsideLength:
                    step = mStepCount;
                    step_size = dst_inside_box / mStepCount;
                    break;
                case DectetRayMode.BaseOnFixedStepLength:
                    step_size = mStepSize;
                    step = (int)(dst_inside_box / step_size);
                    break;
                default:
                    break;
            }

            for (int i = 1; i <= step; i++)
            {
                Vector3 p = pos + light_dir * (step_size * i);
                Gizmos.DrawWireSphere(p, mDetectSphereRadius);
            }
        }

        float LinearizeDepth(float depth)
        {
            float near = mCamera.nearClipPlane;
            float far = mCamera.farClipPlane;

            //depth->Zn
            float z = depth * 2.0f - 1.0f;
            return (2.0f * near * far) / (far + near - z * (far - near));
        }

        float linearizeDepth(float depth)
        {
            float near = mCamera.nearClipPlane;
            float far = mCamera.farClipPlane;
            float x = (far - near) / near;

            return 1.0f / (x * depth + 1);
        }
    }
}