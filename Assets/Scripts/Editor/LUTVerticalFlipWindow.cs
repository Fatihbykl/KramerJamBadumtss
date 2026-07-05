using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
namespace CozyCorner.EditorTools
{
    /// <summary>
    /// Unity Editor tool for vertically flipping LUT textures.
    /// Useful for LUTs that appear "upside down" due to different coordinate system conventions.
    /// </summary>
    public class LUTVerticalFlipWindow : EditorWindow
    {
        private List<Texture2D> texturesToFlip = new List<Texture2D>();
        private Vector2 scrollPosition;
        private bool overwriteOriginal = true;

        [MenuItem("Tools/Cozy Corner/LUT Vertical Flip")]
        public static void ShowWindow()
        {
            LUTVerticalFlipWindow window = GetWindow<LUTVerticalFlipWindow>("LUT Flipper");
            window.minSize = new Vector2(400, 500);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(10, 10, 10, 10) });
            
            DrawHeader();
            EditorGUILayout.Space(10);
            
            DrawTextureList();
            EditorGUILayout.Space(10);
            
            DrawSettings();
            EditorGUILayout.Space(10);
            
            DrawActionButtons();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("LUT Vertical Flipper", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Drag and drop LUT textures here to flip them vertically. This is often needed when LUTs from other software appear incorrectly in Unity.", MessageType.Info);
        }

        private void DrawTextureList()
        {
            EditorGUILayout.LabelField("Textures to Process", EditorStyles.boldLabel);
            
            // Drag and drop area
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag & Drop Textures Here", new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter });
            
            HandleDragAndDrop(dropArea);
            
            EditorGUILayout.Space(5);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, "box");
            
            if (texturesToFlip.Count == 0)
            {
                EditorGUILayout.LabelField("No textures added.", EditorStyles.centeredGreyMiniLabel);
            }
            
