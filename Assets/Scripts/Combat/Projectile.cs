using UnityEngine;

namespace CyberBrass.Combat
{
    /// <summary>
    /// Represents a physical projectile that travels along a trajectory (with velocity and gravity)
    /// and explodes on impact, dealing radial damage.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private Vector3 _velocity;
        private float _gravityScale;
        private float _damage;
        private float _explosionRadius = 5.0f;
        private float _maxLifetime = 10.0f;
        private float _spawnTime;

        /// <summary>
        /// Initializes the projectile with movement parameters and damage.
        /// </summary>
        public void Initialize(Vector3 velocity, float gravityScale, float damage)
        {
            _velocity = velocity;
            _gravityScale = gravityScale;
            _damage = damage;
            _spawnTime = Time.time;
        }

        private void Update()
        {
            // Auto-cleanup if it flies forever
            if (Time.time > _spawnTime + _maxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            // Calculate translation for this frame
            Vector3 translation = _velocity * Time.deltaTime;
            float distance = translation.magnitude;

            if (distance > 0.0001f)
            {
                Vector3 direction = translation / distance;

                // Perform raycast sweep to ensure we never miss/tunnel through targets at high speeds
                if (Physics.Raycast(transform.position, direction, out RaycastHit hit, distance))
                {
                    Explode(hit.point, hit.normal);
                    return;
                }
            }

            // Apply gravity over time to the velocity vector
            if (_gravityScale != 0f)
            {
                _velocity.y += Physics.gravity.y * _gravityScale * Time.deltaTime;
            }

            // Move and align rotation to the velocity trajectory direction
            transform.position += translation;
            if (_velocity != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(_velocity);
            }
        }

        /// <summary>
        /// Triggers the explosion: applies radial damage and instantiates an expanding visual explosion.
        /// </summary>
        private void Explode(Vector3 point, Vector3 normal)
        {
            Debug.Log($"[Projectile] Exploded at {point}. Triggering radial damage.");

            // Apply explosion radial force and log hits
            Collider[] hits = Physics.OverlapSphere(point, _explosionRadius);
            foreach (var hit in hits)
            {
                var rb = hit.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(500f, point, _explosionRadius, 1.0f);
                }
                
                Debug.Log($"[Projectile] Blast hit target object: {hit.name}");
                // TODO: Raise DamageEvent via Combat.DamageSystem for hit components
            }

            // Instantiate expanding visual explosion sphere
            GameObject explosionGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            explosionGo.name = "ExplosionVFX";
            explosionGo.transform.position = point;
            explosionGo.transform.localScale = Vector3.one * 0.2f;
            
            // Remove collider so the visual sphere doesn't block physics
            Destroy(explosionGo.GetComponent<Collider>());

            var renderer = explosionGo.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Set to standard material and color it with Amber CRT Glow palette (#FFB347 / orange)
                renderer.sharedMaterial = new Material(Shader.Find("Standard"));
                renderer.sharedMaterial.color = new Color(1.0f, 0.5f, 0.1f, 1.0f);
            }

            // Attach expansion script to animate the explosion
            explosionGo.AddComponent<ExplosionExpansion>();

            // Destroy the projectile itself
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Helper script to expand and fade out the visual explosion sphere effect.
    /// </summary>
    public class ExplosionExpansion : MonoBehaviour
    {
        private float _timer = 0.0f;
        private float _duration = 0.4f;
        private Vector3 _startScale = Vector3.one * 0.2f;
        private Vector3 _targetScale = Vector3.one * 6.0f;
        private Material _material;

        private void Start()
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                // Use a copy of the material so we don't leak shared materials
                _material = renderer.material;
            }
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            float t = _timer / _duration;

            if (t >= 1.0f)
            {
                Destroy(gameObject);
                return;
            }

            // Expand size
            transform.localScale = Vector3.Lerp(_startScale, _targetScale, t);

            // Fade opacity
            if (_material != null)
            {
                Color c = _material.color;
                // Fade out alpha
                c.a = Mathf.Lerp(1.0f, 0.0f, t);
                _material.color = c;
            }
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                // Clean up instantiated material instance
                Destroy(_material);
            }
        }
    }
}
