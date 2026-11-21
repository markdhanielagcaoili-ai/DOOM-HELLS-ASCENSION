using UnityEngine;

namespace DoomHellsAscension
{
    public class Projectile3D : MonoBehaviour
    {
        public Vector3 velocity;
        public float damage;
        public bool fromPlayer;
        public bool isBFG;
        public string sourceName;
        private float life = 5f;

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
            float scale = (!fromPlayer && GameManager.Instance != null && GameManager.Instance.OverdriveActive) ? 0.55f : 1f;
            float dt = Time.deltaTime * scale;
            transform.position += velocity * dt;
            life -= Time.deltaTime;
            if (life <= 0f || transform.position.y < 0.05f)
            {
                if (isBFG) ExplodeBFG();
                Destroy(gameObject);
                return;
            }

            if (fromPlayer)
            {
                if (GameManager.Instance != null)
                {
                    foreach (var enemy in GameManager.Instance.ActiveEnemies)
                    {
                        if (enemy == null || enemy.IsDead) continue;
                        if (Vector3.Distance(transform.position, enemy.transform.position + Vector3.up * 1.2f) < 1.5f * enemy.Scale)
                        {
                            if (isBFG) ExplodeBFG();
                            else enemy.TakeDamage(damage, 8f, false);
                            Destroy(gameObject);
                            return;
                        }
                    }
                }
                if (Physics.CheckSphere(transform.position, 0.22f, ~0, QueryTriggerInteraction.Ignore))
                {
                    // Avoid hitting player collider immediately
                    var hits = Physics.OverlapSphere(transform.position, 0.22f);
                    foreach (var h in hits)
                    {
                        if (h.GetComponent<FPSPlayerController>() != null || h.GetComponent<DemonEnemyAI>() != null) continue;
                        if (isBFG) ExplodeBFG();
                        Destroy(gameObject);
                        return;
                    }
                }
            }
            else
            {
                if (FPSPlayerController.Instance != null)
                {
                    Vector3 pPos = FPSPlayerController.Instance.transform.position;
                    if (Vector3.Distance(transform.position, pPos) < 1.05f)
                    {
                        FPSPlayerController.Instance.TakeDamage(Mathf.RoundToInt(damage), sourceName);
                        Destroy(gameObject);
                        return;
                    }
                }
            }
        }

        private void ExplodeBFG()
        {
            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayOverdrive();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SpawnBurstParticles(transform.position, new Color(0.72f, 0.36f, 1f), 24);
                foreach (var enemy in GameManager.Instance.ActiveEnemies.ToArray())
                {
                    if (enemy == null || enemy.IsDead) continue;
                    float dist = Vector3.Distance(transform.position, enemy.transform.position);
                    if (dist < 16f)
                    {
                        float falloff = Mathf.Max(0.35f, 1f - dist / 16f);
                        enemy.TakeDamage(Mathf.Round(damage * falloff), 12f, false);
                    }
                }
            }
        }
    }

    public class PickupItem3D : MonoBehaviour
    {
        public PickupKind kind;
        private Vector3 startPos;

        private void Start()
        {
            startPos = transform.position;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
            transform.Rotate(0f, 130f * Time.deltaTime, 0f);
            transform.position = startPos + Vector3.up * (Mathf.Sin(Time.time * 3f) * 0.16f);

            if (FPSPlayerController.Instance != null)
            {
                float d = Vector3.Distance(transform.position, FPSPlayerController.Instance.transform.position);
                if (d < 1.85f)
                {
                    FPSPlayerController.Instance.CollectPickup(kind);
                    Destroy(gameObject);
                }
            }
        }
    }

    public class ShellCasing3D : MonoBehaviour
    {
        private float life = 1.6f;

        private void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f) Destroy(gameObject);
        }
    }
}
