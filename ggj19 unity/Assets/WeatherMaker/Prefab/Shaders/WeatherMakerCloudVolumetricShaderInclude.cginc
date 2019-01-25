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

#ifndef __WEATHER_MAKER_CLOUD_VOLUMETRIC_SHADER__
#define __WEATHER_MAKER_CLOUD_VOLUMETRIC_SHADER__

float ComputeCloudShadowStrength(float3 worldPos, uint dirIndex);

#include "WeatherMakerCloudShaderInclude.cginc"

// comment out to turn off volumetric clouds and remove all shader code for volumetric clouds
#define WEATHER_MAKER_ENABLE_VOLUMETRIC_CLOUDS

// 0 = use sphere, 1 = use plane
#define VOLUMETRIC_CLOUD_USE_PLANE 0

// 1 = normal lighting, 2 = heatmap (raymarch cost)
#define VOLUMETRIC_CLOUD_RENDER_MODE 1

// how many 0 value samples before we start marching ahead faster under the assumtion we are in empty sky
// not seeing a much of a difference here, need to change this some how...
// also causes flickering pixels at horizon
#define VOLUMETRIC_CLOUD_SKIP_SAMPLE_THRESHOLD 0

// max samples to skip for each cloud sample iteration - values higher than 1 cause artifacts, TODO: fix this
#define VOLUMETRIC_CLOUD_SKIP_SAMPLE_MAX 1

// whether point lights should be enabled
#define VOLUMETRIC_CLOUD_ENABLE_POINT_LIGHTS 1

// reduce sample count as optical depth increases
#define VOLUMETRIC_SAMPLE_COUNT_OPTICAL_DEPTH_REDUCER 3.0

// increase lod as optical depth increases
#define VOLUMETRIC_LOD_OPTICAL_DEPTH_MULTIPLIER 5.0

// uncomment to use linear instead of exponential ambient sampling
// #define VOLUMETRIC_CLOUD_AMBIENT_MODE_LINEAR

// control powder base value when powder formula does not apply
#define VOLUMETRIC_POWDER_BASE_VALUE 1.0

// before multiplying powder ray y, add this value to it
#define VOLUMETRIC_POWDER_RAY_Y_ADDER 0.1

// multiply ray y by this and multiply by powder value
#define VOLUMETRIC_POWDER_RAY_Y_MULTIPLIER 2.0

// max henyey greenstein value
#define VOLUMETRIC_MAX_HENYEY_GREENSTEIN 5.0

// multiply height by this before applying cloud detail
#define VOLUMETRIC_DETAIL_HEIGHT_MULTIPLIER 10.0

// cloud detail mapping strength (0 - 1)
#define VOLUMETRIC_DETAIL_MAP_STRENGTH 0.4

// optical depth in horizon fade goes to this power first
#define VOLUMETRIC_HORIZON_FADE_OPTICAL_DEPTH_POWER 1.4

// subtract from optical depth before applying horizon fade, helps prevent higher up clouds becoming slightly transparent
#define VOLUMETRIC_HORIZON_FADE_OPTICAL_DEPTH_SUBTRACTOR 0.05

// max horizon fade, 0 for complete fade
#define VOLUMETRIC_MAX_HORIZON_FADE 0.0

// dither horizon fade to reduce banding
#define VOLUMETRIC_HORIZON_FADE_DITHER 0.005

// min amount of distance for each ray march
#define VOLUMETRIC_MIN_STEP_LENGTH 16.0

// minimum coverage to sample cloud, lower this value if you see missed / hard cloud edges or clouds blinking in and out when moving fast
#define MINIMUM_COVERAGE_FOR_CLOUD 0.1

#if defined(WEATHER_MAKER_ENABLE_VOLUMETRIC_CLOUDS)

uniform sampler3D _CloudNoiseShapeVolumetric;
uniform sampler3D _CloudNoiseDetailVolumetric;
uniform sampler2D _CloudNoiseCurlVolumetric;
uniform sampler2D _WeatherMakerWeatherMapTexture;
uniform uint2 _CloudNoiseSampleCountVolumetric;
uniform float2 _CloudNoiseLodVolumetric;
uniform float4 _CloudNoiseScaleVolumetric; // shape scale, details scale, curl scale, curl multiplier
uniform float _CloudNoiseScalarVolumetric;
uniform fixed4 _CloudColorVolumetric;
uniform fixed4 _CloudDirColorVolumetric;
uniform fixed4 _CloudEmissionColorVolumetric;

uniform float3 _CloudShapeAnimationVelocity;
uniform float3 _CloudDetailAnimationVelocity;

uniform float _CloudDirLightMultiplierVolumetric;
uniform float _CloudPointSpotLightMultiplierVolumetric;
uniform float _CloudAmbientGroundIntensityVolumetric;
uniform float _CloudAmbientSkyIntensityVolumetric;
uniform float _CloudSkyIntensityVolumetric;
uniform float _CloudAmbientSkyHeightMultiplierVolumetric;
uniform float _CloudAmbientGroundHeightMultiplierVolumetric;
uniform float _CloudLightAbsorptionVolumetric;
uniform float _CloudDirLightIndirectMultiplierVolumetric;
uniform float _CloudShapeNoiseMinVolumetric;
uniform float _CloudShapeNoiseMaxVolumetric;
uniform float _CloudPowderMultiplierVolumetric;
uniform float _CloudBottomFadeVolumetric;
uniform float _CloudMaxRayLengthMultiplierVolumetric;
uniform float _CloudRayDitherVolumetric;
uniform float _CloudOpticalDistanceMultiplierVolumetric;
uniform float _CloudHorizonFadeMultiplierVolumetric;

uniform float _CloudCoverVolumetric;
uniform float _CloudCoverSecondaryVolumetric;
uniform float _CloudTypeVolumetric;
uniform float _CloudTypeSecondaryVolumetric;
uniform float _CloudDensityVolumetric;
uniform float _CloudHeightNoisePowerVolumetric;

// cloud layer start from sea level
uniform float _CloudStartVolumetric;
uniform float _CloudStartSquaredVolumetric;
uniform float _CloudPlanetStartVolumetric; // cloud start + planet radius
uniform float _CloudPlanetStartSquaredVolumetric; // cloud start + planet radius, squared

// cloud layer height from start (relative to start)
uniform float _CloudHeightVolumetric;
uniform float _CloudHeightInverseVolumetric;
uniform float _CloudHeightSquaredVolumetric;
uniform float _CloudHeightSquaredInverseVolumetric;

// cloud layer end from sea level
uniform float _CloudEndVolumetric;
uniform float _CloudEndSquaredVolumetric;
uniform float _CloudEndSquaredInverseVolumetric;
uniform float _CloudPlanetEndVolumetric; // cloud end + planet radius
uniform float _CloudPlanetEndSquaredVolumetric; // cloud end + planet radius, squared