            for (int i = 0; i < texturesToFlip.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                texturesToFlip[i] = (Texture2D)EditorGUILayout.ObjectField(texturesToFlip[i], typeof(Texture2D), false);
                
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    texturesToFlip.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear List"))
            {
                texturesToFlip.Clear();
            }
            if (GUILayout.Button("Add Selected in Project"))
            {
                AddSelectedTextures();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            overwriteOriginal = EditorGUILayout.Toggle("Overwrite Original Files", overwriteOriginal);
            if (!overwriteOriginal)
            {
                EditorGUILayout.HelpBox("New files will be created with '_Flipped' suffix.", MessageType.None);
            }
        }

        private void DrawActionButtons()
        {
            GUI.enabled = texturesToFlip.Count > 0;
            
            if (GUILayout.Button("FLIP VERTICALLY", GUILayout.Height(40)))
            {
                ProcessTextures();
            }
            
            GUI.enabled = true;
        }

        private void HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (Object draggedObject in DragAndDrop.objectReferences)
                        {
                            if (draggedObject is Texture2D tex)
                            {
                                AddTextureToList(tex);
                            }
                            else if (draggedObject is DefaultAsset) // Likely a folder
                            {
                                string path = AssetDatabase.GetAssetPath(draggedObject);
                                if (Directory.Exists(path))
                                {
                                    AddTexturesInFolder(path);
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private void AddTexturesInFolder(string folderPath)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null)
                {
                    AddTextureToList(tex);
                }
            }
        }

        private void AddTextureToList(Texture2D tex)
        {
            if (!texturesToFlip.Contains(tex))
                texturesToFlip.Add(tex);
        }

        private void AddSelectedTextures()
        {
            foreach (Object obj in Selection.objects)
            {
                if (obj is Texture2D tex)
                {
                    AddTextureToList(tex);
                }
                else if (obj is DefaultAsset)
                {
                    string path = AssetDatabase.GetAssetPath(obj);
                    if (Directory.Exists(path))
                    {
                        AddTexturesInFolder(path);
                    }
                }
            }
        }

        private void ProcessTextures()
        {
            int successCount = 0;
            
            try
            {
                for (int i = 0; i < texturesToFlip.Count; i++)
                {
                    Texture2D tex = texturesToFlip[i];
                    if (tex == null) continue;

                    string assetPath = AssetDatabase.GetAssetPath(tex);
                    if (string.IsNullOrEmpty(assetPath)) continue;

                    EditorUtility.DisplayProgressBar("Flipping LUTs", $"Processing {tex.name} ({i+1}/{texturesToFlip.Count})...", (float)i / texturesToFlip.Count);
                    
                    if (FlipTexture(tex, assetPath))
                    {
                        successCount++;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Process Complete", $"Successfully flipped {successCount} textures.", "OK");
            }
        }

        private bool FlipTexture(Texture2D originalTex, string assetPath)
        {
            // 1. Ensure texture is readable and uncompressed for pixel access
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return false;

            bool wasReadable = importer.isReadable;
            TextureImporterCompression originalCompression = importer.textureCompression;
            bool wasNormalMap = importer.textureType == TextureImporterType.NormalMap;
            
            // For LUTs, we usually want them as Default or Cookie, but let's stick to current type
            // but ensure we can read it.
            if (!wasReadable || originalCompression != TextureImporterCompression.Uncompressed)
            {
                importer.isReadable = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }

            // 2. Create a copy and flip pixels
            // Re-load to get the readable version
            Texture2D readableTex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (readableTex == null) return false;

            int width = readableTex.width;
            int height = readableTex.height;
            
            // Get pixels. Note: this might fail if the texture is still not readable for some reason
            Color[] pixels;
            try {
                pixels = readableTex.GetPixels();
            } catch (System.Exception e) {
                Debug.LogError($"Failed to get pixels for {readableTex.name}: {e.Message}");
                return false;
            }

            Color[] flippedPixels = new Color[pixels.Length];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    flippedPixels[(height - 1 - y) * width + x] = pixels[y * width + x];
                }
            }

            // We use RGBA32 to ensure we can save it properly regardless of original format
            Texture2D newTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            newTex.SetPixels(flippedPixels);
            newTex.Apply();

            // 3. Save
            byte[] bytes = null;
            string extension = Path.GetExtension(assetPath).ToLower();
            
            if (extension == ".png")
                bytes = newTex.EncodeToPNG();
            else if (extension == ".tga")
                bytes = newTex.EncodeToTGA();
            else if (extension == ".jpg" || extension == ".jpeg")
                bytes = newTex.EncodeToJPG();
            else
            {
                // Fallback to PNG if we don't know how to encode the original extension
                bytes = newTex.EncodeToPNG();
                if (overwriteOriginal) 
                {
                    // If overwriting, we might be changing extension if it wasn't png/tga/jpg
                    // This is rare for LUTs which are usually PNG or TGA
                }
            }

            if (bytes == null) return false;

            string savePath = assetPath;
            if (!overwriteOriginal)
            {
                string dir = Path.GetDirectoryName(assetPath);
                string fileName = Path.GetFileNameWithoutExtension(assetPath);
                string targetExt = (extension == ".png" || extension == ".tga" || extension == ".jpg" || extension == ".jpeg") ? extension : ".png";
                savePath = Path.Combine(dir, fileName + "_Flipped" + targetExt);
            }

            File.WriteAllBytes(savePath, bytes);
            AssetDatabase.ImportAsset(savePath, ImportAssetOptions.ForceUpdate);

            // 4. Restore/Apply settings
            TextureImporter finalImporter = AssetImporter.GetAtPath(savePath) as TextureImporter;
            if (finalImporter != null)
            {
                if (overwriteOriginal)
                {
                    finalImporter.isReadable = wasReadable;
                    finalImporter.textureCompression = originalCompression;
                }
                else
                {
                    // Copy settings from original
                    EditorUtility.CopySerialized(importer, finalImporter);
                    // Ensure the new one is at least as readable as we need it if we didn't overwrite? 
                    // No, usually user wants the same settings.
                }
                AssetDatabase.ImportAsset(savePath, ImportAssetOptions.ForceUpdate);
            }

            return true;
        }
    }
}
#endif
