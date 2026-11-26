using UnityEngine;

/// <summary>Cañon explosivo: cadencia lenta, obus que explota en area. Calcula donde estara el enemigo.</summary>
public class CannonTower : Tower {
    [Header("Cañon explosivo")]
    [SerializeField] private float range = 5.5f;
    [SerializeField] private float damage = 55f;
    [SerializeField, Tooltip("Radio de la explosion")] private float explosionRadius = 1.3f;
    [SerializeField, Tooltip("Segundos entre disparos")] private float fireInterval = 1.8f;
    [SerializeField] private float shellSpeed = 6f;
    [SerializeField] private CannonShell shellPrefab;

    public override float Range => range;

    private float cooldown;

    protected override Vector2 GetAimPoint(EnemyMovement e) {
        // Calcula donde estara el enemigo cuando llegue el obus, siguiendo el camino (curvas incluidas).
        Vector2 origin = firePoint.position;
        Vector2 predicted = e.transform.position;
        for (int i = 0; i < 3; i++) {
            float flightTime = Mathf.Max(0.1f, Vector2.Distance(origin, predicted) / shellSpeed);
            predicted = e.PredictPosition(flightTime);
        }
        return predicted;
    }

    protected override void Tick(float dt) {
        if (cooldown > 0f) cooldown -= dt;
        if (target == null || !IsAimed || cooldown > 0f) return;

        Fire();
        cooldown = fireInterval;
    }

    private void Fire() {
        if (shellPrefab == null) {
            Debug.LogError($"{name}: falta asignar shellPrefab", this);
            enabled = false;
            return;
        }

        CannonShell shell = Instantiate(shellPrefab, firePoint.position, Quaternion.identity);
        shell.Launch(GetAimPoint(target), shellSpeed, damage, explosionRadius);

        Kick(0.16f);
        ShowMuzzleFlash(0.08f);
    }
}