uniform float _CloudPlanetRadiusVolumetric;
uniform float _CloudPlanetRadiusNegativeVolumetric;
uniform float _CloudPlanetRadiusSquaredVolumetric;

uniform float4 _CloudHenyeyGreensteinPhaseVolumetric;
uniform float _CloudShadowThresholdVolumetric;
uniform float _CloudShadowPowerVolumetric;
uniform float _CloudShadowMultiplierVolumetric;
uniform float _CloudRayOffsetVolumetric;
uniform float _CloudMinRayYVolumetric;
uniform float _CloudLightStepMultiplierVolumetric;
uniform uint _CloudDirLightSampleCount;

uniform float _WeatherMakerWeatherMapScale = 0.00001;
uniform float _CloudShadowMapAdder;
uniform float _CloudShadowMapMultiplier;
uniform float _CloudShadowMapMinimum;
uniform float _CloudShadowMapMaximum;
uniform float _CloudShadowMapPower;
uniform float _WeatherMakerCloudVolumetricShadow;

//uniform uint _WeatherMakerShadowCascades;

// cloud dir light rays
uniform uint _CloudDirLightRaySampleCount;
uniform float _CloudDirLightRayDensity;
uniform float _CloudDirLightRayDecay;
uniform float _CloudDirLightRayWeight;
uniform float _CloudDirLightRayBrightness;

uniform float4 _CloudGradientStratus;
uniform float4 _CloudGradientStratoCumulus;
uniform float4 _CloudGradientCumulus;

uniform float3 _CloudConeRandomVectors[6];

// random vectors on the unit sphere
//static const float3 volumetricConeRandomVectors[6] =
//{
	//float3(0.38051305f,  0.92453449f, -0.02111345f),
	//float3(-0.50625799f, -0.03590792f, -0.86163418f),
	//float3(-0.32509218f, -0.94557439f,  0.01428793f),
	//float3(0.09026238f, -0.27376545f,  0.95755165f),
	//float3(0.28128598f,  0.42443639f, -0.86065785f),
	//float3(-0.16852403f,  0.14748697f,  0.97460106f),
//};


//static const float4 STRATUS_GRADIENT = float4(0.0, 0.01912, 0.12752, 0.21854);
//static const float4 STRATOCUMULUS_GRADIENT = float4(0.0, 0.03021, 0.32742, 0.61758);
//static const float4 CUMULUS_GRADIENT = float4(0.0, 0.0625, 0.78, 0.95);

//static const float4 STRATUS_GRADIENT = float4(0.02f, 0.05f, 0.09f, 0.11f);
//static const float4 STRATOCUMULUS_GRADIENT = float4(0.02f, 0.2f, 0.48f, 0.625f);
//static const float4 CUMULUS_GRADIENT = float4(0.01f, 0.0625f, 0.78f, 1.0f);

static const float volumetricPositionShapeScale = _CloudHeightInverseVolumetric * _CloudNoiseScaleVolumetric.x;
static const float volumetricPositionDetailScale = volumetricPositionShapeScale * _CloudNoiseScaleVolumetric.y;
static const float volumetricPositionCurlScale = volumetricPositionShapeScale * _CloudNoiseScaleVolumetric.z;
static const float volumetricPositionCurlIntensity = _CloudNoiseScaleVolumetric.w;
static const float3 volumetricPlanetCenter = float3(_CloudCameraPosition.x, _CloudPlanetRadiusNegativeVolumetric, _CloudCameraPosition.z); // TODO: Support true spherical worlds
static const float4 volumetricSphereInner = float4(volumetricPlanetCenter, _CloudPlanetStartSquaredVolumetric);
static const float4 volumetricSphereOutter = float4(volumetricPlanetCenter, _CloudPlanetEndSquaredVolumetric);
static const fixed3 volumetricCloudAmbientColorGround = (_WeatherMakerAmbientLightColorGround * _CloudAmbientGroundIntensityVolumetric);
static const fixed3 volumetricCloudAmbientColorSky = (_WeatherMakerAmbientLightColorSky * _CloudAmbientSkyIntensityVolumetric);
static const float3 volumetricCloudDownVector = normalize(volumetricPlanetCenter - _CloudCameraPosition);
static const float3 volumetricCloudUpVector = normalize(_CloudCameraPosition - volumetricPlanetCenter);
static const float volumetricCloudNoiseMultiplier = 0.25 * _CloudDensityVolumetric;
static const float volumetricCloudMaxRayLength = _CloudHeightVolumetric * _CloudMaxRayLengthMultiplierVolumetric;
static const float invVolumetricCloudMaxRayLength = 1.0 / volumetricCloudMaxRayLength;
static const float volumetricMaxOpticalDistance = _CloudHeightVolumetric * _CloudOpticalDistanceMultiplierVolumetric;
static const float invVolumetricMaxOpticalDistance = 1.0 / volumetricMaxOpticalDistance;
static const float3 volumetricAnimationShape = (_CloudShapeAnimationVelocity * _WeatherMakerTime.y);
static const float3 volumetricAnimationDetail = (_CloudDetailAnimationVelocity * _WeatherMakerTime.y);

// reduce dir light sample for reflections and cubemaps
static const uint volumetricLightIterations = ceil(min(5.0, _CloudDirLightSampleCount) *
(
	WM_CAMERA_RENDER_MODE_NORMAL +
	lerp(0.0, 0.5, WM_CAMERA_RENDER_MODE_REFLECTION) +
	lerp(0.0, 0.5, WM_CAMERA_RENDER_MODE_CUBEMAP)
));
static const float invVolumetricLightIterations = 1.0f / max(1.0, float(volumetricLightIterations));
static const bool volumetricLightSampleDistant = (_CloudDirLightSampleCount == 6);

// not doing point light samples currently
//static const uint volumetricLightIterationsNonDir = 3u;
//static const float invVolumetricLightIterationsNonDir = 0.9 * (1.0f / float(volumetricLightIterationsNonDir));

static const float volumetricDirLightStepSize = _CloudHeightVolumetric * _CloudLightStepMultiplierVolumetric * invVolumetricLightIterations * invVolumetricLightIterations;
static const float volumetricDirLightConeRadius = volumetricDirLightStepSize;

// reduce sample count for reflections and cubemaps
static const float2 volumetricSampleCountRange = ceil(float2(_CloudNoiseSampleCountVolumetric.x, _CloudNoiseSampleCountVolumetric.y) *
(
	WM_CAMERA_RENDER_MODE_NORMAL +
	lerp(0.0, 0.4, WM_CAMERA_RENDER_MODE_REFLECTION) +
	lerp(0.0, 0.4, WM_CAMERA_RENDER_MODE_CUBEMAP)
));

