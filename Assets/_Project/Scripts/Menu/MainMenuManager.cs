using UnityEngine;
using UnityEngine.SceneManagement; // Esencial para cambiar de escena

// Este script manejará toda la lógica de los botones del menú principal.
public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("El nombre EXACTO de tu archivo de escena de juego (ej. 'MapaPropio').")]
    [SerializeField] private string gameSceneName; // Lo configuraremos en el Inspector

    /// <summary>
    /// Esta es una función pública que será llamada por el evento OnClick del botón "Jugar".
    /// </summary>
    public void PlayGame()
    {
        // Validar que el nombre de la escena no esté vacío
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("MainMenuManager: El nombre de la escena del juego (gameSceneName) no está configurado en el Inspector!");
            return;
        }

        Debug.Log($"Cargando escena: {gameSceneName}...");
        // Cargar la escena del juego usando su nombre
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Esta es una función pública que será llamada por el evento OnClick del botón "Salir".
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");

        // Application.Quit() solo funciona en un build del juego (.exe, .app, etc.).
        // No hace nada en el Editor de Unity.
        // Por eso, añadimos un código especial para que también detenga la reproducción en el Editor,
        // lo cual es útil para probar.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}