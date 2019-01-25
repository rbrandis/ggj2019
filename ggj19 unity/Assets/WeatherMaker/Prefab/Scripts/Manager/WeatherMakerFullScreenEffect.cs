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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// #define COPY_FULL_DEPTH_TEXTURE

namespace DigitalRuby.WeatherMaker
{
    /// <summary>
    /// Down sample scales - do not change the values ever!
    /// </summary>
    public enum WeatherMakerDownsampleScale
    {
        Disabled = 0, // disabled
        FullResolution = 1, // full resolution
        HalfResolution = 2, // divide by 2
        QuarterResolution = 4, // divide by 4
        EighthResolution = 8, // divide by 8
        SixteenthResolution = 16 // divide by 16
    }

    /// <summary>
    /// Temporal reprojection size
    /// </summary>
    public enum WeatherMakerTemporalReprojectionSize
    {
        None = 0,
        One = 1, // only really useful for debugging to make sure image does not change
        TwoByTwo = 2,
        ThreeByThree = 3,
        FourByFour = 4,
        FiveByFive = 5,
        SixBySix = 6,
        SevenBySeven = 7,
        EightByEight = 8
    }

    [System.Serializable]
    public class WeatherMakerFullScreenEffect : System.IDisposable
    {
        private readonly List<WeatherMakerTemporalReprojectionState> temporalStates = new List<WeatherMakerTemporalReprojectionState>();

        [Tooltip("Render queue for this full screen effect. Do not change this at runtime, set it in the inspector once.")]
        public CameraEvent RenderQueue = CameraEvent.BeforeForwardAlpha;

        [Tooltip("Material for rendering/creating the effect")]
        public Material Material;

        [Tooltip("Material for blurring")]
        public Material BlurMaterial;

        [Tooltip("Material to render the final pass if needed, not all setups will need this but it should be set anyway")]
        public Material BlitMaterial;

        [Tooltip("Material to down-sample depth buffer if needed, can be null")]
        public Material DepthMaterial;

        [Tooltip("Material for temporal reprojection")]
        public Material TemporalReprojectionMaterial;

        [Tooltip("Temporal reprojection")]
        public WeatherMakerTemporalReprojectionSize TemporalReprojection = WeatherMakerTemporalReprojectionSize.None;
        private WeatherMakerTemporalReprojectionSize lastTemporalReprojection = (WeatherMakerTemporalReprojectionSize)(-1);

        [Tooltip("Downsample scale for Material")]
        public WeatherMakerDownsampleScale DownsampleScale = WeatherMakerDownsampleScale.FullResolution;
        private WeatherMakerDownsampleScale lastDownSampleScale = (WeatherMakerDownsampleScale)(-1);

        [Tooltip("Downsample scale for render buffer sampling, or 0 to not sample the render buffer.")]
        public WeatherMakerDownsampleScale DownsampleRenderBufferScale = WeatherMakerDownsampleScale.Disabled;
        private WeatherMakerDownsampleScale lastDownsampleRenderBufferScale = (WeatherMakerDownsampleScale)(-1);

        [Tooltip("Name of the texture to set if sampling the render buffer")]
        public string DownsampleRenderBufferTextureName = "_MainTex2";

        [Tooltip("Blur shader type")]
        public BlurShaderType BlurShaderType = BlurShaderType.None;
        private BlurShaderType lastBlurShaderType = (BlurShaderType)0x7FFFFFFF;

        private int lastScreenWidth, lastScreenHeight;

        [Tooltip("ZTest")]
        public UnityEngine.Rendering.CompareFunction ZTest = CompareFunction.Always;

        /// <summary>
        /// The name for the command buffer that will be created for this effect. This should be unique for your project.
        /// </summary>
        public string CommandBufferName { get; set; }

        /// <summary>
        /// Whether the effect is enabled. The effect can be disabled to prevent command buffers from being created.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Action to fire when Material properties should be updated
        /// </summary>
        public System.Action<WeatherMakerCommandBuffer> UpdateMaterialProperties { get; set; }

        private WeatherMakerCommandBuffer weatherMakerCommandBuffer;
        internal bool needsToBeRecreated;

        private readonly List<Camera> cameras = new List<Camera>();

