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

namespace DigitalRuby.WeatherMaker
{
    [CreateAssetMenu(fileName = "WeatherMakerCloudLayerVolumetricProfile", menuName = "WeatherMaker/Cloud Layer Volumetric Profile", order = 41)]
    public class WeatherMakerCloudVolumetricProfileScript : ScriptableObject
    {
        [Header("Clouds - noise")]
        [Tooltip("Texture for cloud noise shape (perlin, worley) - RGBA")]
        public Texture3D CloudNoiseShape;

        [Tooltip("Texture for cloud noise detail (worley) - A")]
        public Texture3D CloudNoiseDetail;

        [Tooltip("Texture for cloud noise curl (turbulence) - RGB (XYZ)")]
        public Texture2D CloudNoiseCurl;

        [MinMaxSlider(16, 256, "Max sample count, higher values use more processing")]
        public RangeOfIntegers CloudNoiseSampleCount = new RangeOfIntegers { Minimum = 64, Maximum = 128 };

        [MinMaxSlider(0.0f, 10.0f, "Minimum and maximum lod for noise sampling")]
        public RangeOfFloats CloudNoiseLod = new RangeOfFloats { Minimum = 0.0f, Maximum = 1.0f };

        [Tooltip("Dir light sample count for shadows")]
        [Range(0, 6)]
        public int CloudDirLightSampleCount = 5;

        [Tooltip("Cloud noise scale (shape, detail, curl, curl intensity)")]
        public Vector4 CloudNoiseScale = new Vector4(0.65f, 1.3f, 0.4f, 0.2f);

        [MinMaxSlider(0.01f, 4.0f, "Cloud noise scalar, x = multiplier, y = adder, zw = reserved.")]
        public RangeOfFloats CloudNoiseScalar = new RangeOfFloats(0.95f, 1.06f);

        [MinMaxSlider(0.0f, 1000.0f, "Cloud noise height power, controls how uniform noise is. Lower values produce more uniform noise at lower heights.")]
        public RangeOfFloats CloudHeightNoisePowerVolumetric = new RangeOfFloats(100.0f, 100.0f);

        [Tooltip("Max ray length multiplier, accumulates noise over longer distance, especially at horizon")]
        [Range(1.0f, 100.0f)]
        public float CloudMaxRayLengthMultiplier = 10.0f;

        [Tooltip("Max optical depth multiplier, determines horizon fade and other sky blending effects")]
        [Range(1.0f, 100.0f)]
        public float CloudOpticalDistanceMultiplier = 10.0f;

        [Tooltip("Fades clouds at horizon/larger optical depths")]
        [Range(0.0f, 10.0f)]
        public float CloudHorizonFadeMultiplier = 1.0f;

        [Tooltip("Dither cloud ray direction to try and avoid banding, 0 for none.")]
        [Range(0.0f, 1.0f)]
        public float CloudRayDither = 0.0f;

        [Tooltip("Bring clouds down at the horizon at the cost of stretching a little over the top.")]
        [Range(0.0f, 0.5f)]
        public float CloudRayOffset = 0.01f;

        [Tooltip("Cloud max ray y value, ray y below this is culled.")]
        [Range(-1.0f, 1.0f)]
        public float CloudMinRayY = -1.0f;

        [Header("Cloud animation/turbulence")]
        [Tooltip("Cloud shape animation/turbulence.")]
        public Vector3 CloudShapeAnimationVelocity = new Vector3(0.0f, -2.0f, 0.0f);

        [Tooltip("Cloud detail animation/turbulence.")]
        public Vector3 CloudDetailAnimationVelocity = new Vector3(0.0f, -1.3f, 0.0f);

        [Header("Clouds - colors")]
        [Tooltip("Cloud color.")]
        public Color CloudColor = Color.white;

        [Tooltip("Cloud emission color, always emits this color regardless of lighting.")]
        public Color CloudEmissionColor = Color.clear;

