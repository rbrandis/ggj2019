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

#define WEATHER_MAKER_TRACK_LIGHT_CHANGES

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DigitalRuby.WeatherMaker
{
    /// <summary>
    /// Auto find light mode
    /// </summary>
    public enum AutoFindLightsMode
    {
        /// <summary>
        /// No auto light find
        /// </summary>
        None,

        /// <summary>
        /// Find lights at game start
        /// </summary>
        Once,

        /// <summary>
        /// Find lights every frame
        /// </summary>
        EveryFrame
    }

    /// <summary>
    /// Orbit type
    /// </summary>
    public enum WeatherMakerOrbitType
    {
        /// <summary>
        /// Orbit as viewed from Earth
        /// </summary>
        FromEarth = 0,

        /// <summary>
        /// Orbit is controlled by script implementing IWeatherMakerCustomOrbit interface
        /// </summary>
        Custom
    }

    /// <summary>
    /// Blur shader type
    /// </summary>
    public enum BlurShaderType
    {
        None,
        GaussianBlur7,
        GaussianBlur17,
        Bilateral
    }

    /// <summary>
    /// Manages lights in world space for use in shaders - you do not need to add the directional light to the Lights list, it is done automatically
    /// </summary>
    [ExecuteInEditMode]
    public class WeatherMakerLightManagerScript : MonoBehaviour
    {
        [Header("Lights")]
        [Tooltip("Whether to find all lights in the scene automatically if no Lights were added programatically. If none, you must manually add / remove lights using the AutoAddLights property. " +
            "To ensure correct behavior, do not change in script, set it once in the inspector and leave it. If this is set to EveryFrame, AddLight and RemoveLight do nothing.")]
        public AutoFindLightsMode AutoFindLights;

        [Tooltip("A list of lights to automatically add to the light manager. Only used if AutoFindLights is false.")]
        public List<Light> AutoAddLights;

        [Tooltip("A list of lights to always ignore, regardless of other settings.")]
        public List<Light> IgnoreLights;

        [Tooltip("How often to update shader state in seconds for each object (camera, collider, etc.), 0 for every frame.")]
        [Range(0.0f, 1.0f)]
        public float ShaderUpdateInterval = (30.0f / 1000.0f); // 30 fps

        [Tooltip("Set this to a custom screen space shadow shader, by default this is the weather maker integrated screen space shadow shader.")]
        public Shader ScreenSpaceShadowShader;

        private readonly Dictionary<Component, float> shaderUpdateCounter = new Dictionary<Component, float>();
        private Component lastShaderUpdateComponent;

        [Tooltip("Spot light quadratic attenuation.")]
        [Range(0.0f, 1000.0f)]
        public float SpotLightQuadraticAttenuation = 25.0f;

        [Tooltip("Point light quadratic attenuation.")]
        [Range(0.0f, 1000.0f)]
        public float PointLightQuadraticAttenuation = 25.0f;

        [Tooltip("Area light quadratic attenuation. Set to 0 to turn off all area lights.")]
        [Range(0.0f, 1000.0f)]
        public float AreaLightQuadraticAttenuation = 50.0f;

        [Tooltip("Multiplier for area light. Spreads and fades light out over x and y size.")]
        [Range(1.0f, 20.0f)]
        public float AreaLightAreaMultiplier = 10.0f;

        [Tooltip("Falloff for area light, as light moves away from center it falls off more as this increases.")]
        [Range(0.0f, 3.0f)]
        public float AreaLightFalloff = 0.5f;

        [Tooltip("How intense is the scatter of directional light in the fog.")]
        [Range(0.0f, 100.0f)]
        public float FogDirectionalLightScatterIntensity = 2.0f;

        [Tooltip("How quickly fog point lights falloff from the center radius. High values fall-off more.")]
        [Range(0.0f, 4.0f)]
        public float FogSpotLightRadiusFalloff = 1.2f;

        [Tooltip("How much the sun reduces fog lights. As sun intensity approaches 1, fog light intensity is reduced by this value.")]
        [Range(0.0f, 1.0f)]
        public float FogLightSunIntensityReducer = 0.8f;

        [Header("Noise textures")]
        [Tooltip("Noise texture for fog and other 3D effects.")]
        public Texture3D NoiseTexture3D;

        [Tooltip("Blue noise texture, useful for dithering and eliminating banding")]
        public Texture2D BlueNoiseTexture;

        /// <summary>
        /// First object in Suns
        /// </summary>
        public WeatherMakerCelestialObjectScript Sun { get { return (Suns == null || Suns.Count == 0 ? null : Suns[0]); } }

        [Header("Celestial objects")]
        [Tooltip("Suns (only one supported for now)")]
        public List<WeatherMakerCelestialObjectScript> Suns = new List<WeatherMakerCelestialObjectScript>();

        [Tooltip("Moons (up to eight are supported)")]
        public List<WeatherMakerCelestialObjectScript> Moons = new List<WeatherMakerCelestialObjectScript>();

        [Header("Shadows")]
        [Tooltip("The texture name for shaders to access the screen space shadow map, null/empty to not use screen space shadows")]
        public string ScreenSpaceShadowsRenderTextureName = "_WeatherMakerShadowMapSSTexture";

        /// <summary>
        /// Directional light intensity multipliers - all are applied to the final directional light intensities
        /// </summary>
        [NonSerialized]
        public readonly Dictionary<string, float> DirectionalLightIntensityMultipliers = new Dictionary<string, float>();

        /// <summary>
        /// Directional light shadow intensity multipliers - all are applied to the final directional light shadow intensities
        /// </summary>
        [NonSerialized]
        public readonly Dictionary<string, float> DirectionalLightShadowIntensityMultipliers = new Dictionary<string, float>();

        /// <summary>
        /// The planes of the current camera view frustum
        /// </summary>
        [HideInInspector]
        public readonly Plane[] CurrentCameraFrustumPlanes = new Plane[6];

        [HideInInspector]
        public Camera CurrentCamera { get; private set; }

        /// <summary>
        /// The corners of the current camera view frustum
        /// </summary>
        public readonly Vector3[] CurrentCameraFrustumCorners = new Vector3[8];
        private readonly Vector3[] currentCameraFrustumCornersNear = new Vector3[4];

        /// <summary>
        /// The current bounds if checking a collider and not a camera
        /// </summary>
        [HideInInspector]
        public Bounds CurrentBounds;

        /// <summary>
        /// Null zones - this is handled automatically as null zone scripts are added
        /// </summary>
        public readonly List<WeatherMakerNullZoneScript> NullZones = new List<WeatherMakerNullZoneScript>();

        private readonly Vector4[] nullZoneArrayMin = new Vector4[MaximumNullZones];
        private readonly Vector4[] nullZoneArrayMax = new Vector4[MaximumNullZones];
        private readonly Vector4[] nullZoneArrayCenter = new Vector4[MaximumNullZones];
        private readonly Vector4[] nullZoneArrayQuaternion = new Vector4[MaximumNullZones];
        private readonly Vector4[] nullZoneArrayParams = new Vector4[MaximumNullZones];

        /// <summary>
        /// Global shared copy of NoiseTexture3D
        /// </summary>
        public static Texture3D NoiseTexture3DInstance { get; private set; }

        /// <summary>
        /// Max number of null zones - the n closest will be sent to shaders.
        /// </summary>
        public const int MaximumNullZones = 16;

        /// <summary>
        /// Maximum number of lights to send to the Weather Maker shaders - reduce if you are having performance problems
        /// This should match the constant 'MAX_LIGHT_COUNT' in WeatherMakerLightShaderInclude.cginc
        /// </summary>
        public const int MaximumLightCount = 16;

        /// <summary>
        /// Max number of moons supported. This should match the constant in WeatherMakerLightShaderInclude.cginc.
        /// </summary>
        public const int MaxMoonCount = 8;

        // dir lights
        private readonly Vector4[] lightPositionsDir = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightDirectionsDir = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightColorsDir = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightViewportPositionsDir = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightPowerDir = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightQuaternionDir = new Vector4[MaximumLightCount];
        private readonly float[] lightDiffDir = new float[MaximumLightCount];

        // point lights
        private readonly Vector4[] lightPositionsPoint = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightDirectionsPoint = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightColorsPoint = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightAttenPoint = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightViewportPositionsPoint = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightPowerPoint = new Vector4[MaximumLightCount];
        private readonly float[] lightDiffPoint = new float[MaximumLightCount];

        // spot lights
        private readonly Vector4[] lightPositionsSpot = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightDirectionsSpot = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightEndsSpot = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightColorsSpot = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightAttenSpot = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightViewportPositionsSpot = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightPowerSpot = new Vector4[MaximumLightCount];
        private readonly float[] lightDiffSpot = new float[MaximumLightCount];

        // area lights
        private readonly Vector4[] lightPositionsArea = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightPositionsEndArea = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightPositionsMinArea = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightPositionsMaxArea = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightRotationArea = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightDirectionArea = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightColorsArea = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightAttenArea = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightViewportPositionsArea = new Vector4[MaximumLightCount];
        private readonly Vector4[] lightPowerArea = new Vector4[MaximumLightCount];
        private readonly float[] lightDiffArea = new float[MaximumLightCount];

        // unused, but needed for GetLightProperties when result is unused
        private Vector4 tempVec = Vector3.zero;

        /// <summary>
        /// A list of all the lights, sorted by importance of light
        /// </summary>
        private readonly List<LightState> lights = new List<LightState>();

        private bool autoFoundLights;
        private Vector3 currentLightSortPosition;

        // state for intersection test
        private delegate bool IntersectsFuncDelegate(ref Bounds bounds);
        private IntersectsFuncDelegate intersectsFuncCurrentCamera;
        private IntersectsFuncDelegate intersectsFuncCurrentBounds;
        private IntersectsFuncDelegate intersectsFunc;

        //private Vector2 lastResolution;

        private void NormalizePlane(ref Plane plane)
        {
            float length = plane.normal.magnitude;
            plane.normal /= length;
            plane.distance /= length;
        }

        private bool IntersectsFunctionCurrentCamera(ref Bounds bounds)
        {
            return GeometryUtility.TestPlanesAABB(CurrentCameraFrustumPlanes, bounds);
        }

        private bool IntersectsFunctionCurrentBounds(ref Bounds bounds)
        {
            return CurrentBounds.Intersects(bounds);
        }

        private bool PointLightIntersect(Light light)
        {
            float range = light.range + 1.0f;
            Bounds lightBounds = new Bounds { center = light.transform.position, extents = new Vector3(range, range, range) };
            return intersectsFunc(ref lightBounds);
        }

        private bool SpotLightIntersect(Light light)
        {
            float radius = light.range * Mathf.Tan(0.5f * light.spotAngle * Mathf.Deg2Rad);
            float h = Mathf.Sqrt((light.range * light.range) + (radius * radius)) + 1.0f;
            Bounds lightBounds = new Bounds { center = light.transform.position + (light.transform.forward * h * 0.5f), extents = new Vector3(h, h, h) };
            return intersectsFunc(ref lightBounds);
        }

        private bool AreaLightIntersect(ref Bounds lightBounds)
        {
            return intersectsFunc(ref lightBounds);
        }

        public WeatherMakerCelestialObjectScript GetCelestialObject(Light light)
        {
            return light.GetComponent<WeatherMakerCelestialObjectScript>();
        }

        private bool IntersectLight(Light light)
        {
            switch (light.type)
            {
                case LightType.Area:
                {
                    Vector3 pos = light.transform.position;
                    Vector3 dir = light.transform.forward;
                    Vector2 areaSize = light.transform.lossyScale * AreaLightAreaMultiplier;
                    float maxValue = Mathf.Max(areaSize.x, areaSize.y);
                    maxValue = Mathf.Max(maxValue, light.range);
                    Bounds bounds = new Bounds(pos + (dir * light.range * 0.5f), Vector3.one * maxValue);
                    return AreaLightIntersect(ref bounds);
                }

                case LightType.Point:
                    return PointLightIntersect(light);

                case LightType.Spot:
                    return SpotLightIntersect(light);

                default:
                    return true;
            }
        }

        private bool ProcessLightProperties
        (
            LightState lightState,
            Camera camera,
            ref Vector4 pos,
            ref Vector4 pos2,
            ref Vector4 pos3,
            ref Vector4 atten,
            ref Vector4 color,
            ref Vector4 dir,
            ref Vector4 dir2,
            ref Vector4 end,
            ref Vector4 viewportPos,
            ref Vector4 lightPower,
            ref float lightDiff
        )
        {
            if (lightState == null)
            {
                return false;
            }
            Light light = lightState.Light;
            if (light == null || !light.enabled || light.color.a <= 0.001f || light.intensity <= 0.001f || light.range <= 0.001f ||
                !IntersectLight(light))
            {
                return false;
            }
            lightDiff = lightState.Update(camera);
            SetShaderViewportPosition(light, camera, ref viewportPos);
            color = new Vector4(light.color.r, light.color.g, light.color.b, light.intensity);
            lightPower = Vector4.zero;
            pos2 = lightPower;
            dir2 = lightPower;

            switch (light.type)
            {
                case LightType.Directional:
                {
                    WeatherMakerCelestialObjectScript obj = GetCelestialObject(light);
                    pos = -light.transform.forward;
                    pos.w = -1.0f;
                    dir = light.transform.forward;
                    dir.w = 0.0f; // not a moon
                    foreach (WeatherMakerCelestialObjectScript moon in Moons)
                    {
                        if (moon.Light == light)
                        {
                            dir.w = 1.0f; // is a moon
                            break;
                        }
                    }
                    end = Vector4.zero;
                    atten = new Vector4(-1.0f, 1.0f, 0.0f, 0.0f);
                    if (light.shadows == LightShadows.None)
                    {
                        if (obj == null)
                        {
                            lightPower = new Vector4(1.0f, 1.0f, light.shadowStrength, 1.0f);
                        }
                        else
                        {
                            lightPower = new Vector4(obj.LightPower, obj.LightMultiplier, light.shadowStrength, 1.0f);
                        }
                    }
                    else if (obj == null)
                    {
                        lightPower = new Vector4(1.0f, 1.0f, light.shadowStrength, 1.0f - light.shadowStrength);
                    }
                    else
                    {
                        lightPower = new Vector4(obj.LightPower, obj.LightMultiplier, light.shadowStrength, 1.0f - light.shadowStrength);
                    }
                    return true;
                }

                case LightType.Spot:
                {
                    float radius = light.range * Mathf.Tan(0.5f * light.spotAngle * Mathf.Deg2Rad);
                    end = light.transform.position + (light.transform.forward * light.range); // center of cone base
                    float rangeSquared = Mathf.Sqrt((radius * radius) + (light.range * light.range));
                    end.w = rangeSquared * rangeSquared; // slant length squared
                    rangeSquared = light.range * light.range;
                    float outerCutOff = Mathf.Cos(light.spotAngle * 0.5f * Mathf.Deg2Rad);
                    float cutOff = 1.0f / (Mathf.Cos(light.spotAngle * 0.25f * Mathf.Deg2Rad) - outerCutOff);
                    atten = new Vector4(outerCutOff, cutOff, SpotLightQuadraticAttenuation / rangeSquared, 1.0f / rangeSquared);
                    pos = light.transform.position; // apex
                    pos.w = Mathf.Pow(light.spotAngle * Mathf.Deg2Rad / Mathf.PI, 0.5f); // falloff resistor, thinner angles do not fall off at edges
                    dir = light.transform.forward; // direction cone is facing from apex
                    dir.w = radius * radius; // radius at base squared
                    return true;
                }

                case LightType.Point:
                {
                    if (!PointLightIntersect(light))
                    {
                        return false;
                    }

                    float rangeSquared = light.range * light.range;
                    pos = light.transform.position;
                    pos.w = rangeSquared;
                    dir = light.transform.position.normalized;
                    dir.w = light.range;
                    end = Vector4.zero;
                    atten = new Vector4(-1.0f, 1.0f, PointLightQuadraticAttenuation / rangeSquared, 1.0f / rangeSquared);
                    return true;
                }

                case LightType.Area:
                {
                    if (AreaLightQuadraticAttenuation > 0.0f)
                    {
                        float range = light.range;
                        float rangeSquared = range * range;
                        dir2 = light.transform.forward;
                        dir2.w = 0.0f;
                        pos = light.transform.position;
                        pos2 = (Vector3)pos + ((Vector3)dir2 * range);
                        Quaternion rot = light.transform.rotation;
                        Vector2 areaSize = light.transform.lossyScale * AreaLightAreaMultiplier;
                        Vector3 minOffset = (Vector3)pos + (new Vector3(-0.5f * areaSize.x, -0.5f * areaSize.y, 0.0f));
                        Vector3 maxOffset = (Vector3)pos + (new Vector3(0.5f * areaSize.x, 0.5f * areaSize.y, range));
                        pos.w = rangeSquared;
                        dir = new Vector4(rot.x, rot.y, rot.z, rot.w);
                        pos3 = minOffset - (Vector3)pos;
                        pos3.w = 0.0f;
                        end = maxOffset - (Vector3)pos;
                        end.w = 0.0f;
                        float attenAvg = (areaSize.x + areaSize.y) * 0.5f;
                        float radiusSquared = (attenAvg * AreaLightFalloff);
                        radiusSquared *= radiusSquared;
                        atten = new Vector4(1.0f / radiusSquared, attenAvg, AreaLightQuadraticAttenuation / rangeSquared, 1.0f / rangeSquared);
                        return true;
                    } break;
                }
            }
            return false;
        }

        private System.Comparison<LightState> lightSorterReference;
        private int LightSorter(LightState lightState1, LightState lightState2)
        {
            Light light1 = lightState1.Light;
            Light light2 = lightState2.Light;
            int compare = 0;

            if (light1 == light2)
            {
                return compare;
            }
            compare = light1.type.CompareTo(light2.type);
            if (compare == 0)
            {
                if (light1.type == LightType.Directional)
                {
                    compare = light2.intensity.CompareTo(light1.intensity);
                    if (compare == 0)
                    {
                        compare = light2.shadows.CompareTo(light1.shadows);
                    }
                }
                else
                {
                    // compare by distance, then by intensity
                    float mag1 = Mathf.Max(0.0f, Vector3.Distance(light1.transform.position, currentLightSortPosition) - light1.range);
                    float mag2 = Mathf.Max(0.0f, Vector3.Distance(light2.transform.position, currentLightSortPosition) - light2.range);
                    compare = mag1.CompareTo(mag2);
                    if (compare == 0)
                    {
                        compare = light2.intensity.CompareTo(light1.intensity);
                    }
                }
            }
            return compare;
        }

        private System.Comparison<WeatherMakerNullZoneScript> nullZoneSorterReference;
        private int NullZoneSorter(WeatherMakerNullZoneScript b1, WeatherMakerNullZoneScript b2)
        {
            // sort by distance from camera
            float d1 = Vector3.SqrMagnitude(b1.bounds.center - currentLightSortPosition);
            float d2 = Vector3.SqrMagnitude(b2.bounds.center - currentLightSortPosition);
            return d1.CompareTo(d2);
        }

        private void SetLightsByTypeToShader(Camera camera, Material m)
        {
            int dirLightCount = 0;
            int pointLightCount = 0;
            int spotLightCount = 0;
            int areaLightCount = 0;
            lightColorsDir[0] = Vector4.zero; // ensure the primary dir light is black if there are no dir lights

            // ensure first light diffs are zeroed out, some shaders look at first light difference with temporal reprojection
            lightDiffPoint[0] = 0.0f;
            lightDiffSpot[0] = 0.0f;
            lightDiffArea[0] = 0.0f;

            for (int i = 0; i < lights.Count; i++)
            {
                LightState light = lights[i];
                if (light == null || light.Light == null)
                {
                    lights.RemoveAt(i--);
                    continue;
                }

                switch (light.Light.type)
                {
                    case LightType.Directional:
                        if (dirLightCount < MaximumLightCount && ProcessLightProperties(light, camera, ref lightPositionsDir[dirLightCount], ref tempVec, ref tempVec, ref tempVec,
                            ref lightColorsDir[dirLightCount], ref lightDirectionsDir[dirLightCount], ref tempVec, ref tempVec, ref lightViewportPositionsDir[dirLightCount],
                            ref lightPowerDir[dirLightCount], ref lightDiffDir[dirLightCount]))
                        {
                            Quaternion rot = light.Light.transform.rotation;
                            lightQuaternionDir[dirLightCount] = new Vector4(rot.x, rot.y, rot.z, rot.w);
                            dirLightCount++;
                        }
                        break;

                    case LightType.Point:
                        if (pointLightCount < MaximumLightCount && ProcessLightProperties(light, camera, ref lightPositionsPoint[pointLightCount], ref tempVec, ref tempVec,
                            ref lightAttenPoint[pointLightCount], ref lightColorsPoint[pointLightCount], ref lightDirectionsPoint[pointLightCount], ref tempVec, ref tempVec,
                            ref lightViewportPositionsPoint[pointLightCount], ref lightPowerPoint[pointLightCount], ref lightDiffPoint[pointLightCount]))
                        {
                            pointLightCount++;
                        }
                        break;

                    case LightType.Spot:
                        if (spotLightCount < MaximumLightCount && ProcessLightProperties(light, camera, ref lightPositionsSpot[spotLightCount], ref tempVec, ref tempVec,
                            ref lightAttenSpot[spotLightCount], ref lightColorsSpot[spotLightCount], ref lightDirectionsSpot[spotLightCount], ref tempVec, ref lightEndsSpot[spotLightCount],
                            ref lightViewportPositionsSpot[spotLightCount], ref lightPowerSpot[spotLightCount], ref lightDiffSpot[spotLightCount]))
                        {
                            spotLightCount++;
                        }
                        break;

                    case LightType.Area:
                        if (areaLightCount < MaximumLightCount && ProcessLightProperties(light, camera, ref lightPositionsArea[areaLightCount], ref lightPositionsEndArea[areaLightCount],
                            ref lightPositionsMinArea[areaLightCount], ref lightAttenArea[areaLightCount], ref lightColorsArea[areaLightCount], ref lightRotationArea[areaLightCount],
                            ref lightDirectionArea[areaLightCount], ref lightPositionsMaxArea[areaLightCount], ref lightViewportPositionsArea[areaLightCount],
                            ref lightPowerArea[areaLightCount], ref lightDiffArea[areaLightCount]))
                        {
                            areaLightCount++;
                        }
                        break;

                    default:
                        break;
                }
            }

            float dirLightDiff = 0.0f;
            foreach (WeatherMakerCelestialObjectScript o in Suns)
            {
                dirLightDiff = Mathf.Max(dirLightDiff, o.Difference);
            }
            foreach (WeatherMakerCelestialObjectScript o in Moons)
            {
                dirLightDiff = Mathf.Max(dirLightDiff, o.Difference);
            }
            dirLightDiff = Mathf.Min(1.0f, dirLightDiff);

            if (m == null)
            {
                // dir lights
                Shader.SetGlobalInt(WMS._WeatherMakerDirLightCount, dirLightCount);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerDirLightPosition, lightPositionsDir);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerDirLightDirection, lightDirectionsDir);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerDirLightColor, lightColorsDir);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerDirLightViewportPosition, lightViewportPositionsDir);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerDirLightPower, lightPowerDir);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerDirLightQuaternion, lightQuaternionDir);
                Shader.SetGlobalFloat(WMS._WeatherMakerDirLightDifference, dirLightDiff);

                // point lights
                Shader.SetGlobalInt(WMS._WeatherMakerPointLightCount, pointLightCount);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerPointLightPosition, lightPositionsPoint);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerPointLightDirection, lightDirectionsPoint);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerPointLightColor, lightColorsPoint);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerPointLightAtten, lightAttenPoint);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerPointLightViewportPosition, lightViewportPositionsPoint);

