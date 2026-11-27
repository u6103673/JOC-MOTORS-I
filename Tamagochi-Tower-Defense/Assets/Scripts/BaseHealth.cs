using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Vida de la base con 2 vidas. Si se sobrevive "surviveTime" segundos, se supera el nivel.
/// </summary>
public class BaseHealth : MonoBehaviour {
    public static BaseHealth main;

    [SerializeField] private float maxHealth = 5f;
    [SerializeField] private int maxLives = 2;
    [SerializeField, Tooltip("Segundos sin enemigos tras perder una vida")] private float clearPause = 2f;
    [SerializeField, Tooltip("Segundos a sobrevivir para superar el nivel")] private float surviveTime = 300f;

    public float Health { get; private set; }
    public int Lives { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsVictory { get; private set; }
    public bool IsFinished => IsGameOver || IsVictory;

    private float elapsed;
    private float invulnerableUntil;

    private RectTransform fill;
    private Image fillImage;
    private Text label;
    private Text timerText;
    private Text gameOverText;
    private Text victoryText;

    private void Awake() {
        main = this;
        Time.timeScale = 1f;
        Health = maxHealth;
        Lives = maxLives;
        BuildUI();
        Refresh();
    }

    public void TakeDamage(float amount) {
        Debug.Log("TakeDamage amount=" + amount + " Health=" + Health + " Lives=" + Lives);
        if (IsFinished) return;
        if (Time.time < invulnerableUntil) return; // evita daño de enemigos que llegan en el mismo frame

        Health = Mathf.Max(0f, Health - amount);

        if (Health <= 0f) {
            Lives--;
            if (Lives <= 0) {
                GameOver();
            } else {
                // Pierde una vida: se limpia la pantalla, se rellena la barra y se pausa el spawner.
                Health = maxHealth;
                invulnerableUntil = Time.time + clearPause;
                if (EnemySpawner.main != null) EnemySpawner.main.ClearEnemiesAndPause(clearPause);
            }
        }
        Refresh();
    }

    private void GameOver() {
        IsGameOver = true;
        gameOverText.gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    private void Win() {
        IsVictory = true;
        victoryText.gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    private void Update() {
        if (!IsFinished) {
            elapsed += Time.deltaTime;
            if (elapsed >= surviveTime) Win();
            RefreshTimer();
        }

        // Con la partida terminada (derrota o victoria), R reinicia el nivel.
        if (IsFinished && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void Refresh() {
        float ratio = Mathf.Clamp01(Health / maxHealth);
        fill.anchorMax = new Vector2(ratio, 1f);
        fillImage.color = Color.Lerp(new Color(0.9f, 0.15f, 0.15f), new Color(0.25f, 0.85f, 0.3f), ratio);
        label.text = "Vida: " + Mathf.CeilToInt(Health) + " / " + Mathf.CeilToInt(maxHealth)
                   + "   Vidas: " + Lives;
        RefreshTimer();
    }

    private void RefreshTimer() {
        int remaining = Mathf.Max(0, Mathf.CeilToInt(surviveTime - elapsed));
        timerText.text = string.Format("{0}:{1:00}", remaining / 60, remaining % 60);
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

        // temporizador arriba en el centro
        timerText = NewText("Timer", canvasGo.transform, 48, TextAnchor.UpperCenter);
        RectTransform tr = timerText.rectTransform;
        tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -30f);
        tr.sizeDelta = new Vector2(300f, 70f);

        gameOverText = NewText("GameOver", canvasGo.transform, 90, TextAnchor.MiddleCenter);
        StretchFull(gameOverText.rectTransform);
        gameOverText.color = new Color(1f, 0.25f, 0.25f);
        gameOverText.text = "GAME OVER\n<size=40>Pulsa R para reiniciar</size>";
        gameOverText.gameObject.SetActive(false);

        victoryText = NewText("Victory", canvasGo.transform, 90, TextAnchor.MiddleCenter);
        StretchFull(victoryText.rectTransform);
        victoryText.color = new Color(0.3f, 1f, 0.4f);
        victoryText.text = "NIVEL SUPERADO\n<size=40>Pulsa R para reiniciar</size>";
        victoryText.gameObject.SetActive(false);
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