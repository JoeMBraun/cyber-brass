using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using CyberBrass.Player;
using CyberBrass.Weapons;

namespace CyberBrass.Tests
{
    /// <summary>
    /// Integration/PlayMode tests verifying the Player movement, physics gravity, jumping,
    /// and weapon firing mechanics inside the Unity runtime environment.
    /// Inherits from InputTestFixture to simulate device inputs programmatically.
    /// </summary>
    public class PlayerPlayModeTests : InputTestFixture
    {
        private GameObject _playerObject;
        private PlayerController _playerController;
        private GameObject _floorObject;
        private GameObject _cameraObject;
        private Keyboard _keyboard;
        private Mouse _mouse;

        /// <summary>
        /// Installs the keyboard and mouse mock devices and builds a simple physics testing arena
        /// containing a player, camera, and ground plane.
        /// </summary>
        public override void Setup()
        {
            base.Setup();

            // Set up virtual input devices
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();

            // Create floor
            _floorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _floorObject.name = "TestFloor";
            _floorObject.transform.position = new Vector3(0, -0.5f, 0);
            _floorObject.transform.localScale = new Vector3(20, 1, 20);

            // Create player
            _playerObject = new GameObject("TestPlayer");
            _playerObject.transform.position = new Vector3(0, 1.5f, 0); // Spawns above floor
            
            // Add CharacterController and PlayerController
            var charController = _playerObject.AddComponent<CharacterController>();
            charController.center = new Vector3(0, 1, 0);
            charController.height = 2f;

            _playerController = _playerObject.AddComponent<PlayerController>();

            // Create camera and link to controller private field via reflection
            _cameraObject = new GameObject("TestCamera");
            _cameraObject.transform.SetParent(_playerObject.transform);
            _cameraObject.transform.localPosition = new Vector3(0, 1.8f, 0);

            FieldInfo camField = typeof(PlayerController).GetField("playerCamera", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (camField != null)
            {
                camField.SetValue(_playerController, _cameraObject.transform);
            }
        }

        /// <summary>
        /// Clears instantiated GameObjects and cleans up simulated input devices.
        /// </summary>
        public override void TearDown()
        {
            Object.Destroy(_playerObject);
            Object.Destroy(_floorObject);
            Object.Destroy(_cameraObject);

            base.TearDown();
        }

        /// <summary>
        /// Verifies that gravity acts on the player when spawned in the air, causing them to fall
        /// and eventually touch the floor (isGrounded becomes true).
        /// </summary>
        [UnityTest]
        public IEnumerator PlayerFallsAndLandsOnFloor()
        {
            var charController = _playerObject.GetComponent<CharacterController>();

            // Initially in mid-air
            Assert.IsFalse(charController.isGrounded, "Player should start in mid-air.");

            // Wait 1 second for physics engine to process gravity and contact colliders
            yield return new WaitForSeconds(1.0f);

            // Should have landed on the floor collider
            Assert.IsTrue(charController.isGrounded, "Player should land on the floor and become grounded.");
            Assert.IsTrue(_playerObject.transform.position.y > -0.1f && _playerObject.transform.position.y < 0.1f, 
                $"Player Y position should snap to ground height (around 0), got: {_playerObject.transform.position.y}");
        }

        /// <summary>
        /// Simulates pressing the spacebar to verify that the jump mechanic alters the player's
        /// vertical position upwards.
        /// </summary>
        [UnityTest]
        public IEnumerator PlayerCanJump()
        {
            // Wait for player to land first
            yield return new WaitForSeconds(0.5f);

            float groundedY = _playerObject.transform.position.y;

            // Trigger jump input
            Press(_keyboard.spaceKey);
            yield return null; // process input frame
            Release(_keyboard.spaceKey);

            // Wait a few frames for physics/movement processing
            yield return new WaitForSeconds(0.2f);

            // Player should be higher than their grounded Y coordinate
            Assert.Greater(_playerObject.transform.position.y, groundedY, 
                "Player Y coordinate should increase after jumping.");
        }

        /// <summary>
        /// Verifies that a weapon successfully instantiates, shoots, performs a hitscan check,
        /// and decrements ammo capacity accordingly.
        /// </summary>
        [UnityTest]
        public IEnumerator WeaponFiresAndDecrementsAmmo()
        {
            // Set up a mock weapon instance on the player
            var weaponGo = new GameObject("TestWeapon");
            weaponGo.transform.SetParent(_cameraObject.transform);
            weaponGo.transform.localPosition = new Vector3(0.3f, -0.3f, 0.5f);

            WeaponInstance weaponInstance = weaponGo.AddComponent<WeaponInstance>();

            // Create scriptable object config
            WeaponBase weaponConfig = ScriptableObject.CreateInstance<WeaponBase>();
            
            // Set stats via reflection (fields are serialized private)
            typeof(WeaponBase).GetField("weaponName", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(weaponConfig, "Test Pistol");
            typeof(WeaponBase).GetField("magazineCapacity", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(weaponConfig, 10);
            typeof(WeaponBase).GetField("fireRate", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(weaponConfig, 0.1f);
            typeof(WeaponBase).GetField("range", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(weaponConfig, 50f);
            typeof(WeaponBase).GetField("spread", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(weaponConfig, 0f);

            weaponInstance.Initialize(weaponConfig);

            Assert.AreEqual(10, weaponInstance.CurrentAmmo, "Ammo should start at the weapon capacity.");

            // Firing origin and direction forward from camera
            Vector3 origin = _cameraObject.transform.position;
            Vector3 direction = _cameraObject.transform.forward;

            // Fire first shot
            bool didFire = weaponInstance.TryFire(origin, direction);
            Assert.IsTrue(didFire, "Weapon should fire successfully.");
            Assert.AreEqual(9, weaponInstance.CurrentAmmo, "Ammo count should decrement by 1.");

            // Attempt immediate second fire (should be throttled by fireRate)
            bool didFireThrottled = weaponInstance.TryFire(origin, direction);
            Assert.IsFalse(didFireThrottled, "Weapon fire should be throttled by the fire rate limit.");

            // Wait for fire rate cooldown
            yield return new WaitForSeconds(0.15f);

            // Fire second shot
            didFire = weaponInstance.TryFire(origin, direction);
            Assert.IsTrue(didFire, "Weapon should fire successfully after cooldown.");
            Assert.AreEqual(8, weaponInstance.CurrentAmmo, "Ammo count should decrement to 8.");

            Object.Destroy(weaponGo);
            Object.Destroy(weaponConfig);
        }
    }
}