// raise LOD for reflections and cubemaps
static const float2 volumetricLod = float2(_CloudNoiseLodVolumetric.x, _CloudNoiseLodVolumetric.y) +
	(1.25 * WM_CAMERA_RENDER_MODE_REFLECTION) +
	(1.5 * WM_CAMERA_RENDER_MODE_CUBEMAP);


// per fragment state
float lightDotEyeDir[MAX_LIGHT_COUNT];

// volumetric clouds --------------------------------------------------------------------------------
// compute start and end pos of cloud ray march
uint SetupCloudRaymarch(float3 worldSpaceCameraPos, float3 rayDir, float depth, out float3 startPos, out float rayLength, out float3 startPos2, out float rayLength2)
{

#if VOLUMETRIC_CLOUD_USE_PLANE

	RayPlaneIntersect(worldSpaceCameraPos, rayDir, float3(0, 1, 0), float3(worldSpaceCameraPos.x, _CloudStartVolumetric, worldSpaceCameraPos.z), rayLength);
	startPos = worldSpaceCameraPos + (rayDir * rayLength);
	rayLength = _CloudHeightVolumetric;
	return (rayLength > 0.01);

#else

	uint iterations;

	// find where we intersect the lower cloud layer
	float2 innerSphere = RaySphereIntersect(worldSpaceCameraPos, rayDir, depth, volumetricSphereInner);

	// find where we intersect the upper cloud layer
	float2 outterSphere = RaySphereIntersect(worldSpaceCameraPos, rayDir, depth, volumetricSphereOutter);

	float intersectAmount = innerSphere.y;
	float distanceToSphere = innerSphere.x;
	float intersectAmount2 = outterSphere.y;
	float distanceToSphere2 = outterSphere.x;

	// we need to handle case where we are in the inner sphere, in the outter sphere and not in either sphere
	UNITY_BRANCH
	if (intersectAmount > 0.0 && distanceToSphere <= 0.0)
	{
		// we are inside the inner sphere, we can see both spheres
		
		// if depth buffer blocks, exit out
		startPos = rayDir * intersectAmount;
		float startPosLength = length(startPos);
		startPos += worldSpaceCameraPos;
		rayLength = min(depth - startPosLength, (intersectAmount2 - intersectAmount));
		iterations = (rayLength > 0.01);

		startPos2 = float3Zero;
		rayLength2 = 0.0;
		//return fixed4(1, 0, 0, 1);
	}
	else if (intersectAmount2 > 0.0 && distanceToSphere2 <= 0.0)
	{
		if (intersectAmount > 0.0)
		{
			// in outter sphere, ray hit both spheres
			// first intersect in the outter sphere
			rayLength = min(depth, distanceToSphere);
			startPos = worldSpaceCameraPos;
			iterations = (rayLength > 0.01);

			// second intersect on backside of outter sphere
			float ignoreRayLength = distanceToSphere + intersectAmount;
			startPos2 = worldSpaceCameraPos + (rayDir * ignoreRayLength);
			rayLength2 = min(depth - ignoreRayLength, (intersectAmount2 - ignoreRayLength));
			iterations += (rayLength2 > 0.01);
			//return fixed4(0, 1, 0, 1);
		}
		else
		{
			// else in outter sphere, ray missed inner sphere
			startPos = worldSpaceCameraPos;
			rayLength = min(depth, intersectAmount2);
			iterations = (rayLength > 0.01);

			startPos2 = float3Zero;
			rayLength2 = 0.0;
			//return fixed4(0, 0, 1, 1);
		}
	}
	else if (intersectAmount2 <= 0.0)
	{
		// we are outside the outter sphere and have no intersection with the outter sphere, nothing to do
		startPos = float3Zero;
		rayLength = 0.0;
		startPos2 = float3Zero;
		rayLength2 = 0.0;
		iterations = 0;
	}
	else if (intersectAmount <= 0.0)
	{
		// we are outside the outter sphere and missed inner sphere, just calculate outter sphere intersect
		startPos = rayDir * distanceToSphere2;

		// if depth buffer blocks, exit out
		float startPosLength = length(startPos);
		startPos += worldSpaceCameraPos;
		rayLength = min(depth - startPosLength, intersectAmount2);
		iterations = (rayLength > 0.01);

		startPos2 = float3Zero;
		rayLength2 = 0.0;
		//return fixed4(0, 0, 0, 1);
	}
	else
	{
		// we are outside the outter sphere and hit both spheres, start at outter sphere intersect point
		startPos = rayDir * distanceToSphere2;

		// if depth buffer blocks, exit out
		float startPosLength = length(startPos);
		startPos += worldSpaceCameraPos;
		rayLength = min(depth - startPosLength, (distanceToSphere - distanceToSphere2));
		iterations = (rayLength > 0.01);

		startPos2 = float3Zero;
		rayLength2 = 0.0;
		//return fixed4(1, 0, 1, 1);
	}

#endif

	return iterations;
}

float4 CloudVolumetricSampleWeather(float3 pos, float heightFrac)
{
	pos.xz -= _CloudCameraPosition.xz;
	pos.xz *= _WeatherMakerWeatherMapScale;
	pos.xz += 0.5; // 0.5, 0.5 is center of weather map at world pos xz of 0,0, as camera moves they will tile through the weather map
	float4 c = tex2Dlod(_WeatherMakerWeatherMapTexture, float4(pos.xz, 0.0, 0.0));
	return c;
}

inline float CloudVolumetricGetCoverage(float4 weatherData)
{
	return weatherData.r;
}

inline float CloudVolumetricGetCloudType(float4 weatherData)
{
	// weather b channel tells the cloud type 0.0 = stratus, 0.5 = stratocumulus, 1.0 = cumulus
	return weatherData.b;
}

inline float CloudVolumetricBeerLambert(float density)
{
	// TODO: Multiply by precipitation or density for rain/snow clouds
	return exp2(-density);
}

float CloudVolumetricPowder(float density, float lightDotEye, float lightIntensity, float3 lightDir, float3 rayDir)
{
	// base powder term
	float powder = 1.0 - exp2(-2.0 * density);

	// increase powder intensity
	powder = saturate(powder * _CloudPowderMultiplierVolumetric);

	// reduce powder as angle from sun decreases, reduce powder as light is at horizon (light is passing through more air)
	// in dim light or when light as horizon, powder effect looks bad
	float powderHorizonLerp = saturate((lightDir.y + VOLUMETRIC_POWDER_RAY_Y_ADDER) * VOLUMETRIC_POWDER_RAY_Y_MULTIPLIER);
	return lerp(VOLUMETRIC_POWDER_BASE_VALUE, powder, (1.0 - (lightDotEye * lightDotEye)) * powderHorizonLerp) * powderHorizonLerp;
}

