using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public abstract class ShopItem : MonoBehaviour
{
    [Header("Shop Item Settings")]
    [SerializeField] protected int cost = 100;
    [SerializeField] protected string itemName = "Item";
    [SerializeField] public TextMeshPro priceText;
    [SerializeField] public GameObject interactionPrompt;

    protected bool isPlayerInRange = false;
    private PlayerInput playerInput;
    private InputAction interactAction;

    protected virtual void Start()
    {
        UpdatePriceDisplay();
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        // Obtener referencia al PlayerInput
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerInput = player.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                // Buscar la acción de interacción (ajusta el nombre según tu Input Actions)
                interactAction = playerInput.actions["Interact"];
                if (interactAction == null)
                {
                    // Si no existe "Interact", buscar otras posibles acciones
                    interactAction = playerInput.actions["Use"];
                    if (interactAction == null)
                    {
                        Debug.LogWarning("No se encontró acción de interacción. Usando detección por tecla E manual.");
                    }
                }
            }
        }
    }

    protected virtual void Update()
    {
        if (isPlayerInRange)
        {
            bool interactPressed = false;

            // Intentar usar Input System primero
            if (interactAction != null)
            {
                interactPressed = interactAction.WasPressedThisFrame();
            }
            else
            {
                // Fallback: usar Keyboard directamente del Input System
                if (Keyboard.current != null)
                {
                    interactPressed = Keyboard.current.eKey.wasPressedThisFrame;
                }
            }

            if (interactPressed)
            {
                TryPurchase();
            }
        }
    }

    protected virtual void TryPurchase()
    {
        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("CurrencyManager no encontrado!");
            return;
        }

        if (CurrencyManager.Instance.GetCurrentCurrency() >= cost)
        {
            if (PerformPurchase())
            {
                CurrencyManager.Instance.SpendCurrency(cost);
                Debug.Log($"{itemName} comprado por {cost} monedas!");
            }
        }
        else
        {
            Debug.Log($"Monedas insuficientes. Necesitas {cost}, tienes {CurrencyManager.Instance.GetCurrentCurrency()}");
        }
    }

    protected abstract bool PerformPurchase();

    protected virtual void UpdatePriceDisplay()
    {
        if (priceText != null)
            priceText.text = $"{itemName}\n${cost}";
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (interactionPrompt != null)
                interactionPrompt.SetActive(true);
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }
    }
}