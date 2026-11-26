using UnityEngine;

/// <summary>Efecto visual de explosion: bola de fuego que se expande y se desvanece. Se autodestruye.</summary>
public class Explosion : MonoBehaviour {
    [SerializeField] private float duration = 0.4f;

    private SpriteRenderer sr;
    private float radius = 1f;
    private float t;
    private float spriteWorldSize = 1f;

    private void Awake() {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null) spriteWorldSize = sr.sprite.bounds.size.x;
        transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        Apply(0f);
    }

    public void Play(float radius) {
        this.radius = radius;
        Apply(0f);
    }

    private void Update() {
        t += Time.deltaTime / duration;
        if (t >= 1f) {
            Destroy(gameObject);
            return;
        }
        Apply(t);
    }

    private void Apply(float k) {
        float ease = 1f - Mathf.Pow(1f - k, 3f);
        float diameter = radius * 2f * Mathf.Lerp(0.3f, 1f, ease);
        transform.localScale = Vector3.one * (diameter / spriteWorldSize);

        if (sr != null) {
            Color c = sr.color;
            c.a = 1f - k * k;
            sr.color = c;
        }
    }
}