float CloudVolumetricHenyeyGreensteinVolumetric(float lightDotEye, float lightIntensity, float3 lightDir)
{
	// f(x) = (1 - g)^2 / (4PI * (1 + g^2 - 2g*cos(x))^[3/2])
	// _CloudHenyeyGreensteinPhase.x = forward, _CloudHenyeyGreensteinPhase.y = back
	static const float g = _CloudHenyeyGreensteinPhaseVolumetric.x;
	static const float gSquared = g * g;
	static const float oneMinusGSquared = (1.0 - gSquared);
	static const float onePlusGSquared = 1.0 + gSquared;
	static const float twoGSquared = 2.0 * g;
	float falloff = onePlusGSquared - (twoGSquared * lightDotEye);
	float forward = (oneMinusGSquared / (pow(falloff, 1.5)));

	static const float g2 = _CloudHenyeyGreensteinPhaseVolumetric.y;
	static const float gSquared2 = g2 * g2;
	static const float oneMinusGSquared2 = (1.0 - gSquared2);
	static const float onePlusGSquared2 = 1.0 + gSquared2;
	static const float twoGSquared2 = 2.0 * g2;
	float falloff2 = onePlusGSquared2 - (twoGSquared2 * lightDotEye);
	float back = lightIntensity * oneMinusGSquared2 / (pow(falloff2, 1.5));

	return min(VOLUMETRIC_MAX_HENYEY_GREENSTEIN, ((forward * _CloudHenyeyGreensteinPhaseVolumetric.z) + (back * _CloudHenyeyGreensteinPhaseVolumetric.w)));

	/*
	float g = _CloudHenyeyGreensteinPhase.x;
	float g2 = g * g;
	float h = _CloudHenyeyGreensteinPhase.z * ((1.0f - g2) / pow((1.0f + g2 - 2.0f * g * lightDotEye), 1.5f));
	g = _CloudHenyeyGreensteinPhase.y;
	g2 = g * g;
	h += (_CloudHenyeyGreensteinPhase.w * ((1.0f - g2) / pow((1.0f + g2 - 2.0f * g * lightDotEye), 1.5f)));
	return h;
	*/
}

float3 CloudVolumetricLightEnergy(float lightDotEye, float densitySample, float eyeDensity, float densityToLight, float lightIntensity, float3 lightDir, float3 rayDir)
{
	// With E as light energy, d as the density sampled for lighting, p as the absorption multiplier for rain, g as our eccentricity in light direction, and θ as the angle between the view and light rays,
	// calculate lighting - E = 2.0 * e−dp * (1 − e−2d) * (1/4π) * (1 − g2 1 + g2 − 2g cos(θ)3/2).
	float beerLambert = CloudVolumetricBeerLambert(densityToLight);
	float powder = CloudVolumetricPowder(densitySample, lightDotEye, lightIntensity, lightDir, rayDir);
	float henyeyGreenstein = CloudVolumetricHenyeyGreensteinVolumetric(lightDotEye, lightIntensity, lightDir);
	return float3(beerLambert, powder, henyeyGreenstein);
}

inline float GetCloudHeightFractionForPoint(float3 worldPos)
{

#if VOLUMETRIC_CLOUD_USE_PLANE

	return ((worldPos.y - _CloudStartVolumetric) * _CloudHeightInverseVolumetric);

#else

	return _CloudHeightInverseVolumetric *
		(distance(worldPos, volumetricPlanetCenter) - _CloudPlanetRadiusVolumetric - _CloudStartVolumetric);

#endif

}

float SmoothStepGradient(float zeroToOne, float4 gradient)
{
	return smoothstep(gradient.x, gradient.y, zeroToOne) - smoothstep(gradient.z, gradient.w, zeroToOne);
}

float GetDensityHeightGradientForHeight(float heightFrac, float cloudType)
{
	// 0 = fully stratus, 0.5 = fully stratocumulus, 1 = fully cumulus
	float stratus = 1.0f - saturate(cloudType * 2.0f);
	float stratocumulus = 1.0f - abs(cloudType - 0.5f) * 2.0f;
	float cumulus = saturate(cloudType - 0.5f) * 2.0f;
	float4 cloudGradient = (_CloudGradientStratus * stratus) + (_CloudGradientStratoCumulus * stratocumulus) + (_CloudGradientCumulus * cumulus);
	return SmoothStepGradient(heightFrac, cloudGradient);
}

float LerpThreeFloat(float v0, float v1, float v2, float a)
{
	return lerp(lerp(v1, v2, (a - 0.5) * 2.0), lerp(v0, v1, a * 2.0), a < 0.5);
}

float4 LerpThreeFloat4(float4 v0, float4 v1, float4 v2, float a)
{
	return float4(LerpThreeFloat(v0.x, v1.x, v2.x, a),
		LerpThreeFloat(v0.y, v1.y, v2.y, a),
		LerpThreeFloat(v0.z, v1.z, v2.z, a),
		LerpThreeFloat(v0.w, v1.w, v2.w, a));
}

