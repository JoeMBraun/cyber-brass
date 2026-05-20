using UnityEngine;

namespace CyberBrass.Weapons
{
    /// <summary>
    /// The runtime behaviour attached to the weapon instance in the player's hand.
    /// Coordinates shooting cycles, reload state, ammo counting, and invokes firing visual/audio effects.
    /// </summary>
    public class WeaponInstance : MonoBehaviour
    {
        [Tooltip("The data asset defining the properties for this weapon.")]
        [SerializeField] private WeaponBase weaponData;

        [Header("Runtime State")]
        [SerializeField] private int currentAmmo;
        private float _lastFireTime;
        private bool _isReloading;
        private bool _isInitialized;

        public WeaponBase WeaponData => weaponData;
        public int CurrentAmmo => currentAmmo;
        public bool IsReloading => _isReloading;

        private void Start()
        {
            if (weaponData != null && !_isInitialized)
            {
                Initialize(weaponData);
            }
        }

        /// <summary>
        /// Initializes the weapon instance with its stats and fills ammo.
        /// </summary>
        public void Initialize(WeaponBase data)
        {
            weaponData = data;
            currentAmmo = data.MagazineCapacity;
            _isInitialized = true;
            _isReloading = false;
            Debug.Log($"[WeaponInstance] {weaponData.WeaponName} initialized with {currentAmmo} rounds.");
        }

        /// <summary>
        /// Attempts to fire the weapon.
        /// Handles fire rate limiting, ammo checking, and reload state.
        /// </summary>
        /// <param name="origin">The point from which the shot is fired (e.g. camera position).</param>
        /// <param name="direction">The direction vector of the shot.</param>
        /// <returns>True if the weapon successfully fired a round, false otherwise.</returns>
        public bool TryFire(Vector3 origin, Vector3 direction)
        {
            if (weaponData == null || _isReloading) return false;

            // Check fire rate throttling
            if (Time.time < _lastFireTime + weaponData.FireRate) return false;

            // Check ammunition
            if (currentAmmo <= 0)
            {
                Debug.Log($"[WeaponInstance] {weaponData.WeaponName} Click! Out of ammo.");
                // TODO: Play dry-fire sound
                return false;
            }

            // Consume bullet and record timestamp
            currentAmmo--;
            _lastFireTime = Time.time;

            ExecuteShoot(origin, direction);
            return true;
        }

        /// <summary>
        /// Executes the shot mechanics: performs a raycast and instantiates muzzle flash VFX.
        /// </summary>
        private void ExecuteShoot(Vector3 origin, Vector3 direction)
        {
            Debug.Log($"[WeaponInstance] Fired {weaponData.WeaponName}. Ammo remaining: {currentAmmo}");

            // Instantiate muzzle flash if configured
            if (weaponData.MuzzleFlashPrefab != null)
            {
                // Instantiate muzzle flash at muzzle transform or self position
                GameObject flash = Instantiate(weaponData.MuzzleFlashPrefab, transform.position, transform.rotation);
                Destroy(flash, 1.0f); // Clean up flash particle after 1 second
            }

            // Apply random spread to the firing direction
            Vector3 finalDirection = direction + Random.insideUnitSphere * weaponData.Spread;
            finalDirection.Normalize();

            // Spawn projectile or perform hitscan check
            if (weaponData.IsProjectile)
            {
                GameObject projGo;
                if (weaponData.ProjectilePrefab != null)
                {
                    projGo = Instantiate(weaponData.ProjectilePrefab, origin, Quaternion.LookRotation(finalDirection));
                }
                else
                {
                    // Generate a beautiful, glowing orange-amber sphere fallback projectile programmatically
                    projGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    projGo.name = "DynamicProjectile";
                    projGo.transform.position = origin;
                    projGo.transform.localScale = Vector3.one * 0.25f;
                    
                    // Remove collider so the script handles sweep collision detection
                    Destroy(projGo.GetComponent<Collider>());

                    var r = projGo.GetComponent<Renderer>();
                    if (r != null)
                    {
                        r.sharedMaterial = new Material(Shader.Find("Standard"));
                        r.sharedMaterial.color = new Color(1.0f, 0.7f, 0.1f);
                    }
                }

                var projectileScript = projGo.GetComponent<CyberBrass.Combat.Projectile>();
                if (projectileScript == null)
                {
                    projectileScript = projGo.AddComponent<CyberBrass.Combat.Projectile>();
                }
                projectileScript.Initialize(finalDirection * weaponData.ProjectileSpeed, weaponData.GravityScale, weaponData.Damage);
            }
            else
            {
                // Perform hitscan check
                if (Physics.Raycast(origin, finalDirection, out RaycastHit hit, weaponData.Range))
                {
                    Debug.Log($"[WeaponInstance] Hit target: {hit.collider.gameObject.name} at distance {hit.distance}m");
                    
                    // TODO: Raise DamageEvent via Combat.DamageSystem
                }
            }
        }

        /// <summary>
        /// Triggers the reloading process.
        /// </summary>
        public void StartReload()
        {
            if (_isReloading || currentAmmo == weaponData.MagazineCapacity) return;
            StartCoroutine(ReloadCoroutine());
        }

        private System.Collections.IEnumerator ReloadCoroutine()
        {
            _isReloading = true;
            Debug.Log($"[WeaponInstance] Reloading {weaponData.WeaponName}...");

            yield return new WaitForSeconds(weaponData.ReloadTime);

            currentAmmo = weaponData.MagazineCapacity;
            _isReloading = false;
            Debug.Log($"[WeaponInstance] Reload complete. Ammo: {currentAmmo}");
        }
    }
}
