using UnityEngine;

namespace DoomHellsAscension
{
    public class Projectile3D : MonoBehaviour
    {
        public Vector3 velocity;
        public float damage;
        public bool fromPlayer;
        public bool isBFG;
        public bool isAcidSpit;
        public float gravity;
        public int acidTickDamage = 4;
        public string sourceName;
        private float life = 5f;
        private float acidDripTimer;

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
            float scale = (!fromPlayer && GameManager.Instance != null && GameManager.Instance.OverdriveActive) ? 0.55f : 1f;
            float dt = Time.deltaTime * scale;
            if (gravity > 0f) velocity.y -= gravity * dt;
            transform.position += velocity * dt;
            if (isAcidSpit && velocity.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(velocity.normalized);
                acidDripTimer -= dt;
                if (acidDripTimer <= 0f && GameManager.Instance != null)
                {
                    acidDripTimer = 0.08f;
                    GameManager.Instance.SpawnBurstParticles(transform.position, new Color(0.32f, 0.98f, 0.14f), 2);
                }
            }
            life -= Time.deltaTime;
            if (life <= 0f || transform.position.y < 0.05f)
            {
                if (isBFG) ExplodeBFG();
                else if (isAcidSpit && GameManager.Instance != null)
                    GameManager.Instance.SpawnBurstParticles(transform.position + Vector3.up * 0.12f, new Color(0.34f, 1.0f, 0.16f), 8);
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
                    if (Vector3.Distance(transform.position, pPos) < (isAcidSpit ? 1.25f : 1.05f))
                    {
                        if (isAcidSpit)
                        {
                            FPSPlayerController.Instance.ApplyAcidSpit(Mathf.RoundToInt(damage), 3.5f, acidTickDamage, sourceName);
                            if (GameManager.Instance != null)
                                GameManager.Instance.SpawnBurstParticles(transform.position, new Color(0.32f, 1.0f, 0.16f), 14);
                        }
                        else
                        {
                            FPSPlayerController.Instance.TakeDamage(Mathf.RoundToInt(damage), sourceName);
                        }
                        Destroy(gameObject);
                        return;
                    }
                }
                // Acid spit splatters cleanly against arena cover blocks/walls too
                if (isAcidSpit && Physics.CheckSphere(transform.position, 0.18f, ~0, QueryTriggerInteraction.Ignore))
                {
                    var hits = Physics.OverlapSphere(transform.position, 0.18f);
                    foreach (var h in hits)
                    {
                        if (h.GetComponent<FPSPlayerController>() != null || h.GetComponentInParent<DemonEnemyAI>() != null) continue;
                        if (GameManager.Instance != null)
                            GameManager.Instance.SpawnBurstParticles(transform.position, new Color(0.34f, 1.0f, 0.16f), 10);
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