float SampleCloudDensity(float3 marchPos, float3 rayDir, float4 weatherData, float heightFrac, float lod, bool sampleDetails)
{
	static const float minNoiseValue = 0.001;

	float noise = 0.0;
	float heightGradientSingle;
	float cloudType = CloudVolumetricGetCloudType(weatherData);
	float coverage = CloudVolumetricGetCoverage(weatherData);

	// avoid sampling out of bounds of the cloud layer or no coverage
	UNITY_BRANCH
	if (volumetricPositionShapeScale > 0.0 && heightFrac >= 0.0 && heightFrac <= 1.0 &&
		(heightGradientSingle = GetDensityHeightGradientForHeight(heightFrac, cloudType)) > minNoiseValue)
	{
        float4 noisePos = float4((marchPos + volumetricAnimationShape) * volumetricPositionShapeScale, lod);

		// https://github.com/greje656/clouds
		// smoothly combine all three cloud layer gradients against the noise gba channels using just the height in the cloud layer
		// this produces nicer looking results with whispy clouds at lower height and a variety of puffy clouds higher up
		// this looks nicer than the gpu gems pro 7 remap and fbm style sampling in my opinion
		float4 noiseSample = tex3Dlod(_CloudNoiseShapeVolumetric, noisePos);

		// create height gradient for the 3 samples of worley noise
		float3 heightGradient = float3(SmoothStepGradient(heightFrac, _CloudGradientStratus),
			SmoothStepGradient(heightFrac, _CloudGradientStratoCumulus), SmoothStepGradient(heightFrac, _CloudGradientCumulus));

		// multiply worley noise samples by height gradients
		noiseSample.gba *= heightGradient; // dont modify perlin / worley in this step

		// combine all into final noise
		noise = (noiseSample.r + noiseSample.g + noiseSample.b + noiseSample.a) * volumetricCloudNoiseMultiplier * heightGradientSingle;
		noise = pow(noise, min(1.0, _CloudHeightNoisePowerVolumetric * heightFrac));

		// smoothstep noise to a range, helps reduce clutter / noise of the clouds
		noise = smoothstep(_CloudShapeNoiseMinVolumetric, _CloudShapeNoiseMaxVolumetric, noise);

		// remap function for noise against coverage, see gpu gems pro 7
		noise = saturate(noise - (1.0 - coverage)) * coverage;

		/*
		// gpu gems pro 7 way
		noise = tex3Dlod(_CloudNoiseShapeVolumetric, noisePos).a * heightGradientSingle;
		coverage = pow(coverage, Remap(heightFrac, 0.7, 0.8, 1.0, 1.0)); // anvil bias, real-time rendering slides
		noise = coverage * saturate(Remap(noise, 1.0 - coverage, 1.0, 0.0, 1.0));
		*/

		// apply details if needed
		UNITY_BRANCH
		if (volumetricPositionDetailScale > 0.0 && sampleDetails && noise > minNoiseValue)
	    {
			noisePos.xyz = (marchPos + volumetricAnimationDetail) * volumetricPositionDetailScale;

			UNITY_BRANCH
			if (volumetricPositionCurlScale > 0.0)
			{
				// modify detail pos using curl lookup
				float4 curlPos = float4((noisePos.xz) * volumetricPositionCurlScale, 0.0, 0.0);
				float3 curl = (tex2Dlod(_CloudNoiseCurlVolumetric, curlPos).rgb * 2.0) - 1.0; // curl tex is 0-1, map to -1,1
				curl *= volumetricPositionCurlIntensity * (1.0 - heightFrac);
				noisePos.xyz += curl;
			}

			// erode details away from noise value - single alpha value, gpu pro 7 way
			float detail = tex3Dlod(_CloudNoiseDetailVolumetric, noisePos).a;
			float detailModifier = lerp(detail, 1.0f - detail, saturate(heightFrac * VOLUMETRIC_DETAIL_HEIGHT_MULTIPLIER));
			noise = saturate(Remap(noise, detailModifier * VOLUMETRIC_DETAIL_MAP_STRENGTH, 1.0, 0.0, 1.0));

			/*
			// erode away cloud using detail texture and height gradient, not much different from gpu pro 7 way and more costly so not using
			float3 erosion = 1.0 - tex3Dlod(_CloudNoiseDetailVolumetric, noisePos);

			// erode differently at different heights and cloud types
			erosion *= heightGradient;

			// compute erosion noise value
			float erosionNoise = (erosion.r + erosion.g + erosion.b) * 0.3334; // average erosion values
			erosionNoise *= smoothstep(1.0, 0.0, noise) * 0.5; // erode less with thicker clouds
			noise = saturate(noise - erosionNoise);
			*/
		}

		// cloud density generally reduces at lower heights, smoothstep is better than lerp here
		noise *= smoothstep(0.0, _CloudBottomFadeVolumetric, heightFrac);
	}

	return min(1.0, noise * _CloudNoiseScalarVolumetric);
}

fixed3 SampleDirLightSources(float3 marchPos, float3 rayDir, float startHeightFrac, float cloudSample, float eyeDensity, float lod)
{
	fixed3 lightTotal = fixed3Zero;
	startHeightFrac = max(0.3, startHeightFrac);
	lod++;

	UNITY_LOOP
	for (uint lightIndex = 0; lightIndex < uint(_WeatherMakerDirLightCount) && _WeatherMakerDirLightColor[lightIndex].a > 0.0; lightIndex++)
	{
		float3 lightDir = _WeatherMakerDirLightPosition[lightIndex].xyz;

		UNITY_BRANCH
		if (lightDir.y < -0.05)
		{
			continue;
		}

		fixed4 lightColor = _WeatherMakerDirLightColor[lightIndex];

		UNITY_BRANCH
		if (lightColor.a < 0.01)
		{
			continue;
		}

		float3 lightStep = (lightDir.xyz * volumetricDirLightStepSize);

		//causes flicker, figure out why...
		//float randomDither = 1.0 + (_CloudRayDitherVolumetric * RandomFloat(lightStep));
		//lightStep *= randomDither; // dither march dir slightly to avoid banding

		float heightFrac;
		float4 weatherData;
		float coneRadius = lightStep;
		float3 samplePos;
		float3 energy;
		float densityToLight = 0.0;
		float3 pos = marchPos + (1 * lightStep);

		UNITY_LOOP
		for (uint i = 0.0; i < volumetricLightIterations; i++)
		{
			// sample in the cone, take the march pos and perturb by random vector and cone radius
			samplePos = pos + (_CloudConeRandomVectors[i] * coneRadius);

			// lookup position for cloud density
			weatherData = CloudVolumetricSampleWeather(samplePos, heightFrac);

			UNITY_BRANCH
			if (CloudVolumetricGetCoverage(weatherData) > MINIMUM_COVERAGE_FOR_CLOUD)
			{
				heightFrac = GetCloudHeightFractionForPoint(samplePos);

				UNITY_BRANCH
				if (WM_CAMERA_RENDER_MODE_NORMAL)
				{
					densityToLight += SampleCloudDensity(samplePos, rayDir, weatherData, heightFrac, lod, true);
				}
				else
				{
					// fast approximation, this is just a reflection, who cares...
					densityToLight += (weatherData.r * weatherData.r * GetDensityHeightGradientForHeight(heightFrac, weatherData.b));
				}
			}

			// march to next positions
			coneRadius += volumetricDirLightConeRadius;
			pos += lightStep;
		}
        
        UNITY_BRANCH
        if (volumetricLightSampleDistant)
        {
            // one final sample farther away for distant cloud
            pos += (lightStep * 9.0);
            weatherData = CloudVolumetricSampleWeather(pos, heightFrac);

    		UNITY_BRANCH
    		if (CloudVolumetricGetCoverage(weatherData) > MINIMUM_COVERAGE_FOR_CLOUD)
    		{
    			heightFrac = GetCloudHeightFractionForPoint(pos);

    			UNITY_BRANCH
    			if (WM_CAMERA_RENDER_MODE_NORMAL)
    			{
    				densityToLight += SampleCloudDensity(pos, rayDir, weatherData, heightFrac, lod, true);
    			}
    			else
    			{
    				// fast approximation, this is just a reflection, who cares...
    				densityToLight += (weatherData.r * weatherData.r * GetDensityHeightGradientForHeight(heightFrac, weatherData.b));
    			}
    		}
        }
        
		energy = CloudVolumetricLightEnergy(lightDotEyeDir[lightIndex], cloudSample, eyeDensity, densityToLight * _CloudLightAbsorptionVolumetric, lightColor.a, lightDir, rayDir);

		// indirect
		lightTotal += (lightColor.rgb * startHeightFrac * lightColor.a * lightColor.a * _CloudDirLightIndirectMultiplierVolumetric) +

		// direct
		(lightColor.rgb * lightColor.a * energy.x * energy.y * energy.z * _CloudDirLightMultiplierVolumetric);

		// if this dir light is bright enough, skip additional dir lights
		UNITY_BRANCH
		if (_WeatherMakerDirLightColor[lightIndex].a > 0.1)
		{
			break;
		}
	}
	return lightTotal * _CloudDirColorVolumetric;
}

