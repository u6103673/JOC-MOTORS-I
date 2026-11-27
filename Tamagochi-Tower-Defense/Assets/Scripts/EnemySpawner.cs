using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemySpawner : MonoBehaviour {
    public static EnemySpawner main;

    [Header("References")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private int baseEnemies = 8;
    [SerializeField] private float enemiesPerSecond = 0.5f;
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField] private float difficultyScalingFactor = 0.75f;

    [Header("Events")]
    public static UnityEvent onEnemyDestroy = new UnityEvent();

    private int currentWave = 1;
    private float timeSinceLastSpawn;
    private int enemiesAlive;
    private int enemiesLeftToSpawn;
    private bool isSpawning = false;
    private float pausedUntil;

    private void Awake() {
        main = this;
        onEnemyDestroy.AddListener(EnemyDestroyed);
    }

    private void OnDestroy() {
        // El evento es estático: sin esto, al reiniciar con R se acumulan listeners de spawners destruidos.
        onEnemyDestroy.RemoveListener(EnemyDestroyed);
        if (main == this) main = null;
    }

    private void Start() {
        StartCoroutine(StartWave());
    }

    private void Update() {
        if (!isSpawning) return;
        if (Time.time < pausedUntil) return; // pausa tras perder una vida

        timeSinceLastSpawn += Time.deltaTime;
        if (timeSinceLastSpawn >= (1f / enemiesPerSecond) && enemiesLeftToSpawn > 0) {
            SpawnEnemy();
            enemiesLeftToSpawn--;
            enemiesAlive++;
            timeSinceLastSpawn = 0f;
        }

        if (enemiesAlive == 0 && enemiesLeftToSpawn == 0) {
            EndWave();
        }
    }

    /// <summary>Elimina todos los enemigos en pantalla y bloquea la generación durante "seconds".</summary>
    public void ClearEnemiesAndPause(float seconds) {
        var list = new List<EnemyMovement>(EnemyMovement.Active);
        foreach (var e in list) {
            e.IsDead = true;          // las torres dejan de apuntarle
            Destroy(e.gameObject);
        }
        enemiesAlive = 0;
        timeSinceLastSpawn = 0f;
        pausedUntil = Time.time + seconds;
    }

    private void EnemyDestroyed() {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
    }

    private IEnumerator StartWave() {
        yield return new WaitForSeconds(timeBetweenWaves);
        isSpawning = true;
        enemiesLeftToSpawn = EnemiesPerWave();
    }

    private void EndWave() {
        isSpawning = false;
        timeSinceLastSpawn = 0f;
        currentWave++;
        StartCoroutine(StartWave());
    }

    private void SpawnEnemy() {
        GameObject prefabToSpawn = enemyPrefabs[0];
        Instantiate(prefabToSpawn, LevelManager.main.startPoint.position, Quaternion.identity);
    }

    private int EnemiesPerWave() {
        return Mathf.RoundToInt(baseEnemies * Mathf.Pow(currentWave, difficultyScalingFactor));
    }
}