        public static RenderTextureDescriptor GetRenderTextureDescriptor(int scale, int mod, int scale2, RenderTextureFormat format, int depth = 0, Camera camera = null)
        {
            RenderTextureDescriptor desc;
            if (UnityEngine.XR.XRDevice.isPresent && (camera == null || camera.stereoEnabled))
            {
                desc = UnityEngine.XR.XRSettings.eyeTextureDesc;
            }
            else if (camera == null)
            {
                desc = new RenderTextureDescriptor(Screen.width, Screen.height, format, depth);
            }
            else
            {
                desc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight, format, depth);
            }
            desc.depthBufferBits = depth;
            desc.width = desc.width / scale;
            desc.height = desc.height / scale;
            desc.autoGenerateMips = false;
            desc.useMipMap = false;
            if (mod > 0)
            {
                while (desc.width % mod != 0) { desc.width++; }
                while (desc.height % mod != 0) { desc.height++; }
            }
            desc.width /= scale2;
            desc.height /= scale2;
            return desc;
        }

        internal static RenderTexture CreateRenderTexture(RenderTextureDescriptor desc, FilterMode filterMode = FilterMode.Bilinear, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            RenderTexture tex = new RenderTexture(desc);
            tex.filterMode = filterMode;
            tex.wrapMode = wrapMode;
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.useMipMap = tex.autoGenerateMips = false;
            tex.Create();
            return tex;
        }

        internal static void DestroyRenderTexture(ref RenderTexture tex)
        {
            if (tex != null)
            {
                try
                {
                    tex.Release();
                }
                catch
                {
                }
                try
                {
                    GameObject.DestroyImmediate(tex);
                }
                catch
                {
                }
                tex = null;
            }
        }

        internal static RenderTexture DestroyRenderTexture(RenderTexture tex)
        {
            DestroyRenderTexture(ref tex);
            return tex;
        }

        internal static void ReleaseCommandBuffer(ref CommandBuffer commandBuffer)
        {
            if (commandBuffer != null)
            {
                try
                {
                    commandBuffer.Release();
                    commandBuffer = null;
                }
                catch
                {
                }
            }
        }

        private void AttachPostProcessing(CommandBuffer commandBuffer, Material material, int w, int h, RenderTextureFormat defaultFormat,
            RenderTargetIdentifier renderedImageId, WeatherMakerTemporalReprojectionState reprojState, Camera camera,
            ref int postSourceId, ref RenderTargetIdentifier postSource)
        {
            if (material.passCount > 2)
            {
                if (renderedImageId == BuiltinRenderTextureType.CameraTarget)
                {
                    Debug.LogError("Weather Maker command buffer post processing cannot blit directly to camera target");
                }
                else
                {
                    if (reprojState == null)
                    {
                        // make a copy of the rendered image, use it to make a new rendered image with post process
                        postSourceId = Shader.PropertyToID("_MainTex2");
                        postSource = new RenderTargetIdentifier(postSourceId);
                        commandBuffer.GetTemporaryRT(postSourceId, GetRenderTextureDescriptor((int)DownsampleScale, 0, 1, defaultFormat, 0, camera), FilterMode.Bilinear);
                        // commandBuffer.GetTemporaryRT(postSourceId, w, h, 0, FilterMode.Bilinear, defaultFormat);

                        // assume third pass is a post process pass, save rendered image for re-use
                        commandBuffer.Blit(renderedImageId, postSource);
                        commandBuffer.Blit(postSource, renderedImageId, material, 2);
                    }
                    else
                    {
                        // take advantage of temporal reproj saving the frame to use it for post process, no need for extra blit
                        commandBuffer.SetGlobalTexture(WMS._MainTex2, reprojState.PrevFrameTexture);
                        commandBuffer.Blit(reprojState.PrevFrameTexture, renderedImageId, material, 2);
                    }
                }
            }
        }