#if VOLUMETRIC_CLOUD_ENABLE_POINT_LIGHTS

fixed3 SamplePointLightSources(float3 marchPos, float3 rayDir, float startHeightFrac, float cloudSample, float eyeDensity, float lod, float4 uv)
{
	fixed3 lightTotal = fixed3Zero;

	UNITY_BRANCH
	if (_WeatherMakerPointLightCount > 0)
	{
		//lod++;

		UNITY_LOOP
		for (uint lightIndex = 0; lightIndex < uint(_WeatherMakerPointLightCount); lightIndex++)
		{
			float3 toLight = _WeatherMakerPointLightPosition[lightIndex].xyz - marchPos;
			float lengthSq = max(0.000001, dot(toLight, toLight));
			fixed atten = (1.0 / (1.0 + (lengthSq * _WeatherMakerPointLightAtten[lightIndex].z)));
			lightTotal += (saturate(atten) * max(0.5, cloudSample) * _WeatherMakerPointLightColor[lightIndex].a * _WeatherMakerPointLightColor[lightIndex].rgb);

			/*
			UNITY_BRANCH
			if (atten > 0.0)
			{
				atten = saturate(atten * lightDither);
				float3 toLightNorm = normalize(toLight);
				float lightStepAmount = length(toLight) * invVolumetricLightIterationsNonDir;
				float3 lightStep = toLightNorm * lightStepAmount;
				float heightFrac;
				float4 weatherData;
				float coneRadius = lightStep;
				float3 samplePos;
				float3 energy;
				float densityToLight = 0.0;
				float3 pos = marchPos + lightStep;

				UNITY_LOOP
				for (uint lightStepIndex = 0.0; lightStepIndex < volumetricLightIterationsNonDir; lightStepIndex++)
				{
					heightFrac = GetCloudHeightFractionForPoint(pos);

					// sample in the cone, take the march pos and perturb by random vector and cone radius
					samplePos = pos + (_CloudConeRandomVectors[lightStepIndex] * coneRadius);

					// lookup position for cloud density
					weatherData = CloudVolumetricSampleWeather(samplePos, heightFrac);
					densityToLight += SampleCloudDensity(samplePos, rayDir, weatherData, heightFrac, lod, (densityToLight < 0.3));

					// march to next positions
					coneRadius += lightStep;
					pos += lightStep;
				}

				fixed4 lightColor = _WeatherMakerPointLightColor[lightIndex];
				fixed3 lightRgb = lightColor.rgb * atten * lightColor.a;
				energy = CloudVolumetricBeerLambert(densityToLight) * _CloudPointSpotLightMultiplierVolumetric;
				lightTotal += (lightRgb * energy);
			}
			*/
		}

		//ApplyDither(lightTotal.rgb, uv.xy, 0.02);
		lightTotal.rgb = max(0.0, lightTotal.rgb + (_WeatherMakerCloudDitherLevel * RandomFloat(rayDir + _WeatherMakerTime.x)));
	}

	return lightTotal;
}

#endif

// https://en.wikipedia.org/wiki/Exponential_integral
float ExponentialIntegral(float v)
{
	return 0.5772156649015328606065 + log(0.0001 + abs(v)) + v * (1.0 + v * (0.25 + v * ((1.0 / 18.0) + v * ((1.0 / 96.0) + v * (1.0 / 600.0)))));
}

fixed3 SampleAmbientLight(float heightFrac, fixed3 skyColor, float4 weatherData)
{

#if defined(VOLUMETRIC_CLOUD_AMBIENT_MODE_LINEAR)

	// reduce sky light at lower heights
	// reduce ground light at higher heights
	fixed groundHeightFrac = 1.0 - min(1.0, heightFrac * _CloudAmbientGroundHeightMultiplierVolumetric);
	skyColor *= min(1.0, heightFrac * _CloudAmbientSkyHeightMultiplierVolumetric);
	fixed3 groundColor = volumetricCloudAmbientColorGround * groundHeightFrac;
	return _CloudEmissionColorVolumetric + skyColor + groundColor;

#else

	// // page 12-15 https://patapom.com/topics/Revision2013/Revision%202013%20-%20Real-time%20Volumetric%20Rendering%20Course%20Notes.pdf
	/*
	float Hp = VolumeTop - _Position.y; // Height to the top of the volume
	float a = -_ExtinctionCoeff * Hp;
	float3 IsotropicScatteringTop = IsotropicLightTop * max( 0.0, exp( a ) - a * Ei( a ));
	float Hb = _Position.y - VolumeBottom; // Height to the bottom of the volume
	a = -_ExtinctionCoeff * Hb;
	float3 IsotropicScatteringBottom = IsotropicLightBottom * max( 0.0, exp( a ) - a * Ei( a ));
	return IsotropicScatteringTop + IsotropicScatteringBottom;
    */

    //float Hp = -intensity * saturate(1.0 - heightFrac);
	static const float ambientSkyPower = 1.0 - _CloudAmbientSkyHeightMultiplierVolumetric;
	float Hp = pow(heightFrac, ambientSkyPower) - 1.0;
	float3 scatterTop = skyColor * max(0.0, exp(Hp) - Hp * ExponentialIntegral(Hp));
	//float Hb = -intensity * heightFrac;
	static const float ambientGroudMultiplier = 1.0 - _CloudAmbientGroundHeightMultiplierVolumetric;
	float Hb = -(heightFrac * ambientGroudMultiplier);
	float3 scatterBottom = volumetricCloudAmbientColorGround * max(0.0, exp(Hb) - Hb * ExponentialIntegral(Hb));
	return _CloudEmissionColorVolumetric + (scatterTop + scatterBottom);

#endif

}

inline fixed4 FinalizeVolumetricCloudColor(fixed4 color, float4 uv, uint marches)
{

#if defined(WEATHER_MAKER_ENABLE_VOLUMETRIC_CLOUDS) && VOLUMETRIC_CLOUD_RENDER_MODE == 2

	color.rgb = (float)marches / float(_CloudNoiseSampleCountVolumetric.y);
	color.a = 1.0;

#else

	if (color.a > 0.0)
	{
		
#if defined(UNITY_COLORSPACE_GAMMA)

		color.rgb *= 1.4;

#else

		color = pow(color, 2.2);

#endif

		if (_WeatherMakerEnableToneMapping)
		{
			color.rgb = FilmicTonemapFull(color.rgb, 2.0);
		}
	}

#endif

	color.a = max(color.a, 0.004);
	return color;
}

