using UnityEngine;

/// <summary>Metralleta: mucha cadencia, poco daño por bala, un poco de dispersion.</summary>
public class MachineGunTower : Tower {
    [Header("Metralleta")]
    [SerializeField] private float range = 3.5f;
    [SerializeField] private float damage = 6f;
    [SerializeField, Tooltip("Disparos por segundo")] private float fireRate = 8f;
    [SerializeField] private float bulletSpeed = 14f;
    [SerializeField] private float spreadDegrees = 3f;
    [SerializeField, Tooltip("Separacion de cada uno de los dos cañones")] private float barrelOffset = 0.0625f;
    [SerializeField] private Projectile bulletPrefab;

    public override float Range => range;

    private float cooldown;
    private bool leftBarrel;

    protected override void Tick(float dt) {
        if (cooldown > 0f) cooldown -= dt;
        if (target == null || !IsAimed || cooldown > 0f) return;

        Fire();
        cooldown += 1f / fireRate;
    }

    private void Fire() {
        if (bulletPrefab == null) {
            Debug.LogError($"{name}: falta asignar bulletPrefab", this);
            enabled = false;
            return;
        }

        float angle = turret.eulerAngles.z + Random.Range(-spreadDegrees, spreadDegrees);
        float rad = angle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        leftBarrel = !leftBarrel;
        Vector3 pos = firePoint.position + turret.up * (leftBarrel ? barrelOffset : -barrelOffset);

        Projectile p = Instantiate(bulletPrefab, pos, Quaternion.Euler(0f, 0f, angle));
        p.Launch(dir, bulletSpeed, damage, range + 0.5f);

        Kick(0.04f);
        ShowMuzzleFlash(0.04f);
    }
}