        private void AttachTemporalReprojection(CommandBuffer commandBuffer, WeatherMakerTemporalReprojectionState reprojState, RenderTargetIdentifier depthTarget,
            RenderTargetIdentifier renderedImageId, Material material, int scale, RenderTextureFormat defaultFormat)
        {
            if (reprojState.NeedsFirstFrameHandling)
            {
                // do a more expensive render once to get the image looking nice, only happens once on first frame
                commandBuffer.Blit(depthTarget, reprojState.PrevFrameTexture, material, 0);

                // copy to sub frame so it's ready for next pass
                commandBuffer.Blit(reprojState.PrevFrameTexture, reprojState.SubFrameTexture);

                // copy to final image
                commandBuffer.Blit(reprojState.PrevFrameTexture, renderedImageId);
            }
            else if (reprojState.IntegratedTemporalReprojection)
            {
                // render fast pass with offset temporal reprojection so it is available in the full pass
                commandBuffer.SetGlobalFloat(WMS._WeatherMakerTemporalReprojectionEnabled, 1.0f);
                commandBuffer.Blit(depthTarget, reprojState.SubFrameTexture, material, 0);

                // perform full pass with full temporal reprojection
                commandBuffer.SetGlobalFloat(WMS._WeatherMakerTemporalReprojectionEnabled, 2.0f);

                //if (scale == 1)
                {
                    commandBuffer.Blit(reprojState.SubFrameTexture, renderedImageId, material, 0);
                }
                /*
                else
                {
                    // render to full res texture and downsample
                    int fullResTexId = Shader.PropertyToID("_WM_FullResTex");
                    RenderTargetIdentifier tempRT = new RenderTargetIdentifier(fullResTexId);
                    commandBuffer.GetTemporaryRT(fullResTexId, GetRenderTextureDescriptor(1, 0, 1, defaultFormat));
                    commandBuffer.Blit(reprojState.SubFrameTexture, tempRT, material, 0);
                    commandBuffer.Blit(tempRT, renderedImageId);
                }
                */

                // copy to previous frame so we can re-use it next frame
                if (reprojState.BlendMode == WeatherMakerTemporalReprojectionState.TemporalReprojectionBlendMode.Blur)
                {
                    commandBuffer.Blit(reprojState.SubFrameTexture, reprojState.PrevFrameTexture);
                }
                else
                {
                    commandBuffer.Blit(renderedImageId, reprojState.PrevFrameTexture);
                }
            }
            else
            {
                // render to sub frame (fast, this is a small texture)
                commandBuffer.SetGlobalFloat(WMS._WeatherMakerTemporalReprojectionEnabled, 1.0f);
                commandBuffer.Blit(depthTarget, reprojState.SubFrameTexture, material, 0);

                // combine sub frame and prev frame to final image
                commandBuffer.Blit(reprojState.PrevFrameTexture, renderedImageId, reprojState.TemporalReprojectionMaterial, 0);

                // copy combined to previous frame for re-use next frame
                commandBuffer.Blit(renderedImageId, reprojState.PrevFrameTexture);
            }

            commandBuffer.SetGlobalFloat(WMS._WeatherMakerTemporalReprojectionEnabled, 0.0f);
        }

