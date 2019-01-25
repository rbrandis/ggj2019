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
using System.ComponentModel;
using System.Globalization;
using System.Linq;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;

// #define COPY_FULL_DEPTH_TEXTURE

namespace DigitalRuby.WeatherMaker
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(WeatherMakerCommandBufferManagerScript))]
    public class WeatherMakerScript : MonoBehaviour, IWeatherMakerProvider
    {
        [Header("Setup")]
        [Tooltip("Camera mode. This affects lighting, visual effects and more. Ensure this is set to the correct value for your game or app.")]
        public CameraMode CameraType;

        [Tooltip("Cameras that render make should render with. Only cameras in this list are rendered. Defaults to main camera.")]
        public List<Camera> AllowCameras = new List<Camera>();

        [Tooltip("A set of names for allowed cameras, useful for plugins that clone or add custom cameras that you want to allow. Names must match exactly.")]
        public List<string> AllowCamerasNames = new List<string>();

        /// <summary>
        /// Main camera
        /// </summary>
        public Camera MainCamera { get { return (AllowCameras == null || AllowCameras.Count == 0 ? Camera.main : AllowCameras[0]); } }

        [Tooltip("Whether this instance is a server (true) or client (false). Set to true if single player. This setting may be managed by a network script.")]
        public bool InstanceIsServer = true;

        [Tooltip("The client id of this instance for networking")]
        public string InstanceClientId;

        /// <summary>
        /// Convenience function that checks for null instance
        /// </summary>
        public static bool IsServer { get { return instance == null || instance.InstanceIsServer; } }

        /// <summary>
        /// Convenience function that checks for null instance
        /// </summary>
        public static string ClientId { get { return instance == null ? null : instance.InstanceClientId; } }

        [Header("Performance")]
        [Tooltip("The performance profile to use. If null, this will be populated automatically according to Unity quality setting.")]
        [SerializeField]
        private WeatherMakerPerformanceProfileScript _PerformanceProfile;

        /// <summary>
        /// Get the performance profile or default profile if null
        /// </summary>
        public WeatherMakerPerformanceProfileScript PerformanceProfile
        {
            get { return _PerformanceProfile ?? defaultProfile ?? (defaultProfile = Resources.Load<WeatherMakerPerformanceProfileScript>("WeatherMakerPerformanceProfile_Default")); }
        }
        private WeatherMakerPerformanceProfileScript defaultProfile;

        private WeatherMakerCommandBufferManagerScript commandBufferManager;
        /// <summary>
        /// Command buffer manager
        /// </summary>
        public WeatherMakerCommandBufferManagerScript CommandBufferManager { get { return commandBufferManager; } }

        /// <summary>
        /// Precipitation manager
        /// </summary>
        public IPrecipitationManager PrecipitationManager { get; set; }

        /// <summary>
        /// Cloud manager
        /// </summary>
        public ICloudManager CloudManager { get; set; }

        /// <summary>
        /// Fog manager
        /// </summary>
        public IFogManager FogManager { get; set; }

        /// <summary>
        /// Wind manager
        /// </summary>
        public IWindManager WindManager { get; set; }

        /// <summary>
        /// Thunder and lightning manager
        /// </summary>
        public IThunderAndLightningManager ThunderAndLightningManager { get; set; }

        /// <summary>
        /// Player sound manager
        /// </summary>
        public IPlayerSoundManager PlayerSoundManager { get; set; }

        /// <summary>
        /// Network manager, null implementation if no networking
        /// </summary>
        public IWeatherMakerNetworkScript NetworkManager
        {
            get { return (_networkScript ?? (_networkScript = new WeatherMakerNullNetworkScript())); }
            set { _networkScript = value; }
        }
        private IWeatherMakerNetworkScript _networkScript;

        /// <summary>
        /// Event that fires when the weather profile changes (old, new, transition duration, client ids (null for all clients))
        /// </summary>
        public event System.Action<WeatherMakerProfileScript, WeatherMakerProfileScript, float, string[]> WeatherProfileChanged;

        /// <summary>
        /// Allows adding additional weather intensity modifiers by key. Note that setting any values in here will override the external intensity
        /// on any built in weather maker particle system scripts.
        /// </summary>
        [System.NonSerialized]
        public readonly System.Collections.Generic.Dictionary<string, float> IntensityModifierDictionary = new System.Collections.Generic.Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase);

        private readonly System.Collections.Generic.List<System.Action> mainThreadActions = new System.Collections.Generic.List<System.Action>();

        /// <summary>
        /// Internal array for non-alloc physics operation
        /// </summary>
        internal static readonly Collider[] tempColliders = new Collider[16];

        /// <summary>
        /// The current weather profile
        /// </summary>
        public WeatherMakerProfileScript CurrentProfile { get; private set; }

        private static readonly string[] cameraStringsForReflection = new string[] { "mirror", "water", "refl" };
        private static readonly string[] cameraStringsForCubeMap = new string[] { "probe" };
        private static readonly string[] cameraStringsForOther = new string[] { "prerender", "depthbuffer", "effects", "preview" };
        private static readonly Dictionary<string, WeatherMakerCameraType> cameraTypes = new Dictionary<string, WeatherMakerCameraType>(System.StringComparer.OrdinalIgnoreCase);

        private void UpdateMainThreadActions()
        {
            lock (mainThreadActions)
            {
                foreach (System.Action action in mainThreadActions)
                {
                    action();
                }
                mainThreadActions.Clear();
            }
        }

        private void SetGlobalShaderProperties()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            else if (PerformanceProfile.EnablePerPixelLighting && SystemInfo.graphicsShaderLevel >= 30)
            {
                if (!Shader.IsKeywordEnabled("WEATHER_MAKER_PER_PIXEL_LIGHTING"))
                {
                    Shader.EnableKeyword("WEATHER_MAKER_PER_PIXEL_LIGHTING");
                }
            }
            else if (Shader.IsKeywordEnabled("WEATHER_MAKER_PER_PIXEL_LIGHTING"))
            {
                Shader.DisableKeyword("WEATHER_MAKER_PER_PIXEL_LIGHTING");
            }

            // global fog params, individual material can override
            // useful for hacks like the tree billboard shader override that account for full screen fog
            // LateUpdate of other scripts can override
            Shader.SetGlobalInt(WMS._WeatherMakerFogMode, 0);
            Shader.SetGlobalFloat(WMS._WeatherMakerFogDensity, 0.0f);
            Shader.SetGlobalFloat(WMS._WeatherMakerFogFactorMax, 1.0f);
            Shader.SetGlobalFloat(WMS._WeatherMakerEnableToneMapping, PerformanceProfile.EnableTonemap ? 1.0f : 0.0f);
            Shader.SetGlobalFloat(WMS._WeatherMakerFogGlobalShadow, 1.0f);
            Shader.SetGlobalFloat(WMS._WeatherMakerCloudVolumetricShadow, 1.0f);
            Shader.SetGlobalFloat(WMS._WeatherMakerCloudGlobalShadow, 1.0f);
        }

        private void CheckPerformanceProfile()
        {
            if (_PerformanceProfile == null && Application.isPlaying)
            {
                string name = "WeatherMakerPerformanceProfile_Default";
                if (UnityEngine.XR.XRDevice.isPresent)
                {
                    name = "WeatherMakerPerformanceProfile_VR";
                }
                else
                {
                    switch (UnityEngine.QualitySettings.GetQualityLevel())
                    {
                        case 0:
                            name = "WeatherMakerPerformanceProfile_Fastest";
                            break;

                        case 1:
                            name = "WeatherMakerPerformanceProfile_Fast";
                            break;

                        case 2:
                            name = "WeatherMakerPerformanceProfile_Simple";
                            break;

                        case 3:
                            name = "WeatherMakerPerformanceProfile_Good";
                            break;

                        case 4:
                            name = "WeatherMakerPerformanceProfile_Beautiful";
                            break;

                        default:
                            name = "WeatherMakerPerformanceProfile_Fantastic";
                            break;

                    }
                }
                _PerformanceProfile = Resources.Load<WeatherMakerPerformanceProfileScript>(name);
                if (_PerformanceProfile == null)
                {
                    Debug.LogError("Unable to auto-load performance profile with name " + name);
                }
            }
        }

        private void Awake()
        {

#if UNITY_EDITOR

            if (GameObject.FindObjectsOfType<WeatherMakerScript>().Length > 1)
            {
                Debug.LogError("Only one WeatherMakerScript should exist in your game. Use the WeatherMakerPrefab and call DontDestroyOnLoad.");
            }

#endif


#if UNITY_EDITOR

            string currBuildSettings = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup);
            if (!currBuildSettings.Contains("WEATHER_MAKER_PRESENT"))
            {
                UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup, currBuildSettings + ";WEATHER_MAKER_PRESENT");
            }