#if WEATHER_MAKER_TRACK_LIGHT_CHANGES

                Shader.SetGlobalFloatArray(WMS._WeatherMakerPointLightDifference, lightDiffPoint);

#endif

                // spot lights
                Shader.SetGlobalInt(WMS._WeatherMakerSpotLightCount, spotLightCount);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerSpotLightPosition, lightPositionsSpot);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerSpotLightColor, lightColorsSpot);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerSpotLightAtten, lightAttenSpot);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerSpotLightDirection, lightDirectionsSpot);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerSpotLightSpotEnd, lightEndsSpot);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerSpotLightViewportPosition, lightViewportPositionsSpot);

#if WEATHER_MAKER_TRACK_LIGHT_CHANGES

                Shader.SetGlobalFloatArray(WMS._WeatherMakerSpotLightDifference, lightDiffSpot);

#endif

                // area lights
                Shader.SetGlobalInt(WMS._WeatherMakerAreaLightCount, areaLightCount);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerAreaLightPosition, lightPositionsArea);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerAreaLightPositionEnd, lightPositionsEndArea);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerAreaLightMinPosition, lightPositionsMinArea);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerAreaLightMaxPosition, lightPositionsMaxArea);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerAreaLightColor, lightColorsArea);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerAreaLightAtten, lightAttenArea);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerAreaLightRotation, lightRotationArea);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerAreaLightDirection, lightDirectionArea);
                Shader.SetGlobalVectorArray(WMS._WeatherMakerAreaLightViewportPosition, lightViewportPositionsArea);

