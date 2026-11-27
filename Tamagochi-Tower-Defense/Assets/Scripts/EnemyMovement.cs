using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour {
    // Registro de enemigos vivos: las torres lo consultan para buscar objetivos.
    public static readonly List<EnemyMovement> Active = new List<EnemyMovement>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() {
        Active.Clear();
    }

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Attributes")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float hitRadius = 0.3f;
    [SerializeField, Tooltip("Vida que le quita a la base si llega al final del camino")] private float damageToBase = 1f;

    private Transform target;
    private int pathIndex = 0;

    /// <summary>Radio (en unidades de mundo) que usan balas y explosiones para golpear al enemigo.</summary>
    public float HitRadius => hitRadius;

    /// <summary>Lo marca EnemyHealth al morir, para que nadie le siga disparando hasta que se destruya.</summary>
    public bool IsDead { get; set; }

    public float MoveSpeed => moveSpeed;

    /// <summary>
    /// Donde estara el enemigo dentro de "time" segundos siguiendo el camino (incluye las curvas).
    /// Lo usa el cañon para no fallar cuando el enemigo gira.
    /// </summary>
    public Vector2 PredictPosition(float time) {
        Vector2 pos = transform.position;
        if (target == null) return pos;

        float remaining = moveSpeed * time;
        Transform[] path = LevelManager.main.path;

        for (int i = pathIndex; i < path.Length; i++) {
            Vector2 next = path[i].position;
            float d = Vector2.Distance(pos, next);
            if (remaining <= d) {
                return d > 0.0001f ? pos + (next - pos) / d * remaining : pos;
            }
            remaining -= d;
            pos = next;
        }
        return pos; // ya habra llegado al final
    }

    /// <summary>Cuanto mas alto, mas cerca esta de la meta. Sirve para elegir "el primero".</summary>
    public float PathProgress {
        get {
            if (target == null) return pathIndex * 1000f;
            return pathIndex * 1000f - Vector2.Distance(transform.position, target.position);
        }
    }

    public Vector2 Velocity {
        get {
            if (rb != null) return rb.linearVelocity;
            if (target == null) return Vector2.zero;
            return ((Vector2)(target.position - transform.position)).normalized * moveSpeed;
        }
    }

    private void OnEnable() {
        Active.Add(this);
    }

    private void OnDisable() {
        Active.Remove(this);
    }

    private void Start() {
        target = LevelManager.main.path[pathIndex];
    }

    private void Update() {
        if (Vector2.Distance(transform.position, target.position) < 0.1f) {
            pathIndex++;

            if (pathIndex >= LevelManager.main.path.Length) {
                // Llego a la meta: quita vida a la base y deja de ser objetivo de las torres.
                IsDead = true;
                if (BaseHealth.main != null) BaseHealth.main.TakeDamage(damageToBase);
                Destroy(gameObject);
                return;
            }

            target = LevelManager.main.path[pathIndex];
        }
    }

    private void FixedUpdate() {
        Vector2 direction = (target.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
    }
}
