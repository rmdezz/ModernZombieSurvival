using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TimeAndWeatherManager : MonoBehaviour
{
    // --- ESTADOS DE CLIMA ---
    public enum WeatherState { Clear, Raining }
    private WeatherState currentWeatherState = WeatherState.Clear;

    [Header("Sun & Time Settings")]
    [SerializeField] private Light sunLight;
    [SerializeField] private float dayDurationInSeconds = 120f;
    [Range(0f, 1f)]
    [SerializeField] private float initialTimeOfDay = 0.3f;

    [Header("Atmosphere Settings")]
    [SerializeField] private Gradient sunColor;
    [SerializeField] private Gradient ambientColor;
    [SerializeField] private Gradient fogColor;
    [SerializeField] private AnimationCurve sunIntensityCurve;

    // --- NUEVO: Referencias para el Clima ---
    [Header("Weather Effects")]
    [Tooltip("El sistema de partículas de la lluvia (debe ser un Prefab o un objeto en la escena).")]
    [SerializeField] private ParticleSystem rainEffect;
    [Tooltip("El AudioSource que contiene el sonido de la lluvia en bucle.")]
    [SerializeField] private AudioSource rainAudioSource;
    [Tooltip("Tiempo que tarda la lluvia en aparecer/desaparecer (fade).")]
    [SerializeField] private float weatherTransitionDuration = 10f;

    // --- NUEVO: Control del Cambio de Clima ---
    [Header("Weather Change Logic")]
    [Tooltip("¿El clima debe cambiar automáticamente?")]
    [SerializeField] private bool changeWeatherAutomatically = true;
    [Tooltip("Tiempo mínimo y máximo (en segundos) que durará un estado de clima antes de intentar cambiar.")]
    [SerializeField] private Vector2 timeBetweenWeatherChanges = new Vector2(60f, 180f);


    private float currentTimeOfDay;
    private Coroutine weatherChangeCoroutine; // Para la rutina principal de cambio de clima
    private Coroutine weatherTransitionCoroutine; // Para la transición suave (fade)

    void Start()
    {
        // --- Validación de Referencias ---
        if (sunLight == null) { Debug.LogError("Sun Light no asignado!", this); enabled = false; return; }
        if (rainEffect == null) { Debug.LogError("Rain Effect (Particle System) no asignado!", this); enabled = false; return; }
        if (rainAudioSource == null) { Debug.LogError("Rain Audio Source no asignado!", this); enabled = false; return; }

        currentTimeOfDay = initialTimeOfDay;
        // Asegurarse de que los efectos de lluvia estén apagados al inicio
        rainEffect.Stop();
        rainAudioSource.Stop();
        rainAudioSource.volume = 0f;

        UpdateAtmosphere();

        if (changeWeatherAutomatically)
        {
            weatherChangeCoroutine = StartCoroutine(WeatherChangeRoutine());
        }
    }

    void Update()
    {
        UpdateTime();
        UpdateAtmosphere();
    }

    private IEnumerator WeatherChangeRoutine()
    {
        while (true) // Bucle infinito para seguir cambiando el clima
        {
            float waitTime = Random.Range(timeBetweenWeatherChanges.x, timeBetweenWeatherChanges.y);
            yield return new WaitForSeconds(waitTime);

            // Decidir el nuevo estado del clima (simple, 50/50 por ahora)
            WeatherState newState = (Random.value > 0.5f) ? WeatherState.Raining : WeatherState.Clear;

            // Solo cambiar si el nuevo estado es diferente al actual
            if (newState != currentWeatherState)
            {
                ChangeWeatherTo(newState);
            }
        }
    }

    public void ChangeWeatherTo(WeatherState newState)
    {
        // Detener cualquier transición anterior que pudiera estar en curso
        if (weatherTransitionCoroutine != null)
        {
            StopCoroutine(weatherTransitionCoroutine);
        }

        // Iniciar la nueva transición
        weatherTransitionCoroutine = StartCoroutine(TransitionWeather(newState));
        currentWeatherState = newState;
    }

    private IEnumerator TransitionWeather(WeatherState targetState)
    {
        Debug.Log($"Iniciando transición a: {targetState}");

        float startVolume = rainAudioSource.volume;
        float targetVolume = (targetState == WeatherState.Raining) ? 1f : 0f;

        // Si vamos a hacer llover, empezar a emitir partículas
        if (targetState == WeatherState.Raining)
        {
            rainEffect.Play();
            rainAudioSource.Play();
        }

        float elapsedTime = 0f;
        while (elapsedTime < weatherTransitionDuration)
        {
            // Hacer el fade del volumen de la lluvia
            rainAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsedTime / weatherTransitionDuration);

            // Podrías también hacer un lerp de los colores de la atmósfera aquí
            // para que el cielo se nuble gradualmente, etc. Por ahora, es un cambio instantáneo de gradiente.

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Asegurar que el estado final sea el correcto
        rainAudioSource.volume = targetVolume;
        if (targetState == WeatherState.Clear)
        {
            rainEffect.Stop();
            rainAudioSource.Stop();
        }

        Debug.Log($"Transición a {targetState} completada.");
    }


    private void UpdateTime()
    {
        currentTimeOfDay += (Time.deltaTime / dayDurationInSeconds);
        if (currentTimeOfDay >= 1f) currentTimeOfDay -= 1f;
    }

    private void UpdateAtmosphere()
    {
        float sunAngle = currentTimeOfDay * 360f;
        sunLight.transform.localRotation = Quaternion.Euler(new Vector3(sunAngle, -30f, 0));

        // (Mejora futura: podríamos tener diferentes gradientes para clima lluvioso vs. despejado)
        // Gradient sunGradientToUse = (currentWeatherState == WeatherState.Raining) ? rainySunColor : sunColor;
        sunLight.color = sunColor.Evaluate(currentTimeOfDay);
        sunLight.intensity = sunIntensityCurve.Evaluate(currentTimeOfDay);

        RenderSettings.ambientLight = ambientColor.Evaluate(currentTimeOfDay);
        RenderSettings.fogColor = fogColor.Evaluate(currentTimeOfDay);
    }
}