        [Tooltip("Cloud dir light gradient color, where center of gradient is sun at horizon, right is 'noon'.")]
        public Gradient CloudDirLightGradientColor = new Gradient();
        internal Gradient lerpCloudGradientColor;

        [Header("Clouds - lights")]
        [Tooltip("Cloud dir light multiplier")]
        [Range(0.0f, 10.0f)]
        public float CloudDirLightMultiplier = 5.0f;

        [Tooltip("Point/spot light multiplier")]
        [Range(0.0f, 10.0f)]
        public float CloudPointSpotLightMultiplier = 1.0f;

        [Tooltip("How far to march vs. cloud height when determining lighting/shadowing for clouds")]
        [Range(0.01f, 1.0f)]
        public float CloudLightStepMultiplier = 1.0f;

        [Tooltip("How much clouds absorb light, affects shadows in the clouds")]
        [Range(0.0f, 64.0f)]
        public float CloudLightAbsorption = 4.0f;

        [Tooltip("Henyey Greenstein Phase/Silver lining (x = forward, y = back, z = forward multiplier, w = back multiplier).")]
        public Vector4 CloudHenyeyGreensteinPhase = new Vector4(0.7f, -0.4f, 0.2f, 1.0f);

        [Tooltip("Indirect directional light multiplier (indirect scattering)")]
        [Range(0.0f, 10.0f)]
        public float CloudDirLightIndirectMultiplier = 1.0f;

        [Header("Clouds - ambient light")]
        [Tooltip("Ambient ground intensity")]
        [Range(0.0f, 100.0f)]
        public float CloudAmbientGroundIntensity = 6.0f;

        [Tooltip("Ambient sky intensity, this is how much the ambient sky color from the day night cycle influences the cloud color")]
        [Range(0.0f, 100.0f)]
        public float CloudAmbientSkyIntensity = 16.0f;

        [Tooltip("Sky intensity, this is how much the actual sky pixel colors influence the cloud color")]
        [Range(0.0f, 100.0f)]
        public float CloudSkyIntensity = 1.0f;

        [Tooltip("Increases ambient ground light towards higher cloud heights")]
        [Range(0.0f, 1.0f)]
        public float CloudAmbientGroundHeightMultiplier = 1.0f;

        [Tooltip("Increases ambient sky light towards lower cloud heights")]
        [Range(0.0f, 1.0f)]
        public float CloudAmbientSkyHeightMultiplier = 1.0f;

        [Header("Clouds - shape")]
        [Tooltip("Stratus cloud gradient, controls cloud density over height (4 control points)")]
        public Gradient CloudGradientStratus;
        internal Vector4 CloudGradientStratusVector;

        [Tooltip("Stratocumulus cloud gradient, controls cloud density over height (4 control points)")]
        public Gradient CloudGradientStratoCumulus;
        internal Vector4 CloudGradientStratoCumulusVector;

        [Tooltip("Cumulus cloud gradient, controls cloud density over height (4 control points)")]
        public Gradient CloudGradientCumulus;
        internal Vector4 CloudGradientCumulusVector;

        [MinMaxSlider(0.0f, 1.0f, "Cloud min noise smoothing value range")]
        public RangeOfFloats CloudShapeNoiseMin = new RangeOfFloats(0.14f, 0.16f);

        [MinMaxSlider(0.0f, 1.0f, "Cloud max noise smoothing value range")]
        public RangeOfFloats CloudShapeNoiseMax = new RangeOfFloats(0.35f, 0.37f);

        [MinMaxSlider(0.0f, 10.0f, "Cloud powder multiplier / dark edge multiplier, brightens up bumps/billows in higher clouds")]
        public RangeOfFloats CloudPowderMultiplier = new RangeOfFloats(3.7f, 4.3f);

        [MinMaxSlider(0.0f, 1.0f, "Fades bottom of clouds as value approaches 1")]
        public RangeOfFloats CloudBottomFade = new RangeOfFloats(0.28f, 0.32f);

        [Header("Clouds - cover")]
        [MinMaxSlider(0.0f, 1.0f, "Cloud cover, controls how many clouds / how thick the clouds are.")]
        public RangeOfFloats CloudCover = new RangeOfFloats(0.35f, 0.4f);

