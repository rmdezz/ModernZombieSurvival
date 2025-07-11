using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class EnemySpawner : MonoBehaviour
{
    /* ════════════════════════════════
     *  1.  Definición de la Oleada
     * ════════════════════════════════*/
    [System.Serializable]
    public class Wave
    {
        public string name = "Wave";
        [Min(1)] public int waveSpawnPointsBudget = 10;               // Presupuesto total de puntos
        public List<GameObject> possibleEnemyPrefabs = new();         // Prefabs permitidos
        [Min(0f)] public float timeBetweenIndividualSpawns = 1f;      // Retardo entre spawns
    }

    /* ════════════════════════════════
     *  2.  Referencias principales
     * ════════════════════════════════*/
    [Header("References")]
    [SerializeField] private Transform spawnPointsContainer;

    /* ════════════════════════════════
     *  3.  Configuración de Oleadas
     * ════════════════════════════════*/
    [Header("Wave Settings")]
    [Tooltip("Oleadas de diseño fijo antes de pasar a las infinitas.")]
    [SerializeField] private Wave[] preDefinedWaves;

    [Tooltip("¿Seguir con oleadas infinitas después de las predefinidas?")]
    [SerializeField] private bool infiniteWaves = true;

    [Tooltip("Incremento de presupuesto por cada oleada infinita adicional.")]
    [SerializeField] private int infiniteWaveBudgetIncrease = 5;

    [Tooltip("Tiempo de descanso entre oleadas.")]
    [SerializeField] private float timeBetweenWaves = 5f;

    /* ════════════════════════════════
     *  4.  UI (opcional)
     * ════════════════════════════════*/
    [Header("UI (optional)")]
    [SerializeField] private TextMeshProUGUI waveTextElement;
    [SerializeField] private TextMeshProUGUI enemiesRemainingTextElement;

    /* ════════════════════════════════
     *  5.  Estado interno
     * ════════════════════════════════*/
    private readonly List<Transform> activeSpawnPoints = new();
    private Coroutine waveSpawnCoroutine;
    private int currentWaveIndex;                  // 0-based
    private int enemiesAliveFromCurrentWave;

    /* ════════════════════════════════
     *  6.  Ciclo de vida
     * ════════════════════════════════*/
    private void Awake()
    {
        /* Validaciones básicas */
        if (spawnPointsContainer == null)
        {
            Debug.LogError("EnemySpawner -> Falta asignar SpawnPointsContainer.", this);
            enabled = false; return;
        }

        if (preDefinedWaves == null || preDefinedWaves.Length == 0)
        {
            Debug.LogError("EnemySpawner -> No hay oleadas predefinidas.", this);
            enabled = false; return;
        }

        for (int i = 0; i < preDefinedWaves.Length; i++)
        {
            if (preDefinedWaves[i].possibleEnemyPrefabs == null ||
                preDefinedWaves[i].possibleEnemyPrefabs.Count == 0)
            {
                Debug.LogError($"EnemySpawner -> La oleada {i + 1} no tiene prefabs asignados.", this);
                enabled = false; return;
            }
        }

        PopulateSpawnPointsFromContainer();
        if (activeSpawnPoints.Count == 0)
        {
            Debug.LogError("EnemySpawner -> No se hallaron puntos de spawn activos.", this);
            enabled = false; return;
        }
    }

    private void Start()
    {
        if (enabled)
            waveSpawnCoroutine = StartCoroutine(WaveSpawnRoutine());
    }

    private void OnDisable()
    {
        if (waveSpawnCoroutine != null)
            StopCoroutine(waveSpawnCoroutine);
    }

    /* ════════════════════════════════
     *  7.  Utilidades de preparación
     * ════════════════════════════════*/
    private void PopulateSpawnPointsFromContainer()
    {
        activeSpawnPoints.Clear();
        foreach (Transform child in spawnPointsContainer)
            if (child.gameObject.activeSelf)
                activeSpawnPoints.Add(child);
    }

    /* ════════════════════════════════
     *  8.  Rutina principal de oleadas
     * ════════════════════════════════*/
    private IEnumerator WaveSpawnRoutine()
    {
        Debug.Log("EnemySpawner arrancó.");
        yield return new WaitForSeconds(timeBetweenWaves);

        while (true)
        {
            /* ── 8.1 Seleccionar la oleada ── */
            Wave currentWave;
            if (currentWaveIndex < preDefinedWaves.Length)
            {
                currentWave = preDefinedWaves[currentWaveIndex];
            }
            else
            {
                if (!infiniteWaves) break;            // Fin del juego
                currentWave = GenerateInfiniteWave();
            }

            /* ── 8.2 Generar lista de enemigos ── */
            List<GameObject> enemiesToSpawn = GenerateEnemyListForWave(currentWave);
            enemiesAliveFromCurrentWave = enemiesToSpawn.Count;
            UpdateWaveUI();

            Debug.Log(
                $"-- Oleada {currentWaveIndex + 1} iniciada: {enemiesAliveFromCurrentWave} enemigos (presupuesto {currentWave.waveSpawnPointsBudget}) --");

            /* ── 8.3 Spawnear enemigos ── */
            foreach (GameObject prefab in enemiesToSpawn)
            {
                SpawnSingleEnemy(prefab);
                yield return new WaitForSeconds(currentWave.timeBetweenIndividualSpawns);
            }

            /* ── 8.4 Esperar a que mueran ── */
            while (enemiesAliveFromCurrentWave > 0)
                yield return null;

            Debug.Log($"-- Oleada {currentWaveIndex + 1} completada --");

            /* Preparar siguiente vuelta */
            currentWaveIndex++;
            UpdateWaveUI(betweenWaves: true);
            yield return new WaitForSeconds(timeBetweenWaves);
        }

        /* Fin definitivo */
        Debug.Log("¡Todas las oleadas finalizadas!");
        UpdateWaveUI(allWavesCompleted: true);
        enabled = false;
    }

    /* ════════════════════════════════
     *  9.  Generar lista según presupuesto
     * ════════════════════════════════*/
    private List<GameObject> GenerateEnemyListForWave(Wave wave)
    {
        var list = new List<GameObject>();
        int budget = wave.waveSpawnPointsBudget;

        /* Prefabs con su costo */
        var prefabsWithCost = wave.possibleEnemyPrefabs
            .Where(p => p != null && p.GetComponent<EnemyInitializer>() != null)
            .Select(p => (prefab: p, cost: p.GetComponent<EnemyInitializer>().GetSpawnCost()))
            .Where(t => t.cost > 0)
            .OrderByDescending(t => t.cost)   // Caros primero para “gastar” rápido
            .ToArray();

        if (prefabsWithCost.Length == 0)
        {
            Debug.LogWarning("Wave sin prefabs válidos.");
            return list;
        }

        int cheapest = prefabsWithCost.Min(t => t.cost);

        /* Gasta el presupuesto */
        while (budget >= cheapest)
        {
            var affordable = prefabsWithCost.Where(t => t.cost <= budget).ToArray();
            if (affordable.Length == 0) break;

            var chosen = affordable[Random.Range(0, affordable.Length)];
            list.Add(chosen.prefab);
            budget -= chosen.cost;
        }

        /* Barajar para variedad */
        return list.OrderBy(_ => Random.value).ToList();
    }

    /* ════════════════════════════════
     * 10.  Generar oleada infinita
     * ════════════════════════════════*/
    private Wave GenerateInfiniteWave()
    {
        int wavesAfterPredefined = currentWaveIndex - preDefinedWaves.Length + 1;
        int baseBudget = preDefinedWaves[^1].waveSpawnPointsBudget;
        int budget = baseBudget + wavesAfterPredefined * infiniteWaveBudgetIncrease;

        return new Wave
        {
            name = $"Oleada Infinita {wavesAfterPredefined + 1}",
            waveSpawnPointsBudget = budget,
            possibleEnemyPrefabs = preDefinedWaves[^1].possibleEnemyPrefabs,
            timeBetweenIndividualSpawns = Mathf.Max(
                0.25f,
                preDefinedWaves[^1].timeBetweenIndividualSpawns * 0.95f)  // Spawn un poco más rápido
        };
    }

    /* ════════════════════════════════
     * 11.  Spawneo individual
     * ════════════════════════════════*/
    private void SpawnSingleEnemy(GameObject prefab)
    {
        if (activeSpawnPoints.Count == 0) return;

        Transform point = activeSpawnPoints[Random.Range(0, activeSpawnPoints.Count)];
        GameObject enemy = Instantiate(prefab, point.position, point.rotation);
        enemy.name = $"{prefab.name}_W{currentWaveIndex + 1}_{Time.frameCount}";

        if (!enemy.activeSelf) enemy.SetActive(true);

        if (enemy.TryGetComponent(out HealthManager hm))
        {
            hm.OnDied += HandleEnemyDeathInWave;
        }
        else
        {
            Debug.LogWarning($"El prefab {enemy.name} no tiene HealthManager.");
            enemiesAliveFromCurrentWave = Mathf.Max(0, enemiesAliveFromCurrentWave - 1);
            UpdateWaveUI();
        }
    }

    /* ════════════════════════════════
     * 12.  Callback de muerte
     * ════════════════════════════════*/
    private void HandleEnemyDeathInWave(HealthManager hm)
    {
        hm.OnDied -= HandleEnemyDeathInWave;                 // Limpieza
        enemiesAliveFromCurrentWave = Mathf.Max(0, enemiesAliveFromCurrentWave - 1);
        UpdateWaveUI();

        /* Ejemplo de recompensa en moneda */
        if (hm != null && hm.GetAssociatedEnemyData() is { } data && CurrencyManager.Instance != null)
            CurrencyManager.Instance.AddCurrency(data.currencyAwardedOnDeath);
    }

    /* ════════════════════════════════
     * 13.  UI helper
     * ════════════════════════════════*/
    private void UpdateWaveUI(bool betweenWaves = false, bool allWavesCompleted = false)
    {
        if (allWavesCompleted)
        {
            if (waveTextElement) waveTextElement.text = "¡TODAS LAS OLEADAS SUPERADAS!";
            if (enemiesRemainingTextElement) enemiesRemainingTextElement.text = "";
            return;
        }

        if (betweenWaves)
        {
            if (waveTextElement) waveTextElement.text = $"Preparando Oleada {currentWaveIndex + 1}";
            if (enemiesRemainingTextElement) enemiesRemainingTextElement.text = "¡Prepárate!";
            return;
        }

        if (waveTextElement)
        {
            string total = infiniteWaves ? "∞" : preDefinedWaves.Length.ToString();
            waveTextElement.text = $"Oleada {currentWaveIndex + 1} / {total}";
        }

        if (enemiesRemainingTextElement)
            enemiesRemainingTextElement.text = $"Enemigos Restantes: {enemiesAliveFromCurrentWave}";
    }
}
