using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CyberBrass.Player;
using CyberBrass.Weapons;

namespace CyberBrass.Editor
{
    /// <summary>
    /// Editor utility script that generates a greybox playground scene containing a floor,
    /// a fully configured FPS player controller, mock weapons, and static targets to test shoot.
    /// Accessible via the Unity Editor top menu bar.
    /// </summary>
    public static class PlaygroundGenerator
    {
        [MenuItem("CyberBrass/Generate Playground Scene")]
        public static void GeneratePlayground()
        {
            // 1. Create a new empty scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Delete standard camera as we will use the player's camera rig
            var defaultCam = GameObject.Find("Main Camera");
            if (defaultCam != null)
            {
                Object.DestroyImmediate(defaultCam);
            }

            // 2. Create Floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0, -0.5f, 0);
            floor.transform.localScale = new Vector3(100, 1, 100);
            
            var floorRenderer = floor.GetComponent<Renderer>();
            if (floorRenderer != null)
            {
                floorRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
                // Set color to Dark Teal / Verdigris palette match (#1E5F74 / #3E7C6A)
                floorRenderer.sharedMaterial.color = new Color(0.12f, 0.24f, 0.20f);
            }

            // 3. Create Player GameObject
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0, 1.0f, 0);
            
            var charController = player.AddComponent<CharacterController>();
            charController.center = new Vector3(0, 1.0f, 0);
            charController.height = 2.0f;
            charController.radius = 0.5f;

            var playerController = player.AddComponent<PlayerController>();

            // Create Camera
            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(player.transform);
            camGo.transform.localPosition = new Vector3(0, 1.8f, 0);
            camGo.transform.localRotation = Quaternion.identity;
            var camera = camGo.AddComponent<Camera>();
            camera.nearClipPlane = 0.01f;
            camGo.AddComponent<AudioListener>();

            // Link camera in PlayerController
            var serializedPlayer = new SerializedObject(playerController);
            serializedPlayer.FindProperty("playerCamera").objectReferenceValue = camGo.transform;
            serializedPlayer.ApplyModifiedProperties();
            
            // 4. Create default WeaponConfig asset
            WeaponBase weaponConfig = AssetDatabase.LoadAssetAtPath<WeaponBase>("Assets/Settings/DefaultPistol.asset");
            if (weaponConfig == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                {
                    AssetDatabase.CreateFolder("Assets", "Settings");
                }
                
                weaponConfig = ScriptableObject.CreateInstance<WeaponBase>();
                var serializedWeapon = new SerializedObject(weaponConfig);
                serializedWeapon.FindProperty("weaponName").stringValue = "The Foreman";
                serializedWeapon.FindProperty("description").stringValue = "Brass-cased revolver. Six shots, devastating, slow reload.";
                serializedWeapon.FindProperty("damage").floatValue = 40f;
                serializedWeapon.FindProperty("fireRate").floatValue = 0.5f;
                serializedWeapon.FindProperty("range").floatValue = 100f;
                serializedWeapon.FindProperty("spread").floatValue = 0.02f;
                serializedWeapon.FindProperty("magazineCapacity").intValue = 6;
                serializedWeapon.FindProperty("reloadTime").floatValue = 2.0f;
                serializedWeapon.ApplyModifiedProperties();
                
                AssetDatabase.CreateAsset(weaponConfig, "Assets/Settings/DefaultPistol.asset");
                AssetDatabase.SaveAssets();
            }

            // Create Weapon GameObject
            var weaponGo = new GameObject("ForemanRevolver");
            weaponGo.transform.SetParent(camGo.transform);
            weaponGo.transform.localPosition = new Vector3(0.3f, -0.3f, 0.6f);
            weaponGo.transform.localRotation = Quaternion.identity;
            
            var weaponMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            weaponMesh.name = "VisualModel";
            weaponMesh.transform.SetParent(weaponGo.transform);
            weaponMesh.transform.localPosition = Vector3.zero;
            weaponMesh.transform.localRotation = Quaternion.Euler(90, 0, 0);
            weaponMesh.transform.localScale = new Vector3(0.06f, 0.25f, 0.06f);
            Object.DestroyImmediate(weaponMesh.GetComponent<CapsuleCollider>());
            
            var weaponRenderer = weaponMesh.GetComponent<Renderer>();
            if (weaponRenderer != null)
            {
                weaponRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
                // Aged Brass color palette match (#B5824A)
                weaponRenderer.sharedMaterial.color = new Color(0.71f, 0.51f, 0.29f);
            }

            var weaponInstance = weaponGo.AddComponent<WeaponInstance>();
            var serializedInstance = new SerializedObject(weaponInstance);
            serializedInstance.FindProperty("weaponData").objectReferenceValue = weaponConfig;
            serializedInstance.ApplyModifiedProperties();

            // 5. Create some shooting target pillars around the scene
            var targetRoot = new GameObject("Targets");
            Vector3[] targetPositions = new[]
            {
                new Vector3(0, 2.5f, 15),
                new Vector3(-10, 2.5f, 20),
                new Vector3(10, 2.5f, 20),
                new Vector3(-15, 2.5f, 10),
                new Vector3(15, 2.5f, 10),
            };

            for (int i = 0; i < targetPositions.Length; i++)
            {
                var target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                target.name = $"TargetPillar_{i + 1}";
                target.transform.SetParent(targetRoot.transform);
                target.transform.position = targetPositions[i];
                target.transform.localScale = new Vector3(1, 2.5f, 1);
                
                var targetRenderer = target.GetComponent<Renderer>();
                if (targetRenderer != null)
                {
                    targetRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
                    // Amber Glow / Rust accent color (#FFB347)
                    targetRenderer.sharedMaterial.color = new Color(0.9f, 0.5f, 0.1f);
                }
            }

            // Save the scene file
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Playground.unity");
            
            Debug.Log("[PlaygroundGenerator] Generated and saved Playground scene at Assets/Scenes/Playground.unity!");
        }
    }
}
