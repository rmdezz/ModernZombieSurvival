using UnityEngine;

public class AmmoBox : ShopItem
{
    protected override bool PerformPurchase()
    {
        PlayerShooting playerShooting = FindObjectOfType<PlayerShooting>();
        if (playerShooting == null)
        {
            Debug.LogError("PlayerShooting no encontrado!");
            return false;
        }

        // Recargar todas las armas al máximo
        bool ammoAdded = playerShooting.RefillAllWeaponsAmmo();

        if (ammoAdded)
        {
            Debug.Log("Toda la munición ha sido recargada al máximo!");
            return true;
        }
        else
        {
            Debug.Log("Ya tienes toda la munición al máximo.");
            return false;
        }
    }
}