#if WEATHER_MAKER_TRACK_LIGHT_CHANGES

                Shader.SetGlobalFloatArray(WMS._WeatherMakerAreaLightDifference, lightDiffArea);

#endif

            }
            else
            {
                // dir lights
                m.SetInt(WMS._WeatherMakerDirLightCount, dirLightCount);
                m.SetVectorArray(WMS._WeatherMakerDirLightPosition, lightPositionsDir);
                m.SetVectorArray(WMS._WeatherMakerDirLightDirection, lightDirectionsDir);
                m.SetVectorArray(WMS._WeatherMakerDirLightColor, lightColorsDir);
                m.SetVectorArray(WMS._WeatherMakerDirLightViewportPosition, lightViewportPositionsDir);
                m.SetVectorArray(WMS._WeatherMakerDirLightPower, lightPowerDir);
                m.SetVectorArray(WMS._WeatherMakerDirLightQuaternion, lightQuaternionDir);
                m.SetFloat(WMS._WeatherMakerDirLightDifference, dirLightDiff);

                // point lights
                m.SetInt(WMS._WeatherMakerPointLightCount, pointLightCount);
                m.SetVectorArray(WMS._WeatherMakerPointLightPosition, lightPositionsPoint);
                m.SetVectorArray(WMS._WeatherMakerPointLightDirection, lightDirectionsPoint);
                m.SetVectorArray(WMS._WeatherMakerPointLightColor, lightColorsPoint);
                m.SetVectorArray(WMS._WeatherMakerPointLightAtten, lightAttenPoint);
                m.SetVectorArray(WMS._WeatherMakerPointLightViewportPosition, lightViewportPositionsPoint);

#if WEATHER_MAKER_TRACK_LIGHT_CHANGES

                m.SetFloatArray(WMS._WeatherMakerPointLightDifference, lightDiffPoint);

#endif

                // spot lights
                m.SetInt(WMS._WeatherMakerSpotLightCount, spotLightCount);
                m.SetVectorArray(WMS._WeatherMakerSpotLightPosition, lightPositionsSpot);
                m.SetVectorArray(WMS._WeatherMakerSpotLightColor, lightColorsSpot);
                m.SetVectorArray(WMS._WeatherMakerSpotLightAtten, lightAttenSpot);
                m.SetVectorArray(WMS._WeatherMakerSpotLightDirection, lightDirectionsSpot);
                m.SetVectorArray(WMS._WeatherMakerSpotLightSpotEnd, lightEndsSpot);
                m.SetVectorArray(WMS._WeatherMakerSpotLightViewportPosition, lightViewportPositionsSpot);

#if WEATHER_MAKER_TRACK_LIGHT_CHANGES

                m.SetFloatArray(WMS._WeatherMakerSpotLightDifference, lightDiffSpot);

#endif

                // area lights
                m.SetInt(WMS._WeatherMakerAreaLightCount, areaLightCount);
                m.SetVectorArray(WMS._WeatherMakerAreaLightPosition, lightPositionsArea);
                m.SetVectorArray(WMS._WeatherMakerAreaLightPositionEnd, lightPositionsEndArea);
                m.SetVectorArray(WMS._WeatherMakerAreaLightMinPosition, lightPositionsMinArea);
                m.SetVectorArray(WMS._WeatherMakerAreaLightMaxPosition, lightPositionsMaxArea);
                m.SetVectorArray(WMS._WeatherMakerAreaLightColor, lightColorsArea);
                m.SetVectorArray(WMS._WeatherMakerAreaLightAtten, lightAttenArea);
                m.SetVectorArray(WMS._WeatherMakerAreaLightRotation, lightRotationArea);
                m.SetVectorArray(WMS._WeatherMakerAreaLightDirection, lightDirectionArea);
                m.SetVectorArray(WMS._WeatherMakerAreaLightViewportPosition, lightViewportPositionsArea);

#if WEATHER_MAKER_TRACK_LIGHT_CHANGES

                m.SetFloatArray(WMS._WeatherMakerAreaLightDifference, lightDiffArea);

#endif

            }
        }

        private void CleanupCelestialObjects()
        {
            for (int i = Suns.Count - 1; i >= 0; i--)
            {
                if (Suns[i] == null)
                {
                    Suns.RemoveAt(i);
                }
            }
            for (int i = 0; i < Moons.Count; i++)
            {
                if (Moons[i] == null)
                {
                    Moons.RemoveAt(i);
                }
            }
        }

        private void UpdateAllLights()
        {
            CleanupCelestialObjects();

            // if no user lights specified, find all the lights in the scene and sort them
            if ((AutoFindLights == AutoFindLightsMode.Once && !autoFoundLights) || AutoFindLights == AutoFindLightsMode.EveryFrame)
            {
                autoFoundLights = true;
                Light[] allLights = GameObject.FindObjectsOfType<Light>();
                lights.Clear();
                foreach (Light light in allLights)
                {
                    if (light != null && light.enabled && light.intensity > 0.0f && light.color.a > 0.0f && (IgnoreLights == null || !IgnoreLights.Contains(light)))
                    {
                        lights.Add(GetOrCreateLightState(light));
                    }
                }
            }
            else
            {
                if (Sun != null)
                {
                    // add the sun if it is on, else remove it
                    if (Sun.LightIsOn)
                    {
                        AddLight(Sun.Light);
                    }
                    else
                    {
                        RemoveLight(Sun.Light);
                    }
                }
                if (Moons != null)
                {
                    // add each moon if it is on, else remove it
                    foreach (WeatherMakerCelestialObjectScript moon in Moons)
                    {
                        if (moon.LightIsOn)
                        {
                            AddLight(moon.Light);
                        }
                        else
                        {
                            RemoveLight(moon.Light);
                        }
                    }
                }

                if (AutoAddLights != null)
                {
                    // add each auto-add light if it is on, else remove it
                    for (int i = AutoAddLights.Count - 1; i >= 0; i--)
                    {
                        Light light = AutoAddLights[i];
                        if (light == null)
                        {

#if UNITY_EDITOR

                            if (Application.isPlaying)

#endif

                            {
                                AutoAddLights.RemoveAt(i);
                            }
                        }
                        else if (light.intensity == 0.0f || !light.enabled || !light.gameObject.activeInHierarchy)
                        {
                            RemoveLight(light);
                        }
                        else
                        {
                            AddLight(light);
                        }
                    }
                }
            }
        }

        private void UpdateNullZones(Material m)
        {

#if UNITY_EDITOR

            if (!Application.isPlaying)
            {
                return;
            }

#endif

            int nullZoneCount = 0;

            // take out null/disabled fog zones
            for (int i = NullZones.Count - 1; i >= 0; i--)
            {
                if (NullZones[i] == null || !NullZones[i].enabled)
                {
                    NullZones.RemoveAt(i);
                }
            }

            NullZones.Sort(nullZoneSorterReference);
            for (int i = 0; i < NullZones.Count && nullZoneCount < MaximumNullZones; i++)
            {
                Bounds nullZoneBoundsInflated = NullZones[i].inflatedBounds;
                if (NullZones[i].NullZoneProfile == null || NullZones[i].NullZoneStrength < 0.001f ||
                    !WeatherMakerReflectionScript.BoxIntersectsFrustum(CurrentCamera, CurrentCameraFrustumPlanes, CurrentCameraFrustumCorners, nullZoneBoundsInflated))
                {
                    continue;
                }

                Bounds nullZoneBounds = NullZones[i].bounds;
                int mask = NullZones[i].CurrentMask;

                if (intersectsFunc(ref nullZoneBoundsInflated))
                {
                    float fade = NullZones[i].CurrentFade;
                    float strength = NullZones[i].NullZoneStrength;
                    Quaternion r = NullZones[i].transform.rotation;
                    Vector3 center = nullZoneBounds.center;
                    float centerW;
                    if (r == Quaternion.identity)
                    {
                        centerW = 0.0f;
                    }
                    else
                    {
                        // it will be up to the shaders to detect that center w is 1.0 and
                        // perform quaternion rotation on any ray cast, point intersect, etc.
                        centerW = 1.0f;

                        // recompute bounds with no rotation - Unity bounds is the largest box that will contain,
                        // not what we want here, we want an AABB with no rotation centered on 0,0,0.
                        NullZones[i].transform.rotation = Quaternion.identity;
                        nullZoneBounds = NullZones[i].BoxCollider.bounds;
                        NullZones[i].transform.rotation = r;
                        nullZoneBounds.center = Vector3.zero;
                    }
                    nullZoneArrayMin[nullZoneCount] = nullZoneBounds.min;
                    nullZoneArrayMin[nullZoneCount].w = mask;
                    nullZoneArrayMax[nullZoneCount] = nullZoneBounds.max;
                    nullZoneArrayMax[nullZoneCount].w = fade;
                    nullZoneArrayCenter[nullZoneCount] = center;
                    nullZoneArrayCenter[nullZoneCount].w = centerW;
                    nullZoneArrayQuaternion[nullZoneCount] = new Vector4(r.x, r.y, r.z, r.w);
                    nullZoneArrayParams[nullZoneCount] = new Vector4(strength, 0.0f, 0.0f, 0.0f);
                    nullZoneCount++;
                }
            }
            if (m == null)
            {
                Shader.SetGlobalInt(WMS._NullZoneCount, nullZoneCount);
                Shader.SetGlobalVectorArray(WMS._NullZonesMin, nullZoneArrayMin);
                Shader.SetGlobalVectorArray(WMS._NullZonesMax, nullZoneArrayMax);
                Shader.SetGlobalVectorArray(WMS._NullZonesCenter, nullZoneArrayCenter);
                Shader.SetGlobalVectorArray(WMS._NullZonesParams, nullZoneArrayParams);
                Shader.SetGlobalVectorArray(WMS._NullZonesQuaternion, nullZoneArrayQuaternion);
            }
            else
            {
                m.SetInt(WMS._NullZoneCount, nullZoneCount);
                m.SetVectorArray(WMS._NullZonesMin, nullZoneArrayMin);
                m.SetVectorArray(WMS._NullZonesMax, nullZoneArrayMax);
                m.SetVectorArray(WMS._NullZonesCenter, nullZoneArrayCenter);
                m.SetVectorArray(WMS._NullZonesParams, nullZoneArrayParams);
                m.SetVectorArray(WMS._NullZonesQuaternion, nullZoneArrayQuaternion);
            }
        }