fixed4 ComputeCloudColorVolumetricHorizonFade(fixed4 color, fixed3 backgroundSkyColor, float opticalDepth, float3 cloudRay)
{
    UNITY_BRANCH
    if (_CloudHorizonFadeMultiplierVolumetric > 0.001)
    {
        // horizon fade
        // calculate horizon fade
        fixed fade = pow(opticalDepth, VOLUMETRIC_HORIZON_FADE_OPTICAL_DEPTH_POWER);
        fade = smoothstep(0.0, 1.0, fade - VOLUMETRIC_HORIZON_FADE_OPTICAL_DEPTH_SUBTRACTOR);

        // increase horizon fade dither to reduce banding as main dir light decreases in intensity
        fade *= (1.0 + (RandomFloat(cloudRay * _WeatherMakerTime.y) * VOLUMETRIC_HORIZON_FADE_DITHER)) * _CloudHorizonFadeMultiplierVolumetric;
		fade = clamp((1.0 - fade), VOLUMETRIC_MAX_HORIZON_FADE, 1.0);
		color *= fade;
        //color.rgb = lerp(backgroundSkyColor, color.rgb, fade);
    }
    return color;
}

fixed4 RaymarchVolumetricClouds(float3 marchPos, float rayLength, float3 rayDir, float3 origRayDir, float4 uv, float depth, fixed3 skyColor, fixed3 backgroundSkyColor,
	inout uint marches, inout float opticalDepth)
{
	fixed4 cloudColor = fixed4Zero;

	UNITY_BRANCH
	if (rayDir.y < _CloudMinRayYVolumetric || rayLength < 0.01)
	{
		return cloudColor;
	}
	else
	{
		uint i = 0;

		// reduce sample count when looking more straight up or down through the clouds, the ray length will be much smaller
		//uint sampleCount = uint(lerp(volumetricSampleCountRange.y, volumetricSampleCountRange.x, abs(rayDir.y)));
		uint sampleCount = uint(lerp(volumetricSampleCountRange.x, volumetricSampleCountRange.y, min(1.0, (0.5 * rayLength * _CloudHeightInverseVolumetric))));
		opticalDepth = min(1.0, distance(_CloudCameraPosition, marchPos) * invVolumetricMaxOpticalDistance);

		// reduce sample count for clouds that are farther away
		sampleCount /= max(1.0, opticalDepth * VOLUMETRIC_SAMPLE_COUNT_OPTICAL_DEPTH_REDUCER);

		float invSampleCount = 1.0 / float(sampleCount);
		float marchLength = max(VOLUMETRIC_MIN_STEP_LENGTH, min(volumetricCloudMaxRayLength, rayLength) * invSampleCount);
		float3 marchDir = rayDir * marchLength;
		//float randomDither = (rayDir * _CloudRayDitherVolumetric * RandomFloat(marchPos));
		//marchDir += randomDither; // dither march dir slightly to avoid banding
		float randomDither = 1.0 + (_CloudRayDitherVolumetric * RandomFloat(rayDir));
		marchDir *= randomDither; // dither march dir slightly to avoid banding

		float heightFrac = 0.0;
		float cloudSample = 0.0;
		float cloudSampleTotal = 0.0;
		float4 lightSample;
		float4 weatherData;

		// increase lod for clouds that are farther away
		float startLod = volumetricLod.x + max(0.0, (opticalDepth * VOLUMETRIC_LOD_OPTICAL_DEPTH_MULTIPLIER) - 1.0);

		float lod = startLod;
		float lodStep = volumetricLod.y * invSampleCount;
		float iterationMultiplier = 1.0;
		float zeroSampleCounter = 0.0;

		UNITY_LOOP
		while ((i += uint(iterationMultiplier)) < sampleCount && cloudColor.a < 0.999 && heightFrac > -0.001 && heightFrac < 1.001)
		{
			heightFrac = GetCloudHeightFractionForPoint(marchPos);
			weatherData = CloudVolumetricSampleWeather(marchPos, heightFrac);

			UNITY_BRANCH
			if (CloudVolumetricGetCoverage(weatherData) > MINIMUM_COVERAGE_FOR_CLOUD)
			{
				cloudSample = SampleCloudDensity(marchPos, rayDir, weatherData, heightFrac, lod, true);
				marches++;

				UNITY_BRANCH
				if (cloudSample > 0.0)
				{

#if VOLUMETRIC_CLOUD_SKIP_SAMPLE_THRESHOLD > 0

					zeroSampleCounter = 0.0;
                
					UNITY_BRANCH
					if (iterationMultiplier > 1.5)
					{
						// move back to last good pos and resample
						marchPos -= (marchDir * iterationMultiplier);
						i -= uint(iterationMultiplier);
						heightFrac = GetCloudHeightFractionForPoint(marchPos);
						weatherData = CloudVolumetricSampleWeather(marchPos, heightFrac);
						lod = startLod + (i * lodStep) - lodStep;
                    
						UNITY_BRANCH
						if (CloudVolumetricGetCoverage(weatherData) <= MINIMUM_COVERAGE_FOR_CLOUD)
						{
							// no coverage
							cloudSample = 0.0;
						}
						else
						{
							// re-sample noise
							cloudSample = SampleCloudDensity(marchPos, rayDir, weatherData, heightFrac, lod, true);
							marches++;
						}
					}

					UNITY_BRANCH
					if (cloudSample > 0.0)

#endif

					{
						cloudSampleTotal += cloudSample;
						lightSample.rgb = SampleAmbientLight(heightFrac, skyColor, weatherData);
						lightSample.rgb += SampleDirLightSources(marchPos, rayDir, heightFrac, cloudSample, cloudSampleTotal, lod);

#if VOLUMETRIC_CLOUD_ENABLE_POINT_LIGHTS

						lightSample.rgb += SamplePointLightSources(marchPos, rayDir, heightFrac, cloudSample, cloudSampleTotal, lod, uv);

#endif

						lightSample.a = cloudSample;
						lightSample.rgb *= cloudSample;

						// accumulate color
						cloudColor = ((1.0 - cloudColor.a) * lightSample) + cloudColor;
					}
				}
			}

#if VOLUMETRIC_CLOUD_SKIP_SAMPLE_THRESHOLD > 0

			zeroSampleCounter += float(cloudSample <= 0.0);
			iterationMultiplier = 1.0 + clamp(zeroSampleCounter - VOLUMETRIC_CLOUD_SKIP_SAMPLE_THRESHOLD, 0.0, VOLUMETRIC_CLOUD_SKIP_SAMPLE_MAX);
			marchPos += (marchDir * iterationMultiplier);

#else

			marchPos += marchDir;

#endif

			lod += lodStep;
		}

		// add last tiny bit of alpha if we are really close to 1
		cloudColor.a = min(cloudColor.a + ((cloudColor.a >= 0.999) * 0.001), 1.0);

		return ComputeCloudColorVolumetricHorizonFade(cloudColor, backgroundSkyColor, opticalDepth, origRayDir); 
	}
}

