using UnityEngine;

/// <summary>Genera un enemigo cada "spawnInterval" segundos en el punto de inicio del nivel.</summary>
public class EnemySpawner : MonoBehaviour {
    [Header("References")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("Attributes")]
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField, Tooltip("0 = infinitos")] private int maxEnemies = 0;

    private float timer;
    private int spawned;

    private void Update() {
        if (maxEnemies > 0 && spawned >= maxEnemies) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval) {
            timer -= spawnInterval;
            Spawn();
        }
    }

    private void Spawn() {
        Vector3 pos = LevelManager.main.startPoint.position;
        Instantiate(enemyPrefab, pos, Quaternion.identity);
        spawned++;
    }
}