        private void AttachBlurBlit(CommandBuffer commandBuffer, RenderTargetIdentifier renderedImageId, RenderTargetIdentifier depthTextureId,
            Material material, BlurShaderType blur, RenderTextureFormat defaultFormat)
        {
            if (material.passCount > 1)
            {
                // assume second pass is a depth writer pass, just writes the chosen depth pixel to a new texture
                // TODO: Would be nice to just use the depth buffer of the render texture to do this in one pass,
                // but Unity does not appear to expose pulling out the depth buffer of a temporary render texture using command buffers
                commandBuffer.Blit(renderedImageId, depthTextureId, material, 1);
            }

            // set blend mode for blitting to camera target
            commandBuffer.SetGlobalFloat(WMS._SrcBlendMode, (float)BlendMode.One);
            commandBuffer.SetGlobalFloat(WMS._DstBlendMode, (float)BlendMode.OneMinusSrcAlpha);

            // blur if requested
            if (blur == BlurShaderType.None)
            {
                // blit texture directly on top of camera without blur, depth aware alpha blend
                commandBuffer.Blit(renderedImageId, BuiltinRenderTextureType.CameraTarget, BlitMaterial);
            }
            else
            {
                // blur texture directly on to camera target, depth and alpha aware blur, alpha blend
                if (blur == BlurShaderType.GaussianBlur7)
                {
                    commandBuffer.SetGlobalFloat(WMS._Blur7, 1.0f);
                    commandBuffer.Blit(renderedImageId, BuiltinRenderTextureType.CameraTarget, BlurMaterial);
                }
                else if (blur == BlurShaderType.GaussianBlur17 || BlurMaterial.passCount < 4)
                {
                    commandBuffer.SetGlobalFloat(WMS._Blur7, 0.0f);
                    commandBuffer.Blit(renderedImageId, BuiltinRenderTextureType.CameraTarget, BlurMaterial);
                }
                else
                {
                    int tmp = Shader.PropertyToID("_MainTex4");
                    int scale = (int)DownsampleScale;
                    RenderTargetIdentifier tmpId = new RenderTargetIdentifier(tmp);
                    RenderTextureDescriptor desc = GetRenderTextureDescriptor(scale, 0, 1, defaultFormat);
                    commandBuffer.GetTemporaryRT(tmp, desc, FilterMode.Bilinear);
                    int blurPass;
                    int blitPass;
                    switch (scale)
                    {
                        default:
                            blurPass = 0;
                            blitPass = 10;
                            break;

                        case 2:
                            blurPass = 2;
                            blitPass = 11;
                            break;

                        case 4:
                            blurPass = 4;
                            blitPass = 12;
                            break;

                        case 8:
                            blurPass = 6;
                            blitPass = 13;
                            break;

                        case 16:
                            blurPass = 8;
                            blitPass = 14;
                            break;
                    }

                    // horizontal blur
                    commandBuffer.Blit(renderedImageId, tmpId, BlurMaterial, blurPass);

                    // vertical blur
                    commandBuffer.Blit(tmp, renderedImageId, BlurMaterial, blurPass + 1);

                    // upsample
                    commandBuffer.Blit(renderedImageId, BuiltinRenderTextureType.CameraTarget, BlurMaterial, blitPass);

                    commandBuffer.ReleaseTemporaryRT(tmp);
                }
            }
        }