#if CREATE_DITHER_TEXTURE_FOR_WEATHER_MAKER_LIGHT_MANAGER

        private void CreateDitherTexture()
        {
            if (DitherTextureInstance != null)
            {
                return;
            }

#if DITHER_4_4

            int size = 4;

#else

            int size = 8;

#endif

            DitherTextureInstance = new Texture2D(size, size, TextureFormat.Alpha8, false, true);
            DitherTextureInstance.filterMode = FilterMode.Point;
            Color32[] c = new Color32[size * size];

            byte b;

#if DITHER_4_4

            b = (byte)(0.0f / 16.0f * 255); c[0] = new Color32(b, b, b, b);
            b = (byte)(8.0f / 16.0f * 255); c[1] = new Color32(b, b, b, b);
            b = (byte)(2.0f / 16.0f * 255); c[2] = new Color32(b, b, b, b);
            b = (byte)(10.0f / 16.0f * 255); c[3] = new Color32(b, b, b, b);

            b = (byte)(12.0f / 16.0f * 255); c[4] = new Color32(b, b, b, b);
            b = (byte)(4.0f / 16.0f * 255); c[5] = new Color32(b, b, b, b);
            b = (byte)(14.0f / 16.0f * 255); c[6] = new Color32(b, b, b, b);
            b = (byte)(6.0f / 16.0f * 255); c[7] = new Color32(b, b, b, b);

            b = (byte)(3.0f / 16.0f * 255); c[8] = new Color32(b, b, b, b);
            b = (byte)(11.0f / 16.0f * 255); c[9] = new Color32(b, b, b, b);
            b = (byte)(1.0f / 16.0f * 255); c[10] = new Color32(b, b, b, b);
            b = (byte)(9.0f / 16.0f * 255); c[11] = new Color32(b, b, b, b);

            b = (byte)(15.0f / 16.0f * 255); c[12] = new Color32(b, b, b, b);
            b = (byte)(7.0f / 16.0f * 255); c[13] = new Color32(b, b, b, b);
            b = (byte)(13.0f / 16.0f * 255); c[14] = new Color32(b, b, b, b);
            b = (byte)(5.0f / 16.0f * 255); c[15] = new Color32(b, b, b, b);

#else

            int i = 0;
            b = (byte)(1.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(49.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(13.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(61.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(4.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(52.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(16.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(64.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);

            b = (byte)(33.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(17.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(45.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(29.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(36.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(20.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(48.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(32.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);

            b = (byte)(9.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(57.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(5.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(53.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(12.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(60.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(8.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(56.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);

            b = (byte)(41.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(25.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(37.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(21.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(44.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(28.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(40.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(24.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);

            b = (byte)(3.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(51.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(15.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(63.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(2.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(50.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(14.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(62.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);

            b = (byte)(35.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(19.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(47.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(31.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(34.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(18.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(46.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(30.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);

            b = (byte)(11.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(59.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(7.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(55.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(10.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(58.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(6.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(54.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);

            b = (byte)(43.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(27.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(39.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(23.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(42.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(26.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(38.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(22.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
#endif

            DitherTextureInstance.SetPixels32(c);
            DitherTextureInstance.Apply();
        }

#endif


        private void SetShaderViewportPosition(Light light, Camera camera, ref Vector4 viewportPosition)
        {
            if (camera == null || WeatherMakerScript.GetCameraType(camera) != WeatherMakerCameraType.Normal)
            {
                return;
            }

            viewportPosition = camera.WorldToViewportPoint(light.transform.position);

            // as dir light leaves viewport, fade out
            Vector2 viewportCenter = new Vector2((camera.rect.min.x + camera.rect.max.x) * 0.5f, (camera.rect.min.y + camera.rect.max.y) * 0.5f);
            float distanceFromCenterViewport = ((Vector2)viewportPosition - viewportCenter).magnitude * 0.5f;
            viewportPosition.w = light.intensity * Mathf.SmoothStep(1.0f, 0.0f, distanceFromCenterViewport);
            if (Sun != null && light == Sun.Light)
            {
                Sun.ViewportPosition = viewportPosition;
                Shader.SetGlobalVector(WMS._WeatherMakerSunViewportPosition, viewportPosition);
                return;
            }
            if (Moons != null)
            {
                foreach (WeatherMakerCelestialObjectScript obj in Moons)
                {
                    if (obj.Light != null && obj.Light == light)
                    {
                        obj.ViewportPosition = viewportPosition;
                        break;
                    }
                }
            }
        }

        private void SetGlobalShaders()
        {
            if (Sun != null)
            {
                // reduce volumetric point/spot lights based on sun strength
                if (Sun.Light != null)
                {
                    float volumetricLightMultiplier = Mathf.Max(0.0f, (1.0f - (Sun.Light.intensity * FogLightSunIntensityReducer)));
                    Shader.SetGlobalFloat(WMS._WeatherMakerVolumetricPointSpotMultiplier, volumetricLightMultiplier);
                }

                // Sun
                Vector3 sunForward = Sun.transform.forward;
                Vector3 sunForward2D = Quaternion.AngleAxis(-90.0f, Vector3.right) * Sun.transform.forward;
                Shader.SetGlobalVector(WMS._WeatherMakerSunDirectionUp, -sunForward);
                Shader.SetGlobalVector(WMS._WeatherMakerSunDirectionUp2D, -sunForward2D);
                Shader.SetGlobalVector(WMS._WeatherMakerSunDirectionDown, Sun.transform.forward);
                Shader.SetGlobalVector(WMS._WeatherMakerSunDirectionDown2D, sunForward2D);
                Shader.SetGlobalVector(WMS._WeatherMakerSunPositionNormalized, Sun.transform.position.normalized);
                Shader.SetGlobalVector(WMS._WeatherMakerSunPositionWorldSpace, Sun.transform.position);
                SunColor = new Vector4(Sun.Light.color.r, Sun.Light.color.g, Sun.Light.color.b, Sun.Light.intensity);
                Shader.SetGlobalVector(WMS._WeatherMakerSunColor, SunColor);
                SunTintColor = new Vector4(Sun.TintColor.r, Sun.TintColor.g, Sun.TintColor.b, Sun.TintColor.a * Sun.TintIntensity);
                Shader.SetGlobalVector(WMS._WeatherMakerSunTintColor, SunTintColor);
                float sunHorizonScaleMultiplier = Mathf.Clamp(Mathf.Abs(Sun.transform.forward.y) * 3.0f, 0.5f, 1.0f);
                sunHorizonScaleMultiplier = Mathf.Min(1.0f, Sun.Scale / sunHorizonScaleMultiplier);
                Shader.SetGlobalVector(WMS._WeatherMakerSunLightPower, new Vector4(Sun.LightPower, Sun.LightMultiplier, Sun.Light.shadowStrength, 1.0f - Sun.Light.shadowStrength));
                Shader.SetGlobalVector(WMS._WeatherMakerSunVar1, new Vector4(sunHorizonScaleMultiplier, Mathf.Pow(Sun.Light.intensity, 0.5f), Mathf.Pow(Sun.Light.intensity, 0.75f), Sun.Light.intensity * Sun.Light.intensity));

                if (Sun.Renderer != null)
                {
                    Sun.Renderer.enabled = (Sun.Light.intensity > 0.0f);
                    if (Sun.RenderHintFast)
                    {
                        Sun.Renderer.sharedMaterial.EnableKeyword("RENDER_HINT_FAST");
                    }
                    else
                    {
                        Sun.Renderer.sharedMaterial.DisableKeyword("RENDER_HINT_FAST");
                    }
                }
            }

            float t = Time.timeSinceLevelLoad;
            Shader.SetGlobalVector(WMS._WeatherMakerTime, new Vector4(t * 0.05f, t, (float)System.Math.Truncate(t * 0.05f), (float)System.Math.Truncate(t)));
            Shader.SetGlobalVector(WMS._WeatherMakerTimeSin, new Vector4(Mathf.Sin(t * 0.05f), Mathf.Sin(t), Mathf.Sin(t * 2.0f), Mathf.Sin(t * 3.0f)));
        }

        private void Initialize()
        {
            if (Application.isPlaying)
            {
                // Create3DNoiseTexture();
                // CreateDitherTexture();

                if (UnityEngine.XR.XRDevice.isPresent)
                {
                    if (ScreenSpaceShadowMode == BuiltinShaderMode.UseBuiltin)
                    {
                        UnityEngine.Rendering.GraphicsSettings.SetCustomShader(BuiltinShaderType.ScreenSpaceShadows, ScreenSpaceShadowShader);
                    }
                    else
                    {
                        Debug.LogWarning("Screen space shadow shader is not using the integrated Weather Maker shader, clouds will not cast shadows and snow overlay will not receive shadows. " +
                            "Set ScreenSpaceShadowShader on WeatherMakerLightManager to WeatherMakerScreenSpaceShadowsShader to fix.");
                    }
                }
            }

            NoiseTexture3DInstance = NoiseTexture3D;
            nullZoneSorterReference = NullZoneSorter;
            lightSorterReference = LightSorter;
            intersectsFuncCurrentCamera = IntersectsFunctionCurrentCamera;
            intersectsFuncCurrentBounds = IntersectsFunctionCurrentBounds;
            intersectsFunc = intersectsFuncCurrentCamera;
        }

        private void Update()
        {
            NullZones.Clear();
            Shader.SetGlobalTexture(WMS._WeatherMakerNoiseTexture3D, NoiseTexture3D);
            Shader.SetGlobalTexture(WMS._WeatherMakerBlueNoiseTexture, BlueNoiseTexture);
        }

        private void LateUpdate()
        {
            Shader.SetGlobalFloat(WMS._WeatherMakerFogDirectionalLightScatterIntensity, FogDirectionalLightScatterIntensity);
            Shader.SetGlobalVector(WMS._WeatherMakerFogLightFalloff, new Vector4(FogSpotLightRadiusFalloff, 0.0f, 0.0f, 0.0f));
            Shader.SetGlobalFloat(WMS._WeatherMakerFogLightSunIntensityReducer, FogLightSunIntensityReducer);
            if (QualitySettings.shadows == ShadowQuality.Disable)
            {
                Shader.DisableKeyword("WEATHER_MAKER_SHADOWS_SPLIT_SPHERES");
                Shader.DisableKeyword("WEATHER_MAKER_SHADOWS_ONE_CASCADE");
                Shader.SetGlobalInt("_WeatherMakerShadowsEnabled", 0);
            }
            else
            {
                Shader.SetGlobalInt("_WeatherMakerShadowsEnabled", 1);
            }
            if (QualitySettings.shadowCascades < 2)
            {
                Shader.EnableKeyword("WEATHER_MAKER_SHADOWS_ONE_CASCADE");
                Shader.DisableKeyword("WEATHER_MAKER_SHADOWS_SPLIT_SPHERES");
            }
            {
                Shader.DisableKeyword("WEATHER_MAKER_SHADOWS_ONE_CASCADE");
                Shader.EnableKeyword("WEATHER_MAKER_SHADOWS_SPLIT_SPHERES");
            }
            SetGlobalShaders();

#if UNITY_EDITOR

            if (!Application.isPlaying && Camera.main != null)
            {
                CameraPreRender(Camera.main);
            }

#endif

        }

        private void OnEnable()
        {
            WeatherMakerScript.EnsureInstance(this, ref instance);
            Initialize();

            // light manager pre-render is high priority as it sets a lot of global state
            WeatherMakerCommandBufferManagerScript.Instance.RegisterPreRender(CameraPreRender, this, true);
        }

        private void OnDisable()
        {
            //WeatherMakerFullScreenEffect.ReleaseRenderTexture(ref screenSpaceShadowsRenderTexture);
        }

        private void OnDestroy()
        {
            WeatherMakerCommandBufferManagerScript.Instance.UnregisterPreRender(this);
            WeatherMakerScript.ReleaseInstance(ref instance);
        }

        /// <summary>
        /// This method calculates frustum planes and corners and sets current camera. Normally this is called automatically,
        /// but for something like a reflection camera render in a pre-cull event, call this manually
        /// </summary>
        /// <param name="camera"></param>
        public void CalculateFrustumPlanes(Camera camera)
        {
            Matrix4x4 mat = camera.projectionMatrix * camera.worldToCameraMatrix;
            CurrentCamera = camera;

            // left
            CurrentCameraFrustumPlanes[0].normal = new Vector3(mat.m30 + mat.m00, mat.m31 + mat.m01, mat.m32 + mat.m02);
            CurrentCameraFrustumPlanes[0].distance = mat.m33 + mat.m03;

            // right
            CurrentCameraFrustumPlanes[1].normal = new Vector3(mat.m30 - mat.m00, mat.m31 - mat.m01, mat.m32 - mat.m02);
            CurrentCameraFrustumPlanes[1].distance = mat.m33 - mat.m03;

            // bottom
            CurrentCameraFrustumPlanes[2].normal = new Vector3(mat.m30 + mat.m10, mat.m31 + mat.m11, mat.m32 + mat.m12);
            CurrentCameraFrustumPlanes[2].distance = mat.m33 + mat.m13;

            // top
            CurrentCameraFrustumPlanes[3].normal = new Vector3(mat.m30 - mat.m10, mat.m31 - mat.m11, mat.m32 - mat.m12);
            CurrentCameraFrustumPlanes[3].distance = mat.m33 - mat.m13;

            // near
            CurrentCameraFrustumPlanes[4].normal = new Vector3(mat.m30 + mat.m20, mat.m31 + mat.m21, mat.m32 + mat.m22);
            CurrentCameraFrustumPlanes[4].distance = mat.m33 + mat.m23;

            // far
            CurrentCameraFrustumPlanes[5].normal = new Vector3(mat.m30 - mat.m20, mat.m31 - mat.m21, mat.m32 - mat.m22);
            CurrentCameraFrustumPlanes[5].distance = mat.m33 - mat.m23;

            // normalize
            NormalizePlane(ref CurrentCameraFrustumPlanes[0]);
            NormalizePlane(ref CurrentCameraFrustumPlanes[1]);
            NormalizePlane(ref CurrentCameraFrustumPlanes[2]);
            NormalizePlane(ref CurrentCameraFrustumPlanes[3]);
            NormalizePlane(ref CurrentCameraFrustumPlanes[4]);
            NormalizePlane(ref CurrentCameraFrustumPlanes[5]);

            Transform ct = camera.transform;
            Vector3 cPos = ct.position;
            camera.CalculateFrustumCorners(new Rect(0.0f, 0.0f, 1.0f, 1.0f), camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, CurrentCameraFrustumCorners);
            camera.CalculateFrustumCorners(new Rect(0.0f, 0.0f, 1.0f, 1.0f), camera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, currentCameraFrustumCornersNear);
            CurrentCameraFrustumCorners[0] = cPos + ct.TransformDirection(CurrentCameraFrustumCorners[0]);
            CurrentCameraFrustumCorners[1] = cPos + ct.TransformDirection(CurrentCameraFrustumCorners[1]);
            CurrentCameraFrustumCorners[2] = cPos + ct.TransformDirection(CurrentCameraFrustumCorners[2]);
            CurrentCameraFrustumCorners[3] = cPos + ct.TransformDirection(CurrentCameraFrustumCorners[3]);
            CurrentCameraFrustumCorners[4] = cPos + ct.TransformDirection(currentCameraFrustumCornersNear[0]);
            CurrentCameraFrustumCorners[5] = cPos + ct.TransformDirection(currentCameraFrustumCornersNear[1]);
            CurrentCameraFrustumCorners[6] = cPos + ct.TransformDirection(currentCameraFrustumCornersNear[2]);
            CurrentCameraFrustumCorners[7] = cPos + ct.TransformDirection(currentCameraFrustumCornersNear[3]);
        }

        /// <summary>
        /// Add a light, unless AutoFindLights is true
        /// </summary>
        /// <param name="l">Light to add</param>
        /// <returns>True if light added, false if not</returns>
        public bool AddLight(Light l)
        {
            if (l != null && AutoFindLights == AutoFindLightsMode.None && !HasLight(l) && IgnoreLights != null && !IgnoreLights.Contains(l))
            {
                lights.Add(GetOrCreateLightState(l));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove a light, unless AutoFindLights is true
        /// </summary>
        /// <param name="light"></param>
        /// <returns>True if light removed, false if not</returns>
        public bool RemoveLight(Light light)
        {
            bool result = false;
            if (light != null && AutoFindLights == AutoFindLightsMode.None)
            {
                for (int i = lights.Count - 1; i >= 0; i--)
                {
                    if (lights[i].Light == light)
                    {
                        ReturnLightStateToCache(lights[i]);
                        lights.RemoveAt(i);
                        result = true;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Called when a camera is about to render - sets up shader and light properties, etc.
        /// </summary>
        /// <param name="camera">The current camera</param>
        private void CameraPreRender(Camera camera)
        {
            if (WeatherMakerScript.ShouldIgnoreCamera(this, camera) || WeatherMakerCommandBufferManagerScript.CameraStack > 1 ||
                camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right || camera.stereoTargetEye == StereoTargetEyeMask.Right)
            {
                return;
            }
            UpdateShaderVariables(camera, null, null);

            // clear screen space shadows texture for next camera
            //if (screenSpaceShadowsRenderTexture != null)
            //{
                //RenderTexture temp = RenderTexture.active;
                //RenderTexture.active = screenSpaceShadowsRenderTexture;
                //GL.Clear(true, true, Color.white);
                //RenderTexture.active = temp;
            //}
        }

        /// <summary>
        /// Update shader variables for an object.
        /// </summary>
        /// <param name="camera">Camera</param>
        /// <param name="material">Material (null for global shader variables)</param>
        /// <param name="collider">Collider</param>
        public void UpdateShaderVariables(Camera camera, Material material, Collider collider)
        {
            if (camera == null && collider == null)
            {
                Debug.LogError("Must pass camera or collider to UpdateShaderVariables method");
                return;
            }
            Component obj;
            if (collider != null)
            {
                obj = collider;
                intersectsFunc = intersectsFuncCurrentBounds;
                CurrentBounds = collider.bounds;
                currentLightSortPosition = collider.transform.position;
            }
            else
            {
                obj = camera;
                intersectsFunc = intersectsFuncCurrentCamera;
                CalculateFrustumPlanes(camera);
                currentLightSortPosition = camera.transform.position;
            }

            // *** NOTE: if getting warnings about array sizes changing, simply restart the Unity editor ***
            float elapsed;
            if (!shaderUpdateCounter.TryGetValue(obj, out elapsed) || elapsed >= ShaderUpdateInterval || lastShaderUpdateComponent != obj)
            {
                shaderUpdateCounter[obj] = 0.0f;
                UpdateAllLights();
                lights.Sort(lightSorterReference);

                // update null zones
                UpdateNullZones(material);
            }
            else
            {
                shaderUpdateCounter[obj] += Time.unscaledDeltaTime;
            }
            lastShaderUpdateComponent = obj;

            // add lights for each type
            SetLightsByTypeToShader(camera, material);
        }

        /// <summary>
        /// Get a color for a gradient given sun position
        /// </summary>
        /// <param name="gradient">Gradient</param>
        /// <returns>Color</returns>
        public static Color GetGradientColorForSun(Gradient gradient)
        {
            if (gradient == null || Instance == null || Instance.Sun == null)
            {
                return EvaluateGradient(gradient, 1.0f);
            }
            float sunGradientLookup = GetGradientLookupValueSun();
            return EvaluateGradient(gradient, sunGradientLookup);
        }

        /// <summary>
        /// Get a gradient lookup value for the sun
        /// </summary>
        /// <returns>Value</returns>
        public static float GetGradientLookupValueSun()
        {
            return GetGradientLookupValue((Instance == null || Instance.Sun == null ? null : Instance.Sun.transform));
        }

        /// <summary>
        /// Get a lookup value for a camera and transform
        /// </summary>
        /// <param name="transform">Transform</param>
        /// <returns>Value</returns>
        public static float GetGradientLookupValue(Transform transform)
        {
            if (transform == null || Instance == null)
            {
                return 1.0f;
            }
            float sunGradientLookup;
            CameraMode mode = WeatherMakerScript.ResolveCameraMode();
            if (mode == CameraMode.OrthographicXY)
            {
                sunGradientLookup = transform.forward.z;
            }
            else if (mode == CameraMode.OrthographicXZ)
            {
                sunGradientLookup = transform.forward.x;
            }
            else
            {
                sunGradientLookup = -transform.forward.y;
            }
            sunGradientLookup = ((sunGradientLookup + 1.0f) * 0.5f);
            return sunGradientLookup;
        }

        /// <summary>
        /// Get a color for a gradient given a lookup value on the gradient
        /// </summary>
        /// <param name="gradient">Gradient</param>
        /// <param name="lookup">Lookup value (0 - 1)</param>
        /// <returns>Color</returns>
        public static Color EvaluateGradient(Gradient gradient, float lookup)
        {
            if (gradient == null)
            {
                return Color.white;
            }
            Color color = gradient.Evaluate(lookup);
            float a = color.a;
            color *= color.a;
            color.a = a;
            return color;
        }

        /// <summary>
        /// Get sun mie dot
        /// </summary>
        /// <returns>Sun mie dot</returns>
        public static float GetSunMieDot()
        {
            if (Instance == null || Instance.Sun == null)
            {
                return 1.0f;
            }
            return Mathf.Pow(1.0f - Vector3.Dot(Vector3.up, -Instance.Sun.transform.forward), 5.0f);
        }

        /// <summary>
        /// Current set of lights
        /// </summary>
        public IEnumerable<LightState> Lights
        {
            get { return lights; }
        }

        /// <summary>
        /// Sun color
        /// </summary>
        public Vector4 SunColor { get; private set; }

        /// <summary>
        /// Sun tint color
        /// </summary>
        public Vector4 SunTintColor { get; private set; }

        /// <summary>
        /// Return whether screen space shadows are enabled
        /// </summary>
        public static BuiltinShaderMode ScreenSpaceShadowMode
        {
            get { return UnityEngine.Rendering.GraphicsSettings.GetShaderMode(UnityEngine.Rendering.BuiltinShaderType.ScreenSpaceShadows); }
        }

        private static WeatherMakerLightManagerScript instance;
        /// <summary>
        /// Shared instance of light manager script
        /// </summary>
        public static WeatherMakerLightManagerScript Instance
        {
            get { return WeatherMakerScript.FindOrCreateInstance(ref instance, true); }
        }

        private readonly List<LightState> lightStateCache = new List<LightState>();
        public class LightState
        {

#if WEATHER_MAKER_TRACK_LIGHT_CHANGES

            private struct PreviousState
            {
                public Vector3 HSV;
                public Quaternion Rotation;
                public Vector3 Position;
                public float Range;
            }

#endif

            public Light Light { get; internal set; }

            public void Reset()
            {
                Light = null;

#if WEATHER_MAKER_TRACK_LIGHT_CHANGES

                states.Clear();

#endif

            }

#if WEATHER_MAKER_TRACK_LIGHT_CHANGES

            private readonly Dictionary<Camera, PreviousState> states = new Dictionary<Camera, PreviousState>();

#endif

            /// <summary>
            /// Update light state
            /// </summary>
            /// <param name="camera">Camera</param>
            /// <returns>Light difference from last frame in camera (0 - 1)</returns>
            internal float Update(Camera camera)
            {

#if WEATHER_MAKER_TRACK_LIGHT_CHANGES

                if (camera == null)
                {
                    return 0.0f;
                }

                PreviousState state;
                if (!states.TryGetValue(camera, out state))
                {
                    states[camera] = state;
                }
                Vector3 curHSV;
                Color.RGBToHSV(Light.color, out curHSV.x, out curHSV.y, out curHSV.z);
                Quaternion curRot = Light.transform.rotation;
                Vector3 curPos = Light.transform.position;
                float curRange = Light.range;
                float diff = Mathf.Abs(state.HSV.x - curHSV.x) + Mathf.Abs(state.HSV.y - curHSV.y) + Mathf.Abs(state.HSV.z - curHSV.z);
                diff += Mathf.Abs(state.Rotation.x - curRot.x) + Mathf.Abs(state.Rotation.y - curRot.y) + Mathf.Abs(state.Rotation.z - curRot.z) + Mathf.Abs(state.Rotation.w - curRot.w);
                float distModifier = Mathf.Min(1.0f, (camera.farClipPlane * 0.01f) / Mathf.Max(0.001f, Vector3.Distance(camera.transform.position, curPos)));
                diff += (distModifier * Vector3.Distance(curPos, state.Position));
                diff += (distModifier * Mathf.Abs(curRange - state.Range));
                diff = Mathf.Min(1.0f, diff);
                state.HSV = curHSV;
                state.Rotation = curRot;
                state.Position = curPos;
                state.Range = curRange;

                return diff;

#else

                return 0.0f;

#endif

            }
        }

        private bool HasLight(Light light)
        {
            if (light == null)
            {
                return false;
            }

            foreach (LightState lightState in lights)
            {
                if (lightState.Light == light)
                {
                    return true;
                }
            }
            return false;
        }

        private LightState GetOrCreateLightState(Light light)
        {
            if (lightStateCache.Count == 0)
            {
                return new LightState { Light = light };
            }
            int idx = lightStateCache.Count - 1;
            LightState lightState = lightStateCache[idx];
            lightState.Light = light;
            lightStateCache.RemoveAt(idx);
            return lightState;
        }

        private void ReturnLightStateToCache(LightState lightState)
        {
            if (lightState != null && !lightStateCache.Contains(lightState))
            {
                lightState.Reset();
                lightStateCache.Add(lightState);
            }
        }
    }
}
