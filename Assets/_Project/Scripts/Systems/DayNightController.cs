using UnityEngine;
using UnityEngine.Rendering;

public class DayNightController : MonoBehaviour
{
    [Header("Referencias de la Escena")]
    public Light directionalLight; // Arrastra tu luz direccional aquí

    [Header("Configuración del Ciclo DIURNO")]
    public Color dayLightColor = Color.white;
    public float initialLightIntensity = 1.0f; // Intensidad de luz normal
    public Color daySkyTintColor = new Color(0.5f, 0.5f, 0.5f); // Tinte gris estándar del skybox
    public float dayExposure = 1.0f; // Exposición normal del skybox

    [Header("Configuración del Ciclo NOCTURNO")]
    public Color nightLightColor = new Color(0.1f, 0.1f, 0.2f);
    public float nightLightIntensity = 0.1f; // Intensidad de luz nocturna
    public Color nightSkyTintColor = new Color(0.05f, 0.05f, 0.1f); // Tinte azul muy oscuro
    public float nightExposure = 0.2f; // Exposición baja para la noche

    [Header("Configuración de Luz Ambiental")]
    public Color dayAmbientColor = new Color(0.74f, 0.79f, 0.88f);
    public Color nightAmbientColor = new Color(0.1f, 0.1f, 0.2f);

    private Material currentSkybox;
    private int totalWaves;
    private int currentWave = 0;

    void Start()
    {
        if (directionalLight == null)
        {
            directionalLight = FindObjectOfType<Light>();
        }
        
        // ¡IMPORTANTE! Obtenemos una instancia del skybox actual para poder modificarlo sin alterar el archivo del proyecto.
        currentSkybox = new Material(RenderSettings.skybox);
        RenderSettings.skybox = currentSkybox;
    }

    public void SetTotalWaves(int waves)
    {
        totalWaves = waves;
    }

    public void AdvanceTimeOfDay()
    {
        currentWave++;
        if (totalWaves == 0) return;

        // El mismo porcentaje de siempre
        float wavePercentage = (float)currentWave / totalWaves;

        // Interpolar la luz direccional (color e intensidad)
        directionalLight.color = Color.Lerp(dayLightColor, nightLightColor, wavePercentage);
        directionalLight.intensity = Mathf.Lerp(initialLightIntensity, nightLightIntensity, wavePercentage);

        // Interpolar la luz ambiental
        RenderSettings.ambientLight = Color.Lerp(dayAmbientColor, nightAmbientColor, wavePercentage);
        
        // Modificamos las propiedades del Skybox Procedural directamente
        
        // Interpolar el tinte del cielo (_SkyTint es el nombre interno de la propiedad)
        currentSkybox.SetColor("_SkyTint", Color.Lerp(daySkyTintColor, nightSkyTintColor, wavePercentage));
        
        // Interpolar la exposición del cielo (_Exposure es el nombre interno de la propiedad)
        currentSkybox.SetFloat("_Exposure", Mathf.Lerp(dayExposure, nightExposure, wavePercentage));

        // Forzamos la actualización para que los cambios se reflejen al instante
        DynamicGI.UpdateEnvironment();
    }
}