        private WeatherMakerCommandBuffer CreateCommandBuffer(Camera camera, WeatherMakerTemporalReprojectionState reprojState, WeatherMakerDownsampleScale downsampleScale)
        {
            if (WeatherMakerScript.Instance == null)
            {
                Debug.LogError("Cannot create command buffer, WeatherMakerScript.Instance is null");
                return null;
            }

            // Debug.Log("Creating command buffer " + CommandBufferName);

            // multi-pass vr uses one command buffer for each eye
            if (camera.stereoTargetEye == StereoTargetEyeMask.Right)
            {
                WeatherMakerCommandBuffer existingCommandBuffer = WeatherMakerScript.Instance.CommandBufferManager.GetCommandBuffer(camera, RenderQueue, CommandBufferName);
                if (existingCommandBuffer != null)
                {
                    return existingCommandBuffer;
                }
            }

            RenderTextureFormat defaultFormat = (camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            CommandBuffer commandBuffer = new CommandBuffer { name = CommandBufferName };

            int frameBufferSourceId = -1;
            int postSourceId = -1;
            RenderTargetIdentifier frameBufferSource = frameBufferSourceId;
            RenderTargetIdentifier postSource = postSourceId;
            Material material = new Material(Material);
            material.name += " (Clone)";

            int scale = (int)downsampleScale;
            if (reprojState != null && reprojState.BlendMode == WeatherMakerTemporalReprojectionState.TemporalReprojectionBlendMode.Blur)
            {
                commandBuffer.SetGlobalFloat(WMS._WeatherMakerDownsampleScale, (float)Mathf.Min(16, (scale * reprojState.ReprojectionSize)));
            }
            else
            {
                commandBuffer.SetGlobalFloat(WMS._WeatherMakerDownsampleScale, scale);
            }

            if (DownsampleRenderBufferScale != WeatherMakerDownsampleScale.Disabled)
            {
                // render camera target to texture, performing separate down-sampling
                frameBufferSourceId = Shader.PropertyToID(DownsampleRenderBufferTextureName);
                frameBufferSource = new RenderTargetIdentifier(frameBufferSourceId);
                commandBuffer.GetTemporaryRT(frameBufferSourceId, GetRenderTextureDescriptor((int)DownsampleRenderBufferScale, 0, 1, defaultFormat, 0, camera), FilterMode.Bilinear);
                commandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, frameBufferSource);
            }

            commandBuffer.SetGlobalFloat(WMS._WeatherMakerTemporalReprojectionEnabled, 0.0f);
            commandBuffer.SetGlobalFloat(WMS._ZTest, (float)ZTest);

            // if no blur, no downsample, no temporal reprojection and no post processing (pass count > 2) then we can just draw directly to camera target
            if (BlurShaderType == BlurShaderType.None && DownsampleScale == WeatherMakerDownsampleScale.FullResolution && reprojState == null && material.passCount < 3)
            {
                // set blend mode for blitting to camera target
                commandBuffer.SetGlobalFloat(WMS._SrcBlendMode, (float)BlendMode.One);
                commandBuffer.SetGlobalFloat(WMS._DstBlendMode, (float)BlendMode.OneMinusSrcAlpha);
                commandBuffer.Blit(frameBufferSource, BuiltinRenderTextureType.CameraTarget, material, 0);
            }
            else
            {
                // render to texture, using current image target as input _MainTex, no blend
                // alpha or blue will use _MainTex to render the final result
                WeatherMakerCameraType cameraType = WeatherMakerScript.GetCameraType(camera);
                int renderTargetRenderedImageId = Shader.PropertyToID("_MainTex3");
                int renderTargetDepthTextureId = Shader.PropertyToID("_WeatherMakerTemporaryDepthTexture");
                RenderTargetIdentifier renderTargetRenderedImage = new RenderTargetIdentifier(renderTargetRenderedImageId);
                RenderTargetIdentifier renderTargetDepthTexture = new RenderTargetIdentifier(renderTargetDepthTextureId);
                BlurShaderType blur = BlurShaderType;

                if (cameraType != WeatherMakerCameraType.Normal)
                {
                    if (BlurMaterial.passCount > 3)
                    {
                        blur = BlurShaderType.Bilateral;
                    }
                    else
                    {
                        blur = BlurShaderType.GaussianBlur17;
                    }
                }

                int mod = (reprojState == null ? 0 : reprojState.ReprojectionSize);
                commandBuffer.GetTemporaryRT(renderTargetRenderedImageId, GetRenderTextureDescriptor(scale, mod, 1, defaultFormat, 0, camera), FilterMode.Bilinear);
                commandBuffer.GetTemporaryRT(renderTargetDepthTextureId, GetRenderTextureDescriptor(scale, mod, 1, RenderTextureFormat.RFloat, 0, camera), FilterMode.Point);

                // set blend mode for blitting to render texture
                commandBuffer.SetGlobalFloat(WMS._SrcBlendMode, (float)BlendMode.One);
                commandBuffer.SetGlobalFloat(WMS._DstBlendMode, (float)BlendMode.Zero);

                if (reprojState == null || reprojState.TemporalReprojectionMaterial == null)
                {
                    // render to final destination
                    commandBuffer.Blit(renderTargetDepthTextureId, renderTargetRenderedImage, material, 0);
                }
                else
                {
                    AttachTemporalReprojection(commandBuffer, reprojState, renderTargetDepthTextureId, renderTargetRenderedImage, material, scale, defaultFormat);
                }

                if (cameraType == WeatherMakerCameraType.Normal)
                {
                    AttachPostProcessing(commandBuffer, material, 0, 0, defaultFormat, renderTargetRenderedImage, reprojState, camera, ref postSourceId, ref postSource);
                    //AttachPostProcessing(commandBuffer, material, w, h, defaultFormat, renderTargetRenderedImageId, reprojState, ref postSourceId, ref postSource);
                }

                AttachBlurBlit(commandBuffer, renderTargetRenderedImage, renderTargetDepthTexture, material, blur, defaultFormat);

                // cleanup
                commandBuffer.ReleaseTemporaryRT(renderTargetRenderedImageId);
                commandBuffer.ReleaseTemporaryRT(renderTargetDepthTextureId);

                commandBuffer.SetGlobalTexture(WMS._MainTex2, new RenderTargetIdentifier(BuiltinRenderTextureType.None));
            }

            if (DownsampleRenderBufferScale != WeatherMakerDownsampleScale.Disabled)
            {
                // cleanup
                commandBuffer.ReleaseTemporaryRT(frameBufferSourceId);
            }

            // add to manager
            weatherMakerCommandBuffer = new WeatherMakerCommandBuffer
            {
                Camera = camera,
                CommandBuffer = commandBuffer,
                Material = material,
                ReprojectionState = reprojState,
                RenderQueue = RenderQueue,
                UpdateMaterial = UpdateMaterialProperties,
                CameraType = WeatherMakerScript.GetCameraType(camera)
            };
            WeatherMakerScript.Instance.CommandBufferManager.AddCommandBuffer(weatherMakerCommandBuffer);
            if (!cameras.Contains(camera))
            {
                cameras.Add(camera);
            }

            return weatherMakerCommandBuffer;
        }