fixed4 ComputeCloudColorVolumetric(float3 rayDir, float4 uv, float depth)
{
	fixed4 cloudLightColors[2] = { fixed4Zero, fixed4Zero };
	float3 skyRay = float3(rayDir.x, abs(rayDir.y), rayDir.z);
	float3 cloudRayDir = normalize(float3(rayDir.x, rayDir.y + _CloudRayOffsetVolumetric, rayDir.z));
	float3 marchPos, marchPos2;
	float rayLength, rayLength2;
	uint iterationIndex;
	uint lightIndex;
	uint iterations = SetupCloudRaymarch(_CloudCameraPosition, cloudRayDir, depth, marchPos, rayLength, marchPos2, rayLength2);
	uint marches = 0;
	float opticalDepth = 1.0;
	fixed3 backgroundSkyColor = CalculateSkyColorUnityStyleFragment(skyRay).rgb;
	fixed3 skyColor;

	UNITY_BRANCH
	if (_CloudSkyIntensityVolumetric > 0.001)
	{
		skyColor = backgroundSkyColor * _CloudSkyIntensityVolumetric;
		skyColor += volumetricCloudAmbientColorSky;
	}
	else
	{
		skyColor = volumetricCloudAmbientColorSky;
	}

	UNITY_BRANCH
	if (iterations > 0)
	{
		UNITY_UNROLL
		for (lightIndex = 0; lightIndex < uint(_WeatherMakerDirLightCount); lightIndex++)
		{
			lightDotEyeDir[lightIndex] = max(0.0, dot(rayDir, _WeatherMakerDirLightPosition[lightIndex].xyz));
		}

		UNITY_LOOP
		for (iterationIndex = 0; iterationIndex < iterations; iterationIndex++)
		{ 
			cloudLightColors[iterationIndex] = RaymarchVolumetricClouds(lerp(marchPos, marchPos2, iterationIndex), lerp(rayLength, rayLength2, iterationIndex), cloudRayDir, rayDir,
				uv, depth, skyColor, backgroundSkyColor, marches, opticalDepth);
		}

		// custom blend
		cloudLightColors[1].rgb = cloudLightColors[0].rgb + (cloudLightColors[1].rgb * (1.0 - cloudLightColors[0].a));
		cloudLightColors[1].a = max(cloudLightColors[0].a, cloudLightColors[1].a);
		return FinalizeVolumetricCloudColor(cloudLightColors[1] * _CloudColorVolumetric, uv, marches);
	}
	else
	{
		// missed cloud layer entirely
		return fixed4Zero;
	}
}

#endif // WEATHER_MAKER_ENABLE_VOLUMETRIC_CLOUDS

float ComputeCloudShadowStrength(float3 worldPos, uint dirIndex)
{
	UNITY_BRANCH
	if (weatherMakerGlobalShadow <= 0.0)
	{
		return weatherMakerGlobalShadow;
	}
	else
	{
		float shadowValue = weatherMakerGlobalShadow;

#if defined(WEATHER_MAKER_ENABLE_VOLUMETRIC_CLOUDS)

		UNITY_BRANCH
		if (_WeatherMakerShadowsEnabled && shadowValue > 0.0 && _CloudCoverVolumetric > 0.01 && dirIndex < uint(_WeatherMakerDirLightCount) && _WeatherMakerDirLightColor[dirIndex].a > 0.0)
		{
			// TODO: Does this work in cubemap?
			//return fmod(round(abs(pos.x)), 2.0) * fmod(round(abs(pos.z)), 2.0);
			float3 startPos;
			float3 startPos2;
			float rayLength;
			float rayLength2;
			float3 rayDir = _WeatherMakerDirLightPosition[dirIndex].xyz;
			float3 cloudRayDir = normalize(float3(rayDir.x, rayDir.y + _CloudRayOffsetVolumetric, rayDir.z));
			SetupCloudRaymarch(worldPos, cloudRayDir, 10000000.0, startPos, rayLength, startPos2, rayLength2);

			UNITY_BRANCH
			if (rayLength > 0.01)
			{
				float maxShadow = max(_CloudShadowMapMinimum, _WeatherMakerDirLightPower[dirIndex].w);
				float3 tenPercent = rayDir * rayLength * 0.1;
				float3 fiftyPercent = rayDir * rayLength * 0.5;
				float3 ninetyPercent = rayDir * rayLength * 0.9;

				// sample at 10, 50 and 90 percent ray length and take the max coverage for shadows
				float3 marchPos = startPos + tenPercent;
				float heightFrac = GetCloudHeightFractionForPoint(marchPos);
				float4 weatherData = CloudVolumetricSampleWeather(marchPos, heightFrac);
				float cloudType = CloudVolumetricGetCloudType(weatherData);
				float cloudCoverage = CloudVolumetricGetCoverage(weatherData) * GetDensityHeightGradientForHeight(heightFrac, cloudType);

				marchPos = startPos + fiftyPercent;
				heightFrac = GetCloudHeightFractionForPoint(marchPos);
				weatherData = CloudVolumetricSampleWeather(marchPos, heightFrac);
				cloudType = CloudVolumetricGetCloudType(weatherData);
				cloudCoverage += (CloudVolumetricGetCoverage(weatherData) * GetDensityHeightGradientForHeight(heightFrac, cloudType), cloudCoverage);

				marchPos = startPos + ninetyPercent;
				heightFrac = GetCloudHeightFractionForPoint(marchPos);
				weatherData = CloudVolumetricSampleWeather(marchPos, heightFrac);
				cloudType = CloudVolumetricGetCloudType(weatherData);
				cloudCoverage += (CloudVolumetricGetCoverage(weatherData) * GetDensityHeightGradientForHeight(heightFrac, cloudType), cloudCoverage);

				cloudCoverage *= 0.33 * _CloudDensityVolumetric * max(0.5, cloudType);

#if defined(VOLUMETRIC_CLOUD_SHADOW_DITHER)

				cloudCoverage *= (1.0 + (VOLUMETRIC_CLOUD_SHADOW_DITHER * RandomFloat(marchPos)));

#endif

				cloudCoverage += _CloudShadowMapAdder;
				cloudCoverage *= _CloudShadowMapMultiplier;
				cloudCoverage = pow(cloudCoverage, _CloudShadowMapPower);
				cloudCoverage = min(_WeatherMakerCloudVolumetricShadow, (1.0 - saturate(cloudCoverage * _WeatherMakerDirLightPower[dirIndex].z)));
				shadowValue = clamp(cloudCoverage, maxShadow, _CloudShadowMapMaximum);
			}

			shadowValue = min(weatherMakerGlobalShadow, shadowValue);
		}

#endif

		return shadowValue;
	}
}

#endif // __WEATHER_MAKER_CLOUD_SHADER__
