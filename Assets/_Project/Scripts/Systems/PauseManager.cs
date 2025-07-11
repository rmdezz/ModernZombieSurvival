using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    [Header("Pause Settings")]
    [Tooltip("El panel de UI que se muestra cuando el juego está en pausa.")]
    [SerializeField] private GameObject pauseMenuPanel;

    // --- NUEVO: Referencias a los componentes del jugador ---
    [Header("Player Components to Disable")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerShooting playerShooting;
    [SerializeField] private CameraLook cameraLook;
    // --- FIN NUEVO ---

    // Referencias a otros componentes para desactivarlos
    // Es mejor tenerlas aquí que buscarlas cada vez
    private PlayerInputActions playerInputActions;
    private bool isGamePaused = false;

    void Awake()
    {
        // Asegurarse de que el panel de pausa esté desactivado al inicio
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Pause Menu Panel no está asignado en el PauseManager!", this);
            enabled = false;
            return;
        }

        // --- NUEVO: Validación de referencias del jugador ---
        // Intentar encontrarlos si no están asignados, lo cual es una buena práctica
        if (playerMovement == null) playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerShooting == null) playerShooting = FindObjectOfType<PlayerShooting>();
        if (cameraLook == null) cameraLook = FindObjectOfType<CameraLook>();
        if (playerMovement == null || playerShooting == null || cameraLook == null)
        {
            Debug.LogError("No se pudieron encontrar todos los componentes del jugador necesarios en la escena!", this);
        }
        // --- FIN NUEVO ---

        playerInputActions = new PlayerInputActions();
        // Suscribirse a la acción de Pausa
        playerInputActions.Player.Pause.performed += ctx => TogglePause();
    }

    void OnEnable()
    {
        playerInputActions.Player.Enable();
    }

    void OnDisable()
    {
        playerInputActions.Player.Disable();
        // Buena práctica: reestablecer la escala de tiempo si el objeto se desactiva
        Time.timeScale = 1f;
    }

    public void TogglePause()
    {
        // Invertir el estado de pausa actual
        isGamePaused = !isGamePaused;

        if (isGamePaused)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }

    public void ResumeGame()
    {
        isGamePaused = false;
        // Ocultar el menú de pausa
        pauseMenuPanel.SetActive(false);
        // Reanudar el tiempo del juego
        Time.timeScale = 1f;
        // Bloquear y ocultar el cursor para volver al modo FPS
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // --- NUEVO: Reactivar los controles del jugador ---
        SetPlayerControlsActive(true);

        Debug.Log("Juego Reanudado.");
    }

    private void PauseGame()
    {
        isGamePaused = true;
        // Mostrar el menú de pausa
        pauseMenuPanel.SetActive(true);
        // Congelar el tiempo del juego. Esto detiene la física y la mayoría de los Update basados en Time.deltaTime.
        Time.timeScale = 0f;
        // Liberar y mostrar el cursor para que el jugador pueda hacer clic en los botones
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // --- NUEVO: Desactivar los controles del jugador ---
        SetPlayerControlsActive(false);

        Debug.Log("Juego Pausado.");
    }

    // --- NUEVA FUNCIÓN ---
    private void SetPlayerControlsActive(bool isActive)
    {
        if (playerMovement != null) playerMovement.enabled = isActive;
        if (playerShooting != null) playerShooting.enabled = isActive;
        if (cameraLook != null) cameraLook.enabled = isActive;
        Debug.Log($"Controles del jugador establecidos en: {isActive}");
    }
    // --- FIN NUEVA FUNCIÓN ---

    // --- Funciones para los Botones ---

    public void LoadMainMenu()
    {
        // Asegurarse de reanudar el tiempo antes de cambiar de escena
        Time.timeScale = 1f;
        // Asume que tu escena de menú principal se llama "MainMenu"
        // Deberías tener una forma más robusta de manejar nombres de escena si el proyecto crece
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}