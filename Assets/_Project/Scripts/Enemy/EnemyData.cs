using UnityEngine;
using System.Collections.Generic;   // Para la lista de drops

/* ════════════════════════════════
 *  Estructura de un posible drop
 * ════════════════════════════════*/
[System.Serializable]
public class LootDrop
{
    [Tooltip("Prefab del pickup que puede soltar este enemigo.")]
    public GameObject itemPrefab;

    [Tooltip("Probabilidad (0-1) de que este item se dropee al morir.")]
    [Range(0f, 1f)]
    public float dropChance = 0.1f;
}

/* ════════════════════════════════
 *  Datos del enemigo
 * ════════════════════════════════*/
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    /* ──────────── Stats ──────────── */
    [Header("Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 3.5f;
    public float angularSpeed = 120f;
    public int attackDamage = 10;
    public float attackRange = 1.5f;
    public float attackRate = 1f;

    /* ────── Apariencia (opcional) ────── */
    [Header("Visuals (Opcional)")]
    public Material enemyMaterial;

    /* ──────────── Recompensa ──────────── */
    [Header("Rewards")]
    [Tooltip("Monedas otorgadas al jugador cuando este enemigo muere.")]
    public int currencyAwardedOnDeath = 10;

    /* ──────────── Spawneo ──────────── */
    [Header("Spawning")]
    [Tooltip("Costo en puntos para el sistema de oleadas.")]
    public int spawnCost = 1;

    /* ──────────── Loot ──────────── */
    [Header("Loot Drops")]
    [Tooltip("Ítems que este enemigo puede dropear al morir.")]
    public List<LootDrop> lootTable = new();   // Lista vacía por defecto
}
