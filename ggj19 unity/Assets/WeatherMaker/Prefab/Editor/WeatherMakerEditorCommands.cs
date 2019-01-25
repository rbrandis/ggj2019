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
using System.IO;

using UnityEngine;
using UnityEditor;
using System.Linq;

namespace DigitalRuby.WeatherMaker
{
    public class WeatherMakerGenerateNormalTexture2DWindow : EditorWindow
    {
        private string inputFile;
        private string outputFile;

        private Texture2D inputFileTexture;
        private Texture2D outputFileTexture;
        private Vector2 scrollPosition;
        private Material material;
        private bool texturesDirty = true;

        private void LoadTextures()
        {
            if (!texturesDirty || string.IsNullOrEmpty(inputFile) || string.IsNullOrEmpty(outputFile))
            {
                return;
            }
            texturesDirty = false;

            try
            {
                if (inputFileTexture == null)
                {
                    inputFileTexture = new Texture2D(1, 1);
                }
                inputFileTexture.LoadImage(File.ReadAllBytes(inputFile));
            }
            catch (Exception ex)
            {
                Debug.LogErrorFormat("Unable to load input texture: {0}", ex.Message);
            }
            try
            {
                if (outputFileTexture == null)
                {
                    outputFileTexture = new Texture2D(1, 1);
                }
                outputFileTexture.LoadImage(File.ReadAllBytes(outputFile));
            }
            catch (Exception ex)
            {
                Debug.LogErrorFormat("Unable to load output texture: {0}", ex.Message);
            }
        }

        private void OnEnable()
        {
            OnFocus();
        }

        private void OnDisable()
        {
            OnLostFocus();
        }

        private void OnFocus()
        {
            inputFile = EditorPrefs.GetString("WeatherMaker_GenerateNormalTexture2DWindow_inputFile");
            outputFile = EditorPrefs.GetString("WeatherMaker_GenerateNormalTexture2DWindow_outputFile");
            LoadTextures();
        }

        private void OnLostFocus()
        {
            EditorPrefs.SetString("WeatherMaker_GenerateNormalTexture2DWindow_inputFile", inputFile);
            EditorPrefs.SetString("WeatherMaker_GenerateNormalTexture2DWindow_outputFile", outputFile);
            texturesDirty = true;
        }

        private void OnDestroy()
        {
            OnLostFocus();
        }

