using UnityEngine;

public class HealthKit : ShopItem
{
    protected override bool PerformPurchase()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player no encontrado!");
            return false;
        }

        HealthManager playerHealth = player.GetComponent<HealthManager>();
        if (playerHealth == null)
        {
            Debug.LogError("HealthManager del jugador no encontrado!");
            return false;
        }

        if (playerHealth.CurrentHealth >= playerHealth.MaxHealth)
        {
            Debug.Log("Ya tienes la salud al máximo.");
            return false;
        }

        playerHealth.Heal(playerHealth.MaxHealth); // Curar al máximo
        Debug.Log("¡Salud restaurada al máximo!");
        return true;
    }
}