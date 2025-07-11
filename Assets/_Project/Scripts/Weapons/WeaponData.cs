using UnityEngine;

// Añade un atributo para poder crear instancias de este objeto desde el menú Assets->Create
[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Game Data/Weapon Data", order = 1)]
public class WeaponData : ScriptableObject // ¡Hereda de ScriptableObject, no de MonoBehaviour!
{
    [Header("Info")]
    [Tooltip("Nombre del arma para mostrar en UI, etc.")]
    public string weaponName = "Default Weapon";

    [Header("Shooting")]
    [Tooltip("Daño infligido por cada disparo/bala.")]
    [Min(0)] // Asegura que el daño no sea negativo en el Inspector
    public float damage = 10f;

    [Tooltip("Alcance máximo del disparo (para Raycast).")]
    [Min(0.1f)]
    public float range = 100f;

    [Tooltip("Tiempo mínimo en segundos entre disparos (Cadencia). 0.1 = 10 disparos/segundo.")]
    [Min(0.01f)]
    public float fireRate = 0.5f; // 2 disparos por segundo por defecto

    [Header("Ammo & Reloading")]
    [Tooltip("Número máximo de balas en el cargador.")]
    [Min(1)] public int magazineSize = 15;

    [Tooltip("Número máximo de balas que se pueden llevar en reserva para esta arma.")]
    [Min(0)] public int maxTotalAmmo = 90;

    [Tooltip("Tiempo en segundos que tarda la recarga.")]
    [Min(0.1f)] public float reloadTime = 1.5f;

    [Header("View Model")]
    [Tooltip("El prefab que contiene el modelo 3D y el Animator del arma (brazos+arma).")]
    public GameObject viewModelPrefab;

    // --- Podríamos añadir más datos aquí en el futuro ---

    // [Header("Visuals & Audio")]
    // --- NUEVO: Campos para feedback ---
    public AudioClip fireSound;
    public AudioClip impactFleshSound;
    // public AudioClip impactEnvironmentSound; // Si lo necesitas después
    public GameObject muzzleFlashPrefab;
    public GameObject impactFleshVFXPrefab;
    // public GameObject impactEnvironmentVFXPrefab; // Si lo necesitas después
    // --- FIN NUEVO ---

    // public GameObject muzzleFlashPrefab;
    // public GameObject bulletImpactEffectPrefab;
    // public AudioClip fireSound;
    // public AudioClip reloadSound;
}