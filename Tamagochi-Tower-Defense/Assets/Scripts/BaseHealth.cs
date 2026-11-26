using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Vida de la base. Cada enemigo que llega al final del camino le quita vida.
/// Dibuja sola una barra arriba a la izquierda; basta con poner este script en un objeto vacio.
/// </summary>
public class BaseHealth : MonoBehaviour {
    public static BaseHealth main;

    [SerializeField] private float maxHealth = 100f;

    public float Health { get; private set; }
    public bool IsGameOver { get; private set; }

    private RectTransform fill;
    private Image fillImage;
    private Text label;
    private Text gameOverText;

    private void Awake() {
        main = this;
        Time.timeScale = 1f;
        Health = maxHealth;
        BuildUI();
        Refresh();
    }

    public void TakeDamage(float amount) {
        if (IsGameOver) return;

        Health = Mathf.Max(0f, Health - amount);
        Refresh();

        if (Health <= 0f) GameOver();
    }

    private void GameOver() {
        IsGameOver = true;
        gameOverText.gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    private void Update() {
        // Con la partida terminada, R reinicia el nivel.
        if (IsGameOver && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void Refresh() {
        float ratio = Mathf.Clamp01(Health / maxHealth);
        fill.anchorMax = new Vector2(ratio, 1f);
        fillImage.color = Color.Lerp(new Color(0.9f, 0.15f, 0.15f), new Color(0.25f, 0.85f, 0.3f), ratio);
        label.text = "Vida: " + Mathf.CeilToInt(Health) + " / " + Mathf.CeilToInt(maxHealth);
    }

    // ---------------------------------------------------------------- UI

    private void BuildUI() {
        var canvasGo = new GameObject("BaseHealthUI");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // marco + fondo
        RectTransform bg = NewRect("Background", canvasGo.transform);
        bg.anchorMin = bg.anchorMax = bg.pivot = new Vector2(0f, 1f);
        bg.anchoredPosition = new Vector2(30f, -30f);
        bg.sizeDelta = new Vector2(420f, 40f);
        bg.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        // relleno: se estira cambiando el anchor derecho
        fill = NewRect("Fill", bg);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = new Vector2(4f, 4f);
        fill.offsetMax = new Vector2(-4f, -4f);
        fillImage = fill.gameObject.AddComponent<Image>();

        label = NewText("Label", bg, 24, TextAnchor.MiddleCenter);
        StretchFull(label.rectTransform);

        gameOverText = NewText("GameOver", canvasGo.transform, 90, TextAnchor.MiddleCenter);
        StretchFull(gameOverText.rectTransform);
        gameOverText.color = new Color(1f, 0.25f, 0.25f);
        gameOverText.text = "GAME OVER\n<size=40>Pulsa R para reiniciar</size>";
        gameOverText.gameObject.SetActive(false);
    }

    private static RectTransform NewRect(string name, Transform parent) {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Text NewText(string name, Transform parent, int size, TextAnchor anchor) {
        RectTransform rt = NewRect(name, parent);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.alignment = anchor;
        t.color = Color.white;
        t.supportRichText = true;
        t.raycastTarget = false;
        return t;
    }

    private static void StretchFull(RectTransform rt) {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
