using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseController : MonoBehaviour
{
    public GameObject pauseMenuPanel;
    private bool isPaused = false;

    private MenuInputActions inputActions;

    public MonoBehaviour cameraScript;

    [SerializeField] private PlayerShooting playerShooting;

    private void Awake()
    {
        inputActions = new MenuInputActions();

        // Suscribirse al evento de la acción "Pause"
        inputActions.PauseMap.Pause.performed += ctx => TogglePause();
    }

    private void OnEnable()
    {
        inputActions.PauseMap.Enable();
    }

    private void OnDisable()
    {
        inputActions.PauseMap.Disable();
    }

    private void Start()
    {
        pauseMenuPanel.SetActive(false);
    }

    private void TogglePause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        // Mostrar cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        cameraScript.enabled = false;

        // Desactivar input del jugador completamente
        playerShooting.enabled = false;
    }

    public void Resume()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        // Ocultar cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        cameraScript.enabled = true;

        // Reactivar input
        playerShooting.enabled = true;
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        Destroy(pauseMenuPanel.transform.root.gameObject);
        // Cerrar todas las escenas activas por si acaso
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(scene);
            }
        }
        foreach (var cam in Camera.allCameras)
        {
            if (cam != Camera.main)
            {
                Destroy(cam.gameObject);
            }
        }
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}