#endif

            WMS.Initialize();
            commandBufferManager = GetComponentInChildren<WeatherMakerCommandBufferManagerScript>();
            if (commandBufferManager == null)
            {
                Debug.LogError("CommandBufferManager needs to be set on WeatherMakerScript");
            }
            if (AllowCameras == null || AllowCameras.Count == 0 || AllowCameras[0] == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera == null)
                {

#if UNITY_EDITOR

                    if (GameObject.FindObjectOfType<UnityEngine.Networking.NetworkManager>() == null)
                    {
                        Debug.LogError("Please setup allow camera list on WeatherMakerScript");
                    }

#endif

                }
                else
                {
                    AllowCameras = new List<Camera> { mainCamera };
                }
            }

            CheckPerformanceProfile();
        }

        private void RemoveDestroyedCameras()
        {
            for (int i = AllowCameras.Count - 1; i >= 0; i--)
            {
                if (AllowCameras[i] == null)
                {
                    AllowCameras.RemoveAt(i);
                }
            }
        }

        private void Update()
        {
            RemoveDestroyedCameras();
            CheckPerformanceProfile();
            SetGlobalShaderProperties();
        }

        private void LateUpdate()
        {

#if UNITY_EDITOR

            if (transform.position != Vector3.zero || transform.localScale != Vector3.one || transform.rotation != Quaternion.identity)
            {
                Debug.LogError("For correct rendering, weather maker manager script/prefab should have position and rotation of 0, and scale of 1.");
            }

#endif

            UpdateMainThreadActions();
            UpdateExternalIntensities();
        }

        private void OnEnable()
        {
            nullInstances.Clear();
            WMS.Initialize();
            PrecipitationManager = FindIfNull<IPrecipitationManager, WeatherMakerPrecipitationManagerScript>(PrecipitationManager);
            CloudManager = FindIfNull<ICloudManager, WeatherMakerCloudManagerScript>(CloudManager);
            if (CloudManager == null)
            {
                CloudManager = FindIfNull<ICloudManager, WeatherMakerCloudManager2DScript>(CloudManager);
            }
            FogManager = FindIfNull<IFogManager, WeatherMakerFogManagerScript>(FogManager);
            WindManager = FindIfNull<IWindManager, WeatherMakerWindManagerScript>(WindManager);
            ThunderAndLightningManager = FindIfNull<IThunderAndLightningManager, WeatherMakerThunderAndLightningManagerScript>(ThunderAndLightningManager);
            PlayerSoundManager = FindIfNull<IPlayerSoundManager, WeatherMakerPlayerSoundManagerScript>(PlayerSoundManager);

            // wire up lightning bolt lights to the light manager
            if (Application.isPlaying)
            {
                if (WeatherMakerLightManagerScript.Instance != null && WeatherMakerThunderAndLightningScript.Instance != null)
                {
                    WeatherMakerThunderAndLightningScript.Instance.LightningBoltScript.LightAddedCallback += LightningLightAdded;
                    WeatherMakerThunderAndLightningScript.Instance.LightningBoltScript.LightRemovedCallback += LightningLightRemoved;
                }
            }
        }

        private void OnDisable()
        {
            cameraTypes.Clear();
            nullInstances.Clear();
        }

        private void OnDestroy()
        {
            TweenFactory.Clear();
            WeatherMakerObjectExtensions.Clear();

            // remove lightning bolt lights from the light manager
            if (Application.isPlaying && WeatherMakerLightManagerScript.Instance != null && WeatherMakerThunderAndLightningScript.Instance != null)
            {
                WeatherMakerThunderAndLightningScript.Instance.LightningBoltScript.LightAddedCallback -= LightningLightAdded;
                WeatherMakerThunderAndLightningScript.Instance.LightningBoltScript.LightRemovedCallback -= LightningLightRemoved;
            }

            WeatherMakerScript.ReleaseInstance(ref instance);
        }

        private TInterface FindIfNull<TInterface, T>(TInterface value) where TInterface : class where T : UnityEngine.Component, TInterface
        {
            if (value == null)
            {
                value = gameObject.GetComponentInChildren<T>();
            }
            return value as TInterface;
        }

        private void LightningLightAdded(Light l)
        {
            if (WeatherMakerLightManagerScript.Instance != null)
            {
                WeatherMakerLightManagerScript.Instance.AddLight(l);
            }
        }

        private void LightningLightRemoved(Light l)
        {
            if (WeatherMakerLightManagerScript.Instance != null)
            {
                WeatherMakerLightManagerScript.Instance.RemoveLight(l);
            }
        }

        private void UpdateExternalIntensities()
        {
            if (WeatherMakerThunderAndLightningScript.Instance != null)
            {
                WeatherMakerThunderAndLightningScript.Instance.ExternalIntensityMultiplier = 1.0f;
                foreach (float multiplier in IntensityModifierDictionary.Values)
                {
                    WeatherMakerThunderAndLightningScript.Instance.ExternalIntensityMultiplier *= multiplier;
                }
            }
            if (WeatherMakerWindScript.Instance != null)
            {
                WeatherMakerWindScript.Instance.ExternalIntensityMultiplier = 1.0f;
                foreach (float multiplier in IntensityModifierDictionary.Values)
                {
                    WeatherMakerWindScript.Instance.ExternalIntensityMultiplier *= multiplier;
                }
            }
        }

        /// <summary>
        /// Determine if a camera should be rendered in weather maker
        /// </summary>
        /// <param name="script">Script</param>
        /// <param name="camera">Camera</param>
        /// <param name="ignoreReflections">Whether to ignore reflection cameras (i.e. water reflection or mirror)</param>
        /// <returns>True to ignore, false to render</returns>
        public static bool ShouldIgnoreCamera(MonoBehaviour script, Camera camera, bool ignoreReflections = true)
        {
            if (Instance == null || script == null)
            {
                return true;
            }

#if UNITY_EDITOR

            else if (camera.cameraType == UnityEngine.CameraType.SceneView || camera.cameraType == UnityEngine.CameraType.Preview)
            {
                return true;
            }

#endif

            if (camera == null || !script.enabled || (Instance.AllowCameras.Count == 0 && Instance.AllowCamerasNames.Count == 0))
            {
                return true;
            }

            WeatherMakerCameraType type = WeatherMakerScript.GetCameraType(camera);

            // if camera is not in allow list and
            // camera is not an allowed reflection camera and
            // camera is not an allowed reflection probe camera
            // then ignore it
            bool notInAllowList = !Instance.AllowCameras.Contains(camera);
            bool notInAllowNameList = true;
            foreach (string s in Instance.AllowCamerasNames)
            {
                if (camera.CachedName() == s)
                {
                    notInAllowNameList = false;
                    break;
                }
            }
            bool ignoreReflection = (ignoreReflections || type != WeatherMakerCameraType.Reflection);
            bool ignoreProbe = (Instance.PerformanceProfile.IgnoreReflectionProbes || type != WeatherMakerCameraType.CubeMap);
            bool ignore = notInAllowList && notInAllowNameList && ignoreReflection && ignoreProbe;
            return ignore;
        }

        /// <summary>
        /// Get the type of a camera
        /// </summary>
        /// <param name="c">Camera</param>
        /// <returns>Camera type</returns>
        public static WeatherMakerCameraType GetCameraType(Camera c)
        {
            string name = c.CachedName();

            // cache types, save string compares
            WeatherMakerCameraType type;
            if (cameraTypes.TryGetValue(name, out type))
            {
                return type;
            }

            foreach (string s in cameraStringsForOther)
            {
                if (name.IndexOf(s, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    type = WeatherMakerCameraType.Other;
                    cameraTypes[name] = type;
                    return type;
                }
            }
            foreach (string s in cameraStringsForCubeMap)
            {
                if (name.IndexOf(s, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    type = WeatherMakerCameraType.CubeMap;
                    cameraTypes[name] = type;
                    return type;
                }
            }
            foreach (string s in cameraStringsForReflection)
            {
                if (name.IndexOf(s, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    type = WeatherMakerCameraType.Reflection;
                    cameraTypes[name] = type;
                    return type;
                }
            }
            type = WeatherMakerCameraType.Normal;
            cameraTypes[name] = type;
            return type;
        }

        /// <summary>
        /// Call whenever the weather profile needs to change, handles client/server, etc.
        /// If no networking or network server, this will perform the transition.
        /// WeatherProfileChanged will then be called.
        /// </summary>
        /// <param name="oldProfile">Old weather profile</param>
        /// <param name="newProfile">New weather profile</param>
        /// <param name="transitionDuration">Transition duration</param>
        /// <param name="holdDuration">Hold duration</param>
        /// <param name="clientIds">Client ids to send to (null for none or single player)</param>
        /// <param name="performTransition">Whether to perform the actual transition</param>
        public void RaiseWeatherProfileChanged(WeatherMakerProfileScript oldProfile, WeatherMakerProfileScript newProfile, float transitionDuration,
            float holdDuration, bool performTransition, string[] clientIds)
        {
            if (performTransition)
            {
                Debug.LogFormat("Changing weather profile {0} to {1}, transition time: {2}, hold time: {3}",
                    (oldProfile == null ? "None" : oldProfile.name), (newProfile == null ? "None" : newProfile.name), transitionDuration, (holdDuration <= 0.0f ? "Unknown" : holdDuration.ToString(CultureInfo.InvariantCulture)));
                oldProfile = (oldProfile == null ? CurrentProfile : oldProfile);
                newProfile.TransitionFrom(this, oldProfile, transitionDuration);
            }

            // notify listeners
            if (WeatherProfileChanged != null)
            {
                WeatherProfileChanged.Invoke(oldProfile, newProfile, transitionDuration, clientIds);
            }
        }

        /// <summary>
        /// Queue an action to run on the main thread - this action should run as fast as possible to avoid locking the main thread.
        /// </summary>
        /// <param name="action">Action to run</param>
        public static void QueueOnMainThread(System.Action action)
        {
            if (Instance != null)
            {
                lock (Instance.mainThreadActions)
                {
                    Instance.mainThreadActions.Add(action);
                }
            }
        }

        /// <summary>
        /// Resolve camera mode if mode is Auto. Will resolve against camera or Camera.main if camera is null
        /// </summary>
        /// <param name="mode">Camera mode (null for current camera type or auto)</param>
        /// <param name="camera">Camera or null for main camera</param>
        /// <returns>Camera mode</returns>
        public static CameraMode ResolveCameraMode(CameraMode? mode = null, Camera camera = null)
        {
            camera = (camera == null ? Camera.main : camera);
            if (mode == null)
            {
                mode = (Instance == null ? CameraMode.Auto : Instance.CameraType);
            }
            if (camera == null)
            {
                if (mode.Value == CameraMode.Auto)
                {
                    return CameraMode.Perspective;
                }
                return mode.Value;
            }
            else if (mode.Value == CameraMode.OrthographicXY || (mode.Value == CameraMode.Auto && camera.orthographic))
            {
                return CameraMode.OrthographicXY;
            }
            else if (mode.Value == CameraMode.Perspective || (mode.Value == CameraMode.Auto && !camera.orthographic))
            {
                return CameraMode.Perspective;
            }
            return CameraMode.OrthographicXZ;
        }

        /// <summary>
        /// Get the client id of a game object
        /// </summary>
        /// <param name="obj">Transform</param>
        /// <returns>Client id or null if not found</returns>
        public static string GetClientId(Transform obj)
        {
            if (GetClientIdFunc == null)
            {
                NetworkIdentity id = obj.GetComponentInChildren<NetworkIdentity>();
                if (id == null && obj.transform.parent != null)
                {
                    id = obj.transform.parent.GetComponent<NetworkIdentity>();
                }
                return (id == null ? null : id.netId.Value.ToString(CultureInfo.InvariantCulture));
            }
            return GetClientIdFunc(obj);
        }

        /// <summary>
        /// Determine if an object is a player (including network players)
        /// </summary>
        /// <param name="obj">Transform</param>
        /// <returns>True if player, false if not</returns>
        public static bool IsPlayer(Transform obj)
        {
            return (obj.GetComponent<AudioListener>() != null);
        }

        /// <summary>
        /// Get whether an object is the local player
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns>True if local player, false otherwise</returns>
        public static bool IsLocalPlayer(Transform obj)
        {
            AudioListener listener = obj.GetComponent<AudioListener>();
            return (listener != null && listener.enabled);
        }

        private static readonly HashSet<System.Type> nullInstances = new HashSet<System.Type>();

        /// <summary>
        /// Find or create a singleton object. In OnDestroy, you must call ReleaseInstance to avoid memory leaks.
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="instance">Singleton instance variable</param>
        /// <param name="required">Whether the instance is required</param>
        /// <returns>Result, can be null</returns>
        public static T FindOrCreateInstance<T>(ref T instance, bool required = false) where T : MonoBehaviour
        {
            if (instance == null && !nullInstances.Contains(typeof(T)) && !StackTraceUtility.ExtractStackTrace().Contains("OnDestroy"))
            {
                T[] scripts = GameObject.FindObjectsOfType<T>();
                foreach (T script in scripts)
                {
                    if (script.enabled)
                    {
                        instance = script;
                        nullInstances.Remove(typeof(T));
                        break;
                    }
                }
                if (instance == null)
                {
                    if (required)
                    {
                        if (Application.isPlaying)
                        {
                            Debug.LogErrorFormat("Object of type {0} is required, please ensure it is active in the WeatherMakerPrefab", typeof(T));
                        }
                    }
                    else
                    {
                        nullInstances.Add(typeof(T));
                    }
                }
            }
            return instance;
        }

        /// <summary>
        /// Ensures that an object is the correct singleton for itself
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="obj">Object</param>
        /// <param name="instance">Singleton reference</param>
        public static void EnsureInstance<T>(MonoBehaviour obj, ref T instance) where T : MonoBehaviour
        {
            if (instance == null)
            {
                instance = obj as T;

                if (instance == null)
                {
                    Debug.LogError("Incorrect object type passed to EnsureInstance, must be of type T");
                }
            }
            else if (instance != obj)
            {
                Debug.LogErrorFormat("Multiple instances of {0} detected, this is not supported", typeof(T).FullName);
            }
        }

        /// <summary>
        /// Release an instance created with FindOrCreateInstance
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="instance">Instance to release</param>
        public static void ReleaseInstance<T>(ref T instance) where T : MonoBehaviour
        {
            instance = null;
            nullInstances.Remove(typeof(T));
        }

        /// <summary>
        /// Optional function that can return the network identity of an object.
        // If not using Unity networking, you must supply the implementation to return the client id (equivelant to Unity netId).
        /// </summary>
        public static System.Func<Transform, string> GetClientIdFunc;

        private static WeatherMakerScript instance;
        /// <summary>
        /// Shared instance of weather maker manager script
        /// </summary>
        public static WeatherMakerScript Instance
        {
            get { return FindOrCreateInstance<WeatherMakerScript>(ref instance, true); }
        }
    }

    /// <summary>
    /// Interface for all weather maker managers
    /// </summary>
    public interface IWeatherMakerManager
    {
        /// <summary>
        /// Handle a weather profile change
        /// </summary>
        /// <param name="oldProfile">Old weather profile</param>
        /// <param name="newProfile">New weather profile</param>
        /// <param name="transitionDuration">Transition delay</param>
        /// <param name="transitionDuration">Transition duration</param>
        void WeatherProfileChanged(WeatherMakerProfileScript oldProfile, WeatherMakerProfileScript newProfile, float transitionDelay, float transitionDuration);
    }
}