        [MinMaxSlider(0.0f, 1.0f, "Secondary / connected cloud cover, this controls how much the weather map cover texture is used.")]
        public RangeOfFloats CloudCoverSecondary = new RangeOfFloats(0.0f, 0.0f);

        [MinMaxSlider(0.0f, 1.0f, "Cloud type - 0 is lowest flattest type of cloud, 1 is largest and puffiest (cummulus)")]
        public RangeOfFloats CloudType = new RangeOfFloats(0.4f, 0.6f);

        [MinMaxSlider(0.0f, 1.0f, "Secondary cloud type, this controls how much the weather map type texture is used.")]
        public RangeOfFloats CloudTypeSecondary = new RangeOfFloats(0.0f, 0.0f);

        [MinMaxSlider(0.0f, 1.0f, "Cloud density, controls how well formed the clouds are.")]
        public RangeOfFloats CloudDensity = new RangeOfFloats(0.95f, 1.0f);

        [Tooltip("Allowed flat layers")]
        [EnumFlag]
        public WeatherMakerVolumetricCloudsFlatLayerMask FlatLayerMask = WeatherMakerVolumetricCloudsFlatLayerMask.Four;

        [Header("Clouds - dir light rays")]
        [Tooltip("The number of dir light ray samples. Set to 0 to disable dir light rays.")]
        [Range(0, 100)]
        public int CloudDirLightRaySampleCount = 0;

        [Tooltip("Dir light ray spread (0 - 1).")]
        [Range(0.0f, 1.0f)]
        public float CloudDirLightRaySpread = 0.65f;

        [Tooltip("Increases the dir light ray brightness")]
        [Range(0.0f, 10.0f)]
        public float CloudDirLightRayBrightness = 0.075f;

        [Tooltip("Combined with each dir light ray march, this determines how much light is accumulated each step.")]
        [Range(0.0f, 1000.0f)]
        public float CloudDirLightRayStepMultiplier = 21.0f;

        [Tooltip("Determines light fall-off from start of dir light ray. Set to 1 for no fall-off.")]
        [Range(0.5f, 1.0f)]
        public float CloudDirLightRayDecay = 0.97f;

        [Tooltip("Dir light ray tint color. Alpha value determines tint intensity.")]
        public Color CloudDirLightRayTintColor = Color.white;

        [Tooltip("Controls dithering intensity of dir light rays.")]
        [Range(-1.0f, 1.0f)]
        public float CloudDirLightRayDither = 0.075f;

        [Tooltip("Controls dithering appearance of dir light rays.")]
        public Vector4 CloudDirLightRayDitherMagic = new Vector4(2.34325f, 5.235345f, 1024.0f, 1024.0f);

        /// <summary>
        /// Progress for all internal lerp variables
        /// </summary>
        internal float lerpProgress;

        public static Vector4 CloudHeightGradientToVector4(Gradient gradient)
        {
            int keyCount = gradient.colorKeys.Length;
            float p1 = 0.0f;
            float p2 = 0.0f;
            float p3 = 0.0f;
            float p4 = 0.0f;
            if (keyCount > 0)
            {
                p1 = gradient.colorKeys[0].time;
                if (keyCount > 1)
                {
                    p2 = gradient.colorKeys[1].time;
                    if (keyCount > 2)
                    {
                        p3 = gradient.colorKeys[2].time;
                        if (keyCount > 3)
                        {
                            p4 = gradient.colorKeys[3].time;
                        }
                        else
                        {
                            p4 = p3;
                        }
                    }
                    else
                    {
                        p3 = gradient.colorKeys[2].time;
                    }
                }
                else
                {
                    p2 = p1;
                }
            }
            return new Vector4(p1, p2, p3, p4);
        }
    }

    [System.Flags]
    public enum WeatherMakerVolumetricCloudsFlatLayerMask
    {
        One = 1,
        Two = 2,
        Three = 4,
        Four = 8
    }
}