        /// <summary>
        /// Call from LateUpdate in script
        /// </summary>
        public void SetupEffect
        (
            Material material,
            Material blitMaterial,
            Material blurMaterial,
            BlurShaderType blurShaderType,
            WeatherMakerDownsampleScale downSampleScale,
            WeatherMakerDownsampleScale downsampleRenderBufferScale,
            Material temporalReprojectionMaterial,
            WeatherMakerTemporalReprojectionSize temporalReprojection,
            System.Action<WeatherMakerCommandBuffer> updateMaterialProperties,
            bool enabled
        )
        {
            Enabled = enabled;
            needsToBeRecreated = false;
            if (Enabled)
            {
                if (material != Material)
                {
                    needsToBeRecreated = true;
                    Material = material;
                }
                if (BlitMaterial != blitMaterial)
                {
                    needsToBeRecreated = true;
                    BlitMaterial = blitMaterial;
                }
                if (BlurMaterial != blurMaterial)
                {
                    needsToBeRecreated = true;
                    BlurMaterial = blurMaterial;
                }
                if (TemporalReprojectionMaterial != temporalReprojectionMaterial)
                {
                    needsToBeRecreated = true;
                    TemporalReprojectionMaterial = temporalReprojectionMaterial;
                }
                BlurShaderType = blurShaderType;
                DownsampleScale = (downSampleScale == WeatherMakerDownsampleScale.Disabled ? WeatherMakerDownsampleScale.FullResolution : downSampleScale);
                DownsampleRenderBufferScale = downsampleRenderBufferScale;
                TemporalReprojection = temporalReprojection;
                if (UpdateMaterialProperties != updateMaterialProperties)
                {
                    needsToBeRecreated = true;
                    UpdateMaterialProperties = updateMaterialProperties;
                }
                needsToBeRecreated |= lastDownSampleScale != DownsampleScale ||
                    lastDownsampleRenderBufferScale != DownsampleRenderBufferScale ||
                    lastBlurShaderType != BlurShaderType ||
                    lastTemporalReprojection != TemporalReprojection ||
                    lastScreenWidth != Screen.width ||
                    lastScreenHeight != Screen.height;
                lastDownSampleScale = DownsampleScale;
                lastDownsampleRenderBufferScale = DownsampleRenderBufferScale;
                lastBlurShaderType = BlurShaderType;
                lastTemporalReprojection = TemporalReprojection;
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;

                if (needsToBeRecreated)
                {
                    WeatherMakerScript.Instance.CommandBufferManager.RemoveCommandBuffers(CommandBufferName);
                }
            }
            else
            {
                Dispose();
            }
        }

