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
using UnityEngine.UI;

namespace DigitalRuby.WeatherMaker
{

#if UNITY_EDITOR

    public class WeatherMakerNoiseGeneratorScript : MonoBehaviour
    {
        [Header("Noise")]
        [Tooltip("Noise material")]
        public Material NoiseMaterial;

        [Tooltip("Noise render texture")]
        public RenderTexture NoiseRenderTexture;

        [Tooltip("Dimensions of each slice")]
        [Range(16, 2048)]
        public int Size = 128;

        [Tooltip("Number of slices")]
        [Range(1, 1024)]
        public int Count = 128;

        [Tooltip("Step iteration (z) for noise function, 0 for auto based on count")]
        [Range(0.0f, 1.0f)]
        public float Step = 0.0f;

        [Tooltip("How many frames to advance one frame")]
        [Range(1, 60)]
        public int FPSFrame = 1;
        private int fpsFrameCounter;
        private int frameIndex;
        private float frame;

        [Tooltip("Whether to auto set the frame, set to false to allow ManualFrame override")]
        public bool AutoStepFrame = true;

        [Tooltip("Manually set the frame")]
        [Range(0, 1024)]
        public float ManualFrame;

        private Text frameLabel;

        private void Awake()
        {
            GameObject obj = GameObject.Find("FrameLabel");
            if (obj != null)
            {
                frameLabel = obj.GetComponent<Text>();
            }
        }

        private void Update()
        {
            if (++fpsFrameCounter >= FPSFrame || !AutoStepFrame)
            {
                float step = (Step <= 0.0f ? 1.0f / (float)Count : Step);
                fpsFrameCounter = 0;
                frame = (AutoStepFrame ? (float)frameIndex * step : (float)ManualFrame * step);
                if (frameLabel != null)
                {
                    frameLabel.text = "Frame: " + (AutoStepFrame ? frameIndex : ManualFrame);
                }
                
                NoiseMaterial.SetFloat(WMS._Frame, frame);
                NoiseMaterial.SetFloat(WMS._EndFrame, (float)Count * step);
                NoiseMaterial.SetFloat(WMS._FrameStep, step);
                if (AutoStepFrame)
                {
                    if (++frameIndex >= Count)
                    {
                        frameIndex = 0;
                    }
                }
                else
                {
                    frameIndex = 0;
                }
                Graphics.Blit(null, NoiseRenderTexture, NoiseMaterial, 0);
            }
        }

        public void ExportClicked()
        {
            RenderTexture renderTexture = new RenderTexture(Size, Size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            renderTexture.autoGenerateMips = false;
            renderTexture.name = "WeatherMakerDemoNoiseScript";
            Texture2D tex2D = new Texture2D(Size, Size, TextureFormat.ARGB32, false, false);
            tex2D.filterMode = FilterMode.Bilinear;
            tex2D.wrapMode = TextureWrapMode.Clamp;
            frame = 0.0f;
            float step = (Step <= 0.0f ? 1.0f / (float)Count : Step);
            NoiseMaterial.SetFloat(WMS._EndFrame, (float)Count * step);
            NoiseMaterial.SetFloat(WMS._FrameStep, step);
            for (int i = 0; i < Count; i++)
            {
                frame = (float)i * step;
                if (frameLabel != null)
                {
                    frameLabel.text = "Frame: " + i;
                }
                NoiseMaterial.SetFloat(WMS._Frame, frame);
                Graphics.Blit(null, renderTexture, NoiseMaterial, 0);
                RenderTexture.active = renderTexture;
                tex2D.ReadPixels(new Rect(0, 0, Size, Size), 0, 0, false);
                tex2D.Apply();
                RenderTexture.active = null;
                GL.Flush();
                byte[] imageData = tex2D.EncodeToPNG();
                string docsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
                docsPath = System.IO.Path.Combine(docsPath, "WeatherMakerNoiseTexture");
                System.IO.Directory.CreateDirectory(docsPath);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(docsPath, "WeatherMakerNoiseTexture_" + i.ToString("D4") + ".png"), imageData);
            }
            renderTexture.Release();
            Destroy(renderTexture);
            Destroy(tex2D);
        }
    }

#endif

}
