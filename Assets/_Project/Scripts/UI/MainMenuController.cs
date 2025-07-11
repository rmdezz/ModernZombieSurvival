using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para manejar escenas

public class MainMenuController : MonoBehaviour
{
    // Esta función se llamará desde el botón de "Iniciar Juego"
    public void StartGame()
    {
        // Carga la escena del juego usando el nombre que le diste
        SceneManager.LoadScene("MAPA"); 
    }

    // Esta función se llamará desde el botón de "Salir"
    public void QuitGame()
    {
        // Escribe un mensaje en la consola para saber que funciona en el editor
        Debug.Log("SALIENDO DEL JUEGO..."); 
        Application.Quit(); // Esta línea solo funciona en el juego ya compilado
    }
}