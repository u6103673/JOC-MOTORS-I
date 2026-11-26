using UnityEngine;

/// <summary>
/// Lanzallamas: alcance corto, daña a todos los enemigos dentro de un cono
/// y les prende fuego (daño extra durante unos segundos).
/// </summary>
public class FlamethrowerTower : Tower {
    [Header("Lanzallamas")]
    [SerializeField] private float range = 2.3f;
    [SerializeField, Tooltip("Apertura total del cono en grados")] private float coneAngle = 36f;
    [SerializeField, Tooltip("Daño por segundo a todo enemigo dentro del cono")] private float dps = 22f;
    [SerializeField, Tooltip("Daño por segundo de la quemadura")] private float burnDps = 10f;
    [SerializeField, Tooltip("Segundos que dura la quemadura")] private float burnDuration = 3f;
    [SerializeField] private float fireAngleTolerance = 12f;
    [SerializeField, Tooltip("Material tipo Sprites/Default con la textura soft.png")] private Material flameMaterial;

    public override float Range => range;

    private ParticleSystem flames;
    private bool emitting;

    protected override void Awake() {
        base.Awake();
        CreateFlames();
    }

    private void CreateFlames() {
        var go = new GameObject("Flames");
        go.transform.SetParent(firePoint, false);
        flames = go.AddComponent<ParticleSystem>();
        flames.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // hay que parar antes de configurar

        const float life = 0.5f;

        var main = flames.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.8f, life);
        main.startSpeed = new ParticleSystem.MinMaxCurve(range / life * 0.85f, range / life * 1.05f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.maxParticles = 250;

        var emission = flames.emission;
        emission.rateOverTime = 110f;
        emission.enabled = false;

        var shape = flames.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = coneAngle * 0.5f * 0.6f;
        shape.radius = 0.02f;
        shape.rotation = new Vector3(0f, 90f, 0f); // el cono sale por +X en vez de +Z

        var col = flames.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] {
                new GradientColorKey(new Color(1f, 1f, 0.75f), 0f),
                new GradientColorKey(new Color(1f, 0.65f, 0.1f), 0.3f),
                new GradientColorKey(new Color(0.9f, 0.2f, 0.05f), 0.7f),
                new GradientColorKey(new Color(0.25f, 0.2f, 0.2f), 1f)
            },
            new[] {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.85f, 0.3f),
                new GradientAlphaKey(0.55f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = grad;

        var size = flames.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.4f, 1f, 1.7f));

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortingOrder = 9;
        if (flameMaterial == null) flameMaterial = new Material(Shader.Find("Sprites/Default"));
        r.sharedMaterial = flameMaterial;

        flames.Play(); // la emision esta apagada; solo queda "encendido" el sistema
    }

    protected override void Tick(float dt) {
        bool firing = target != null && aimError <= fireAngleTolerance;
        SetEmitting(firing);
        if (!firing) return;

        Vector2 origin = firePoint.position;
        Vector2 forward = turret.right;
        float half = coneAngle * 0.5f;

        var list = EnemyMovement.Active;
        for (int i = 0; i < list.Count; i++) {
            EnemyMovement e = list[i];
            if (e == null || e.IsDead) continue;

            Vector2 to = (Vector2)e.transform.position - origin;
            float dist = to.magnitude;
            if (dist > range + e.HitRadius) continue;

            // el radio del enemigo ensancha un poco el cono
            float allow = dist > e.HitRadius ? Mathf.Asin(e.HitRadius / dist) * Mathf.Rad2Deg : 90f;
            if (Vector2.Angle(forward, to) > half + allow) continue;

            EnemyHealth h = EnemyHealth.Of(e);
            h.TakeDamage(dps * dt, false);
            h.Ignite(burnDps, burnDuration);
        }
    }

    private void SetEmitting(bool value) {
        if (emitting == value) return;
        emitting = value;
        var em = flames.emission;
        em.enabled = value;
    }

    private void OnDisable() {
        if (flames != null) SetEmitting(false);
    }

    protected override void OnDrawGizmosSelected() {
        base.OnDrawGizmosSelected();
        Transform t = turret != null ? turret : transform;
        Vector3 o = firePoint != null ? firePoint.position : transform.position;
        float half = coneAngle * 0.5f;
        Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.9f);
        Gizmos.DrawLine(o, o + Quaternion.Euler(0, 0, half) * t.right * range);
        Gizmos.DrawLine(o, o + Quaternion.Euler(0, 0, -half) * t.right * range);
    }
}
