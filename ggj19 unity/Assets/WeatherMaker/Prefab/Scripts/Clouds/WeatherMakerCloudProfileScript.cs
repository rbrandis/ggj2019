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
    public enum WeatherMakerCloudType
    {
        None = 0,
        Light = 10,
        LightScattered = 15,
        LightMedium = 20,
        LightMediumScattered = 25,
        Medium = 30,
        MediumScattered = 35,
        MediumHeavy = 40,
        MediumHeavyScattered = 45,
        HeavyDark = 50,
        HeavyScattered = 55,
        HeavyBright = 60,
        Storm = 70,
        Custom = 250
    }

    [CreateAssetMenu(fileName = "WeatherMakerCloudProfile", menuName = "WeatherMaker/Cloud Profile", order = 40)]
    public class WeatherMakerCloudProfileScript : ScriptableObject
    {
        [Header("Layers")]
        [Tooltip("The first, and lowest cloud layer, null for none")]
        public WeatherMakerCloudLayerProfileScript CloudLayer1;

        [Tooltip("The second, and second lowest cloud layer, null for none")]
        public WeatherMakerCloudLayerProfileScript CloudLayer2;

        [Tooltip("The third, and third lowest cloud layer, null for none")]
        public WeatherMakerCloudLayerProfileScript CloudLayer3;

        [Tooltip("The fourth, and highest cloud layer, null for none")]
        public WeatherMakerCloudLayerProfileScript CloudLayer4;

        [Tooltip("Allow a single layer of volumetric clouds. In the future, more volumetric layers might be supported")]
        public WeatherMakerCloudVolumetricProfileScript CloudLayerVolumetric1;

        [Header("Lighting")]
        [Tooltip("How much to multiply directional light intensities by when clouds are showing. Ignored for volumetric clouds.")]
        [Range(0.0f, 1.0f)]
        public float DirectionalLightIntensityMultiplier = 1.0f;

        [Tooltip("How much to multiply directional light shadow strengths by when clouds are showing. Ignored for volumetric clouds.")]
        [Range(0.0f, 1.0f)]
        public float DirectionalLightShadowStrengthMultiplier = 1.0f;

        [Tooltip("How much clouds affect directional light shadow strength, lower values ensure no reduction. Ignored for volumetric clouds.")]
        [Range(0.0f, 3.0f)]
        public float CloudShadowStrength = 1.0f;

        [Tooltip("How much clouds affect directional light intensity, lower values ensure no reduction. Ignorec for volumetric clouds.")]
        [Range(0.0f, 3.0f)]
        public float CloudLightStrength = 1.0f;

        [Tooltip("Add a global shadow for volumetric clouds. This will cast at a minimum this amount of shadow everywhere. 1 for none, 0 for full shadow.")]
        [Range(0.0f, 1.0f)]
        public float CloudVolumetricShadow = 1.0f;

        [Tooltip("Cloud dither level, helps with night clouds banding")]
        [Range(0.0f, 1.0f)]
        public float CloudDitherLevel = 0.0008f;

        [Header("Weather map (volumetric only)")]
        [Tooltip("Set a custom weather map texture, bypassing the auto-generated weather map")]
        public Texture WeatherMapRenderTextureOverride;

        [Tooltip("Set a custom weather map texture mask, this will mask out all areas of the weather map based on lower alpha values.")]
        public Texture WeatherMapRenderTextureMask;

        [Tooltip("Velocity of weather map mask in uv coordinates (0 - 1)")]
        public Vector2 WeatherMapRenderTextureMaskVelocity;

        [Tooltip("Offset of weather map mask (0 - 1). Velocity is applied automatically but you can set manually as well.")]
        public Vector2 WeatherMapRenderTextureMaskOffset;

        [Tooltip("Clamp for weather map mask offset to ensure that it does not go too far out of bounds.")]
        public Vector2 WeatherMapRenderTextureMaskOffsetClamp = new Vector2(-1.1f, 1.1f);

        [Tooltip("Weather map scale, position is scaled by this to sample into weather map texture")]
        [Range(0.00000001f, 1.0f)]
        public float WeatherMapScale = 0.00001f;

        [Tooltip("Weather map cloud coverage velocity, xy units per second, z change per second")]
        public Vector3 WeatherMapCloudCoverageVelocity = new Vector3(11.0f, 15.0f, 0.0f);

        [MinMaxSlider(0.01f, 100.0f, "Scale of cloud coverage. Higher values produce smaller clouds.")]
        public RangeOfFloats WeatherMapCloudCoverageScale = new RangeOfFloats(4.0f, 16.0f);

        [MinMaxSlider(-360.0f, 360.0f, "Rotation of cloud coverage. Rotates coverage map around center of weather map.")]
        public RangeOfFloats WeatherMapCloudCoverageRotation;

        [MinMaxSlider(-1.0f, 1.0f, "Cloud coverage adder. Higher values create more cloud coverage.")]
        public RangeOfFloats WeatherMapCloudCoverageAdder = new RangeOfFloats { Minimum = 0.0f, Maximum = 0.0f };

        [MinMaxSlider(0.0f, 16.0f, "Cloud coverage power. Higher values create more firm cloud coverage edges.")]
        public RangeOfFloats WeatherMapCloudCoveragePower = new RangeOfFloats { Minimum = 1.0f, Maximum = 1.0f };

        [Tooltip("Weather map cloud type velocity, xy units per second, z change per second")]
        public Vector3 WeatherMapCloudTypeVelocity = new Vector3(17.0f, 10.0f, 0.0f);

        [MinMaxSlider(0.01f, 100.0f, "Scale of cloud types. Higher values produce more jagged clouds.")]
        public RangeOfFloats WeatherMapCloudTypeScale = new RangeOfFloats(2.0f, 8.0f);

        [MinMaxSlider(-360.0f, 360.0f, "Rotation of cloud type. Rotates cloud type map around center of weather map.")]
        public RangeOfFloats WeatherMapCloudTypeRotation;

        [MinMaxSlider(-1.0f, 1.0f, "Cloud type adder. Higher values create more cloud type.")]
        public RangeOfFloats WeatherMapCloudTypeAdder = new RangeOfFloats { Minimum = 0.0f, Maximum = 0.0f };

        [MinMaxSlider(0.0f, 16.0f, "Cloud type power. Higher values create more firm cloud type edges.")]
        public RangeOfFloats WeatherMapCloudTypePower = new RangeOfFloats { Minimum = 1.0f, Maximum = 1.0f };

        [Header("Planet (volumetric only)")]
        [Tooltip("Cloud height.")]
        [Range(1000.0f, 20000.0f)]
        public float CloudHeight = 1500;

        [Tooltip("Cloud height top - clouds extend from CloudHeight to this value.")]
        [Range(2000.0f, 10000.0f)]
        public float CloudHeightTop = 4000;

        [Tooltip("Planet radius for sphere cloud mapping. 1200000.0 seems to work well.")]
        public float CloudPlanetRadius = 1200000.0f;

        [Header("Camera")]
        [Tooltip("How much to scale camera position, smaller values will cause the clouds to move less with the camera. 0 for no cloud movement at all.")]
        [Range(0.0f, 1.0f)]
        public float CameraPositionScale = 1.0f;

        private const float scaleReducer = 0.1f;

        /// <summary>
        /// Checks whether clouds are enabled
        /// </summary>
        public bool CloudsEnabled { get; private set; }

        /// <summary>
        /// Sum of cloud cover, max of 1
        /// </summary>
        public float CloudCoverTotal { get; private set; }

        /// <summary>
        /// Sum of cloud density, max of 1
        /// </summary>
        public float CloudDensityTotal { get; private set; }

        /// <summary>
        /// Sum of cloud light absorption, max of 1
        /// </summary>
        public float CloudLightAbsorptionTotal { get; private set; }

        /// <summary>
        /// A value of 0 to 1 that is a guide on how much to block the direct intensity of directional light, i.e. sun light reflecting off of water that makes the nice bright spots right in line of field of view to the sun
        /// </summary>
        public float CloudDirectionalLightDirectBlock { get; private set; }

        private Vector3 cloudNoiseVelocityAccum1;
        private Vector3 cloudNoiseVelocityAccum2;
        private Vector3 cloudNoiseVelocityAccum3;
        private Vector3 cloudNoiseVelocityAccum4;

        private Vector3 velocityAccumCoverage;
        private Vector3 velocityAccumType;

        private static Vector4[] randomVectors;

        public void SetShaderCloudParameters(Material cloudMaterial, Camera camera)
        {
            if (!Application.isPlaying)
            {
                return;
            }
            else if (WeatherMakerDayNightCycleManagerScript.Instance == null)
            {
                Debug.LogError("Missing WeatherMakerDayNightCycleManagerScript");
                return;
            }
            else if (WeatherMakerLightManagerScript.Instance == null)
            {
                Debug.LogError("Missing WeatherMakerLightManagerScript");
                return;
            }

            cloudMaterial.SetFloat(WMS._CloudCoverVolumetric, CloudLayerVolumetric1.CloudCover.LastValue);
            Shader.SetGlobalFloat(WMS._CloudCoverVolumetric, CloudLayerVolumetric1.CloudCover.LastValue);
            cloudMaterial.SetFloat(WMS._CloudCoverSecondaryVolumetric, CloudLayerVolumetric1.CloudCoverSecondary.LastValue);
            Shader.SetGlobalFloat(WMS._CloudCoverSecondaryVolumetric, CloudLayerVolumetric1.CloudCoverSecondary.LastValue);
            cloudMaterial.SetFloat(WMS._CloudTypeVolumetric, CloudLayerVolumetric1.CloudType.LastValue);
            Shader.SetGlobalFloat(WMS._CloudTypeVolumetric, CloudLayerVolumetric1.CloudType.LastValue);
            cloudMaterial.SetFloat(WMS._CloudTypeSecondaryVolumetric, CloudLayerVolumetric1.CloudTypeSecondary.LastValue);
            Shader.SetGlobalFloat(WMS._CloudTypeSecondaryVolumetric, CloudLayerVolumetric1.CloudTypeSecondary.LastValue);
            cloudMaterial.SetFloat(WMS._CloudDensityVolumetric, CloudLayerVolumetric1.CloudDensity.LastValue);
            Shader.SetGlobalFloat(WMS._CloudDensityVolumetric, CloudLayerVolumetric1.CloudDensity.LastValue);
            cloudMaterial.SetFloat(WMS._CloudHeightNoisePowerVolumetric, CloudLayerVolumetric1.CloudHeightNoisePowerVolumetric.LastValue);
            Shader.SetGlobalFloat(WMS._CloudHeightNoisePowerVolumetric, CloudLayerVolumetric1.CloudHeightNoisePowerVolumetric.LastValue);
            cloudMaterial.SetInt(WMS._CloudDirLightRaySampleCount, CloudLayerVolumetric1.CloudDirLightRaySampleCount);

            if (CloudsEnabled)
            {
                if (CloudLayerVolumetric1.CloudCover.LastValue > 0.001f)
                {
                    cloudMaterial.SetTexture(WMS._CloudNoiseShapeVolumetric, CloudLayerVolumetric1.CloudNoiseShape);
                    cloudMaterial.SetTexture(WMS._CloudNoiseDetailVolumetric, CloudLayerVolumetric1.CloudNoiseDetail);
                    cloudMaterial.SetTexture(WMS._CloudNoiseCurlVolumetric, CloudLayerVolumetric1.CloudNoiseCurl);
                    cloudMaterial.SetVector(WMS._CloudNoiseSampleCountVolumetric, (WeatherMakerScript.Instance == null ? CloudLayerVolumetric1.CloudNoiseSampleCount.ToVector2() :
                        WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudSampleCount.ToVector2()));
                    cloudMaterial.SetVector(WMS._CloudNoiseLodVolumetric, (WeatherMakerScript.Instance == null ? CloudLayerVolumetric1.CloudNoiseLod.ToVector2() :
                        WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudLod.ToVector2()));
                    cloudMaterial.SetVector(WMS._CloudNoiseScaleVolumetric, CloudLayerVolumetric1.CloudNoiseScale);
                    cloudMaterial.SetFloat(WMS._CloudNoiseScalarVolumetric, CloudLayerVolumetric1.CloudNoiseScalar.LastValue);
                    cloudMaterial.SetColor(WMS._CloudColorVolumetric, CloudLayerVolumetric1.CloudColor);
                    if (CloudLayerVolumetric1.lerpCloudGradientColor == null)
                    {
                        Color gradColor = WeatherMakerLightManagerScript.GetGradientColorForSun(CloudLayerVolumetric1.CloudDirLightGradientColor);
                        cloudMaterial.SetColor(WMS._CloudDirColorVolumetric, gradColor);
                    }
                    else
                    {
                        Color oldColor = WeatherMakerLightManagerScript.GetGradientColorForSun(CloudLayerVolumetric1.lerpCloudGradientColor);
                        Color newColor = WeatherMakerLightManagerScript.GetGradientColorForSun(CloudLayerVolumetric1.CloudDirLightGradientColor);
                        Color lerpColor = Color.Lerp(oldColor, newColor, CloudLayerVolumetric1.lerpProgress);
                        cloudMaterial.SetColor(WMS._CloudDirColorVolumetric, lerpColor);
                    }
                    cloudMaterial.SetColor(WMS._CloudEmissionColorVolumetric, CloudLayerVolumetric1.CloudEmissionColor);
                    cloudMaterial.SetVector(WMS._CloudShapeAnimationVelocity, CloudLayerVolumetric1.CloudShapeAnimationVelocity);
                    cloudMaterial.SetVector(WMS._CloudDetailAnimationVelocity, CloudLayerVolumetric1.CloudDetailAnimationVelocity);
                    cloudMaterial.SetFloat(WMS._CloudDirLightMultiplierVolumetric, CloudLayerVolumetric1.CloudDirLightMultiplier);
                    cloudMaterial.SetFloat(WMS._CloudPointSpotLightMultiplierVolumetric, CloudLayerVolumetric1.CloudPointSpotLightMultiplier);
                    cloudMaterial.SetFloat(WMS._CloudAmbientGroundIntensityVolumetric, CloudLayerVolumetric1.CloudAmbientGroundIntensity);
                    cloudMaterial.SetFloat(WMS._CloudAmbientSkyIntensityVolumetric, CloudLayerVolumetric1.CloudAmbientSkyIntensity);
                    cloudMaterial.SetFloat(WMS._CloudSkyIntensityVolumetric, CloudLayerVolumetric1.CloudSkyIntensity);
                    cloudMaterial.SetFloat(WMS._CloudAmbientSkyHeightMultiplierVolumetric, CloudLayerVolumetric1.CloudAmbientSkyHeightMultiplier);
                    cloudMaterial.SetFloat(WMS._CloudAmbientGroundHeightMultiplierVolumetric, CloudLayerVolumetric1.CloudAmbientGroundHeightMultiplier);
                    cloudMaterial.SetFloat(WMS._CloudLightAbsorptionVolumetric, CloudLayerVolumetric1.CloudLightAbsorption);
                    cloudMaterial.SetFloat(WMS._CloudDirLightIndirectMultiplierVolumetric, CloudLayerVolumetric1.CloudDirLightIndirectMultiplier);
                    cloudMaterial.SetFloat(WMS._CloudShapeNoiseMinVolumetric, CloudLayerVolumetric1.CloudShapeNoiseMin.LastValue);
                    cloudMaterial.SetFloat(WMS._CloudShapeNoiseMaxVolumetric, CloudLayerVolumetric1.CloudShapeNoiseMax.LastValue);
                    cloudMaterial.SetFloat(WMS._CloudPowderMultiplierVolumetric, CloudLayerVolumetric1.CloudPowderMultiplier.LastValue);
                    cloudMaterial.SetFloat(WMS._CloudBottomFadeVolumetric, CloudLayerVolumetric1.CloudBottomFade.LastValue);
                    cloudMaterial.SetFloat(WMS._CloudMaxRayLengthMultiplierVolumetric, CloudLayerVolumetric1.CloudMaxRayLengthMultiplier);
                    Shader.SetGlobalFloat(WMS._CloudMaxRayLengthMultiplierVolumetric, CloudLayerVolumetric1.CloudMaxRayLengthMultiplier); // shadow map, etc. depend on this variable
                    cloudMaterial.SetFloat(WMS._CloudOpticalDistanceMultiplierVolumetric, CloudLayerVolumetric1.CloudOpticalDistanceMultiplier);
                    Shader.SetGlobalFloat(WMS._CloudOpticalDistanceMultiplierVolumetric, CloudLayerVolumetric1.CloudOpticalDistanceMultiplier); // shadow map, etc. depend on this variable
                    cloudMaterial.SetFloat(WMS._CloudHorizonFadeMultiplierVolumetric, CloudLayerVolumetric1.CloudHorizonFadeMultiplier);
                    cloudMaterial.SetFloat(WMS._CloudRayDitherVolumetric, CloudLayerVolumetric1.CloudRayDither);
                    cloudMaterial.SetFloat(WMS._CloudDirLightSampleCount, (WeatherMakerScript.Instance == null ? CloudLayerVolumetric1.CloudDirLightSampleCount :
                        WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudDirLightSampleCount));

                    // lower cloud level sphere
                    // assign global shader so shadow map can see them
                    Shader.SetGlobalFloat(WMS._CloudStartVolumetric, CloudHeight);
                    cloudMaterial.SetFloat(WMS._CloudStartVolumetric, CloudHeight);
                    Shader.SetGlobalFloat(WMS._CloudStartSquaredVolumetric, CloudHeight * CloudHeight);
                    cloudMaterial.SetFloat(WMS._CloudStartSquaredVolumetric, CloudHeight * CloudHeight);
                    Shader.SetGlobalFloat(WMS._CloudPlanetStartVolumetric, CloudHeight + CloudPlanetRadius);
                    cloudMaterial.SetFloat(WMS._CloudPlanetStartVolumetric, CloudHeight + CloudPlanetRadius);
                    Shader.SetGlobalFloat(WMS._CloudPlanetStartSquaredVolumetric, Mathf.Pow(CloudHeight + CloudPlanetRadius, 2.0f));
                    cloudMaterial.SetFloat(WMS._CloudPlanetStartSquaredVolumetric, Mathf.Pow(CloudHeight + CloudPlanetRadius, 2.0f));

                    // height of top minus bottom cloud layer
                    float height = CloudHeightTop - CloudHeight;
                    cloudMaterial.SetFloat(WMS._CloudHeightVolumetric, height);
                    Shader.SetGlobalFloat(WMS._CloudHeightVolumetric, height);
                    cloudMaterial.SetFloat(WMS._CloudHeightInverseVolumetric, 1.0f / height);
                    Shader.SetGlobalFloat(WMS._CloudHeightInverseVolumetric, 1.0f / height);
                    height *= height;
                    cloudMaterial.SetFloat(WMS._CloudHeightSquaredVolumetric, height);
                    Shader.SetGlobalFloat(WMS._CloudHeightSquaredVolumetric, height);
                    cloudMaterial.SetFloat(WMS._CloudHeightSquaredInverseVolumetric, 1.0f / height);
                    Shader.SetGlobalFloat(WMS._CloudHeightSquaredInverseVolumetric, 1.0f / height);

                    // upper cloud level sphere
                    cloudMaterial.SetFloat(WMS._CloudEndVolumetric, CloudHeightTop);
                    Shader.SetGlobalFloat(WMS._CloudEndVolumetric, CloudHeightTop);
                    height = CloudHeightTop * CloudHeightTop;
                    cloudMaterial.SetFloat(WMS._CloudEndSquaredVolumetric, height);
                    Shader.SetGlobalFloat(WMS._CloudEndSquaredVolumetric, height);
                    cloudMaterial.SetFloat(WMS._CloudEndSquaredInverseVolumetric, 1.0f / height);
                    Shader.SetGlobalFloat(WMS._CloudEndSquaredInverseVolumetric, 1.0f / height);
                    cloudMaterial.SetFloat(WMS._CloudPlanetEndVolumetric, CloudHeightTop + CloudPlanetRadius);
                    Shader.SetGlobalFloat(WMS._CloudPlanetEndVolumetric, CloudHeightTop + CloudPlanetRadius);
                    cloudMaterial.SetFloat(WMS._CloudPlanetEndSquaredVolumetric, Mathf.Pow(CloudHeightTop + CloudPlanetRadius, 2.0f));
                    Shader.SetGlobalFloat(WMS._CloudPlanetEndSquaredVolumetric, Mathf.Pow(CloudHeightTop + CloudPlanetRadius, 2.0f));

                    cloudMaterial.SetFloat(WMS._CloudPlanetRadiusVolumetric, CloudPlanetRadius);
                    Shader.SetGlobalFloat(WMS._CloudPlanetRadiusVolumetric, CloudPlanetRadius);
                    cloudMaterial.SetFloat(WMS._CloudPlanetRadiusNegativeVolumetric, -CloudPlanetRadius);
                    Shader.SetGlobalFloat(WMS._CloudPlanetRadiusNegativeVolumetric, -CloudPlanetRadius);
                    cloudMaterial.SetFloat(WMS._CloudPlanetRadiusSquaredVolumetric, CloudPlanetRadius * CloudPlanetRadius);
                    Shader.SetGlobalFloat(WMS._CloudPlanetRadiusSquaredVolumetric, CloudPlanetRadius * CloudPlanetRadius);

                    cloudMaterial.SetVector(WMS._CloudHenyeyGreensteinPhaseVolumetric, CloudLayerVolumetric1.CloudHenyeyGreensteinPhase);
                    cloudMaterial.SetFloat(WMS._CloudRayOffsetVolumetric, CloudLayerVolumetric1.CloudRayOffset);
                    cloudMaterial.SetFloat(WMS._CloudMinRayYVolumetric, CloudLayerVolumetric1.CloudMinRayY);
                    cloudMaterial.SetFloat(WMS._CloudLightStepMultiplierVolumetric, CloudLayerVolumetric1.CloudLightStepMultiplier);

                    if (CloudLayerVolumetric1.CloudGradientStratusVector == Vector4.zero)
                    {
                        CloudLayerVolumetric1.CloudGradientStratusVector = WeatherMakerCloudVolumetricProfileScript.CloudHeightGradientToVector4(CloudLayerVolumetric1.CloudGradientStratus);
                    }
                    cloudMaterial.SetVector(WMS._CloudGradientStratus, CloudLayerVolumetric1.CloudGradientStratusVector);
                    Shader.SetGlobalVector(WMS._CloudGradientStratus, CloudLayerVolumetric1.CloudGradientStratusVector);

                    if (CloudLayerVolumetric1.CloudGradientStratoCumulusVector == Vector4.zero)
                    {
                        CloudLayerVolumetric1.CloudGradientStratoCumulusVector = WeatherMakerCloudVolumetricProfileScript.CloudHeightGradientToVector4(CloudLayerVolumetric1.CloudGradientStratoCumulus);
                    }
                    cloudMaterial.SetVector(WMS._CloudGradientStratoCumulus, CloudLayerVolumetric1.CloudGradientStratoCumulusVector);
                    Shader.SetGlobalVector(WMS._CloudGradientStratoCumulus, CloudLayerVolumetric1.CloudGradientStratoCumulusVector);

                    if (CloudLayerVolumetric1.CloudGradientCumulusVector == Vector4.zero)
                    {
                        CloudLayerVolumetric1.CloudGradientCumulusVector = WeatherMakerCloudVolumetricProfileScript.CloudHeightGradientToVector4(CloudLayerVolumetric1.CloudGradientCumulus);
                    }
                    cloudMaterial.SetVector(WMS._CloudGradientCumulus, CloudLayerVolumetric1.CloudGradientCumulusVector);
                    Shader.SetGlobalVector(WMS._CloudGradientCumulus, CloudLayerVolumetric1.CloudGradientCumulusVector);

                    if (randomVectors == null)
                    {
                        randomVectors = new Vector4[6];
                        for (int i = 0; i < randomVectors.Length; i++)
                        {
                            randomVectors[i] = Random.onUnitSphere;
                        }
                        Shader.SetGlobalVectorArray(WMS._CloudConeRandomVectors, randomVectors);
                    }
                }

                // flat
                cloudMaterial.SetTexture(WMS._CloudNoise1, CloudLayer1.CloudNoise ?? Texture2D.blackTexture);
                cloudMaterial.SetTexture(WMS._CloudNoise2, CloudLayer2.CloudNoise ?? Texture2D.blackTexture);
                cloudMaterial.SetTexture(WMS._CloudNoise3, CloudLayer3.CloudNoise ?? Texture2D.blackTexture);
                cloudMaterial.SetTexture(WMS._CloudNoise4, CloudLayer4.CloudNoise ?? Texture2D.blackTexture);

                WMS.SetColorArray(cloudMaterial, "_CloudColor",
                    CloudLayer1.CloudColor * WeatherMakerLightManagerScript.GetGradientColorForSun(CloudLayer1.CloudGradientColor),
                    CloudLayer2.CloudColor * WeatherMakerLightManagerScript.GetGradientColorForSun(CloudLayer2.CloudGradientColor),
                    CloudLayer3.CloudColor * WeatherMakerLightManagerScript.GetGradientColorForSun(CloudLayer3.CloudGradientColor),
                    CloudLayer4.CloudColor * WeatherMakerLightManagerScript.GetGradientColorForSun(CloudLayer4.CloudGradientColor));
                WMS.SetColorArray(cloudMaterial, "_CloudEmissionColor",
                    CloudLayer1.CloudEmissionColor,
                    CloudLayer2.CloudEmissionColor,
                    CloudLayer3.CloudEmissionColor,
                    CloudLayer4.CloudEmissionColor);
                WMS.SetFloatArray(cloudMaterial, "_CloudAmbientMultiplier",
                    CloudLayer1.CloudAmbientMultiplier,
                    CloudLayer2.CloudAmbientMultiplier,
                    CloudLayer3.CloudAmbientMultiplier,
                    CloudLayer4.CloudAmbientMultiplier);
                WMS.SetVectorArray(cloudMaterial, "_CloudNoiseScale",
                    CloudLayer1.CloudNoiseScale * scaleReducer,
                    CloudLayer2.CloudNoiseScale * scaleReducer,
                    CloudLayer3.CloudNoiseScale * scaleReducer,
                    CloudLayer4.CloudNoiseScale * scaleReducer);
                WMS.SetVectorArray(cloudMaterial, "_CloudNoiseMultiplier",
                    CloudLayer1.CloudNoiseMultiplier,
                    CloudLayer2.CloudNoiseMultiplier,
                    CloudLayer3.CloudNoiseMultiplier,
                    CloudLayer4.CloudNoiseMultiplier);
                WMS.SetVectorArray(cloudMaterial, "_CloudNoiseVelocity", cloudNoiseVelocityAccum1, cloudNoiseVelocityAccum2, cloudNoiseVelocityAccum3, cloudNoiseVelocityAccum4);

                WMS.SetFloatArrayRotation(cloudMaterial, "_CloudNoiseRotation",
                    CloudLayer1.CloudNoiseRotation.LastValue,
                    CloudLayer2.CloudNoiseRotation.LastValue,
                    CloudLayer3.CloudNoiseRotation.LastValue,
                    CloudLayer4.CloudNoiseRotation.LastValue);
                /*
                if (CloudLayer1.CloudNoiseMask != null || CloudLayer2.CloudNoiseMask != null || CloudLayer3.CloudNoiseMask != null || CloudLayer4.CloudNoiseMask != null)
                {
                    cloudMaterial.SetTexture(WMS._CloudNoiseMask1, CloudLayer1.CloudNoiseMask ?? Texture2D.whiteTexture);
                    cloudMaterial.SetTexture(WMS._CloudNoiseMask2, CloudLayer2.CloudNoiseMask ?? Texture2D.whiteTexture);
                    cloudMaterial.SetTexture(WMS._CloudNoiseMask3, CloudLayer3.CloudNoiseMask ?? Texture2D.whiteTexture);
                    cloudMaterial.SetTexture(WMS._CloudNoiseMask4, CloudLayer4.CloudNoiseMask ?? Texture2D.whiteTexture);
                    WeatherMakerShaderIds.SetVectorArray(cloudMaterial, "_CloudNoiseMaskOffset",
                        CloudLayer1.CloudNoiseMaskOffset,
                        CloudLayer2.CloudNoiseMaskOffset,
                        CloudLayer3.CloudNoiseMaskOffset,
                        CloudLayer4.CloudNoiseMaskOffset);
                    WeatherMakerShaderIds.SetVectorArray(cloudMaterial, "_CloudNoiseMaskVelocity", cloudNoiseMaskVelocityAccum1, cloudNoiseMaskVelocityAccum2, cloudNoiseMaskVelocityAccum3, cloudNoiseMaskVelocityAccum4);
                    WeatherMakerShaderIds.SetFloatArray(cloudMaterial, "_CloudNoiseMaskScale",
                        (CloudLayer1.CloudNoiseMask == null ? 0.0f : CloudLayer1.CloudNoiseMaskScale * scaleReducer),
                        (CloudLayer2.CloudNoiseMask == null ? 0.0f : CloudLayer2.CloudNoiseMaskScale * scaleReducer),
                        (CloudLayer3.CloudNoiseMask == null ? 0.0f : CloudLayer3.CloudNoiseMaskScale * scaleReducer),
                        (CloudLayer4.CloudNoiseMask == null ? 0.0f : CloudLayer4.CloudNoiseMaskScale * scaleReducer));
                    WeatherMakerShaderIds.SetFloatArrayRotation(cloudMaterial, "_CloudNoiseMaskRotation",
                        CloudLayer1.CloudNoiseMaskRotation.LastValue,
                        CloudLayer2.CloudNoiseMaskRotation.LastValue,
                        CloudLayer3.CloudNoiseMaskRotation.LastValue,
                        CloudLayer4.CloudNoiseMaskRotation.LastValue);
                }
                */
                WMS.SetFloatArray(cloudMaterial, "_CloudHeight",
                    CloudLayer1.CloudHeight,
                    CloudLayer2.CloudHeight,
                    CloudLayer3.CloudHeight,
                    CloudLayer4.CloudHeight);
                WMS.SetFloatArray(cloudMaterial, "_CloudCover",
                    CloudLayer1.CloudCover,
                    CloudLayer2.CloudCover,
                    CloudLayer3.CloudCover,
                    CloudLayer4.CloudCover);
                WMS.SetFloatArray(cloudMaterial, "_CloudDensity",
                    CloudLayer1.CloudDensity,
                    CloudLayer2.CloudDensity,
                    CloudLayer3.CloudDensity,
                    CloudLayer4.CloudDensity);
                WMS.SetFloatArray(cloudMaterial, "_CloudLightAbsorption",
                    CloudLayer1.CloudLightAbsorption,
                    CloudLayer2.CloudLightAbsorption,
                    CloudLayer3.CloudLightAbsorption,
                    CloudLayer4.CloudLightAbsorption);
                WMS.SetFloatArray(cloudMaterial, "_CloudSharpness",
                    CloudLayer1.CloudSharpness,
                    CloudLayer2.CloudSharpness,
                    CloudLayer3.CloudSharpness,
                    CloudLayer4.CloudSharpness);
                WMS.SetFloatArray(cloudMaterial, "_CloudRayOffset",
                    CloudLayer1.CloudRayOffset,
                    CloudLayer2.CloudRayOffset,
                    CloudLayer3.CloudRayOffset,
                    CloudLayer4.CloudRayOffset);

                if (WeatherMakerLightManagerScript.Instance != null)
                {
                    // if we have volumetric clouds and a sun or moon with shadows and we have a screen space shadow texture, use screen space shadows
                    if (CloudLayerVolumetric1.CloudCover.LastValue > 0.001f &&
                        QualitySettings.shadows != ShadowQuality.Disable &&
                        WeatherMakerLightManagerScript.ScreenSpaceShadowMode != UnityEngine.Rendering.BuiltinShaderMode.Disabled &&
                        WeatherMakerLightManagerScript.Instance != null &&
                        ((WeatherMakerLightManagerScript.Instance.Sun != null && 
                        WeatherMakerLightManagerScript.Instance.Sun.LightIsOn &&
                        WeatherMakerLightManagerScript.Instance.Sun.Light.shadows != LightShadows.None) ||
                        (WeatherMakerLightManagerScript.Instance.Moons.Count > 0 &&
                        WeatherMakerLightManagerScript.Instance.Moons[0].LightIsOn &&
                        WeatherMakerLightManagerScript.Instance.Moons[0].Light.shadows != LightShadows.None)))
                    {
                        // do not reduce light intensity or shadows, screen space shadows are being used
                        WeatherMakerLightManagerScript.Instance.DirectionalLightIntensityMultipliers.Remove("WeatherMakerFullScreenCloudsScript");
                        WeatherMakerLightManagerScript.Instance.DirectionalLightShadowIntensityMultipliers.Remove("WeatherMakerFullScreenCloudsScript");
                    }
                    else
                    {
                        float cover = CloudCoverTotal * (1.5f - CloudLightAbsorptionTotal);
                        float sunIntensityMultiplier = Mathf.Clamp(1.0f - (cover * CloudLightStrength), 0.2f, 1.0f);
                        float sunShadowMultiplier = Mathf.Lerp(1.0f, 0.0f, Mathf.Clamp(((CloudDensityTotal + cover) * CloudShadowStrength), 0.0f, 1.0f));
                        float sunIntensityMultiplierWithoutLightStrength = Mathf.Clamp(1.0f - (cover * cover * 0.85f), 0.2f, 1.0f);
                        float cloudShadowReducer = sunIntensityMultiplierWithoutLightStrength;
                        cloudShadowReducer = Mathf.Min(cloudShadowReducer, Shader.GetGlobalFloat(WMS._WeatherMakerCloudGlobalShadow));
                        Shader.SetGlobalFloat(WMS._WeatherMakerCloudGlobalShadow, cloudShadowReducer);

                        // we rely on sun intensity and shadow reduction to reduce weather maker effects, we are not getting cloud shadows
                        WeatherMakerLightManagerScript.Instance.DirectionalLightIntensityMultipliers["WeatherMakerFullScreenCloudsScript"] = sunIntensityMultiplier * Mathf.Lerp(1.0f, DirectionalLightIntensityMultiplier, cover);
                        WeatherMakerLightManagerScript.Instance.DirectionalLightShadowIntensityMultipliers["WeatherMakerFullScreenCloudsScript"] = sunShadowMultiplier * Mathf.Lerp(1.0f, DirectionalLightShadowStrengthMultiplier, cover);
                    }
                }

                cloudMaterial.SetFloat(WMS._WeatherMakerCloudDitherLevel, CloudDitherLevel);
            }
            else if (WeatherMakerLightManagerScript.Instance != null)
            {
                WeatherMakerLightManagerScript.Instance.DirectionalLightIntensityMultipliers.Remove("WeatherMakerFullScreenCloudsScript");
                WeatherMakerLightManagerScript.Instance.DirectionalLightShadowIntensityMultipliers.Remove("WeatherMakerFullScreenCloudsScript");
            }

            Shader.SetGlobalFloat(WMS._WeatherMakerCloudVolumetricShadow, CloudVolumetricShadow);
            Shader.SetGlobalFloat(WMS._WeatherMakerWeatherMapScale, WeatherMapScale);
        }

        private void LoadDefaultLayerIfNeeded(ref WeatherMakerCloudLayerProfileScript script)
        {
            if (script == null)
            {
                script = Resources.Load<WeatherMakerCloudLayerProfileScript>("WeatherMakerCloudLayerProfile_None");
            }
        }

        private void LoadDefaultLayerIfNeeded(ref WeatherMakerCloudVolumetricProfileScript script)
        {
            if (script == null)
            {
                script = Resources.Load<WeatherMakerCloudVolumetricProfileScript>("WeatherMakerCloudLayerProfileVolumetric_None");
            }
        }

        public void UpdateWeatherMap(Material weatherMapMaterial, Camera camera)
        {
            if (weatherMapMaterial != null)
            {
                weatherMapMaterial.SetFloat(WMS._CloudCoverVolumetric, CloudLayerVolumetric1.CloudCover.LastValue);
                weatherMapMaterial.SetFloat(WMS._CloudCoverSecondaryVolumetric, CloudLayerVolumetric1.CloudCoverSecondary.LastValue);
                weatherMapMaterial.SetFloat(WMS._CloudDensityVolumetric, CloudLayerVolumetric1.CloudDensity.LastValue);
                weatherMapMaterial.SetFloat(WMS._CloudTypeVolumetric, CloudLayerVolumetric1.CloudType.LastValue);
                weatherMapMaterial.SetFloat(WMS._CloudTypeSecondaryVolumetric, CloudLayerVolumetric1.CloudTypeSecondary.LastValue);
                weatherMapMaterial.SetVector(WMS._CloudCoverageVelocity, velocityAccumCoverage);
                weatherMapMaterial.SetVector(WMS._CloudTypeVelocity, velocityAccumType);
                weatherMapMaterial.SetFloat(WMS._CloudCoverageFrequency, WeatherMapCloudCoverageScale.LastValue);
                weatherMapMaterial.SetFloat(WMS._CloudTypeFrequency, WeatherMapCloudTypeScale.LastValue);
                float r = WeatherMapCloudCoverageRotation.LastValue * Mathf.Deg2Rad;
                weatherMapMaterial.SetVector(WMS._CloudCoverageRotation, new Vector2(Mathf.Sin(r), Mathf.Cos(r)));
                r = WeatherMapCloudTypeRotation.LastValue * Mathf.Deg2Rad;
                weatherMapMaterial.SetVector(WMS._CloudTypeRotation, new Vector2(Mathf.Sin(r), Mathf.Cos(r)));
                weatherMapMaterial.SetFloat(WMS._CloudCoverageAdder, WeatherMapCloudCoverageAdder.LastValue);
                weatherMapMaterial.SetFloat(WMS._CloudTypeAdder, WeatherMapCloudTypeAdder.LastValue);
                weatherMapMaterial.SetFloat(WMS._CloudCoveragePower, WeatherMapCloudCoveragePower.LastValue);
                weatherMapMaterial.SetFloat(WMS._CloudTypePower, WeatherMapCloudTypePower.LastValue);
                weatherMapMaterial.SetVector(WMS._MaskOffset, WeatherMapRenderTextureMaskOffset);
            }

            if (camera != null)
            {
                Vector3 cameraPos = (camera.transform.position * CameraPositionScale);
                Shader.SetGlobalVector(WMS._CloudCameraPosition, cameraPos);
            }
        }

        public void EnsureNonNullLayers()
        {
            LoadDefaultLayerIfNeeded(ref CloudLayer1);
            LoadDefaultLayerIfNeeded(ref CloudLayer2);
            LoadDefaultLayerIfNeeded(ref CloudLayer3);
            LoadDefaultLayerIfNeeded(ref CloudLayer4);
            LoadDefaultLayerIfNeeded(ref CloudLayerVolumetric1);
        }

        public WeatherMakerCloudProfileScript Clone()
        {
            WeatherMakerCloudProfileScript clone = ScriptableObject.Instantiate(this);
            clone.EnsureNonNullLayers();
            clone.CloudLayer1 = ScriptableObject.Instantiate(clone.CloudLayer1);
            clone.CloudLayer2 = ScriptableObject.Instantiate(clone.CloudLayer2);
            clone.CloudLayer3 = ScriptableObject.Instantiate(clone.CloudLayer3);
            clone.CloudLayer4 = ScriptableObject.Instantiate(clone.CloudLayer4);
            clone.CloudLayerVolumetric1 = ScriptableObject.Instantiate(clone.CloudLayerVolumetric1);
            CopyStateTo(clone);
            return clone;
        }

        public void Update()
        {
            EnsureNonNullLayers();
            CloudsEnabled =
            (
                (CloudLayerVolumetric1.CloudColor.a > 0.0f && CloudLayerVolumetric1.CloudCover.LastValue > 0.001f) ||
                (CloudLayer1.CloudNoise != null && CloudLayer1.CloudColor.a > 0.0f && CloudLayer1.CloudCover > 0.0f) ||
                (CloudLayer2.CloudNoise != null && CloudLayer2.CloudColor.a > 0.0f && CloudLayer2.CloudCover > 0.0f) ||
                (CloudLayer3.CloudNoise != null && CloudLayer3.CloudColor.a > 0.0f && CloudLayer3.CloudCover > 0.0f) ||
                (CloudLayer4.CloudNoise != null && CloudLayer4.CloudColor.a > 0.0f && CloudLayer4.CloudCover > 0.0f)
            );
            CloudCoverTotal = Mathf.Min(1.0f, (CloudLayer1.CloudCover + CloudLayer2.CloudCover + CloudLayer3.CloudCover + CloudLayer4.CloudCover +
                (CloudLayerVolumetric1.CloudCover.LastValue)));
            CloudDensityTotal = Mathf.Min(1.0f,
                (CloudLayerVolumetric1.CloudCover.LastValue * CloudLayerVolumetric1.CloudDensity.LastValue) +
                (CloudLayer1.CloudCover * CloudLayer1.CloudDensity) +
                (CloudLayer2.CloudCover * CloudLayer2.CloudDensity) +
                (CloudLayer3.CloudCover * CloudLayer1.CloudDensity) +
                (CloudLayer4.CloudCover * CloudLayer4.CloudDensity));
            CloudLightAbsorptionTotal = Mathf.Min(1.0f,
                 (Mathf.Clamp(1.0f - (CloudLayerVolumetric1.CloudCover.LastValue * CloudLayerVolumetric1.CloudDensity.LastValue), 0.0f, 1.0f)) +
                (CloudLayer1.CloudCover * CloudLayer1.CloudLightAbsorption) +
                (CloudLayer2.CloudCover * CloudLayer2.CloudLightAbsorption) +
                (CloudLayer3.CloudCover * CloudLayer3.CloudLightAbsorption) +
                (CloudLayer4.CloudCover * CloudLayer4.CloudLightAbsorption));
            CloudDirectionalLightDirectBlock = Mathf.Min(1.0f, (CloudCoverTotal + CloudDensityTotal) * 1.2f);
            float velMult = Time.deltaTime * 0.005f;
            //cloudNoiseMaskVelocityAccum1 += (CloudLayer1.CloudNoiseMaskVelocity * velMult);
            //cloudNoiseMaskVelocityAccum2 += (CloudLayer2.CloudNoiseMaskVelocity * velMult);
            //cloudNoiseMaskVelocityAccum3 += (CloudLayer3.CloudNoiseMaskVelocity * velMult);
            //cloudNoiseMaskVelocityAccum4 += (CloudLayer4.CloudNoiseMaskVelocity * velMult);
            cloudNoiseVelocityAccum1 += (CloudLayer1.CloudNoiseVelocity * velMult);
            cloudNoiseVelocityAccum2 += (CloudLayer2.CloudNoiseVelocity * velMult);
            cloudNoiseVelocityAccum3 += (CloudLayer3.CloudNoiseVelocity * velMult);
            cloudNoiseVelocityAccum4 += (CloudLayer4.CloudNoiseVelocity * velMult);
            velocityAccumCoverage += (WeatherMapCloudCoverageVelocity * Time.deltaTime * WeatherMapScale);
            velocityAccumType += (WeatherMapCloudTypeVelocity * Time.deltaTime * WeatherMapScale);
            WeatherMapRenderTextureMaskOffset += (WeatherMapRenderTextureMaskVelocity * Time.deltaTime);

            // ensure mask offset does not go to far out of bounds
            WeatherMapRenderTextureMaskOffset.x = Mathf.Clamp(WeatherMapRenderTextureMaskOffset.x, WeatherMapRenderTextureMaskOffsetClamp.x, WeatherMapRenderTextureMaskOffsetClamp.y);
            WeatherMapRenderTextureMaskOffset.y = Mathf.Clamp(WeatherMapRenderTextureMaskOffset.y, WeatherMapRenderTextureMaskOffsetClamp.x, WeatherMapRenderTextureMaskOffsetClamp.y);
        }

        public void CopyStateTo(WeatherMakerCloudProfileScript other)
        {
            other.velocityAccumCoverage = velocityAccumCoverage;
            other.velocityAccumType = velocityAccumType;
            other.cloudNoiseVelocityAccum1 = this.cloudNoiseVelocityAccum1;
            other.cloudNoiseVelocityAccum2 = this.cloudNoiseVelocityAccum2;
            other.cloudNoiseVelocityAccum3 = this.cloudNoiseVelocityAccum3;
            other.cloudNoiseVelocityAccum4 = this.cloudNoiseVelocityAccum4;
            other.CloudCoverTotal = this.CloudCoverTotal;
            other.CloudDensityTotal = this.CloudDensityTotal;
            other.CloudLightAbsorptionTotal = this.CloudLightAbsorptionTotal;
            other.CloudDirectionalLightDirectBlock = this.CloudDirectionalLightDirectBlock;
            other.CloudsEnabled = this.CloudsEnabled;
            other.WeatherMapRenderTextureMaskVelocity = this.WeatherMapRenderTextureMaskVelocity;
            other.WeatherMapRenderTextureMaskOffset = this.WeatherMapRenderTextureMaskOffset;
        }
    }
}
