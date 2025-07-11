using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Shop Settings")]
    [SerializeField] private Transform shopSpawnPoint;
    [SerializeField] private GameObject shopEnvironment;

    private Transform playerOriginalPosition;
    private bool isInShop = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (shopEnvironment != null)
            shopEnvironment.SetActive(false);
    }

    public void TeleportToShop()
    {
        if (isInShop)
        {
            Debug.Log("Ya estamos en la tienda, ignorando TeleportToShop.");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player no encontrado!");
            return;
        }

        // Guardar posici�n original
        playerOriginalPosition = player.transform;
        Vector3 originalPos = player.transform.position;
        Quaternion originalRot = player.transform.rotation;

        // Activar tienda
        if (shopEnvironment != null)
            shopEnvironment.SetActive(true);

        // Teletransportar jugador
        if (shopSpawnPoint != null)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
                player.transform.position = shopSpawnPoint.position;
                player.transform.rotation = shopSpawnPoint.rotation;
                controller.enabled = true;
            }
            else
            {
                player.transform.position = shopSpawnPoint.position;
                player.transform.rotation = shopSpawnPoint.rotation;
            }
        }

        isInShop = true;

        // NO deshabilitar el spawner, solo pausar su l�gica
        // El spawner seguir� corriendo pero estar� esperando en el while loop
        Debug.Log($"�Bienvenido a la tienda! IsInShop ahora = {isInShop}");
    }

    public void ExitShop(Transform exitDestination)
    {
        Debug.Log($"ExitShop llamado. Estado actual isInShop: {isInShop}");

        if (!isInShop)
        {
            Debug.Log("ExitShop llamado pero no estamos en la tienda.");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player no encontrado en ExitShop!");
            return;
        }

        // Teletransportar de vuelta
        if (exitDestination != null)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
                player.transform.position = exitDestination.position;
                player.transform.rotation = exitDestination.rotation;
                controller.enabled = true;
            }
            else
            {
                player.transform.position = exitDestination.position;
                player.transform.rotation = exitDestination.rotation;
            }
            Debug.Log($"Jugador teletransportado a: {exitDestination.position}");
        }

        // Desactivar tienda
        if (shopEnvironment != null)
            shopEnvironment.SetActive(false);

        isInShop = false;
        Debug.Log($"isInShop cambiado a: {isInShop}");

        // Ya no necesitamos reactivar el spawner porque nunca lo deshabilitamos
        Debug.Log("�Has salido de la tienda completamente! El spawner deber�a continuar autom�ticamente.");
    }

    public bool IsInShop()
    {
        return isInShop;
    }
}