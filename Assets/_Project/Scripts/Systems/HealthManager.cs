using UnityEngine;
using System;

public class HealthManager : MonoBehaviour
{
    [Header("Default Health Settings (Usado si no se inicializa desde otro script)")]
    [SerializeField] private float initialMaxHealthValue = 100f;
    private float currentHealth;
    private float maxHealth;

    [Header("Audio Feedback (Optional)")]
    [SerializeField] private AudioClip damageSound;
    private AudioSource audioSource; // Para el sonido de daño de esta entidad

    public event Action<HealthManager> OnDied; // Pasa la instancia de este HealthManager
    public event Action<float> OnHealthChanged;

    private EnemyData associatedEnemyData; // Asignado por EnemyInitializer
    private EnemyAI enemyAIComponent;       // --- NUEVO: Referencia a EnemyAI ---

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;
    public EnemyData GetAssociatedEnemyData() => associatedEnemyData;

    void Awake()
    {
        if (maxHealth <= 0)
        {
            SetInitialHealth(initialMaxHealthValue, initialMaxHealthValue);
        }

        audioSource = GetComponent<AudioSource>(); // Este AudioSource es para el 'damageSound'
        if (audioSource == null && damageSound != null)
        {
            Debug.LogWarning($"HealthManager en {gameObject.name} tiene un 'damageSound' pero no AudioSource.", this);
        }

        // --- NUEVO: Obtener referencia a EnemyAI ---
        enemyAIComponent = GetComponent<EnemyAI>();
        // No es un error crítico si no lo tiene, podría ser un objeto destructible sin IA.
        // if (enemyAIComponent == null && GetComponent<NavMeshAgent>() != null) // Si es un agente pero no tiene EnemyAI
        // {
        //     Debug.LogWarning($"HealthManager en {gameObject.name} parece ser un enemigo (tiene NavMeshAgent) pero no tiene EnemyAI para la animación de muerte.", this);
        // }
    }

    public void SetInitialHealth(float health, float maxHealthValue)
    {
        this.maxHealth = Mathf.Max(1f, maxHealthValue);
        this.currentHealth = Mathf.Clamp(health, 0f, this.maxHealth);
        OnHealthChanged?.Invoke(this.currentHealth);
    }

    public void SetEnemyDataReference(EnemyData data)
    {
        associatedEnemyData = data;
    }

    public void TakeDamage(float damageAmount)
    {
        if (IsDead || damageAmount <= 0) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0f);

        if (damageSound != null && audioSource != null && audioSource.isActiveAndEnabled)
        {
            audioSource.PlayOneShot(damageSound, 1.0f);
        }

        OnHealthChanged?.Invoke(currentHealth);

        if (IsDead)
        {
            Die();
        }
    }

    public void Heal(float healAmount)
    {
        if (IsDead || healAmount <= 0) return;
        currentHealth += healAmount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }

    public void FullHeal()
    {
        if (IsDead) return;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }

    private void Die()
    {
        OnDied?.Invoke(this); // Notificar a los suscriptores (como EnemySpawner, CurrencyManager)

        // --- NUEVO: Llamar a la animación de muerte en EnemyAI ANTES de desactivar el GameObject ---
        if (enemyAIComponent != null)
        {
            enemyAIComponent.TriggerDeathSequence(); // Un método que maneja la animación y la desactivación final
        }
        else
        {
            // Si no hay EnemyAI para manejar una animación de muerte, desactivar inmediatamente
            Debug.Log($"{gameObject.name} ha muerto y se desactivará (no hay EnemyAI para animación).");
            gameObject.SetActive(false);
        }
    }
}