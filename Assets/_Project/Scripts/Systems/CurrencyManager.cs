using UnityEngine;
using System;

public class CurrencyManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static CurrencyManager Instance { get; private set; }

    [Header("Initial Settings")]
    [SerializeField] private int startingCurrency = 0;

    private int currentPlayerCurrency;

    public event Action<int> OnCurrencyChanged; // Evento para notificar a la UI, etc.

    public int CurrentPlayerCurrency => currentPlayerCurrency;

    void Awake()
    {
        // --- Singleton Setup ---
        if (Instance == null)
        {
            Instance = this;
            // Opcional: si quieres que persista entre cargas de escena
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Debug.LogWarning("CurrencyManager: Ya existe una instancia. Destruyendo esta.", this);
            Destroy(gameObject);
            return; // Importante salir aquí para no ejecutar el resto del Awake en la instancia duplicada
        }

        currentPlayerCurrency = startingCurrency;
    }

    void Start()
    {
        // Notificar el valor inicial a cualquier UI que esté escuchando
        // Es bueno hacerlo en Start para dar tiempo a otras UI a suscribirse en su Awake.
        OnCurrencyChanged?.Invoke(currentPlayerCurrency);
    }

    public void AddCurrency(int amount)
    {
        if (amount <= 0)
        {
            // Debug.LogWarning("Se intentó añadir una cantidad no positiva o cero de moneda.", this); // Podría ser válido querer añadir 0
            return;
        }
        currentPlayerCurrency += amount;
        OnCurrencyChanged?.Invoke(currentPlayerCurrency);
        Debug.Log($"Moneda añadida: {amount}. Total actual: {currentPlayerCurrency}");
    }

    public int GetCurrentCurrency()
    {
        return currentPlayerCurrency;
    }

    public bool SpendCurrency(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("Se intentó gastar una cantidad no positiva o cero de moneda.", this);
            return false;
        }

        if (currentPlayerCurrency >= amount)
        {
            currentPlayerCurrency -= amount;
            OnCurrencyChanged?.Invoke(currentPlayerCurrency);
            Debug.Log($"Moneda gastada: {amount}. Total actual: {currentPlayerCurrency}");
            return true; // Transacción exitosa
        }
        else
        {
            Debug.Log("Fondos insuficientes para gastar " + amount + ". Actual: " + currentPlayerCurrency);
            return false; // Fondos insuficientes
        }
    }
}