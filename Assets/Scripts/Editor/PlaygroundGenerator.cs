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
            
            // Add targeting crosshair HUD component
            player.AddComponent<CyberBrass.UI.Crosshair>();

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
            
            // 4. Create default WeaponConfig asset (Always recreate to ensure latest settings apply)
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }
            
            WeaponBase weaponConfig = ScriptableObject.CreateInstance<WeaponBase>();
            var serializedWeapon = new SerializedObject(weaponConfig);
            serializedWeapon.FindProperty("weaponName").stringValue = "Telegraph Launcher";
            serializedWeapon.FindProperty("description").stringValue = "Shoots arcing brass grenades that travel along a trajectory and explode.";
            serializedWeapon.FindProperty("damage").floatValue = 50f;
            serializedWeapon.FindProperty("fireRate").floatValue = 0.6f;
            serializedWeapon.FindProperty("range").floatValue = 100f;
            serializedWeapon.FindProperty("spread").floatValue = 0.01f;
            serializedWeapon.FindProperty("magazineCapacity").intValue = 6;
            serializedWeapon.FindProperty("reloadTime").floatValue = 2.0f;
            
            // Projectile trajectory settings
            serializedWeapon.FindProperty("isProjectile").boolValue = true;
            serializedWeapon.FindProperty("projectileSpeed").floatValue = 20.0f; // Slower travel speed to see the projectile fly
            serializedWeapon.FindProperty("gravityScale").floatValue = 0.6f;    // Arcing gravity trajectory
            serializedWeapon.ApplyModifiedProperties();
            
            AssetDatabase.CreateAsset(weaponConfig, "Assets/Settings/DefaultPistol.asset");
            AssetDatabase.SaveAssets();

            // Create Weapon GameObject
            var weaponGo = new GameObject("ForemanRevolver");
            weaponGo.transform.SetParent(camGo.transform);
            weaponGo.transform.localPosition = new Vector3(0.3f, -0.35f, 0.55f);
            weaponGo.transform.localRotation = Quaternion.identity;

            // Parent container for the gun visual pieces
            var visualRoot = new GameObject("VisualModel");
            visualRoot.transform.SetParent(weaponGo.transform);
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localRotation = Quaternion.identity;

            // Common materials matching the color palette
            Material brassMat = new Material(Shader.Find("Standard"));
            brassMat.color = new Color(0.71f, 0.51f, 0.29f); // Aged Brass (#B5824A)
            
            Material copperMat = new Material(Shader.Find("Standard"));
            copperMat.color = new Color(0.24f, 0.49f, 0.42f); // Oxidized Copper / Verdigris (#3E7C6A)

            Material sootMat = new Material(Shader.Find("Standard"));
            sootMat.color = new Color(0.1f, 0.09f, 0.08f); // Soot Black (#1A1614)
            sootMat.SetFloat("_Glossiness", 0.1f); // Matte finish

            Material glowMat = new Material(Shader.Find("Standard"));
            glowMat.color = new Color(1.0f, 0.6f, 0.1f); // Amber CRT Glow (#FFB347)
            glowMat.EnableKeyword("_EMISSION");
            glowMat.SetColor("_EmissionColor", new Color(1.0f, 0.6f, 0.1f) * 1.5f);

            // 1. Receiver (Soot Black Frame)
            var receiver = GameObject.CreatePrimitive(PrimitiveType.Cube);
            receiver.name = "Receiver";
            receiver.transform.SetParent(visualRoot.transform);
            receiver.transform.localPosition = new Vector3(0, 0, 0);
            receiver.transform.localScale = new Vector3(0.05f, 0.09f, 0.14f);
            receiver.GetComponent<Renderer>().sharedMaterial = sootMat;
            Object.DestroyImmediate(receiver.GetComponent<Collider>());

            // 2. Barrel (Aged Brass)
            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Barrel";
            barrel.transform.SetParent(visualRoot.transform);
            barrel.transform.localPosition = new Vector3(0, 0.01f, 0.19f);
            barrel.transform.localRotation = Quaternion.Euler(90, 0, 0);
            barrel.transform.localScale = new Vector3(0.03f, 0.15f, 0.03f); // Cylinder height is along Z
            barrel.GetComponent<Renderer>().sharedMaterial = brassMat;
            Object.DestroyImmediate(barrel.GetComponent<Collider>());

            // 3. Cylinder Chamber Drum (Oxidized Copper / Verdigris)
            var cylinderDrum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinderDrum.name = "ChamberDrum";
            cylinderDrum.transform.SetParent(visualRoot.transform);
            cylinderDrum.transform.localPosition = new Vector3(0, -0.01f, -0.02f);
            cylinderDrum.transform.localRotation = Quaternion.Euler(90, 0, 0);
            cylinderDrum.transform.localScale = new Vector3(0.045f, 0.05f, 0.045f);
            cylinderDrum.GetComponent<Renderer>().sharedMaterial = copperMat;
            Object.DestroyImmediate(cylinderDrum.GetComponent<Collider>());

            // 4. Grip (Soot Black handle with Aged Brass backing)
            var grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Grip";
            grip.transform.SetParent(visualRoot.transform);
            grip.transform.localPosition = new Vector3(0, -0.09f, -0.06f);
            grip.transform.localRotation = Quaternion.Euler(20, 0, 0);
            grip.transform.localScale = new Vector3(0.045f, 0.12f, 0.04f);
            grip.GetComponent<Renderer>().sharedMaterial = sootMat;
            Object.DestroyImmediate(grip.GetComponent<Collider>());

            var gripPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gripPlate.name = "GripPlate";
            gripPlate.transform.SetParent(grip.transform);
            gripPlate.transform.localPosition = new Vector3(0, 0, -0.55f);
            gripPlate.transform.localScale = new Vector3(1.1f, 0.95f, 0.2f);
            gripPlate.GetComponent<Renderer>().sharedMaterial = brassMat;
            Object.DestroyImmediate(gripPlate.GetComponent<Collider>());

            // 5. Exposed Vacuum Tube (Glowing Amber counter on top)
            var tubeBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tubeBase.name = "TubeBase";
            tubeBase.transform.SetParent(visualRoot.transform);
            tubeBase.transform.localPosition = new Vector3(0, 0.055f, 0.03f);
            tubeBase.transform.localScale = new Vector3(0.025f, 0.015f, 0.07f);
            tubeBase.GetComponent<Renderer>().sharedMaterial = sootMat;
            Object.DestroyImmediate(tubeBase.GetComponent<Collider>());

            var tube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tube.name = "VacuumTube";
            tube.transform.SetParent(visualRoot.transform);
            tube.transform.localPosition = new Vector3(0, 0.07f, 0.03f);
            tube.transform.localRotation = Quaternion.Euler(90, 0, 0);
            tube.transform.localScale = new Vector3(0.015f, 0.025f, 0.015f);
            tube.GetComponent<Renderer>().sharedMaterial = glowMat;
            Object.DestroyImmediate(tube.GetComponent<Collider>());

            // 6. Holographic Sight Frame (Brass U-shape at the back of the receiver)
            var sightLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sightLeft.name = "SightLeft";
            sightLeft.transform.SetParent(visualRoot.transform);
            sightLeft.transform.localPosition = new Vector3(-0.02f, 0.065f, -0.05f);
            sightLeft.transform.localScale = new Vector3(0.006f, 0.04f, 0.01f);
            sightLeft.GetComponent<Renderer>().sharedMaterial = brassMat;
            Object.DestroyImmediate(sightLeft.GetComponent<Collider>());

            var sightRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sightRight.name = "SightRight";
            sightRight.transform.SetParent(visualRoot.transform);
            sightRight.transform.localPosition = new Vector3(0.02f, 0.065f, -0.05f);
            sightRight.transform.localScale = new Vector3(0.006f, 0.04f, 0.01f);
            sightRight.GetComponent<Renderer>().sharedMaterial = brassMat;
            Object.DestroyImmediate(sightRight.GetComponent<Collider>());

            // 7. Holographic Sight Reticle (Glowing Amber transparent projection)
            var sightReticle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sightReticle.name = "SightReticle";
            sightReticle.transform.SetParent(visualRoot.transform);
            sightReticle.transform.localPosition = new Vector3(0, 0.065f, -0.05f);
            sightReticle.transform.localScale = new Vector3(0.034f, 0.034f, 0.002f);
            
            // Set standard material with transparent rendering support
            Material reticleMat = new Material(Shader.Find("Standard"));
            reticleMat.color = new Color(1.0f, 0.6f, 0.1f, 0.7f); // Transparent Amber
            // Set rendering mode to transparent/fade
            reticleMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            reticleMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            reticleMat.SetInt("_ZWrite", 0);
            reticleMat.DisableKeyword("_ALPHATEST_ON");
            reticleMat.EnableKeyword("_ALPHABLEND_ON");
            reticleMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            reticleMat.renderQueue = 3000;
            
            reticleMat.EnableKeyword("_EMISSION");
            reticleMat.SetColor("_EmissionColor", new Color(1.0f, 0.6f, 0.1f) * 2.0f);
            
            sightReticle.GetComponent<Renderer>().sharedMaterial = reticleMat;
            Object.DestroyImmediate(sightReticle.GetComponent<Collider>());

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

                // Add Rigidbody to allow physical movement and explosion reactions
                target.AddComponent<Rigidbody>();
                
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
