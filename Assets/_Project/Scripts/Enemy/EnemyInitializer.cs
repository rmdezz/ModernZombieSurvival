using UnityEngine;
using UnityEngine.AI;

public class EnemyInitializer : MonoBehaviour
{
    [Tooltip("ScriptableObject con las estadísticas y parámetros de este enemigo.")]
    [SerializeField] private EnemyData enemyData;

    /* ────────────
     *  Getters públicos
     * ──────────── */
    public EnemyData Data => enemyData;                     // Acceso a los datos
    public int GetSpawnCost() =>                           // Costo para el sistema de oleadas
        enemyData != null ? enemyData.spawnCost : 9999;    // 9999 ⇒ prácticamente “inspawnable”

    /* ────────────
     *  Inicialización
     * ──────────── */
    private void Awake()
    {
        if (enemyData == null)
        {
            Debug.LogError(
                $"[EnemyInitializer] EnemyData no asignado en {gameObject.name}. " +
                "Se usarán valores por defecto de los componentes.",
                this);

            gameObject.name = $"{nameof(EnemyInitializer)}_Default_{Random.Range(1000, 9999)}";
            return;
        }

        /* 1. Componentes a configurar */
        HealthManager hm = GetComponent<HealthManager>();
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        EnemyAI ai = GetComponent<EnemyAI>();
        Renderer rend = GetComponentInChildren<Renderer>();

        /* 2. Salud */
        if (hm != null)
        {
            hm.SetInitialHealth(enemyData.maxHealth, enemyData.maxHealth);
            hm.SetEnemyDataReference(enemyData);
        }
        else
            Debug.LogWarning($"[{name}] sin HealthManager.", this);

        /* 3. Movimiento */
        if (agent != null)
        {
            agent.speed = enemyData.moveSpeed;
            agent.angularSpeed = enemyData.angularSpeed;
        }
        else
            Debug.LogWarning($"[{name}] sin NavMeshAgent.", this);

        /* 4. Ataque  */
        if (ai != null)
        {
            ai.InitializeAttackParameters(
                enemyData.attackDamage,
                enemyData.attackRate,
                enemyData.attackRange);
        }
        else
            Debug.LogWarning($"[{name}] sin EnemyAI.", this);

        /* 5. Material visual (opcional) */
        if (rend != null && enemyData.enemyMaterial != null)
            rend.material = enemyData.enemyMaterial;

        /* 6. Nombre para depurar en la jerarquía */
        gameObject.name = $"{enemyData.name}_Inst_{Random.Range(1000, 9999)}";

        Debug.Log($"[{gameObject.name}] inicializado con {enemyData.name} (SpawnCost={enemyData.spawnCost}).");
    }
}
