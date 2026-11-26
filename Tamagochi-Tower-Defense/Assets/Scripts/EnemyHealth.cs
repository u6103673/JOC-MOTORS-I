using System;
using UnityEngine;

/// <summary>
/// Vida del enemigo. No hace falta ponerla en el prefab: las torres la anaden solas
/// la primera vez que dañan a un enemigo (EnemyHealth.Of). Si quieres otra vida,
/// añadela al prefab del enemigo y cambia maxHealth.
/// </summary>
public class EnemyHealth : MonoBehaviour {
    /// <summary>Se dispara al morir un enemigo (util luego para dar dinero, puntos, etc.).</summary>
    public static event Action<EnemyHealth> Died;

    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private int reward = 10;
    [SerializeField] private bool showHealthBar = true;

    public int Reward => reward;
    public float Health => health;
    public bool IsDead => dead;

    private float health;
    private bool dead;
    private EnemyMovement movement;
    private SpriteRenderer sr;
    private Color baseColor = Color.white;
    private float flashTimer;
    private float burnTimeLeft;
    private float burnDps;

    private Transform barRoot;
    private Transform barFill;
    private const float BarWidth = 0.6f;

    private static Sprite pixelSprite;

    public static EnemyHealth Of(EnemyMovement enemy) {
        if (!enemy.TryGetComponent(out EnemyHealth h)) {
            h = enemy.gameObject.AddComponent<EnemyHealth>();
        }
        return h;
    }

    private void Awake() {
        health = maxHealth;
        movement = GetComponent<EnemyMovement>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    public void TakeDamage(float amount, bool flash = true) {
        if (dead || amount <= 0f) return;

        health -= amount;
        if (flash) flashTimer = 0.07f;
        UpdateBar();

        if (health <= 0f) Die();
    }

    /// <summary>Prende fuego al enemigo: dps de daño durante "duration" segundos (no se acumula, se refresca).</summary>
    public void Ignite(float dps, float duration) {
        if (dead) return;
        burnDps = Mathf.Max(burnDps, dps);
        burnTimeLeft = Mathf.Max(burnTimeLeft, duration);
    }

    private void Update() {
        if (dead) return;

        if (burnTimeLeft > 0f) {
            burnTimeLeft -= Time.deltaTime;
            TakeDamage(burnDps * Time.deltaTime, false);
            if (burnTimeLeft <= 0f) burnDps = 0f;
        }

        if (sr == null) return;

        if (flashTimer > 0f) {
            flashTimer -= Time.deltaTime;
            sr.color = Color.Lerp(baseColor, Color.white, 0.75f);
        } else if (burnTimeLeft > 0f) {
            sr.color = Color.Lerp(baseColor, new Color(1f, 0.55f, 0.1f, baseColor.a), 0.55f);
        } else {
            sr.color = baseColor;
        }
    }

    private void Die() {
        dead = true;
        if (movement != null) movement.IsDead = true;
        Died?.Invoke(this);
        Destroy(gameObject);
    }

    // ---------- barra de vida (aparece solo cuando el enemigo esta herido) ----------

    private void UpdateBar() {
        if (!showHealthBar) return;
        if (barRoot == null) CreateBar();

        barRoot.gameObject.SetActive(true);
        float ratio = Mathf.Clamp01(health / maxHealth);
        barFill.localScale = new Vector3(BarWidth * ratio, 0.07f, 1f);
    }

    private void CreateBar() {
        if (pixelSprite == null) {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            pixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0f, 0.5f), 1f);
        }

        Vector3 s = transform.lossyScale;
        float sx = Mathf.Max(0.01f, Mathf.Abs(s.x));
        float sy = Mathf.Max(0.01f, Mathf.Abs(s.y));

        // La raiz compensa la escala del enemigo, asi la barra mide siempre lo mismo en el mundo.
        var root = new GameObject("HealthBar");
        barRoot = root.transform;
        barRoot.SetParent(transform, false);
        barRoot.localPosition = new Vector3(0f, 0.5f / sy, 0f);
        barRoot.localScale = new Vector3(1f / sx, 1f / sy, 1f);

        var bg = new GameObject("BG").AddComponent<SpriteRenderer>();
        bg.transform.SetParent(barRoot, false);
        bg.sprite = pixelSprite;
        bg.color = new Color(0f, 0f, 0f, 0.65f);
        bg.sortingOrder = 20;
        bg.transform.localPosition = new Vector3(-BarWidth * 0.5f - 0.015f, 0f, 0f);
        bg.transform.localScale = new Vector3(BarWidth + 0.03f, 0.1f, 1f);

        var fill = new GameObject("Fill").AddComponent<SpriteRenderer>();
        fill.transform.SetParent(barRoot, false);
        fill.sprite = pixelSprite;
        fill.color = new Color(0.25f, 0.9f, 0.3f, 1f);
        fill.sortingOrder = 21;
        fill.transform.localPosition = new Vector3(-BarWidth * 0.5f, 0f, 0f);
        barFill = fill.transform;
    }
}
