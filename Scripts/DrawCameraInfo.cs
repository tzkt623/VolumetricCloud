using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tezcat.Framework.Exp
{
    public class DrawCameraInfo : MonoBehaviour
    {
        [Header("Base")]
        public Camera mCamera;
        public bool mDrawAllRay = false;

        [Header("Space Info")]
        public Vector3 mCurrentWorldPosition;
        public Vector3 mViewSpacePosition;
        public Vector3 mClipSpacePosition;
        public float mClipSize;
        public Vector3 mNDCSpacePosition;

        [Space]
        public Transform mSampleObject;
        public Transform mClipObject;
        public Transform mNDCObject;
        public Transform mViewObject;

        [Space]
        public Transform mClipSpace;
        public Transform mNDCSpace;
        public Transform mViewSpace;

        [Header("NDC Map")]
        public bool mUseScreenPos = false;
        public bool mUseFreePos = false;
        public bool mMultFar = false;
        public Vector3 mScreenNDC;
        public float mDepth;
        public Vector4 mViewPos;

        [Header("Cloud Info")]
        public Light mLight;
        public Transform mBox;
        [Min(0.1f)]
        public float mStepSize = 10;
        public bool mDrawLight = false;
        public bool mDrawDetecteRay = false;
        public bool mDrawCameraRay = false;


        // Start is called before the first frame update
        void Start()
        {

        }


        private void OnDrawGizmos()
        {
            this.cameraInfo();
            this.sapceViewer();
            this.ndcFunc();
        }

        private void ndcFunc()
        {
            /*
             * WolrdSpace * ViewMatrix => ViewSpace
             * ViewSpace * ProjectionMatrix => ClipSpace
             * ClipSpace / ClipSpace.w => NDCSpace
             */

            /*
             * NDCSpace * ClipSpace.w => ClipSpace
             * ClipSpace * InvProjectionMatrix => ViewSpace
             * ViewSpace * InvViewMatrix => WorldSpace
             */

            if (mUseScreenPos)
            {
                Vector2 uv = new Vector2(Input.mousePosition.x / mCamera.pixelWidth, Input.mousePosition.y / mCamera.pixelHeight);
                uv = uv * 2.0f - Vector2.one;
                mScreenNDC = uv;
            }

            mScreenNDC = Vector3.Min(mScreenNDC, Vector3.one);
            mScreenNDC = Vector3.Max(mScreenNDC, -Vector3.one);

            if (mMultFar)
            {
                mDepth = Mathf.Min(mDepth, 1);
                mDepth = Mathf.Max(mDepth, mCamera.nearClipPlane / mCamera.farClipPlane);
            }
            else
            {
                mDepth = Mathf.Min(mDepth, mCamera.farClipPlane);
                mDepth = Mathf.Max(mDepth, mCamera.nearClipPlane);
            }

            Vector4 ndcPos = new Vector4(mScreenNDC.x, mScreenNDC.y, mScreenNDC.z, 0);
            float far = mCamera.farClipPlane;
            Vector4 clipPos = mMultFar ? new Vector4(ndcPos.x, ndcPos.y, ndcPos.z, 1) * far : new Vector4(ndcPos.x, ndcPos.y, ndcPos.z, 1);

            var viewPos = mCamera.projectionMatrix.inverse * clipPos;
            mViewPos = viewPos;
            Vector4 viewPos3 = new Vector4(viewPos.x, viewPos.y, viewPos.z, 0.0f) * mDepth;

            Vector3 viewPos3World = mCamera.cameraToWorldMatrix * viewPos3;
            //Debug.Log(viewPos3World);
            Gizmos.DrawLine(mCamera.transform.position, mCamera.transform.position + viewPos3World);
            if (!mUseFreePos)
            {
                mSampleObject.position = mCamera.transform.position + viewPos3World;
                mCurrentWorldPosition = mSampleObject.localPosition;
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

        private void cameraInfo()
        {
            var resolution = Screen.currentResolution;
            var camera_pos = mCamera.transform.position;
            var plane_length = mCamera.nearClipPlane;
            var rate_f_n = mCamera.farClipPlane / mCamera.nearClipPlane;

            var near_height = plane_length * Mathf.Tan(Mathf.Deg2Rad * mCamera.fieldOfView * 0.5f);
            var near_width = near_height * mCamera.aspect;

            var near_center_pos = camera_pos + mCamera.transform.forward * plane_length;
            var right_offset = mCamera.transform.right * near_width;
            var up_offset = mCamera.transform.up * near_height;
            var lb = near_center_pos - right_offset - up_offset;
            var rt = near_center_pos + right_offset + up_offset;
            var rb = near_center_pos + right_offset - up_offset;

            var lengthH = rt - rb;

            Vector3 box_min = mBox.position - mBox.localScale / 2;
            Vector3 box_max = mBox.position + mBox.localScale / 2;

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

                        if (mDrawDetecteRay)
                        {
                            var ray_dir = Vector3.Normalize(camera_ray_end_pos - camera_pos);
                            var box_info = rayBoxDst(box_min, box_max, camera_pos, ray_dir);
                            float dst_to_box = box_info.x;
                            float dst_inside_box = box_info.y;

                            if (dst_inside_box > 0)
                            {
                                Gizmos.color = Color.cyan;

                                Gizmos.DrawLine(camera_pos, camera_pos + ray_dir * dst_to_box);
                                float step_size = mStepSize;
                                float total_size = 0;

                                while (total_size < dst_inside_box)
                                {
                                    Vector3 p = camera_pos + ray_dir * (dst_to_box + total_size);
                                    total_size += step_size;
                                    Gizmos.DrawWireSphere(p, 0.02f);

                                    if (mDrawLight)
                                    {
                                        this.lightRayMatch(p, step_size, box_min, box_max);
                                    }
                                }

                                Gizmos.color = Color.white;
                            }
                        }
                    }
                }
            }


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
            Gizmos.DrawLine(far_sp - right_offset * rate_f_n, far_sp + right_offset * rate_f_n);
            Gizmos.DrawLine(far_sp - up_offset * rate_f_n, far_sp + up_offset * rate_f_n);

            var rate = mCamera.nearClipPlane / mCamera.farClipPlane;
            var dir = near_up_sp - camera_pos;
            var far_up_sp = camera_pos + dir / rate;
            Gizmos.DrawWireSphere(far_up_sp, 0.02f);
        }

        private void lightRayMatch(Vector3 pos, float stepSize, Vector3 boxMin, Vector3 boxMax)
        {
            Gizmos.color = Color.yellow;
            var light_dir = -mLight.transform.forward;

            var box_info = rayBoxDst(boxMin, boxMax, pos, light_dir);
            float dst_to_box = box_info.x;
            float dst_inside_box = box_info.y;
            Gizmos.DrawLine(pos, pos + light_dir * dst_inside_box);

            int step = (int)(dst_inside_box / stepSize);
            float step_size = stepSize;

            for (int i = 1; i < step; i++)
            {
                Vector3 p = pos + light_dir * (step_size * i);
                Gizmos.DrawWireSphere(p, 0.02f);
            }
            Gizmos.color = Color.white;
        }

        private void sapceViewer()
        {
            if (mUseFreePos)
            {
                mSampleObject.localPosition = mCurrentWorldPosition;
            }

            var wpos = mSampleObject.transform.position;

            Vector4 sample_pos = new Vector4(wpos.x, wpos.y, wpos.z, 1);

            mSampleObject.localScale = mSampleObject.localScale;
            mViewObject.localScale = mSampleObject.localScale;
            mClipObject.localScale = mSampleObject.localScale;
            mNDCObject.localScale = mSampleObject.localScale;

            //View
            var view_pos = mCamera.worldToCameraMatrix * sample_pos;
            var save_mat = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(mViewSpace.transform.position, mViewSpace.transform.rotation, Vector3.one);
            Gizmos.DrawFrustum(Vector3.zero, mCamera.fieldOfView, mCamera.farClipPlane, mCamera.nearClipPlane, mCamera.aspect);
            Gizmos.matrix = save_mat;
            mViewObject.localPosition = new Vector3(view_pos.x, view_pos.y, -view_pos.z);
            mViewSpacePosition = mViewObject.localPosition;

            //Clip
            var clip_pos = mCamera.projectionMatrix * view_pos;
            Gizmos.DrawWireCube(mClipSpace.transform.position, new Vector3(clip_pos.w, clip_pos.w, clip_pos.w) * 2);
            mClipSize = clip_pos.w;
            var clip_coord = new Vector3(clip_pos.x, clip_pos.y, clip_pos.z);
            //Gizmos.DrawSphere(mClipSpace.transform.position + clip_coord, 0.05f);
            mClipObject.localPosition = clip_coord;
            mClipSpacePosition = mClipObject.localPosition;

            //NDC
            var ndc_pos = new Vector3(clip_pos.x / clip_pos.w, clip_pos.y / clip_pos.w, clip_pos.z / clip_pos.w);
            Gizmos.DrawWireCube(mNDCSpace.transform.position, Vector3.one * 2);
            //ndc_pos.z = (this.LinearizeDepth(ndc_pos.z) / mCamera.farClipPlane) * 2 - 1;
            mNDCObject.localPosition = ndc_pos;
            mNDCSpacePosition = mNDCObject.localPosition;
            //Gizmos.DrawSphere(mNDCSpace.transform.position + ndc_pos, 0.05f);
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