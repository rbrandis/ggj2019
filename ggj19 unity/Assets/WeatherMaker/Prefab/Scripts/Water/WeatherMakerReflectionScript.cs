//
// Weather Maker for Unity
// (c) 2016 Digital Ruby, LLC
// Source code may be used for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// 
// *** A NOTE ABOUT PIRACY ***
// 
// If you got this asset off of leak forums or any other horrible evil pirate site, please consider buying it from the Unity asset store at https://www.assetstore.unity3d.com/en/#!/content/60955?aid=1011lGnL. This asset is only legally available from the Unity Asset Store.
// 
// I'm a single indie dev supporting my family by spending hundreds and thousands of hours on this and other assets. It's very offensive, rude and just plain evil to steal when I (and many others) put so much hard work into the software.
// 
// Thank you.
//
// *** END NOTE ABOUT PIRACY ***
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalRuby.WeatherMaker
{
    [ExecuteInEditMode]
    public class WeatherMakerReflectionScript : MonoBehaviour
    {
        #region Public properties

        [Tooltip("Renderer to draw reflection in")]
        public Renderer ReflectRenderer;

        [Tooltip("What layers to reflect, set to none or 0 to disable reflections.")]
        public LayerMask ReflectionMask = ~(1 << 4);

        [Tooltip("Reflection mask for recursion. Set to 0 to match the ReflectionMask property.")]
        public LayerMask ReflectionMaskRecursion = 0;

        [Tooltip("Whether to reflect the skybox")]
        public bool ReflectSkybox;

        [Tooltip("Reflection texture name for shaders to use")]
        public string ReflectionSamplerName = "_ReflectionTex";
        private string ReflectionSamplerName2;

        [Tooltip("Maximum per pixel lights in reflection")]
        [Range(0, 128)]
        public int MaximumPerPixelLightsToReflect = 8;

        [Tooltip("Near clip plane offset for reflection")]
        public float ClipPlaneOffset = 0.07f;

        [Tooltip("Render texture size. Based on aspect ratio this will use this size as the width or height.")]
        [Range(64, 4096)]
        public int RenderTextureSize = 1024;

        [Tooltip("The reflection camera render path. Set to 'UsePlayerSettings' to take on the observing camera rendering path. DO NOT CHANGE AT RUNTIME.")]
        public RenderingPath ReflectionCameraRenderingPath = RenderingPath.Forward;

        [Tooltip("Whether normal is forward. True for quads, false for planes (up)")]
        public bool NormalIsForward = true;

        [Tooltip("Aspect ratio (width/height) for reflection camera, 0 for default.")]
        [Range(0.0f, 10.0f)]
        public float AspectRatio = 0.0f;

        [Tooltip("Field of view for reflection camera, 0 for default.")]
        [Range(0.0f, 360.0f)]
        public float FieldOfView = 0.0f;

        [Tooltip("Near plane for reflection camera, 0 for default.")]
        public float NearPlane = 0.0f;

        [Tooltip("Far plane for reflection camera, 0 for default.")]
        public float FarPlane = 0.0f;

        [Tooltip("Recursion limit. Reflections will render off each other up to this many times. Be careful for performance.")]
        [Range(0, 10)]
        public int RecursionLimit = 0;

        [Tooltip("Reduce render texture size as recursion increases, formula = Mathf.Pow(RecursionRenderTextureSizeReducerPower, recursionLevel) * RenderTextureSize.")]
        [Range(0.1f, 1.0f)]
        public float RecursionRenderTextureSizeReducerPower = 0.75f;

        [Tooltip("Render texture format for reflection")]
        public RenderTextureFormat RenderTextureFormat = RenderTextureFormat.ARGB32;

        /// <summary>
        /// Set to true to negate the transform normal (i.e. underwater reflection vs above water reflection)
        /// </summary>
        public bool TransformNormalNegate { get; set; }

        /// <summary>
        /// The current recursion level of mirrors being rendered
        /// </summary>
        public static int CurrentRecursionLevel { get; private set; }

        #endregion Public properties

        private const string mirrorRecursionLimitKeyword = "MIRROR_RECURSION_LIMIT";

        private readonly List<ReflectionCameraInfo> currentCameras = new List<ReflectionCameraInfo>();
        private readonly List<ReflectionCameraInfo> cameraCache = new List<ReflectionCameraInfo>();
        private readonly List<KeyValuePair<RenderTexture, RenderTexture>> currentRenderTextures = new List<KeyValuePair<RenderTexture, RenderTexture>>();
        private readonly Dictionary<Camera, List<KeyValuePair<RenderTexture, StereoTargetEyeMask>>> sourceCamerasToRenderTextures = new Dictionary<Camera, List<KeyValuePair<RenderTexture, StereoTargetEyeMask>>>();

        // prevent too many renders
        private static int renderCount;
        private const int maxRenderCount = 100;
        private const int waterLayerInverse = -17;

        private bool initialized;

        /// <summary>
        /// Information about a camera reflection
        /// </summary>
        public class ReflectionCameraInfo
        {
            /// <summary>
            /// The observing camera
            /// </summary>
            public Camera SourceCamera;

            /// <summary>
            /// The reflection camera
            /// </summary>
            public Camera ReflectionCamera;

            /// <summary>
            /// Whether the source camera is a reflection camera
            /// </summary>
            public bool SourceCameraIsReflection;

            /// <summary>
            /// Target render texture
            /// </summary>
            public RenderTexture TargetTexture;

            /// <summary>
            /// Second target render texture, only needed if VR is enabled. This is for the right eye.
            /// Unity does not provide a way to call camera.Render() with single pass stereo,
            /// each eye must be rendered to a separate render texture regardless of VR settings.
            /// </summary>
            public RenderTexture TargetTexture2;
        }

        /// <summary>
        /// Exact frustum box intersect, unlike Unity frustum culling this does not sometimes fail on objects that are not in the frustum
        /// All credit goes to https://www.iquilezles.org/www/articles/frustumcorrect/frustumcorrect.htm, very smart person :)
        /// Also http://www.flipcode.com/archives/Frustum_Culling.shtml was helpful
        /// </summary>
        /// <param name="camera">Camera</param>
        /// <param name="frustumPlanes">Frustum planes</param>
        /// <param name="frustumCorners">Frustum corners</param>
        /// <param name="box">AABB box</param>
        /// <returns>True if intersect or contained in frustum, false otherwise. Returns true if camera is null.</returns>
        public static bool BoxIntersectsFrustum(Camera camera, Plane[] frustumPlanes, Vector3[] frustumCorners, Bounds box)
        {
            if (camera == null || frustumPlanes == null || frustumCorners == null)
            {
                return false;
            }

            /*
            float fViewLen = camera.farClipPlane - camera.nearClipPlane;

            // use some trig to find the height of the frustum at the far plane
            float fHeight = fViewLen * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);

            // with an aspect ratio of 1, the width will be the same
            float fWidth = fHeight * camera.aspect;

            // halfway point between near/far planes starting at the origin and extending along the z axis
            Vector3 P = new Vector3(0.0f, 0.0f, camera.nearClipPlane + fViewLen * 0.5f);

            // the calculate far corner of the frustum
            Vector3 Q = new Vector3(fWidth, fHeight, fViewLen);

            // the vector between P and Q
            Vector3 vDiff = P - Q;

            // the radius becomes the length of this vector
            float sphereRadiusSquared = vDiff.sqrMagnitude;

            // get the look vector of the camera from the view matrix
            Vector3 forward = camera.transform.forward;

            // calculate the center of the sphere
            Vector3 sphereCenter = camera.transform.position + (forward * ((fViewLen * 0.5f) + camera.nearClipPlane));
            */

            // near plane is tiny usually, no need to include it in calculations
            Vector3 sphereCenter = camera.transform.position + (camera.transform.forward * camera.farClipPlane * 0.5f);
            float sphereRadiusSquared = Vector3.SqrMagnitude(frustumCorners[0] - camera.transform.position);
            Vector3 closestPointInAabb = Vector3.Min(Vector3.Max(sphereCenter, box.min), box.max);
            double distanceSquared = (closestPointInAabb - sphereCenter).sqrMagnitude;
            bool inSphere = (distanceSquared < sphereRadiusSquared);
            if (!inSphere)
            {
                return false;
            }

            /*
            // check box outside/inside of frustum - seems bugged for some bounds..., maybe Unity planes are different than the planes from this code
            for( int i=0; i<6; i++ )
            {
                int out = 0;
                out += ((dot( fru.mPlane[i], vec4(box.mMinX, box.mMinY, box.mMinZ, 1.0f) ) < 0.0 )?1:0);
                out += ((dot( fru.mPlane[i], vec4(box.mMaxX, box.mMinY, box.mMinZ, 1.0f) ) < 0.0 )?1:0);
                out += ((dot( fru.mPlane[i], vec4(box.mMinX, box.mMaxY, box.mMinZ, 1.0f) ) < 0.0 )?1:0);
                out += ((dot( fru.mPlane[i], vec4(box.mMaxX, box.mMaxY, box.mMinZ, 1.0f) ) < 0.0 )?1:0);
                out += ((dot( fru.mPlane[i], vec4(box.mMinX, box.mMinY, box.mMaxZ, 1.0f) ) < 0.0 )?1:0);
                out += ((dot( fru.mPlane[i], vec4(box.mMaxX, box.mMinY, box.mMaxZ, 1.0f) ) < 0.0 )?1:0);
                out += ((dot( fru.mPlane[i], vec4(box.mMinX, box.mMaxY, box.mMaxZ, 1.0f) ) < 0.0 )?1:0);
                out += ((dot( fru.mPlane[i], vec4(box.mMaxX, box.mMaxY, box.mMaxZ, 1.0f) ) < 0.0 )?1:0);
                if( out==8 ) return false;
            }
            */

            bool insideFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, box);
            if (!insideFrustum)
            {
                return false;
            }

            // check frustum outside/inside box 
            int o = 0;
            for (int i = 0; i < 8; i++)
            {
                o += ((frustumCorners[i].x > box.max.x) ? 1 : 0);
                if (o == 8)
                {
                    return false;
                }
            }

            o = 0;
            for (int i = 0; i < 8; i++)
            {
                o += ((frustumCorners[i].x < box.min.x) ? 1 : 0);
                if (o == 8)
                {
                    return false;
                }
            }

            o = 0;
            for (int i = 0; i < 8; i++)
            {
                o += ((frustumCorners[i].y > box.max.y) ? 1 : 0);
                if (o == 8)
                {
                    return false;
                }
            }

            o = 0;
            for (int i = 0; i < 8; i++)
            {
                o += ((frustumCorners[i].y < box.min.y) ? 1 : 0);
                if (o == 8)
                {
                    return false;
                }
            }

            o = 0;
            for (int i = 0; i < 8; i++)
            {
                o += ((frustumCorners[i].z > box.max.z) ? 1 : 0);
                if (o == 8)
                {
                    return false;
                }
            }

            o = 0;
            for (int i = 0; i < 8; i++)
            {
                o += ((frustumCorners[i].z < box.min.z) ? 1 : 0);
                if (o == 8)
                {
                    return false;
                }
            }

            return true; 
        }

        public ReflectionCameraInfo QueueReflection(Camera sourceCamera)
        {
            // this frustum cull test is expensive, but well worth it if it prevents a reflection from needing to be rendered
            bool isReflection;
            if (WeatherMakerLightManagerScript.Instance == null || ShouldIgnoreCamera(sourceCamera, out isReflection))
            {
                return null;
            }

            // HACK: Reflections need to ensure the frustum planes and corners are up to date. This is normally done in a pre-render
            //  event, but we cannot do that as rendering a reflection in pre-render mucks up state and such
            WeatherMakerLightManagerScript.Instance.CalculateFrustumPlanes(sourceCamera);

            // get up to date frustum info
            if (!BoxIntersectsFrustum(sourceCamera, WeatherMakerLightManagerScript.Instance.CurrentCameraFrustumPlanes,
                WeatherMakerLightManagerScript.Instance.CurrentCameraFrustumCorners, ReflectRenderer.bounds))
            {
                return null;
            }

            // Debug.LogFormat("WeatherMakerReflectionScript rendering in {0}, bounds: {1}", sourceCamera.name, ReflectRenderer.bounds);
            ReflectionCameraInfo cam = CreateReflectionCamera(sourceCamera, isReflection);
            RenderReflectionCamera(cam);
            return cam;
        }

        public Camera CameraRenderingReflection(Camera sourceCamera)
        {
            for (int i = 0; i < currentCameras.Count; i++)
            {
                if (currentCameras[i].SourceCamera == sourceCamera)
                {
                    return currentCameras[i].ReflectionCamera;
                }
            }
            return null;
        }

        /// <summary>
        /// Determines whether a camera is a reflection camera
        /// </summary>
        /// <param name="cam">Camera</param>
        /// <param name="camName">Receives camera name for re-use later</param>
        /// <returns>True if cam is a reflection camera, false otherwise</returns>
        public static bool CameraIsReflection(Camera cam, out string camName)
        {
            camName = cam.name;
            return camName.IndexOf("water", StringComparison.OrdinalIgnoreCase) >= 0 ||
                camName.IndexOf("refl", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Render a reflection camera.
        /// </summary>
        /// <param name="info">Camera info</param>
        /// <param name="reflectionTransform">Reflection transform (parent of reflection camera)</param>
        /// <param name="reflectionNormal">Reflection normal vector (up for planes, forward for quads)</param>
        /// <param name="clipPlaneOffset">Clip plane offset for near clipping</param>
        public static void RenderReflection
        (
            ReflectionCameraInfo info,
            Transform reflectionTransform,
            Vector3 reflectionNormal,
            float clipPlaneOffset
        )
        {
            if (info.SourceCamera.stereoEnabled)
            {
                if (info.SourceCamera.stereoTargetEye == StereoTargetEyeMask.Both || info.SourceCamera.stereoTargetEye == StereoTargetEyeMask.Left)
                {
                    RenderReflectionInternal(info, reflectionTransform, reflectionNormal, clipPlaneOffset, StereoTargetEyeMask.Left, info.TargetTexture);
                }
                if (info.SourceCamera.stereoTargetEye == StereoTargetEyeMask.Both || info.SourceCamera.stereoTargetEye == StereoTargetEyeMask.Right)
                {
                    RenderReflectionInternal(info, reflectionTransform, reflectionNormal, clipPlaneOffset, StereoTargetEyeMask.Right, info.TargetTexture2);
                }
            }
            else
            {
                RenderReflectionInternal(info, reflectionTransform, reflectionNormal, clipPlaneOffset, StereoTargetEyeMask.Both, info.TargetTexture);
            }
        }

        /// <summary>
        /// Render a reflection camera. Reflection camera should already be setup with a render texture.
        /// </summary>
        /// <param name="info">Camera info</param>
        /// <param name="reflectionTransform">Reflection transform</param>
        /// <param name="reflectionNormal">Reflection normal vector</param>
        /// <param name="clipPlaneOffset">Clip plane offset for near clipping</param>
        /// <param name="eye">Stereo eye mask</param>
        /// <param name="targetTexture">Target texture</param>
        private static void RenderReflectionInternal
        (
            ReflectionCameraInfo info,
            Transform reflectionTransform,
            Vector3 reflectionNormal,
            float clipPlaneOffset,
            StereoTargetEyeMask eye,
            RenderTexture targetTexture
        )
        {
            bool oldInvertCulling = GL.invertCulling;

            // find out the reflection plane: position and normal in world space
            Vector3 pos = reflectionTransform.position;

            // Render reflection
            // Reflect camera around reflection plane
            if (info.SourceCameraIsReflection && GL.invertCulling)
            {
                reflectionNormal = -reflectionNormal;
            }

            float d = -Vector3.Dot(reflectionNormal, pos) - clipPlaneOffset;
            Vector4 reflectionPlane = new Vector4(reflectionNormal.x, reflectionNormal.y, reflectionNormal.z, d);

            Matrix4x4 reflection;
            CalculateReflectionMatrix(out reflection, reflectionPlane);
            Vector3 savedPos = info.SourceCamera.transform.position;
            Vector3 reflectPos = reflection.MultiplyPoint(savedPos);
            Matrix4x4 worldToCameraMatrix = info.SourceCamera.worldToCameraMatrix;
            if (info.SourceCamera.stereoEnabled && eye == StereoTargetEyeMask.Left)
            {
                worldToCameraMatrix[12] += (info.SourceCamera.stereoSeparation * 0.5f);
                info.ReflectionCamera.projectionMatrix = info.SourceCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
            }
            else if (info.SourceCamera.stereoEnabled && eye == StereoTargetEyeMask.Right)
            {
                worldToCameraMatrix[12] -= (info.SourceCamera.stereoSeparation * 0.5f);
                info.ReflectionCamera.projectionMatrix = info.SourceCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
            }
            else
            {
                info.ReflectionCamera.projectionMatrix = info.SourceCamera.projectionMatrix;
            }
            info.ReflectionCamera.worldToCameraMatrix = worldToCameraMatrix * reflection;
            if (clipPlaneOffset > 0.0f)
            {
                // Optimization: Setup oblique projection matrix so that near plane is our reflection plane.
                // This way we clip everything below/above it for free.
                Vector4 clipPlane = CameraSpacePlane(info.ReflectionCamera, pos, reflectionNormal, clipPlaneOffset, GL.invertCulling ? -1.0f : 1.0f);
                info.ReflectionCamera.projectionMatrix = info.ReflectionCamera.CalculateObliqueMatrix(clipPlane);
            }
            GL.invertCulling = !GL.invertCulling;
            info.ReflectionCamera.transform.position = reflectPos;
            if (++renderCount < maxRenderCount)
            {
                // set the eye mask so that any command buffers know how to render this camera
                info.ReflectionCamera.stereoTargetEye = eye;
                info.ReflectionCamera.targetTexture = targetTexture;
                info.ReflectionCamera.Render();
                info.ReflectionCamera.targetTexture = null;
            }
            info.ReflectionCamera.transform.position = savedPos;
            GL.invertCulling = oldInvertCulling;
        }

        private void AddRenderTextureForSourceCamera(Camera sourceCamera, RenderTexture tex, StereoTargetEyeMask eyeMask)
        {
            List<KeyValuePair<RenderTexture, StereoTargetEyeMask>> tmp;
            if (!sourceCamerasToRenderTextures.TryGetValue(sourceCamera, out tmp))
            {
                sourceCamerasToRenderTextures[sourceCamera] = tmp = new List<KeyValuePair<RenderTexture, StereoTargetEyeMask>>();
            }
            tmp.Add(new KeyValuePair<RenderTexture, StereoTargetEyeMask>(tex, eyeMask));
        }

        private bool ShouldIgnoreCamera(Camera sourceCamera, out bool isReflection)
        {
            string camName;
            isReflection = CameraIsReflection(sourceCamera, out camName);

#if UNITY_EDITOR

            if (sourceCamera.cameraType == CameraType.Preview || camName.IndexOf("preview", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

#endif

            // ignore water and reflection cameras
            if (isReflection && (sourceCamera.transform.parent == null || sourceCamera.transform.parent.GetComponent<WeatherMakerReflectionScript>() == null))
            {
                return true;
            }

            return false;
        }

        private void CleanupCamera(ReflectionCameraInfo info, bool destroyCamera)
        {
            if (info.ReflectionCamera == null)
            {
                return;
            }
            //info.ReflectCamera.targetTexture = null;
            if (destroyCamera

#if UNITY_EDITOR

                && Application.isPlaying

#endif

            )
            {
                DestroyImmediate(info.ReflectionCamera.gameObject);
            }
        }

        private void CleanupCameras(bool destroyCameras)
        {
            cameraCache.AddRange(currentCameras);
            currentCameras.Clear();
            for (int i = cameraCache.Count - 1; i >= 0; i--)
            {
                CleanupCamera(cameraCache[i], destroyCameras);
                if (destroyCameras)
                {
                    cameraCache.RemoveAt(i);
                }
            }
        }

        private void LateUpdate()
        {

#if UNITY_EDITOR

            if (renderCount != 0)
            {
                // Debug.Log("Render count: " + renderCount);
            }

#endif

            CleanupCameras(false);
            renderCount = 0;
        }

        private void OnEnable()
        {
            ReflectRenderer = (ReflectRenderer == null ? GetComponent<Renderer>() : ReflectRenderer);
            if (ReflectRenderer == null)
            {
                Debug.LogError("Reflect renderer is null");
                return;
            }

            if (!initialized && ReflectRenderer.sharedMaterial != null)
            {
                initialized = true;
                if (Application.isPlaying)
                {
                    ReflectRenderer.sharedMaterial = ReflectRenderer.material;
                }
                ReflectRenderer.sharedMaterial.DisableKeyword(mirrorRecursionLimitKeyword);
                for (int i = 0; i < transform.childCount; i++)
                {
                    Camera cam = transform.GetChild(i).GetComponent<Camera>();
                    if (cam != null)
                    {
                        cameraCache.Add(new ReflectionCameraInfo { ReflectionCamera = cam });
                    }
                }
            }
            WeatherMakerCommandBufferManagerScript.Instance.RegisterPreCull(CameraPreCull, this);
            WeatherMakerCommandBufferManagerScript.Instance.RegisterPostRender(CameraPostRender, this);
            ReflectionSamplerName2 = (ReflectionSamplerName + "2");
        }

        private void OnDisable()
        {
            CleanupCameras(true);
            sourceCamerasToRenderTextures.Clear();
        }

        private void OnDestroy()
        {
            if (Application.isPlaying && ReflectRenderer.sharedMaterial != null && ReflectRenderer.sharedMaterial.name.IndexOf("(clone)", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Destroy(ReflectRenderer.sharedMaterial);
            }

            WeatherMakerCommandBufferManagerScript.Instance.UnregisterPreCull(this);
            WeatherMakerCommandBufferManagerScript.Instance.UnregisterPostRender(this);
        }

        private void OnWillRenderObject()
        {
        }

        private void CameraPreCull(Camera camera)
        {
            if (ReflectRenderer.sharedMaterial == null)
            {
                return;
            }

            KeyValuePair<RenderTexture, RenderTexture> kv = new KeyValuePair<RenderTexture, RenderTexture>
            (
                ReflectRenderer.sharedMaterial.GetTexture(ReflectionSamplerName) as RenderTexture,
                ReflectRenderer.sharedMaterial.GetTexture(ReflectionSamplerName2) as RenderTexture
            );
            currentRenderTextures.Add(kv);
            QueueReflection(camera);
        }

        private void CameraPostRender(Camera camera)
        {
            if (currentRenderTextures.Count != 0)
            {
                int idx = currentRenderTextures.Count - 1;
                KeyValuePair<RenderTexture, RenderTexture> kv = currentRenderTextures[idx];
                ReflectRenderer.sharedMaterial.SetTexture(ReflectionSamplerName, kv.Key);
                ReflectRenderer.sharedMaterial.SetTexture(ReflectionSamplerName2, kv.Value);
                currentRenderTextures.RemoveAt(idx);
            }
            for (int i = currentCameras.Count - 1; i >= 0; i--)
            {
                if (currentCameras[i].SourceCamera == camera)
                {
                    CleanupCamera(currentCameras[i], false);
                    currentCameras.RemoveAt(i);
                }
            }
            List<KeyValuePair<RenderTexture, StereoTargetEyeMask>> texturesToRelease;
            string tmp;
            bool isReflectionCamera = CameraIsReflection(camera, out tmp);
            if (sourceCamerasToRenderTextures.TryGetValue(camera, out texturesToRelease))
            {
                // free up temporary render textures
                // if in multi-pass, we only free the texture for the current rendering eye
                // if in single-pass, we free them all
                StereoTargetEyeMask mask = StereoTargetEyeMask.Both;
                for (int i = texturesToRelease.Count - 1; i >= 0; i--)
                {
                    if (isReflectionCamera ||
                        UnityEngine.XR.XRDevice.isPresent && UnityEngine.XR.XRSettings.eyeTextureDesc.vrUsage == VRTextureUsage.OneEye)
                    {
                        switch (camera.stereoActiveEye)
                        {
                            default:
                                mask = StereoTargetEyeMask.Both;
                                break;

                            case Camera.MonoOrStereoscopicEye.Left:
                                mask = StereoTargetEyeMask.Left;
                                break;

                            case Camera.MonoOrStereoscopicEye.Right:
                                mask = StereoTargetEyeMask.Right;
                                break;
                        }
                    }
                    KeyValuePair<RenderTexture, StereoTargetEyeMask> tex = texturesToRelease[i];
                    if (tex.Key != null && (mask & tex.Value) != StereoTargetEyeMask.None)
                    {
                        if (tex.Key.IsCreated())
                        {
                            tex.Key.Release();
                            GameObject.DestroyImmediate(tex.Key);
                        }
                        texturesToRelease.RemoveAt(i);
                    }
                }
            }
        }

        private void SyncCameraSettings(Camera reflectCamera, Camera sourceCamera)
        {
            reflectCamera.nearClipPlane = (NearPlane <= 0.0f ? sourceCamera.nearClipPlane : NearPlane);
            reflectCamera.farClipPlane = (FarPlane <= 0.0f ? sourceCamera.farClipPlane : FarPlane);
            reflectCamera.aspect = (AspectRatio <= 0.0f ? sourceCamera.aspect : AspectRatio);
            if (!reflectCamera.stereoEnabled)
            {
                reflectCamera.fieldOfView = (FieldOfView <= 0.0f ? sourceCamera.fieldOfView : FieldOfView);
            }
            reflectCamera.orthographic = sourceCamera.orthographic;
            reflectCamera.orthographicSize = sourceCamera.orthographicSize;
            reflectCamera.renderingPath = (ReflectionCameraRenderingPath == RenderingPath.UsePlayerSettings ? sourceCamera.renderingPath : ReflectionCameraRenderingPath);
            reflectCamera.backgroundColor = Color.red;
            reflectCamera.clearFlags = ReflectSkybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            reflectCamera.cullingMask = (CurrentRecursionLevel == 0 || ReflectionMaskRecursion.value == 0 ? ReflectionMask : ReflectionMaskRecursion);
            reflectCamera.stereoSeparation = sourceCamera.stereoSeparation;
            reflectCamera.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
            //reflectCamera.transform.rotation = sourceCamera.transform.rotation;
            //reflectCamera.transform.position = sourceCamera.transform.position;

            if (ReflectSkybox)
            {
                if (sourceCamera.gameObject.GetComponent(typeof(Skybox)))
                {
                    Skybox sb = (Skybox)reflectCamera.gameObject.GetComponent(typeof(Skybox));
                    if (!sb)
                    {
                        sb = (Skybox)reflectCamera.gameObject.AddComponent(typeof(Skybox));
                        sb.hideFlags = HideFlags.HideAndDontSave;
                    }
                    sb.material = ((Skybox)sourceCamera.GetComponent(typeof(Skybox))).material;
                }
            }
        }

        private ReflectionCameraInfo CreateReflectionCamera(Camera sourceCamera, bool sourceCameraIsReflection)
        {
            // don't render if we are not enabled
            if (ReflectRenderer == null || !ReflectRenderer.enabled || ReflectRenderer.sharedMaterial == null || sourceCamera == null)
            {
                return null;
            }

            // only render reflection cameras with this script
            WeatherMakerReflectionScript reflScript = (sourceCameraIsReflection && sourceCamera.transform.parent != null ? sourceCamera.transform.parent.GetComponent<WeatherMakerReflectionScript>() : null);
            if (sourceCameraIsReflection && (reflScript == null || reflScript == this))
            {
                // don't render ourselves in our camera
                ReflectRenderer.sharedMaterial.EnableKeyword(mirrorRecursionLimitKeyword);
                return null;
            }

            // recursion limit hit, bail...
            if (reflScript != null && currentCameras.Count >=

#if UNITY_EDITOR

                (Application.isPlaying ? RecursionLimit : 0)

#else

                RecursionLimit

#endif

            )
            {
                ReflectRenderer.sharedMaterial.EnableKeyword(mirrorRecursionLimitKeyword);
                return null;
            }

            ReflectionCameraInfo info;
            if (cameraCache.Count == 0)
            {
                GameObject obj = new GameObject("MirrorReflectionCamera");
                obj.hideFlags = HideFlags.HideAndDontSave;
                obj.SetActive(false);
                obj.transform.parent = transform;
                Camera newReflectionCamera = obj.AddComponent<Camera>();
                newReflectionCamera.enabled = false;
                info = new ReflectionCameraInfo
                {
                    SourceCamera = sourceCamera,
                    ReflectionCamera = newReflectionCamera
                };
            }
            else
            {
                int idx = cameraCache.Count - 1;
                info = cameraCache[idx];
                cameraCache.RemoveAt(idx);
                CleanupCamera(info, false);
            }
            info.SourceCamera = sourceCamera;
            info.SourceCameraIsReflection = sourceCameraIsReflection;
            RenderTextureFormat textureFormat = (sourceCamera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            int size = Math.Max(32, (int)(Mathf.Pow(RecursionRenderTextureSizeReducerPower, (float)CurrentRecursionLevel) * (float)RenderTextureSize));
            if (WeatherMakerScript.Instance != null)
            {
                size = WeatherMakerScript.Instance.PerformanceProfile.ReflectionTextureSize;
            }
            info.TargetTexture = RenderTexture.GetTemporary(size, size, 16, textureFormat);
            info.TargetTexture.wrapMode = TextureWrapMode.Clamp;
            info.TargetTexture.filterMode = FilterMode.Bilinear;
            AddRenderTextureForSourceCamera(sourceCamera, info.TargetTexture, StereoTargetEyeMask.Left);
            if (sourceCamera.stereoEnabled)
            {
                info.TargetTexture2 = RenderTexture.GetTemporary(size, size, 16, textureFormat);
                info.TargetTexture2.wrapMode = TextureWrapMode.Clamp;
                info.TargetTexture2.filterMode = FilterMode.Bilinear;
                AddRenderTextureForSourceCamera(sourceCamera, info.TargetTexture2, StereoTargetEyeMask.Right);
            }
            else
            {
                info.TargetTexture2 = info.TargetTexture;
            }
            currentCameras.Add(info);
            return info;
        }

        private void RenderReflectionCamera(ReflectionCameraInfo info)
        {
            // bail if we don't have a camera or renderer
            if (info == null || info.ReflectionCamera == null || info.SourceCamera == null || ReflectRenderer == null || ReflectRenderer.sharedMaterial == null || !ReflectRenderer.enabled)
            {
                return;
            }

            CurrentRecursionLevel = currentCameras.Count + (info.SourceCameraIsReflection ? 1 : 0);
            renderCount++;
            Camera sourceCamera = info.SourceCamera;
            Camera reflectionCamera = info.ReflectionCamera;
            int oldPixelLightCount = QualitySettings.pixelLightCount;
            int oldCullingMask = reflectionCamera.cullingMask;
            bool oldSoftParticles = QualitySettings.softParticles;
            int oldAntiAliasing = QualitySettings.antiAliasing;
            ShadowQuality oldShadows = QualitySettings.shadows;
            SyncCameraSettings(reflectionCamera, sourceCamera);
            if (WeatherMakerScript.Instance != null && QualitySettings.shadows != ShadowQuality.Disable &&
                WeatherMakerLightManagerScript.ScreenSpaceShadowMode != UnityEngine.Rendering.BuiltinShaderMode.Disabled)
            {
                QualitySettings.shadows = WeatherMakerScript.Instance.PerformanceProfile.ReflectionShadows;
            }

            // MAGIC MIRROR RECURSION OPTIMIZATION
            if (currentCameras.Count > 1)
            {
                if (currentCameras.Count > 3)
                {
                    QualitySettings.shadows = ShadowQuality.Disable;
                }
                else if (WeatherMakerScript.Instance == null && QualitySettings.shadows != ShadowQuality.Disable &&
                    WeatherMakerLightManagerScript.ScreenSpaceShadowMode != UnityEngine.Rendering.BuiltinShaderMode.Disabled)
                {
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                }
                QualitySettings.antiAliasing = 0;
                QualitySettings.softParticles = false;
                QualitySettings.pixelLightCount = 0;
                reflectionCamera.cullingMask &= waterLayerInverse;
            }
            else
            {
                QualitySettings.pixelLightCount = MaximumPerPixelLightsToReflect;
            }

            // get reflection normal vector
            Transform reflectionTransform = transform;
            Vector3 reflectionNormal = (NormalIsForward ? -reflectionTransform.forward : reflectionTransform.up);
            if (TransformNormalNegate)
            {
                reflectionNormal = -reflectionNormal;
            }

            // use shared reflection render function
            RenderReflection(info, reflectionTransform, reflectionNormal, ClipPlaneOffset);

            // restore render state
            reflectionCamera.cullingMask = oldCullingMask;
            QualitySettings.pixelLightCount = oldPixelLightCount;
            QualitySettings.softParticles = oldSoftParticles;
            QualitySettings.antiAliasing = oldAntiAliasing;
            QualitySettings.shadows = oldShadows;
            ReflectRenderer.sharedMaterial.SetTexture(ReflectionSamplerName, info.TargetTexture);
            ReflectRenderer.sharedMaterial.SetTexture(ReflectionSamplerName2, info.TargetTexture2);
            ReflectRenderer.sharedMaterial.DisableKeyword(mirrorRecursionLimitKeyword);

            currentCameras.Remove(info);
            info.SourceCamera = null;
            info.TargetTexture = null;
            info.TargetTexture2 = null;
            cameraCache.Add(info);
        }

        // Given position/normal of the plane, calculates plane in camera space.
        private static Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float clipPlaneOffset, float sideSign)
        {
            Vector3 offsetPos = pos + (normal * clipPlaneOffset);
            Matrix4x4 m = cam.worldToCameraMatrix;
            Vector3 cpos = m.MultiplyPoint(offsetPos);
            Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
        }

        // Calculates reflection matrix around the given plane
        public static void CalculateReflectionMatrix(out Matrix4x4 reflectionMat, Vector4 plane)
        {
            reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
            reflectionMat.m01 = (-2F * plane[0] * plane[1]);
            reflectionMat.m02 = (-2F * plane[0] * plane[2]);
            reflectionMat.m03 = (-2F * plane[3] * plane[0]);

            reflectionMat.m10 = (-2F * plane[1] * plane[0]);
            reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
            reflectionMat.m12 = (-2F * plane[1] * plane[2]);
            reflectionMat.m13 = (-2F * plane[3] * plane[1]);

            reflectionMat.m20 = (-2F * plane[2] * plane[0]);
            reflectionMat.m21 = (-2F * plane[2] * plane[1]);
            reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
            reflectionMat.m23 = (-2F * plane[3] * plane[2]);

            reflectionMat.m30 = 0F;
            reflectionMat.m31 = 0F;
            reflectionMat.m32 = 0F;
            reflectionMat.m33 = 1F;
        }
    }
}