        private void OnGUI()
        {
            const float textHeight = 20.0f;
            const float textButtonWidth = 100.0f;
            const float submitButtonWidth = 150.0f;
            EditorGUIUtility.labelWidth = 80.0f;
            EditorGUI.BeginChangeCheck();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("Input Image", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            inputFile = EditorGUILayout.TextField(new GUIContent(string.Empty, inputFile), inputFile, GUILayout.ExpandWidth(true), GUILayout.Height(textHeight));
            if (GUILayout.Button("Input...", GUILayout.MaxWidth(textButtonWidth)))
            {
                string newFile = EditorUtility.OpenFilePanelWithFilters("Select input image", string.Empty, WeatherMakerEditorCommands.ImageFilesFilter);
                if (!string.IsNullOrEmpty(newFile))
                {
                    inputFile = newFile;
                    LoadTextures();
                }
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Label("Output Image", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            outputFile = EditorGUILayout.TextField(new GUIContent(string.Empty, outputFile), outputFile, GUILayout.ExpandWidth(true), GUILayout.Height(textHeight));
            if (GUILayout.Button("Output...", GUILayout.MaxWidth(textButtonWidth)))
            {
                string newFile = EditorUtility.SaveFilePanel("Select output image", string.Empty, "Normals.png", "png");
                if (!string.IsNullOrEmpty(newFile))
                {
                    outputFile = newFile;
                    LoadTextures();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(inputFileTexture, GUILayout.MaxWidth(256.0f), GUILayout.MaxHeight(256.0f));
            GUILayout.Label(outputFileTexture, GUILayout.MaxWidth(256.0f), GUILayout.MaxHeight(256.0f));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            if (GUILayout.Button("Generate Normals", GUILayout.MaxWidth(submitButtonWidth)))
            {
                // do normals
                if (material == null)
                {
                    Shader shader = Shader.Find("WeatherMaker/WeatherMakerNormalGenerator2DShader");
                    material = new Material(shader);
                }
                Texture2D input = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                input.wrapMode = TextureWrapMode.Repeat;
                input.filterMode = FilterMode.Bilinear;
                input.LoadImage(File.ReadAllBytes(inputFile));
                material.SetTexture(WMS._MainTex, input);
                Texture2D output = new Texture2D(input.width, input.height, TextureFormat.ARGB32, false);
                output.wrapMode = TextureWrapMode.Repeat;
                RenderTexture outputRT = new RenderTexture(input.width, input.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                RenderTexture active = RenderTexture.active;
                RenderTexture.active = outputRT;
                Graphics.Blit(input, material, 0);
                output.ReadPixels(new Rect(0, 0, input.width, input.height), 0, 0);
                output.Apply();
                RenderTexture.active = active;
                GameObject.DestroyImmediate(material);
                if (Path.GetExtension(outputFile).Equals(".png", StringComparison.OrdinalIgnoreCase))
                {
                    File.WriteAllBytes(outputFile, output.EncodeToPNG());
                }
                else
                {
                    File.WriteAllBytes(outputFile, output.EncodeToJPG(92));
                }
                UnityEngine.Object.DestroyImmediate(input);
                UnityEngine.Object.DestroyImmediate(output);
            }

            if (EditorGUI.EndChangeCheck())
            {
                texturesDirty = true;
                OnFocus();
            }

            EditorGUILayout.EndScrollView();
        }
    }

    public static class WeatherMakerEditorCommands
    {
        public static readonly string[] ImageFilesFilter = new string[] { "Image Files", "png,jpg,jpeg", "All Files", "*" };

        private static void Create3DNoiseTextureByPickingAsset()
        {
            string inputFile = EditorUtility.OpenFilePanel("Select asset to convert to 3D texture", string.Empty, string.Empty);
            string rootPath = Application.dataPath;
            if (rootPath.EndsWith("/Assets", StringComparison.OrdinalIgnoreCase))
            {
                rootPath = rootPath.Substring(0, rootPath.Length - 6);
            }
            inputFile = inputFile.Substring(rootPath.Length);
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(inputFile);
            if (asset == null)
            {
                EditorUtility.DisplayDialog("Unable to create 3D texture", "Unable to load asset", "OK");
                return;
            }

            int size = 32;
            TextureFormat format = TextureFormat.RGB24;
            byte[] bytes = asset.bytes;
            int count = size * size * size;
            Color32[] colors = new Color32[count];
            int j = 0;

            for (int i = 0; i < count; i++)
            {
                colors[i].r = bytes[j++];
                colors[i].g = bytes[j++];
                colors[i].b = bytes[j++];
                colors[i].a = format == TextureFormat.RGBA32 ? bytes[j++] : (byte)255;
            }

            Texture3D t = new Texture3D(size, size, size, format, true);
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Bilinear;
            t.SetPixels32(colors, 0);
            t.Apply(true);

            /*
            byte[] bytes = data.bytes;
            uint height = BitConverter.ToUInt32(data.bytes, 12);
            uint width = BitConverter.ToUInt32(data.bytes, 16);
            uint pitch = BitConverter.ToUInt32(data.bytes, 20);
            uint depth = BitConverter.ToUInt32(data.bytes, 24);
            uint formatFlags = BitConverter.ToUInt32(data.bytes, 20 * 4);
            BitConverter.ToUInt32(data.bytes, 21 * 4); // uint fourCC = 
            uint bitdepth = BitConverter.ToUInt32(data.bytes, 22 * 4);
            if (bitdepth == 0)
            {
                bitdepth = pitch / width * 8;
            }

            Color32[] c = new Color32[width * height * depth];

            uint index = 128;
            if (data.bytes[21 * 4] == 'D' && data.bytes[21 * 4 + 1] == 'X' && data.bytes[21 * 4 + 2] == '1' &&
                data.bytes[21 * 4 + 3] == '0' && (formatFlags & 0x4) != 0)
            {
                uint format = BitConverter.ToUInt32(data.bytes, (int)index);
                if (format >= 60 && format <= 65)
                {
                    bitdepth = 8;
                }
                else if (format >= 48 && format <= 52)
                {
                    bitdepth = 16;
                }
                else if (format >= 27 && format <= 32)
                {
                    bitdepth = 32;
                }
                index += 20;
            }

            Texture3D t = new Texture3D((int)width, (int)height, (int)depth, (bitdepth == 8 ? TextureFormat.Alpha8 : TextureFormat.ARGB32), false);
            t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Repeat;
            t.name = "Noise 3D (Weather Maker)";

            uint byteDepth = bitdepth / 8;
            pitch = (width * bitdepth + 7) / 8;

            for (int d = 0; d < depth; ++d)
            {
                for (int h = 0; h < height; ++h)
                {
                    for (int w = 0; w < width; ++w)
                    {
                        byte v = bytes[index + w * byteDepth];
                        c[w + h * width + d * width * height] = new Color32(v, v, v, v);
                    }

                    index += pitch;
                }
            }

            t.SetPixels32(c);
            t.Apply();
            */

            UnityEditor.AssetDatabase.CreateAsset(t, "Assets/My3DTexture.asset");
            EditorUtility.DisplayDialog("3D texture saved", "New texture asset created as 'Assets/My3DTexture.asset", "OK");
        }

        [MenuItem("Window/Weather Maker/Add Weather Maker to Scene", false, priority = 30)]
        public static void AddWeatherMakerPrefab()
        {
            GameObject prefab;
            if (Camera.main == null || !Camera.main.orthographic)
            {
                string[] results3D = AssetDatabase.FindAssets("WeatherMakerPrefab");
                if (results3D.Length == 0)
                {
                    Debug.LogError("Unable to find WeatherMakerPrefab.prefab in project");
                    return;
                }
                string path = AssetDatabase.GUIDToAssetPath(results3D[0]);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            else
            {
                string[] results2D = AssetDatabase.FindAssets("WeatherMakerPrefab2D");
                if (results2D.Length == 0)
                {
                    Debug.LogError("Unable to find WeatherMakerPrefab2D.prefab in project");
                    return;
                }
                string path = AssetDatabase.GUIDToAssetPath(results2D[0]);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            if (prefab == null)
            {
                Debug.LogError("Unable to deserialize prefab");
                return;
            }
            var obj = GameObject.Instantiate(prefab);
            obj.name = obj.name.Replace("(Clone)", string.Empty);
            obj.transform.parent = null;
            Undo.RegisterCreatedObjectUndo(obj, "Add Weather Maker");
        }

        [MenuItem("Window/Weather Maker/Generate Normal Texture (2D)", false, priority = 51)]
        public static void GenerateNormalTexture2D()
        {
            EditorWindow window = EditorWindow.GetWindow(typeof(WeatherMakerGenerateNormalTexture2DWindow));
            window.titleContent = new GUIContent("Normals");
        }

        [MenuItem("Window/Weather Maker/Generate 3D Texture", false, priority = 52)]
        public static void Generate3DTexture()
        {
            const bool generateMip = false;

            string inputFolder = EditorUtility.OpenFolderPanel("Select input assets folder of images, i.e. myproject/assets/subfolder/images/", string.Empty, string.Empty);
            if (string.IsNullOrEmpty(inputFolder))
            {
                return;
            }

            Texture3D tex3D = null;
            string[] allFiles = Directory.GetFiles(inputFolder);
            List<string> files1 = new List<string>();
            List<string> files2 = new List<string>();
            List<string> files3 = new List<string>();
            List<string> files4 = new List<string>();
            List<string>[] filesArray = new List<string>[] { files1, files2, files3, files4 };
            foreach (string file in allFiles.OrderBy(f => f))
            {
                if (Path.GetExtension(file).Equals(".jpg", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(file).Equals(".png", StringComparison.OrdinalIgnoreCase))
                {
                    files1.Add(file);
                }
            }
            if (files1.Count == 0)
            {
                // try rgba channel approach, 4 folders, one for each channel
                string[] subFolders = Directory.GetDirectories(inputFolder);
                if (subFolders.Length == 4)
                {
                    for (int i = 0; i < subFolders.Length; i++)
                    {
                        allFiles = Directory.GetFiles(subFolders[i]);
                        foreach (string file in allFiles.OrderBy(f => f))
                        {
                            if (Path.GetExtension(file).Equals(".jpg", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(file).Equals(".png", StringComparison.OrdinalIgnoreCase))
                            {
                                filesArray[i].Add(file);
                            }
                        }
                    }
                }
            }
            if (files1.Count == 0 || (files2.Count != 0 && (files1.Count != files2.Count || files2.Count != files3.Count || files3.Count != files4.Count)))
            {
                EditorUtility.DisplayDialog("Unable to create 3D texture", "No files found or mismatching file counts in sub folders", "OK");
            }
            else
            {
                Color32[] allPixels;
                TextureFormat format = TextureFormat.ARGB32;// (files2.Count == 0 ? TextureFormat.Alpha8 : TextureFormat.ARGB32);
                Texture2D tex2D = new Texture2D(1, 1, format, false, false);
                int idx = 0;

                try
                {
                    tex2D.LoadImage(File.ReadAllBytes(files1[0]));
                    tex3D = new Texture3D(tex2D.width, tex2D.height, files1.Count, format, generateMip);
                    tex3D.filterMode = FilterMode.Bilinear;
                    tex3D.wrapMode = TextureWrapMode.Repeat;
                    tex3D.name = "Texture3D (Weather Maker)";
                    allPixels = new Color32[tex2D.width * tex2D.height * files1.Count];
                    if (files2.Count == 0)
                    {
                        foreach (string file in files1)
                        {
                            tex2D.LoadImage(File.ReadAllBytes(file));
                            Color32[] pixels = tex2D.GetPixels32();
                            for (int i = 0; i < pixels.Length; i++)
                            {
                                Color32 p = pixels[i];
                                if (format == TextureFormat.Alpha8 && files2.Count == 0)
                                {
                                    byte b = Math.Min(p.r, p.a);
                                    allPixels[idx].r = b;
                                    allPixels[idx].g = b;
                                    allPixels[idx].b = b;
                                    allPixels[idx].a = b;
                                }
                                else
                                {
                                    allPixels[idx] = p;
                                }
                                idx++;
                            }
                        }
                    }
                    else
                    {
                        int lastLength = -1;
                        Color32[] subPixels = new Color32[tex2D.width * tex2D.height];
                        for (int fileIndex = 0; fileIndex < filesArray[0].Count; fileIndex++)
                        {
                            for (int subFileIndex = 0; subFileIndex < 4; subFileIndex++)
                            {
                                string file = filesArray[subFileIndex][fileIndex];
                                tex2D.LoadImage(File.ReadAllBytes(file));
                                Color32[] imgPixels = tex2D.GetPixels32();
                                if (lastLength != -1 && subPixels.Length != lastLength)
                                {
                                    EditorUtility.DisplayDialog("Unable to create 3D texture", "Mismatching image size in " + file, "OK");
                                    GameObject.DestroyImmediate(tex3D);
                                    return;
                                }
                                lastLength = subPixels.Length;
                                for (int pixelIndex = 0; pixelIndex < imgPixels.Length; pixelIndex++)
                                {
                                    switch (subFileIndex)
                                    {
                                        case 0:
                                            subPixels[pixelIndex].r = Math.Min(imgPixels[pixelIndex].r, imgPixels[pixelIndex].a);
                                            break;

                                        case 1:
                                            subPixels[pixelIndex].g = Math.Min(imgPixels[pixelIndex].r, imgPixels[pixelIndex].a);
                                            break;

                                        case 2:
                                            subPixels[pixelIndex].b = Math.Min(imgPixels[pixelIndex].r, imgPixels[pixelIndex].a);
                                            break;

                                        case 3:
                                            subPixels[pixelIndex].a = Math.Min(imgPixels[pixelIndex].r, imgPixels[pixelIndex].a);
                                            break;
                                    }
                                }
                            }
                            foreach (Color32 pixel in subPixels)
                            {
                                allPixels[idx++] = pixel;
                            }
                        }
                    }
                    tex3D.SetPixels32(allPixels);
                    tex3D.Apply(true);
                    UnityEditor.AssetDatabase.CreateAsset(tex3D, "Assets/My3DTexture.asset");
                    EditorUtility.DisplayDialog("3D texture saved", "New texture asset created as 'Assets/My3DTexture.asset", "OK");
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Unable to create 3D texture", ex.ToString(), "OK");
                    GameObject.DestroyImmediate(tex3D);
                }
                finally
                {
                    GameObject.DestroyImmediate(tex2D);
                }
            }
        }

        [MenuItem("Window/Weather Maker/Extract 3D Texture", false, priority = 53)]
        public static void Extract3DTexture()
        {
            string inputFile = EditorUtility.OpenFilePanel("Select 3d texture to extract, i.e. myproject/assets/subfolder/my3dtexture.asset/", string.Empty, "asset");
            if (string.IsNullOrEmpty(inputFile))
            {
                return;
            }

            string rootPath = Application.dataPath;
            if (rootPath.EndsWith("/Assets", StringComparison.OrdinalIgnoreCase))
            {
                rootPath = rootPath.Substring(0, rootPath.Length - 6);
            }
            string assetPath = inputFile.Substring(rootPath.Length);
            Texture3D tex3D = AssetDatabase.LoadAssetAtPath<Texture3D>(assetPath);
            if (tex3D == null)
            {
                EditorUtility.DisplayDialog("Unable to load 3D texture", "Error loading asset", "OK");
                return;
            }
            int size = tex3D.width * tex3D.height;
            Color32[] allPixels = tex3D.GetPixels32();
            Color32[] subPixels = new Color32[size];
            string docsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
            docsPath = System.IO.Path.Combine(docsPath, "WeatherMakerNoiseTexture/Extracted3DTexture");
            System.IO.Directory.CreateDirectory(docsPath);
            for (int i = 0; i < tex3D.depth; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    Array.Copy(allPixels, i * size, subPixels, 0, size);
                    string outputPath;
                    switch (j)
                    {
                        case 0:
                            outputPath = System.IO.Path.Combine(docsPath, "1");
                            for (int k = 0; k < subPixels.Length; k++)
                            {
                                subPixels[k].r = subPixels[k].g = subPixels[k].b = subPixels[k].r;
                                subPixels[k].a = 255;
                            }
                            break;

                        case 1:
                            outputPath = System.IO.Path.Combine(docsPath, "2");
                            for (int k = 0; k < subPixels.Length; k++)
                            {
                                subPixels[k].r = subPixels[k].g = subPixels[k].b = subPixels[k].g;
                                subPixels[k].a = 255;
                            }
                            break;

                        case 2:
                            outputPath = System.IO.Path.Combine(docsPath, "3");
                            for (int k = 0; k < subPixels.Length; k++)
                            {
                                subPixels[k].r = subPixels[k].g = subPixels[k].b = subPixels[k].b;
                                subPixels[k].a = 255;
                            }
                            break;

                        default:
                            outputPath = System.IO.Path.Combine(docsPath, "4");
                            for (int k = 0; k < subPixels.Length; k++)
                            {
                                subPixels[k].r = subPixels[k].g = subPixels[k].b = subPixels[k].a;
                                subPixels[k].a = 255;
                            }
                            break;
                    }
                    System.IO.Directory.CreateDirectory(outputPath);
                    Texture2D tex = new Texture2D(tex3D.width, tex3D.height, TextureFormat.ARGB32, false, false);
                    tex.SetPixels32(subPixels);
                    byte[] png = tex.EncodeToPNG();
                    GameObject.DestroyImmediate(tex);
                    File.WriteAllBytes(Path.Combine(outputPath, "ExtractedImage_" + i.ToString("D4") + ".png"), png);
                }
            }
            EditorUtility.DisplayDialog("3D texture extracted", "Saved png to " + docsPath, "OK");
        }

        [MenuItem("Window/Weather Maker/Create 3D Texture from TextAsset", false, priority = 54)]
        public static void Create3DTextureFromTextAsset()
        {
            Create3DNoiseTextureByPickingAsset();
        }

        [MenuItem("Window/Weather Maker/Combine Normal and Alpha Textures", false, priority = 55)]
        public static void CombineNormalAndAlphaTextures()
        {
            string normalMapFile = EditorUtility.OpenFilePanelWithFilters("Select normal map texture", string.Empty, ImageFilesFilter);
            if (string.IsNullOrEmpty(normalMapFile))
            {
                return;
            }
            string alphaFile = EditorUtility.OpenFilePanelWithFilters("Select alpha texture", string.Empty, ImageFilesFilter);
            if (string.IsNullOrEmpty(alphaFile))
            {
                return;
            }
            try
            {
                Texture2D normalMapTexture = new Texture2D(1, 1);
                normalMapTexture.LoadImage(File.ReadAllBytes(normalMapFile));
                Texture2D alphaTexture = new Texture2D(1, 1);
                alphaTexture.LoadImage(File.ReadAllBytes(alphaFile));
                if (normalMapTexture.width != alphaTexture.width || normalMapTexture.height != alphaTexture.height)
                {
                    throw new InvalidOperationException("Normal map and alpha image must be same size");
                }
                Texture2D final = new Texture2D(normalMapTexture.width, normalMapTexture.height, TextureFormat.ARGB32, false, false);
                string combinedFile = EditorUtility.SaveFilePanel("Save final texture", string.Empty, "Combined.png", "png");
                if (string.IsNullOrEmpty(combinedFile))
                {
                    return;
                }

                Color32[] normalMapPixels = normalMapTexture.GetPixels32();
                Color32[] alphaPixels = alphaTexture.GetPixels32();
                Color32[] finalPixels = new Color32[normalMapPixels.Length];
                for (int i = 0; i < normalMapPixels.Length; i++)
                {
                    Vector3 normal = new Vector3(normalMapPixels[i].r / 255.0f, normalMapPixels[i].g / 255.0f, normalMapPixels[i].b / 255.0f).normalized;
                    finalPixels[i] = new Color32((byte)(normal.x * 255.0f), (byte)(normal.y * 255.0f), (byte)(normal.z * 255.0f), alphaPixels[i].a);
                }
                final.SetPixels32(finalPixels);
                final.Apply();
                if (Path.GetExtension(combinedFile).Equals(".png", StringComparison.OrdinalIgnoreCase))
                {
                    File.WriteAllBytes(combinedFile, final.EncodeToPNG());
                }
                else
                {
                    File.WriteAllBytes(combinedFile, final.EncodeToJPG(92));
                }
                EditorUtility.DisplayDialog("Success", "Combined texture saved at " + combinedFile, "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", ex.Message, "OK");
            }
        }
    }
}
