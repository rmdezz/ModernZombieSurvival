using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))] // Para sonidos de idle/ataque del enemigo
// [RequireComponent(typeof(Animator))] // El Animator estará en el modelo hijo
public class EnemyAI : MonoBehaviour
{
    [Header("AI Detection Settings")]
    [SerializeField] private float detectionRange = 20f;
    [Tooltip("Con qué frecuencia la IA actualiza su lógica (segundos).")]
    [SerializeField] private float aiUpdateRate = 0.25f;

    [Header("Default Attack Settings (Sobrescritos por EnemyData)")]
    [SerializeField] private int defaultAttackDamage = 10;
    [SerializeField] private float defaultAttackRate = 1f;
    [SerializeField] private float defaultAttackRange = 1.5f;

    private int currentAttackDamage;
    private float currentAttackRate;
    // currentAttackRange se refleja en agent.stoppingDistance

    [Header("Audio Settings")]
    [SerializeField] private List<AudioClip> idleGrunts;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private float minTimeBetweenGrunts = 3f;
    [SerializeField] private float maxTimeBetweenGrunts = 7f;

    [Header("Animation Settings")]
    [Tooltip("Tiempo que tarda la animación de muerte en completarse antes de desactivar el objeto.")]
    [SerializeField] private float deathAnimationDuration = 2f; // Ajusta según tu animación

    private NavMeshAgent agent;
    private Transform playerTransform;
    private HealthManager playerHealthManager;
    private AudioSource enemyAudioSource; // Renombrado para claridad
    private Animator animator;          // --- NUEVO: Referencia al Animator ---

    private float timeSinceLastAttack = 0f;
    private float nextAiUpdateTime = 0f;
    private float nextGruntTime = 0f;
    private bool isDead = false; // --- NUEVO: Para evitar acciones si ya está muriendo ---

    // --- NUEVO: Hashes de parámetros del Animator para eficiencia ---
    private static readonly int AnimSpeedParam = Animator.StringToHash("Speed");
    private static readonly int AnimAttackParam = Animator.StringToHash("Attack");
    private static readonly int AnimDieParam = Animator.StringToHash("Die");
    private static readonly int AnimHitParam = Animator.StringToHash("Hit");

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyAudioSource = GetComponent<AudioSource>(); // AudioSource para gruñidos y ataque del enemigo

