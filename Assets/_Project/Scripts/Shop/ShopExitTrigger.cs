using UnityEngine;
using UnityEngine.InputSystem;

public class ShopExitTrigger : MonoBehaviour
{
    [Header("Exit Settings")]
    [SerializeField] private Transform exitDestination;
    [SerializeField] private GameObject exitPrompt;

    private bool isPlayerInRange = false;
    private PlayerInput playerInput;
    private InputAction interactAction;

    void Start()
    {
        if (exitPrompt != null)
            exitPrompt.SetActive(false);

        // Obtener referencia al PlayerInput
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerInput = player.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                interactAction = playerInput.actions["Interact"];
                if (interactAction == null)
                {
                    interactAction = playerInput.actions["Use"];
                }
            }
        }
    }

    void Update()
    {
        if (isPlayerInRange)
        {
            bool interactPressed = false;

            if (interactAction != null)
            {
                interactPressed = interactAction.WasPressedThisFrame();
            }
            else
            {
                if (Keyboard.current != null)
                {
                    interactPressed = Keyboard.current.eKey.wasPressedThisFrame;
                }
            }

            if (interactPressed)
            {
                ExitShop();
            }
        }
    }

    void ExitShop()
    {
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        if (shopManager != null)
        {
            shopManager.ExitShop(exitDestination);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (exitPrompt != null)
                exitPrompt.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (exitPrompt != null)
                exitPrompt.SetActive(false);
        }
    }
}