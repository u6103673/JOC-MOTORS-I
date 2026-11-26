using UnityEngine;

/// <summary>
/// Laser: rayo continuo que atraviesa a todos los enemigos de la linea.
/// Cuanto mas tiempo lleva disparando sin parar, mas se calienta y mas daño hace.
/// Tras "overheatAfter" segundos disparando seguidos se sobrecalienta y queda
/// inutilizable durante "cooldownDuration" segundos.
/// </summary>
public class LaserTower : Tower {
    [Header("Laser")]
    [SerializeField] private float range = 5f;
    [SerializeField, Tooltip("Daño por segundo recien encendido")] private float minDps = 12f;
    [SerializeField, Tooltip("Daño por segundo con el laser al maximo")] private float maxDps = 60f;
    [SerializeField, Tooltip("Segundos de disparo continuo hasta el maximo daño")] private float rampTime = 2.5f;
    [SerializeField] private float beamWidth = 0.09f;
    [SerializeField] private float fireAngleTolerance = 2.5f;
    [SerializeField] private Color coolColor = new Color(0.2f, 0.95f, 1f);
    [SerializeField] private Color hotColor = new Color(1f, 0.35f, 0.6f);
    [SerializeField, Tooltip("Material tipo Sprites/Default")] private Material beamMaterial;

    [Header("Sobrecalentamiento")]
    [SerializeField, Tooltip("Segundos disparando seguidos hasta sobrecalentarse")] private float overheatAfter = 5f;
    [SerializeField, Tooltip("Segundos que tarda en enfriarse del todo")] private float cooldownDuration = 3f;

    public override float Range => range;

    private LineRenderer core;
    private LineRenderer glow;
    private SpriteRenderer turretSprite;
    private float heat;        // 0..1, sube el daño
    private float firingTime;  // segundos de disparo acumulados (0..overheatAfter)
    private bool overheated;
    private float cooldownLeft;

    protected override void Awake() {
        base.Awake();
        if (beamMaterial == null) beamMaterial = new Material(Shader.Find("Sprites/Default"));
        glow = CreateLine("BeamGlow", 9);
        core = CreateLine("BeamCore", 10);
        turretSprite = turret.GetComponent<SpriteRenderer>();
    }

    private LineRenderer CreateLine(string lineName, int order) {
        var go = new GameObject(lineName);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial = beamMaterial;
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.numCapVertices = 6;
        lr.sortingOrder = order;
        lr.enabled = false;
        return lr;
    }

    protected override void Tick(float dt) {
        // ---- sobrecalentado: apagado hasta que se enfrie ----
        if (overheated) {
            cooldownLeft -= dt;
            heat = 0f;
            HideBeam();
            if (cooldownLeft <= 0f) {
                overheated = false;
                firingTime = 0f;
            }
            UpdateTint();
            return;
        }

        bool firing = target != null && aimError <= fireAngleTolerance;
        int hits = 0;

        if (firing) {
            Vector2 start = firePoint.position;
            Vector2 dir = turret.right;
            Vector2 end = start + dir * range;
            float dps = Mathf.Lerp(minDps, maxDps, heat);

            var list = EnemyMovement.Active;
            for (int i = 0; i < list.Count; i++) {
                EnemyMovement e = list[i];
                if (e == null || e.IsDead) continue;

                float d = DistanceToSegment(e.transform.position, start, end);
                if (d <= e.HitRadius + beamWidth * 0.5f) {
                    EnemyHealth.Of(e).TakeDamage(dps * dt, false);
                    hits++;
                }
            }

            DrawBeam(start, end);
        } else {
            HideBeam();
        }

        // Se calienta mientras golpea; se enfria rapido si no.
        if (hits > 0) heat = Mathf.Min(1f, heat + dt / rampTime);
        else heat = Mathf.Max(0f, heat - dt / rampTime * 2f);

        // Sobrecalentamiento: acumula mientras dispara, se enfria despacio si no.
        if (firing) {
            firingTime += dt;
            if (firingTime >= overheatAfter) {
                overheated = true;
                cooldownLeft = cooldownDuration;
                HideBeam();
            }
        } else {
            firingTime = Mathf.Max(0f, firingTime - dt * overheatAfter / cooldownDuration);
        }

        UpdateTint();
    }

    private void UpdateTint() {
        if (turretSprite == null) return;

        Color c;
        if (overheated) {
            // rojo parpadeante que se va apagando segun se enfria
            float level = Mathf.Clamp01(cooldownLeft / cooldownDuration);
            float blink = 0.5f + 0.5f * Mathf.Sin(Time.time * 14f);
            Color hot = Color.Lerp(new Color(1f, 0.25f, 0.2f), new Color(1f, 0.6f, 0.3f), blink);
            c = Color.Lerp(Color.white, hot, level);
        } else {
            c = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.4f), firingTime / overheatAfter);
        }
        turretSprite.color = c;
    }

    private void HideBeam() {
        if (core != null) core.enabled = false;
        if (glow != null) glow.enabled = false;
    }

    private void DrawBeam(Vector2 start, Vector2 end) {
        Color c = Color.Lerp(coolColor, hotColor, heat);
        float pulse = 1f + 0.1f * Mathf.Sin(Time.time * 45f);
        float w = beamWidth * (0.7f + 0.6f * heat) * pulse;

        Vector3 a = new Vector3(start.x, start.y, 0f);
        Vector3 b = new Vector3(end.x, end.y, 0f);

        glow.enabled = true;
        glow.SetPosition(0, a);
        glow.SetPosition(1, b);
        glow.startWidth = glow.endWidth = w * 3.2f;
        Color g = c; g.a = 0.3f;
        glow.startColor = glow.endColor = g;

        core.enabled = true;
        core.SetPosition(0, a);
        core.SetPosition(1, b);
        core.startWidth = core.endWidth = w;
        core.startColor = core.endColor = Color.Lerp(c, Color.white, 0.75f);
    }

    private void OnDisable() {
        HideBeam();
    }
}
