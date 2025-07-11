using UnityEngine;
using System.Collections.Generic; // Para List

public class LootDropper : MonoBehaviour
{
    // Este script necesita saber qué loot puede dropear. Lo obtendrá del HealthManager.
    private HealthManager healthManager;
    private List<LootDrop> lootTable;

    void Awake()
    {
        healthManager = GetComponent<HealthManager>();
        if (healthManager == null)
        {
            Debug.LogError("LootDropper necesita un HealthManager en el mismo GameObject.", this);
            enabled = false;
            return;
        }

        // Suscribirse al evento de muerte
        healthManager.OnDied += HandleDeath;
    }

    void OnDestroy()
    {
        // Desuscribirse
        if (healthManager != null)
        {
            healthManager.OnDied -= HandleDeath;
        }
    }

    private void HandleDeath(HealthManager hm) // El evento OnDied ahora pasa el HealthManager
    {
        // Obtener la tabla de loot del EnemyData asociado a este HealthManager
        EnemyData data = hm.GetAssociatedEnemyData();
        if (data != null && data.lootTable != null)
        {
            DropLoot(data.lootTable);
        }
    }

    private void DropLoot(List<LootDrop> table)
    {
        foreach (LootDrop drop in table)
        {
            // Tirar un "dado" virtual de 0.0 a 1.0
            float randomValue = Random.Range(0f, 1f);

            // Si nuestro tiro es menor o igual a la probabilidad de drop, el item se dropea
            if (randomValue <= drop.dropChance)
            {
                if (drop.itemPrefab != null)
                {
                    // Dropear el item en la posición actual del enemigo
                    Instantiate(drop.itemPrefab, transform.position, Quaternion.identity);
                    Debug.Log($"Enemigo {gameObject.name} dropeó {drop.itemPrefab.name}");

                    // Nota: Con esta lógica, un enemigo podría dropear MÚLTIPLES items si tienes
                    // varias entradas en la lootTable y tienes suerte. Esto puede ser deseable o no.
                    // Si solo quieres que dropee UN item de la lista, necesitarías una lógica diferente
                    // (ej. sumar todas las probabilidades y elegir uno basado en un solo tiro).
                    // Por ahora, esta lógica es simple y funcional.
                }
            }
        }
    }
}