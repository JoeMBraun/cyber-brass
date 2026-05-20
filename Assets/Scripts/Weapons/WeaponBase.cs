using UnityEngine;

namespace CyberBrass.Weapons
{
    /// <summary>
    /// Supported ammunition types in the CyberBrass arsenal.
    /// </summary>
    public enum AmmoType
    {
        BrassCased,       // Standard physical casing bullets (e.g. The Foreman)
        PneumaticFlechette,// Pressurized air darts (e.g. Sparrowhawk)
        EnergyCell,       // Battery/arcs (e.g. Ohm-Cutter)
        ShotgunShell,     // Shrapnel shells (e.g. Brasswork)
        CoilRail          // High velocity slugs (e.g. Telegraph)
    }

    /// <summary>
    /// A ScriptableObject that defines the static properties, configurations, and assets for a weapon.
    /// New weapons can be added to the project by creating assets from this definition.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "CyberBrass/Weapon Config", order = 1)]
    public class WeaponBase : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("The user-facing name of the weapon.")]
        [SerializeField] private string weaponName;
        [Tooltip("A short lore or mechanical description of the weapon.")]
        [TextArea(2, 5)]
        [SerializeField] private string description;

        [Header("Stats")]
        [Tooltip("Damage dealt per shot or impact.")]
        [SerializeField] private float damage = 20f;
        [Tooltip("Time in seconds between consecutive shots.")]
        [SerializeField] private float fireRate = 0.2f;
        [Tooltip("Maximum effective range of the weapon.")]
        [SerializeField] private float range = 100f;
        [Tooltip("Spread angle multiplier representing projectile deviation.")]
        [SerializeField] private float spread = 0.05f;

        [Header("Projectile Settings")]
        [Tooltip("If true, fires a physical projectile instead of a hitscan raycast.")]
        [SerializeField] private bool isProjectile = false;
        [Tooltip("The projectile prefab to spawn.")]
        [SerializeField] private GameObject projectilePrefab;
        [Tooltip("Velocity speed of the spawned projectile.")]
        [SerializeField] private float projectileSpeed = 30f;
        [Tooltip("Gravity multiplier applied to the projectile (0 = straight path, 1 = normal gravity).")]
        [SerializeField] private float gravityScale = 0f;

        [Header("Ammunition")]
        [Tooltip("Maximum amount of ammo in one magazine/cylinder.")]
        [SerializeField] private int magazineCapacity = 6;
        [Tooltip("The ammo type this weapon consumes.")]
        [SerializeField] private AmmoType ammoType = AmmoType.BrassCased;
        [Tooltip("Time in seconds to reload the weapon.")]
        [SerializeField] private float reloadTime = 2.5f;

        [Header("Assets")]
        [Tooltip("The visual prefab spawned in the player's hand.")]
        [SerializeField] private GameObject weaponModelPrefab;
        [Tooltip("Muzzle flash particle effect prefab.")]
        [SerializeField] private GameObject muzzleFlashPrefab;

        #region Public Properties

        public string WeaponName => weaponName;
        public string Description => description;
        public float Damage => damage;
        public float FireRate => fireRate;
        public float Range => range;
        public float Spread => spread;
        public int MagazineCapacity => magazineCapacity;
        public AmmoType AmmoType => ammoType;
        public float ReloadTime => reloadTime;
        public GameObject WeaponModelPrefab => weaponModelPrefab;
        public GameObject MuzzleFlashPrefab => muzzleFlashPrefab;
        public bool IsProjectile => isProjectile;
        public GameObject ProjectilePrefab => projectilePrefab;
        public float ProjectileSpeed => projectileSpeed;
        public float GravityScale => gravityScale;

        #endregion
    }

}
