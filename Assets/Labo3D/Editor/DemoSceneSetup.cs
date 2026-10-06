using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Labo3D.EditorTools
{
    public static class DemoSceneSetup
    {
        const string ScenePath = "Assets/Labo3D/Scenes/Demo.unity";
        const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        [MenuItem("Labo3D/Rebuild Demo Scene")]
        public static void Build()
        {
            if (!File.Exists(FontPath))
            {
                AssetDatabase.importPackageCompleted += OnTmpImported;
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                Debug.Log("[Labo3D] Import des ressources TextMeshPro, la scène suit.");
                return;
            }

            BuildScene();
        }

        static void OnTmpImported(string packageName)
        {
            AssetDatabase.importPackageCompleted -= OnTmpImported;
            EditorApplication.delayCall += BuildScene;
        }

        static void BuildScene()
        {
            EnsureFolder("Assets/Labo3D");
            EnsureFolder("Assets/Labo3D/Materials");
            EnsureFolder("Assets/Labo3D/Scenes");

            Shader hologramShader = Shader.Find("Labo3D/Holographic");
            Shader dissolveShader = Shader.Find("Labo3D/Dissolve");
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (hologramShader == null || dissolveShader == null || litShader == null)
                throw new InvalidOperationException(
                    "Un shader est introuvable. Holographic=" + (hologramShader != null)
                    + " Dissolve=" + (dissolveShader != null)
                    + " Lit=" + (litShader != null));

            Material ground = CreateLit("Assets/Labo3D/Materials/Ground.mat", litShader, new Color(0.22f, 0.25f, 0.28f));
            Material player = CreateLit("Assets/Labo3D/Materials/Player.mat", litShader, new Color(0.75f, 0.78f, 0.82f));
            Material propA = CreateLit("Assets/Labo3D/Materials/PropSlate.mat", litShader, new Color(0.35f, 0.42f, 0.48f));
            Material propB = CreateLit("Assets/Labo3D/Materials/PropSand.mat", litShader, new Color(0.55f, 0.48f, 0.36f));
            Material pedestalMat = CreateLit("Assets/Labo3D/Materials/Pedestal.mat", litShader, new Color(0.16f, 0.17f, 0.19f));

            Material hologram = CreateMaterial("Assets/Labo3D/Materials/Holographic.mat", hologramShader);
            hologram.SetColor("_BaseColor", new Color(0.15f, 0.85f, 1f, 1f));
            hologram.SetFloat("_RimPower", 2.5f);
            hologram.SetFloat("_ScanSpeed", 1.5f);
            hologram.SetFloat("_ScanScale", 10f);
            hologram.SetFloat("_Alpha", 0.35f);

            Material dissolve = CreateMaterial("Assets/Labo3D/Materials/Dissolve.mat", dissolveShader);
            dissolve.SetColor("_BaseColor", new Color(0.75f, 0.32f, 0.18f, 1f));
            dissolve.SetFloat("_Dissolve", 0f);
            dissolve.SetFloat("_EdgeWidth", 0.06f);
            dissolve.SetColor("_EdgeColor", new Color(1f, 0.55f, 0.1f, 1f));
            dissolve.SetFloat("_NoiseScale", 3.5f);
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            int highest = QualitySettings.names.Length - 1;
            if (highest >= 0)
                QualitySettings.SetQualityLevel(highest, true);

            RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            RenderSettings.ambientMode = AmbientMode.Skybox;

            GameObject lightGo = new GameObject("Directional Light");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
            if (profile != null)
            {
                GameObject volumeGo = new GameObject("Global Volume");
                Volume volume = volumeGo.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.sharedProfile = profile;
            }

            GameObject groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGo.name = "Ground";
            groundGo.transform.localScale = new Vector3(6f, 1f, 6f);
            groundGo.GetComponent<Renderer>().sharedMaterial = ground;

            CreateBox("PropA", new Vector3(3.2f, 0.5f, 2.5f), new Vector3(1.2f, 1f, 1.2f), propA);
            CreateBox("PropB", new Vector3(-2.4f, 0.75f, 5.5f), new Vector3(1.6f, 1.5f, 1.6f), propB);
            CreateBox("PropC", new Vector3(6f, 1f, 9f), new Vector3(2f, 2f, 2f), propA);

            GameObject hologramExhibit = CreateExhibit("Hologramme", PrimitiveType.Sphere, new Vector3(4.2f, 0f, 12f), 1.5f, hologram, pedestalMat);
            GameObject dissolveExhibit = CreateExhibit("Dissolution", PrimitiveType.Cube, new Vector3(-4.2f, 0f, 12f), 1.3f, dissolve, pedestalMat);

            GameObject playerGo = new GameObject("Player");
            playerGo.transform.position = new Vector3(0f, 0.2f, -2f);
            CharacterController controller = playerGo.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.4f;
            controller.center = new Vector3(0f, 1f, 0f);
            ThirdPersonController motor = playerGo.AddComponent<ThirdPersonController>();

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(playerGo.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            UnityEngine.Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());
            body.GetComponent<Renderer>().sharedMaterial = player;

            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 250f;
            camera.fieldOfView = 60f;
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraGo.AddComponent<AudioListener>();
            UniversalAdditionalCameraData cameraData = cameraGo.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;

            TMP_Text hint = CreateHint();

            GameObject director = new GameObject("GameController");
            GameController game = director.AddComponent<GameController>();

            SerializedObject motorObject = new SerializedObject(motor);
            motorObject.FindProperty("viewCamera").objectReferenceValue = camera;
            motorObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject gameObject = new SerializedObject(game);
            gameObject.FindProperty("hologramRenderer").objectReferenceValue = hologramExhibit.GetComponent<Renderer>();
            gameObject.FindProperty("dissolveRenderer").objectReferenceValue = dissolveExhibit.GetComponent<Renderer>();
            gameObject.FindProperty("hintLabel").objectReferenceValue = hint;
            gameObject.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            Debug.Log("[Labo3D] Scène Demo enregistrée.");
        }

        static GameObject CreateExhibit(string name, PrimitiveType primitive, Vector3 position, float size, Material exhibitMaterial, Material pedestalMaterial)
        {
            GameObject root = new GameObject(name);
            root.transform.position = position;

            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "Pedestal";
            pedestal.transform.SetParent(root.transform, false);
            pedestal.transform.localScale = new Vector3(size * 0.95f, 0.15f, size * 0.95f);
            pedestal.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            pedestal.GetComponent<Renderer>().sharedMaterial = pedestalMaterial;

            GameObject mesh = GameObject.CreatePrimitive(primitive);
            mesh.name = "Mesh";
            mesh.transform.SetParent(root.transform, false);
            UnityEngine.Object.DestroyImmediate(mesh.GetComponent<Collider>());
            mesh.GetComponent<Renderer>().sharedMaterial = exhibitMaterial;

            float half = size * 0.5f;
            float top = 0.3f;
            mesh.transform.localScale = Vector3.one * size;
            mesh.transform.localPosition = new Vector3(0f, top + half, 0f);
            return mesh;
        }

        static void CreateBox(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
        }

        static TMP_Text CreateHint()
        {
            GameObject canvasGo = new GameObject("HintCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            UnityEngine.UI.CanvasScaler scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            GameObject panel = new GameObject("HintPanel", typeof(RectTransform));
            panel.transform.SetParent(canvasGo.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0f, 0f);
            panelRect.pivot = new Vector2(0f, 0f);
            panelRect.anchoredPosition = new Vector2(24f, 24f);
            panelRect.sizeDelta = new Vector2(860f, 180f);
            UnityEngine.UI.Image image = panel.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.04f, 0.06f, 0.09f, 0.72f);
            image.raycastTarget = false;

            GameObject textGo = new GameObject("HintLabel", typeof(RectTransform));
            textGo.transform.SetParent(panel.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 12f);
            textRect.offsetMax = new Vector2(-16f, -12f);

            TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
            text.fontSize = 22f;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.color = Color.white;
            text.raycastTarget = false;
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null)
                text.font = font;
            text.text = "Labo3D";
            return text;
        }

        static Material CreateLit(string path, Shader shader, Color color)
        {
            Material material = CreateMaterial(path, shader);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.32f);
            material.SetFloat("_Metallic", 0f);
            return material;
        }

        static Material CreateMaterial(string path, Shader shader)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);

            Material material = new Material(shader)
            {
                name = Path.GetFileNameWithoutExtension(path)
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