        // --- NUEVO: Obtener Animator del modelo hijo ---
        // Asumimos que el modelo con el Animator es un hijo directo o nieto.
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"No se encontró Animator en los hijos de {gameObject.name}. Las animaciones no funcionarán.", this);
            // No desactivamos el script completo, podría seguir funcionando sin animaciones visuales.
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            playerHealthManager = playerObject.GetComponent<HealthManager>();
            if (playerHealthManager == null) Debug.LogError("EnemyAI: Player no tiene HealthManager.", this);
        }
        else
        {
            Debug.LogError("EnemyAI: No se encontró el Player.", this);
            enabled = false; return;
        }

        InitializeAttackParameters(defaultAttackDamage, defaultAttackRate, defaultAttackRange);
        ScheduleNextGrunt();
    }

    public void InitializeAttackParameters(int damage, float rate, float range)
    {
        currentAttackDamage = damage;
        currentAttackRate = rate;
        if (agent != null)
        {
            agent.stoppingDistance = range;
        }
        // Debug.Log($"{gameObject.name} AI params: Dmg={currentAttackDamage}, Rate={currentAttackRate}, Range(StopDist)={range}");
    }

    void Update()
    {
        if (isDead || playerTransform == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        // --- NUEVO: Actualizar parámetro de velocidad del Animator ---
        if (animator != null)
        {
            // Usar la velocidad actual del agente normalizada por su velocidad configurada (si quieres que Speed sea 0-1)
            // o simplemente la magnitud si tu blend tree lo maneja diferente.
            // float normalizedSpeed = agent.velocity.magnitude / agent.speed;
            // animator.SetFloat(AnimSpeedParam, normalizedSpeed);
            // Una forma más simple: si tiene destino y no está en rango de ataque, está moviéndose.
            bool isMoving = agent.hasPath && Vector3.Distance(transform.position, agent.destination) > agent.stoppingDistance + 0.1f;
            animator.SetFloat(AnimSpeedParam, isMoving ? 1f : 0f); // Asume que 0 es idle, >0 es moverse
        }


        if (Time.time < nextAiUpdateTime) return;
        nextAiUpdateTime = Time.time + aiUpdateRate;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= detectionRange)
        {
            agent.SetDestination(playerTransform.position);
            if (distanceToPlayer <= agent.stoppingDistance && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance) // Condición más robusta
            {
                Attack();
            }
        }
        HandleIdleGrunts();
    }

    public void TriggerHitAnimation()
    {
        if (isDead || animator == null) return;

        // Activar el trigger de la animación de reacción al golpe
        animator.SetTrigger(AnimHitParam);
        // Debug.Log($"{gameObject.name} reaccionó a un golpe.");
    }

    void Attack()
    {
        if (isDead || Time.time < timeSinceLastAttack + currentAttackRate) return;

        LookAtPlayer();

        if (Vector3.Distance(transform.position, playerTransform.position) <= agent.stoppingDistance + 0.2f) // Umbral un poco mayor
        {
            // --- NUEVO: Disparar Trigger de Animación de Ataque ---
            if (animator != null)
            {
                animator.SetTrigger(AnimAttackParam);
            }

            // El sonido y el daño podrían retrasarse para coincidir con la animación (usando Animation Events)
            // Por ahora, son inmediatos después de activar el trigger de animación.
            if (attackSound != null && enemyAudioSource != null && !enemyAudioSource.isPlaying)
            {
                enemyAudioSource.PlayOneShot(attackSound);
            }

            if (playerHealthManager != null)
            {
                playerHealthManager.TakeDamage(currentAttackDamage);
            }
            timeSinceLastAttack = Time.time;
        }
    }

    void LookAtPlayer()
    {
        if (isDead) return;
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        if (agent.updateRotation) // Si el agente controla la rotación, dejar que él lo haga
        {
            // NavMeshAgent ya debería rotar hacia el destino (jugador)
        }
        else // Si queremos control manual de rotación (ej. para ataques mientras está quieto)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, agent.angularSpeed * Time.deltaTime * 2f); // *2 para más rapidez
        }
    }

    // --- NUEVO: Método llamado por HealthManager cuando el enemigo muere ---
    public void TriggerDeathSequence()
    {
        if (isDead) return; // Evitar múltiples llamadas
        isDead = true;

        Debug.Log($"{gameObject.name} iniciando secuencia de muerte.");

        if (animator != null)
        {
            animator.SetTrigger(AnimDieParam);
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath(); // Limpiar cualquier ruta pendiente
            agent.enabled = false; // Desactivar completamente el NavMeshAgent
        }

        // Desactivar colliders para que no bloquee al jugador u otros disparos
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        // Si tienes colliders en hijos (hitboxes), también deberías desactivarlos.
        // foreach(Collider childCol in GetComponentsInChildren<Collider>()) childCol.enabled = false;


        // Desactivar este script de IA para que no siga ejecutando lógica
        this.enabled = false;

        // El HealthManager lanzó el evento OnDied, ahora esperamos a que la animación termine
        // y luego el HealthManager (o este script) desactivará el GameObject.
        // Si queremos que este script desactive el GO después de la animación:
        StartCoroutine(DeactivateAfterAnimation());
    }

    private IEnumerator DeactivateAfterAnimation()
    {
        // Esperar la duración de la animación de muerte
        // O esperar a que termine un estado específico del animator
        yield return new WaitForSeconds(deathAnimationDuration);
        Debug.Log($"{gameObject.name} desactivado después de animación de muerte.");
        gameObject.SetActive(false); // Finalmente, desactivar el GameObject
    }


    void HandleIdleGrunts()
    {
        if (isDead || Time.time >= nextGruntTime && enemyAudioSource != null && !enemyAudioSource.isPlaying && idleGrunts.Count > 0)
        {
            int randomIndex = Random.Range(0, idleGrunts.Count);
            enemyAudioSource.PlayOneShot(idleGrunts[randomIndex]);
            ScheduleNextGrunt();
        }
    }

    void ScheduleNextGrunt()
    {
        nextGruntTime = Time.time + Random.Range(minTimeBetweenGrunts, maxTimeBetweenGrunts);
    }

    void OnDrawGizmosSelected()
    {
        // ... (sin cambios significativos, pero asegúrate de que agent no sea null) ...
        if (agent != null) Gizmos.DrawWireSphere(transform.position, agent.stoppingDistance);
        else Gizmos.DrawWireSphere(transform.position, defaultAttackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}