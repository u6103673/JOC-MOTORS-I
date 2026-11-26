using UnityEngine;

/// <summary>
/// Base de todas las torres: busca objetivo (el enemigo mas avanzado dentro del alcance),
/// gira la torreta hacia el y llama a Tick() cada frame. Cada tipo de torre solo implementa
/// como ataca.
/// </summary>
public abstract class Tower : MonoBehaviour {
    [Header("Info (para la tienda / colocacion)")]
    public string displayName = "Torre";
    public int cost = 100;

    [Header("Puntería")]
    [SerializeField] protected float turnSpeed = 540f;
    [SerializeField, Tooltip("Grados de error permitidos para empezar a disparar")]
    protected float aimTolerance = 8f;

    [Header("Referencias")]
    [SerializeField] protected Transform turret;
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected GameObject muzzleFlash;

    /// <summary>Alcance de la torre en unidades de mundo (1 casilla = 1 unidad).</summary>
    public abstract float Range { get; }

    protected EnemyMovement target;
    protected float aimError = 180f;
    protected bool IsAimed => aimError <= aimTolerance;

    private float retargetTimer;
    private Vector3 turretRest;
    private float recoil;
    private float flashOffTime;

    protected virtual void Awake() {
        if (turret == null) turret = transform;
        if (firePoint == null) firePoint = turret;
        turretRest = turret.localPosition;
        if (muzzleFlash != null) muzzleFlash.SetActive(false);
    }

    protected virtual void Update() {
        float dt = Time.deltaTime;

        retargetTimer -= dt;
        if (!IsValidTarget(target) || retargetTimer <= 0f) {
            target = FindTarget();
            retargetTimer = 0.2f;
        }

        if (target != null) {
            aimError = AimAt(GetAimPoint(target), dt);
        } else {
            aimError = 180f;
        }

        Tick(dt);

        // retroceso al disparar
        recoil = Mathf.MoveTowards(recoil, 0f, dt * 0.6f);
        turret.localPosition = turretRest - turret.right * recoil;

        if (muzzleFlash != null && muzzleFlash.activeSelf && Time.time >= flashOffTime) {
            muzzleFlash.SetActive(false);
        }
    }

    /// <summary>Logica de ataque de cada torre. Se llama cada frame.</summary>
    protected abstract void Tick(float dt);

    /// <summary>Punto al que apunta la torreta. Por defecto, la posicion actual del enemigo.</summary>
    protected virtual Vector2 GetAimPoint(EnemyMovement enemy) {
        return enemy.transform.position;
    }

    protected virtual EnemyMovement FindTarget() {
        EnemyMovement best = null;
        float bestScore = float.MinValue;
        Vector2 pos = transform.position;
        float r = Range;
        var list = EnemyMovement.Active;

        for (int i = 0; i < list.Count; i++) {
            EnemyMovement e = list[i];
            if (e == null || e.IsDead) continue;
            if (Vector2.Distance(pos, e.transform.position) > r + e.HitRadius) continue;

            float score = e.PathProgress; // el que va mas adelantado
            if (score > bestScore) {
                bestScore = score;
                best = e;
            }
        }
        return best;
    }

    protected bool IsValidTarget(EnemyMovement e) {
        if (e == null || e.IsDead || !e.isActiveAndEnabled) return false;
        return Vector2.Distance(transform.position, e.transform.position) <= Range + e.HitRadius;
    }

    /// <summary>Gira la torreta hacia el punto y devuelve el error de angulo en grados.</summary>
    protected float AimAt(Vector2 point, float dt) {
        Vector2 dir = point - (Vector2)turret.position;
        float desired = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float current = turret.eulerAngles.z;
        float next = Mathf.MoveTowardsAngle(current, desired, turnSpeed * dt);
        turret.rotation = Quaternion.Euler(0f, 0f, next);
        return Mathf.Abs(Mathf.DeltaAngle(next, desired));
    }

    protected void Kick(float amount) {
        recoil = Mathf.Max(recoil, amount);
    }

    protected void ShowMuzzleFlash(float duration = 0.05f) {
        if (muzzleFlash == null) return;
        muzzleFlash.transform.localScale = Vector3.one * Random.Range(0.8f, 1.25f);
        muzzleFlash.SetActive(true);
        flashOffTime = Time.time + duration;
    }

    protected static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b) {
        Vector2 ab = b - a;
        float len2 = ab.sqrMagnitude;
        if (len2 < 0.0001f) return Vector2.Distance(p, a);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
        return Vector2.Distance(p, a + ab * t);
    }

    protected virtual void OnDrawGizmosSelected() {
        Gizmos.color = new Color(1f, 1f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, Range);
    }
}
