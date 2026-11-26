using UnityEngine;

/// <summary>Obus: vuela en arco hasta el punto objetivo y explota haciendo daño en area.</summary>
public class CannonShell : MonoBehaviour {
    [SerializeField] private Explosion explosionPrefab;
    [SerializeField, Tooltip("Cuanto crece el obus en lo alto del arco (efecto de altura)")] private float arcScale = 0.7f;
    [SerializeField, Range(0f, 1f), Tooltip("Daño que recibe quien esta justo en el borde de la explosion")] private float edgeDamage = 0.4f;

    private Vector2 start;
    private Vector2 end;
    private float duration;
    private float t;
    private float damage;
    private float radius;
    private Vector3 baseScale;

    public void Launch(Vector2 targetPoint, float speed, float damage, float radius) {
        start = transform.position;
        end = targetPoint;
        this.damage = damage;
        this.radius = radius;
        duration = Mathf.Max(0.1f, Vector2.Distance(start, end) / speed);
        baseScale = transform.localScale;
    }

    private void Update() {
        t += Time.deltaTime / duration;
        float k = Mathf.Clamp01(t);

        transform.position = Vector2.Lerp(start, end, k);
        transform.localScale = baseScale * (1f + arcScale * Mathf.Sin(Mathf.PI * k));
        transform.Rotate(0f, 0f, 360f * Time.deltaTime);

        if (t >= 1f) Explode();
    }

    private void Explode() {
        Vector2 center = transform.position;

        var list = EnemyMovement.Active;
        for (int i = 0; i < list.Count; i++) {
            EnemyMovement e = list[i];
            if (e == null || e.IsDead) continue;

            float d = Mathf.Max(0f, Vector2.Distance(center, e.transform.position) - e.HitRadius);
            if (d > radius) continue;

            float falloff = Mathf.Lerp(1f, edgeDamage, d / radius);
            EnemyHealth.Of(e).TakeDamage(damage * falloff);
        }

        if (explosionPrefab != null) {
            Explosion fx = Instantiate(explosionPrefab, center, Quaternion.identity);
            fx.Play(radius);
        }

        Destroy(gameObject);
    }
}
