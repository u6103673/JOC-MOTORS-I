using UnityEngine;

/// <summary>Bala recta: golpea al primer enemigo que toca y desaparece.</summary>
public class Projectile : MonoBehaviour {
    private Vector2 direction;
    private float speed;
    private float damage;
    private float maxDistance;
    private float travelled;

    public void Launch(Vector2 dir, float speed, float damage, float maxDistance) {
        this.direction = dir.normalized;
        this.speed = speed;
        this.damage = damage;
        this.maxDistance = maxDistance;
    }

    private void Update() {
        float step = speed * Time.deltaTime;
        Vector2 from = transform.position;
        Vector2 to = from + direction * step;

        // Se comprueba todo el tramo recorrido, asi una bala rapida no atraviesa enemigos.
        var list = EnemyMovement.Active;
        EnemyMovement hit = null;
        float bestT = float.MaxValue;
        for (int i = 0; i < list.Count; i++) {
            EnemyMovement e = list[i];
            if (e == null || e.IsDead) continue;

            Vector2 c = e.transform.position;
            Vector2 seg = to - from;
            float t = Mathf.Clamp01(Vector2.Dot(c - from, seg) / Mathf.Max(seg.sqrMagnitude, 0.0001f));
            if (Vector2.Distance(c, from + seg * t) <= e.HitRadius && t < bestT) {
                bestT = t;
                hit = e;
            }
        }

        if (hit != null) {
            EnemyHealth.Of(hit).TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        transform.position = to;
        travelled += step;
        if (travelled >= maxDistance) Destroy(gameObject);
    }
}
