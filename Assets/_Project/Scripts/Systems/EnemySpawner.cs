using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnEntry
    {
        [Tooltip("Prefab del enemigo a spawnear.")]
        public GameObject enemyPrefab;
        [Tooltip("Cuántos de ESTE tipo de enemigo spawnear en esta oleada.")]
        [Min(1)]
        public int count = 1;
    }

    [System.Serializable]
    public class Wave
    {
        public string name;
        [Tooltip("Lista de los tipos de enemigos y cuántos de cada uno para esta oleada.")]
        public List<EnemySpawnEntry> enemiesInWave;
        [Tooltip("Tiempo entre cada enemigo individual spawneado DENTRO de esta oleada.")]
        public float timeBetweenIndividualSpawns = 1f;
    }

    [Header("References")]
    [Tooltip("Prefab de enemigo por defecto si una entrada de oleada no tiene uno asignado (opcional).")]
    [SerializeField] private GameObject defaultEnemyPrefab;
    [Tooltip("El GameObject padre que contiene todos los Transforms de los puntos de spawn.")]
    [SerializeField] private Transform spawnPointsContainer;

    [Header("Wave Settings")]
    [Tooltip("Define las oleadas del juego.")]
    [SerializeField] private Wave[] waves;
    [Tooltip("Tiempo en segundos de descanso entre el final de una oleada y el inicio de la siguiente.")]
    [SerializeField] private float timeBetweenWaves = 5f;
    private int currentWaveIndex = 0;

    private int enemiesAliveFromCurrentWave;

    [Header("UI Feedback (Opcional)")]
    [SerializeField] private TextMeshProUGUI waveTextElement;
    [SerializeField] private TextMeshProUGUI enemiesRemainingTextElement;

    private List<Transform> activeSpawnPoints = new List<Transform>();
    private Coroutine waveSpawnCoroutine;

    void Awake()
    {
        if (spawnPointsContainer == null) { Debug.LogError("ERROR: Spawn Points Container no asignado!", this); enabled = false; return; }
        if (waves == null || waves.Length == 0) { Debug.LogError("ERROR: No hay oleadas definidas!", this); enabled = false; return; }

        bool hasValidWaveData = false;
        foreach (var wave in waves)
        {
            if (wave.enemiesInWave != null && wave.enemiesInWave.Count > 0)
            {
                foreach (var entry in wave.enemiesInWave)
                {
                    if (entry.enemyPrefab != null)
                    {
                        hasValidWaveData = true; break;
                    }
                }
            }
            if (hasValidWaveData) break;
        }
        if (!hasValidWaveData && defaultEnemyPrefab == null)
        {
            Debug.LogError("ERROR: Ninguna oleada tiene enemigos definidos Y no hay Default Enemy Prefab!", this);
            enabled = false; return;
        }

        if (waveTextElement == null) Debug.LogWarning("Wave Text Element no asignado.", this);
        if (enemiesRemainingTextElement == null) Debug.LogWarning("Enemies Remaining Text Element no asignado.", this);

        PopulateSpawnPointsFromContainer();
        if (activeSpawnPoints.Count == 0)
        {
            Debug.LogError("ERROR: No hay puntos de spawn activos encontrados!", this);
            enabled = false;
        }
    }

    void Start()
    {
        if (enabled)
            waveSpawnCoroutine = StartCoroutine(WaveSpawnRoutine());
    }

    void OnDisable()
    {
        if (waveSpawnCoroutine != null)
            StopCoroutine(waveSpawnCoroutine);
    }

    private void PopulateSpawnPointsFromContainer()
    {
        activeSpawnPoints.Clear();
        foreach (Transform child in spawnPointsContainer)
            if (child.gameObject.activeSelf)
                activeSpawnPoints.Add(child);
        Debug.Log($"Encontrados {activeSpawnPoints.Count} puntos de spawn activos.");
    }

    // ...existing code hasta donde encuentres el while loop...

    private IEnumerator WaveSpawnRoutine()
    {
        Debug.Log("Enemy Spawner (Wave System con Mix de Enemigos y Recompensas) iniciado.");
        yield return new WaitForSeconds(timeBetweenWaves);

        while (currentWaveIndex < waves.Length)
        {
            Wave currentWave = waves[currentWaveIndex];
            enemiesAliveFromCurrentWave = 0;

            int totalEnemiesThisWave = 0;
            foreach (EnemySpawnEntry entry in currentWave.enemiesInWave)
            {
                totalEnemiesThisWave += entry.count;
            }
            enemiesAliveFromCurrentWave = totalEnemiesThisWave;
            UpdateWaveUI();

            Debug.Log($"-- Iniciando Oleada {currentWaveIndex + 1} {(string.IsNullOrEmpty(currentWave.name) ? "" : $": {currentWave.name}")} con {totalEnemiesThisWave} enemigos en total --");

            foreach (EnemySpawnEntry entry in currentWave.enemiesInWave)
            {
                GameObject prefabToUse = entry.enemyPrefab != null ? entry.enemyPrefab : defaultEnemyPrefab;
                if (prefabToUse == null)
                {
                    Debug.LogWarning($"No hay prefab para una entrada en la oleada {currentWave.name}, saltando {entry.count} enemigos.");
                    enemiesAliveFromCurrentWave -= entry.count;
                    UpdateWaveUI();
                    continue;
                }

                for (int i = 0; i < entry.count; i++)
                {
                    SpawnSingleEnemy(prefabToUse);
                    bool isLastEnemyOverall = (enemiesAliveFromCurrentWave <= 1 && i == entry.count - 1);

                    if (!isLastEnemyOverall)
                    {
                        yield return new WaitForSeconds(currentWave.timeBetweenIndividualSpawns);
                    }
                }
            }

            // Esperar a que termine la oleada
            while (enemiesAliveFromCurrentWave > 0)
            {
                yield return null;
            }

            Debug.Log($"-- Oleada {currentWaveIndex + 1} completada --");

            // NUEVO: Teletransportar a la tienda al final de la oleada
            ShopManager shopManager = ShopManager.Instance;
            if (shopManager != null)
            {
                shopManager.TeleportToShop();
                Debug.Log("Jugador teletransportado a la tienda!");
            }
            else
            {
                Debug.LogError("ShopManager no encontrado! No se puede teletransportar a la tienda.");
            }

            // MODIFICADO: Bucle más robusto para esperar salida de tienda
            Debug.Log("Esperando a que el jugador salga de la tienda...");
            bool playerStillInShop = true;
            int timeoutCounter = 0;
            const int maxTimeout = 1200; // 10 minutos timeout (0.5s * 1200 = 600s)

            while (playerStillInShop)
            {
                // Buscar ShopManager en cada iteración por si acaso
                ShopManager currentShopManager = ShopManager.Instance;

                if (currentShopManager == null)
                {
                    Debug.LogError("ShopManager desapareció durante la espera!");
                    break;
                }

                playerStillInShop = currentShopManager.IsInShop();

                if (!playerStillInShop)
                {
                    Debug.Log("¡El jugador ha salido de la tienda! Continuando con la siguiente oleada...");
                    break;
                }

                // Debugging cada 4 segundos (8 iteraciones * 0.5s)
                if (timeoutCounter % 8 == 0)
                {
                    Debug.Log($"Aún esperando salida de tienda... IsInShop = {playerStillInShop}, Contador: {timeoutCounter}");
                }

                timeoutCounter++;
                if (timeoutCounter >= maxTimeout)
                {
                    Debug.LogError("Timeout esperando salida de tienda. Continuando de todas formas...");
                    break;
                }

                yield return new WaitForSeconds(0.5f);
            }

            // ...existing code hasta línea 210...

            Debug.Log("Ya regresó al juego");

            // NUEVO: Verificar que el spawner esté habilitado
            if (!enabled)
            {
                Debug.LogError("EnemySpawner está deshabilitado después de salir de la tienda!");
            }

            // NUEVO: Verificar que la corrutina pueda continuar
            Debug.Log($"Continuando a la siguiente oleada. currentWaveIndex será: {currentWaveIndex + 1}");

            currentWaveIndex++;

            if (currentWaveIndex < waves.Length)
            {
                Debug.Log($"Preparando oleada {currentWaveIndex + 1} de {waves.Length}");
                UpdateWaveUI(true);

                Debug.Log($"Esperando {timeBetweenWaves} segundos antes de la siguiente oleada...");
                yield return new WaitForSeconds(timeBetweenWaves);

                Debug.Log($"¡Iniciando oleada {currentWaveIndex + 1}!");
                // El bucle while debería continuar automáticamente aquí
            }
            else
            {
                Debug.Log("¡Todas las oleadas completadas!");
                UpdateWaveUI(false, true);
                enabled = false;
                yield break;
            }
        }
    }

    private void SpawnSingleEnemy(GameObject enemyPrefabToInstantiate)
    {
        if (activeSpawnPoints.Count == 0) { Debug.LogWarning("No hay puntos de spawn activos."); return; }
        if (enemyPrefabToInstantiate == null) { Debug.LogWarning("Se intentó spawnear un prefab nulo."); return; }

        Transform chosenSpawnPoint = activeSpawnPoints[Random.Range(0, activeSpawnPoints.Count)];
        GameObject newEnemyGO = Instantiate(enemyPrefabToInstantiate, chosenSpawnPoint.position, chosenSpawnPoint.rotation);

        if (!newEnemyGO.activeSelf) newEnemyGO.SetActive(true);
        newEnemyGO.name = enemyPrefabToInstantiate.name + "_Wave" + (currentWaveIndex + 1) + "_" + Time.frameCount;

        HealthManager enemyHealth = newEnemyGO.GetComponent<HealthManager>();
        if (enemyHealth != null)
        {
            // --- MODIFICADO: Asegurarse de que el manejador ahora espera HealthManager ---
            enemyHealth.OnDied += HandleEnemyDeathInWave;
        }
        else
        {
            Debug.LogWarning($"El prefab {newEnemyGO.name} no tiene HealthManager. La oleada podría no progresar.", newEnemyGO);
            enemiesAliveFromCurrentWave = Mathf.Max(0, enemiesAliveFromCurrentWave - 1);
            UpdateWaveUI();
        }
    }

    // --- MODIFICADO: HandleEnemyDeathInWave ahora recibe HealthManager ---
    private void HandleEnemyDeathInWave(HealthManager deceasedEnemyHealthManager)
    {
        enemiesAliveFromCurrentWave = Mathf.Max(0, enemiesAliveFromCurrentWave - 1);
        // Debug.Log($"Un enemigo murió. Vivos en oleada: {enemiesAliveFromCurrentWave}");
        UpdateWaveUI();

        // --- NUEVO: Otorgar moneda al jugador ---
        if (deceasedEnemyHealthManager != null)
        {
            EnemyData data = deceasedEnemyHealthManager.GetAssociatedEnemyData(); // Usar el getter
            if (data != null)
            {
                if (CurrencyManager.Instance != null) // Verificar que el CurrencyManager exista
                {
                    CurrencyManager.Instance.AddCurrency(data.currencyAwardedOnDeath);
                }
                else
                {
                    Debug.LogWarning("CurrencyManager.Instance es nulo. No se pudo añadir moneda.");
                }
            }
            else
            {
                Debug.LogWarning($"Enemigo ({deceasedEnemyHealthManager.gameObject.name}) murió pero no tenía EnemyData para recompensa.");
            }
        }

        // --- NUEVO/IMPORTANTE: Desuscribirse del evento ---
        // Es buena práctica desuscribirse para evitar referencias fantasma si el HealthManager
        // no se destruyera o si el EnemySpawner persistiera de alguna forma.
        if (deceasedEnemyHealthManager != null)
        {
            deceasedEnemyHealthManager.OnDied -= HandleEnemyDeathInWave;
        }
    }

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
            if (waveTextElement) waveTextElement.text = $"Preparando Oleada: {currentWaveIndex + 1}";
            if (enemiesRemainingTextElement) enemiesRemainingTextElement.text = "¡Prepárate!";
        }
        else
        {
            if (waveTextElement) waveTextElement.text = $"Oleada: {currentWaveIndex + 1} / {waves.Length}";
            if (enemiesRemainingTextElement) enemiesRemainingTextElement.text = $"Enemigos Restantes: {enemiesAliveFromCurrentWave}";
        }
    }
}