        /// <summary>
        /// Update the full screen effect, usually called from OnPreRender or OnPreCull
        /// </summary>
        /// <param name="camera">Camera</param>
        /// <param name="integratedTemporalReprojection">Whether to use integrated temporal reprojection (if temporal reprojection is enabled)</param>
        public void PreCullCamera(Camera camera, bool integratedTemporalReprojection = true)
        {
            //integratedTemporalReprojection = false;
            if (WeatherMakerScript.Instance != null && WeatherMakerScript.Instance.CommandBufferManager != null && Enabled && Material != null && camera != null)
            {
                WeatherMakerCameraType cameraType = WeatherMakerScript.GetCameraType(camera);
                if (cameraType == WeatherMakerCameraType.Other)
                {
                    return;
                }

                WeatherMakerTemporalReprojectionState reprojState = null;

                // setup temporal reprojection state
                reprojState = temporalStates.Find(b => b.Camera == camera);

                if (reprojState == null)
                {
                    // temporal reprojection is not currently possible with cubemap
                    if (TemporalReprojection != WeatherMakerTemporalReprojectionSize.None && TemporalReprojectionMaterial != null &&
                        (cameraType == WeatherMakerCameraType.Normal || cameraType == WeatherMakerCameraType.Reflection))
                    {
                        reprojState = new WeatherMakerTemporalReprojectionState(camera, TemporalReprojectionMaterial, integratedTemporalReprojection);
                        temporalStates.Add(reprojState);
                        needsToBeRecreated = true;
                    }
                }
                else if (TemporalReprojection == WeatherMakerTemporalReprojectionSize.None || TemporalReprojectionMaterial == null)
                {
                    reprojState.Dispose();
                    temporalStates.Remove(reprojState);
                    reprojState = null;
                }
                else
                {
                    // if transition from first frame, recreate again
                    needsToBeRecreated |= reprojState.NeedsFirstFrameHandling;
                    reprojState.NeedsFirstFrameHandling = false;
                }

                if (reprojState != null)
                {
                    // if first frame, recreate command buffer
                    needsToBeRecreated |= reprojState.NeedsFirstFrameHandling;
                }

                WeatherMakerDownsampleScale downsampleScale;
                if (cameraType == WeatherMakerCameraType.Normal)
                {
                    downsampleScale = DownsampleScale;
                }
                else
                {
                    if (camera.pixelWidth > 2000)
                    {
                        downsampleScale = WeatherMakerDownsampleScale.QuarterResolution;
                    }
                    else if (camera.pixelWidth > 1000)
                    {
                        downsampleScale = WeatherMakerDownsampleScale.HalfResolution;
                    }
                    else
                    {
                        downsampleScale = DownsampleScale;
                    }
                }

                if (needsToBeRecreated || !WeatherMakerScript.Instance.CommandBufferManager.ContainsCommandBuffer(camera, RenderQueue, CommandBufferName))
                {
                    WeatherMakerCommandBuffer cmdBuffer = CreateCommandBuffer(camera, reprojState, downsampleScale);
                    if (reprojState != null)
                    {
                        reprojState.CommandBuffer = cmdBuffer;
                    }
                }

                if (reprojState != null)
                {
                    WeatherMakerTemporalReprojectionSize reprojSize = (cameraType == WeatherMakerCameraType.Normal ? TemporalReprojection : WeatherMakerTemporalReprojectionSize.FourByFour);
                    reprojState.PreCullFrame(camera, downsampleScale, reprojSize);
                }
            }
        }

        public void PreRenderCamera(Camera camera)
        {
            // pre render handled by command buffer manager
        }

        public void PostRenderCamera(Camera camera)
        {
            if (camera != null && Enabled)
            {
                WeatherMakerTemporalReprojectionState reprojState = temporalStates.Find(b => b.Camera == camera);
                if (reprojState != null)
                {
                    reprojState.PostRenderFrame(camera);
                }
            }
        }

        /// <summary>
        /// Cleanup all resources and set Enabled to false
        /// </summary>
        public void Dispose()
        {
            lastDownSampleScale = (WeatherMakerDownsampleScale)(-1);
            lastDownsampleRenderBufferScale = (WeatherMakerDownsampleScale)(-1);
            lastBlurShaderType = (BlurShaderType)0x7FFFFFFF;
            lastTemporalReprojection = (WeatherMakerTemporalReprojectionSize)(-1);
            if (WeatherMakerScript.Instance != null)
            {
                foreach (Camera c in cameras)
                {
                    if (c != null)
                    {
                        WeatherMakerScript.Instance.CommandBufferManager.RemoveCommandBuffer(c, CommandBufferName);
                    }
                }
            }
            cameras.Clear();
            foreach (WeatherMakerTemporalReprojectionState state in temporalStates)
            {
                state.Dispose();
            }
            temporalStates.Clear();
            Enabled = false;
        }
    }
}