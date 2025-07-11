using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic; // Necesario para List
using TMPro;

public class PlayerShooting : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Camera playerCamera;
    // --- MODIFICADO: Lista de armas iniciales en lugar de una sola ---
    [Tooltip("Armas con las que el jugador empieza. La primera es la equipada por defecto.")]
    [SerializeField] private List<WeaponData> startingWeapons;
    private WeaponData currentWeaponData; // El arma actualmente equipada
    private int currentWeaponIndex = 0;   // Índice del arma actual en la lista

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI ammoTextElement;
    // (Opcional) Podrías añadir un TextMeshProUGUI para el nombre del arma
    // [SerializeField] private TextMeshProUGUI weaponNameTextElement;

    // --- MODIFICADO: Estos podrían ser defaults si el WeaponData no especifica los suyos ---
    [Header("Default Impact Feedback (Flesh Only)")]
    [Tooltip("Sonido de impacto en carne por defecto si el arma no tiene uno específico.")]
    [SerializeField] private AudioClip defaultImpactFleshSound;
    [Tooltip("VFX de impacto en carne por defecto si el arma no tiene uno específico.")]
    [SerializeField] private GameObject defaultImpactFleshVFXPrefab;

    // Estado interno del arma (específico del arma actualmente equipada)
    private int currentAmmoInMagazine;
    private int currentTotalAmmo;
    private bool isReloading = false;
    private float nextTimeToShoot = 0f;

    // --- NUEVO: Diccionario para guardar el estado de munición de cada arma ---
    // Esto es clave para que cada arma recuerde su munición.
    // La Key es el ScriptableObject WeaponData, Value es una clase/struct con el estado.
    private class WeaponAmmoState
    {
        public int ammoInMagazine;
        public int totalAmmoReserve;
        public WeaponAmmoState(int mag, int reserve) { ammoInMagazine = mag; totalAmmoReserve = reserve; }
    }
    private Dictionary<WeaponData, WeaponAmmoState> weaponStates = new Dictionary<WeaponData, WeaponAmmoState>();


    private PlayerInputActions playerInputActions;
    // 'fireActionPerformed' ya no es necesaria con el actual 'TryShoot' llamado desde 'performed'

    void Awake()
    {
        if (playerCamera == null)
        {
            Debug.LogError("Player Camera no está asignada en PlayerShooting!", this);
            playerCamera = GetComponentInChildren<Camera>(true);
            if (playerCamera == null) { Debug.LogError("No se pudo encontrar Camera. PlayerShooting no funcionará.", this); enabled = false; return; }
        }

        // --- MODIFICADO: Validar startingWeapons en lugar de currentWeaponData ---
        if (startingWeapons == null || startingWeapons.Count == 0)
        {
            Debug.LogError("ERROR: No hay Starting Weapons asignadas en PlayerShooting!", this);
            enabled = false; return;
        }
        // Asegurarse de que todas las armas en la lista inicial no sean nulas
        for (int i = 0; i < startingWeapons.Count; i++)
        {
            if (startingWeapons[i] == null)
            {
                Debug.LogError($"ERROR: Starting Weapon en el índice {i} es nula!", this);
                enabled = false; return;
            }
        }

        if (ammoTextElement == null) Debug.LogWarning("Ammo Text Element no asignado. La UI no se actualizará.", this);

        playerInputActions = new PlayerInputActions();
        playerInputActions.Player.Fire.performed += ctx => TryShoot();
        playerInputActions.Player.Reload.performed += ctx => StartReload();

        // --- NUEVO: Suscripción a inputs para cambiar de arma ---
        playerInputActions.Player.SwitchWeapon1.performed += ctx => EquipWeaponByIndex(0); // Tecla 1
        playerInputActions.Player.SwitchWeapon2.performed += ctx => EquipWeaponByIndex(1); // Tecla 2
    }

    void Start()
    {
        // --- NUEVO: Inicializar el estado de munición para todas las armas iniciales ---
        foreach (WeaponData weapon in startingWeapons)
        {
            if (!weaponStates.ContainsKey(weapon))
            {
                weaponStates.Add(weapon, new WeaponAmmoState(weapon.magazineSize, weapon.maxTotalAmmo));
            }
        }

        // Equipar la primera arma de la lista al inicio
        EquipWeaponByIndex(0); // Llama a la nueva función para manejar el equipamiento
        isReloading = false;
    }

    void OnEnable()
    {
        playerInputActions.Player.Enable(); // Habilita todo el Action Map "Player"
        isReloading = false;
    }

    void OnDisable()
    {
        playerInputActions.Player.Disable(); // Deshabilita todo el Action Map "Player"
    }

    // --- NUEVA FUNCIÓN: Para equipar un arma por su índice en la lista 'startingWeapons' ---
    private void EquipWeaponByIndex(int weaponIndex)
    {
        if (isReloading)
        {
            // Opcional: podrías cancelar la recarga aquí si lo deseas
            // StopCoroutine("ReloadCoroutine"); // Necesitaría guardar la referencia a la corutina
            // isReloading = false;
            Debug.Log("No se puede cambiar de arma mientras se recarga.");
            return;
        }

        if (weaponIndex < 0 || weaponIndex >= startingWeapons.Count)
        {
            Debug.LogWarning($"Índice de arma {weaponIndex} fuera de rango.");
            return;
        }

        WeaponData newWeapon = startingWeapons[weaponIndex];
        if (newWeapon == null)
        {
            Debug.LogError($"Arma en el índice {weaponIndex} es nula en startingWeapons.");
            return;
        }

        // Si ya es el arma actual, no hacer nada (a menos que quieras una animación de re-equipar)
        if (currentWeaponData == newWeapon && currentWeaponIndex == weaponIndex && !isReloading) return;


        currentWeaponIndex = weaponIndex;
        currentWeaponData = newWeapon;

        // Cargar el estado de munición del arma que se está equipando
        if (weaponStates.TryGetValue(currentWeaponData, out WeaponAmmoState state))
        {
            currentAmmoInMagazine = state.ammoInMagazine;
            currentTotalAmmo = state.totalAmmoReserve;
        }
        else // Si por alguna razón no estaba en el diccionario, inicializarla (no debería pasar si Start funcionó)
        {
            Debug.LogWarning($"Estado no encontrado para {currentWeaponData.weaponName}, inicializando.");
            currentAmmoInMagazine = currentWeaponData.magazineSize;
            currentTotalAmmo = currentWeaponData.maxTotalAmmo;
            weaponStates[currentWeaponData] = new WeaponAmmoState(currentAmmoInMagazine, currentTotalAmmo);
        }

        isReloading = false; // Asegurar que no se quede en estado de recarga al cambiar
        nextTimeToShoot = 0f; // Permitir disparar la nueva arma inmediatamente
        UpdateAmmoUI();
        // if (weaponNameTextElement != null) weaponNameTextElement.text = currentWeaponData.weaponName;
        Debug.Log($"Arma equipada: {currentWeaponData.weaponName} (Munición: {currentAmmoInMagazine}/{currentTotalAmmo})");

        // Aquí iría la lógica para cambiar el modelo visual del arma, reproducir sonido de equipar, etc.
    }

    // --- NUEVA FUNCIÓN: Para cambiar de arma con la rueda del ratón ---
    private void SwitchWeaponByScroll(float scrollValue)
    {
        if (isReloading || startingWeapons.Count <= 1) return;

        int newIndex = currentWeaponIndex;
        if (scrollValue > 0f) newIndex--; // Rueda hacia arriba, arma anterior (o siguiente si inviertes)
        else if (scrollValue < 0f) newIndex++; // Rueda hacia abajo, arma siguiente

        // Manejar el bucle de la lista de armas
        if (newIndex >= startingWeapons.Count) newIndex = 0;
        else if (newIndex < 0) newIndex = startingWeapons.Count - 1;

        EquipWeaponByIndex(newIndex);
    }

    public bool AddReserveAmmoToCurrentWeapon(int amount)
    {
        if (currentWeaponData == null || amount <= 0) return false;

        // Comprobar si ya estamos al máximo de la reserva total para esta arma
        // (Asumiendo que weaponStates guarda el estado actual)
        if (weaponStates.TryGetValue(currentWeaponData, out WeaponAmmoState state))
        {
            if (state.totalAmmoReserve >= currentWeaponData.maxTotalAmmo)
            {
                Debug.Log($"Reserva de munición para {currentWeaponData.weaponName} ya está al máximo.");
                return false; // Ya al máximo
            }

            state.totalAmmoReserve += amount;
            // Asegurarse de no exceder la capacidad máxima total del arma
            state.totalAmmoReserve = Mathf.Min(state.totalAmmoReserve, currentWeaponData.maxTotalAmmo);

            // Actualizar nuestra variable local también, aunque el estado está en el diccionario
            currentTotalAmmo = state.totalAmmoReserve;

            UpdateAmmoUI();
            return true; // Munición añadida
        }
        return false; // No se encontró estado para el arma (no debería pasar si está equipada)
    }

    private void TryShoot()
    {
        if (currentWeaponData == null || isReloading || Time.time < nextTimeToShoot) return;

        if (currentAmmoInMagazine <= 0)
        {
            Debug.Log("¡Sin munición! Necesitas recargar.");
            // Aquí podrías reproducir un sonido de "clic vacío"
            // AudioSource.PlayClipAtPoint(currentWeaponData.emptyClipSound, playerCamera.transform.position);
            // StartReload(); // Opcional: Recarga automática
            return;
        }

        nextTimeToShoot = Time.time + currentWeaponData.fireRate;
        Shoot();
        currentAmmoInMagazine--;
        weaponStates[currentWeaponData].ammoInMagazine = currentAmmoInMagazine; // Guardar estado
        UpdateAmmoUI();
    }

    private void Shoot()
    {
        if (currentWeaponData.fireSound != null)
        {
            AudioSource.PlayClipAtPoint(currentWeaponData.fireSound, playerCamera.transform.position, 0.8f);
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hitInfo;

        if (Physics.Raycast(ray, out hitInfo, currentWeaponData.range))
        {
            // Debug.DrawRay(ray.origin, ray.direction * hitInfo.distance, Color.red, 0.1f);
            // Debug.Log($"Raycast golpeó: {hitInfo.transform.name} (Tag: {hitInfo.transform.tag})");

            HealthManager targetHealth = hitInfo.transform.GetComponentInParent<HealthManager>(); // Usar GetComponentInParent
            if (targetHealth != null)
            {
                // Debug.Log($"HealthManager encontrado en {hitInfo.transform.name}. Aplicando daño.");
                targetHealth.TakeDamage(currentWeaponData.damage);

                // Usar el sonido/VFX del WeaponData si existe, si no, el default del PlayerShooting
                AudioClip soundToPlay = currentWeaponData.impactFleshSound != null ? currentWeaponData.impactFleshSound : defaultImpactFleshSound;
                GameObject vfxToPlay = currentWeaponData.impactFleshVFXPrefab != null ? currentWeaponData.impactFleshVFXPrefab : defaultImpactFleshVFXPrefab;

                if (soundToPlay != null)
                {
                    AudioSource.PlayClipAtPoint(soundToPlay, hitInfo.point, 0.7f);
                }
                if (vfxToPlay != null)
                {
                    Instantiate(vfxToPlay, hitInfo.point, Quaternion.LookRotation(hitInfo.normal));
                }
            }
        }
        // else { Debug.DrawRay(ray.origin, ray.direction * currentWeaponData.range, Color.green, 0.1f); }
    }

    private void StartReload()
    {
        if (currentWeaponData == null || isReloading || currentAmmoInMagazine == currentWeaponData.magazineSize || currentTotalAmmo <= 0)
        {
            return;
        }
        StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        if (currentWeaponData == null) yield break;

        Debug.Log($"Recargando {currentWeaponData.weaponName}...");
        isReloading = true;
        // Aquí podrías reproducir sonido de inicio de recarga o animación
        // if(currentWeaponData.reloadStartSound) audioSource.PlayOneShot(currentWeaponData.reloadStartSound);

        yield return new WaitForSeconds(currentWeaponData.reloadTime);

        int ammoNeeded = currentWeaponData.magazineSize - currentAmmoInMagazine;
        int ammoToTransfer = Mathf.Min(ammoNeeded, currentTotalAmmo);

        currentAmmoInMagazine += ammoToTransfer;
        currentTotalAmmo -= ammoToTransfer;

        // Guardar el nuevo estado de munición en el diccionario
        weaponStates[currentWeaponData].ammoInMagazine = currentAmmoInMagazine;
        weaponStates[currentWeaponData].totalAmmoReserve = currentTotalAmmo;

        isReloading = false;
        UpdateAmmoUI();
        Debug.Log($"¡Recarga completa para {currentWeaponData.weaponName}!");
        // Aquí podrías reproducir sonido de fin de recarga
        // if(currentWeaponData.reloadEndSound) audioSource.PlayOneShot(currentWeaponData.reloadEndSound);
    }

    private void UpdateAmmoUI()
    {
        if (currentWeaponData == null || ammoTextElement == null) return;
        ammoTextElement.text = $"{currentWeaponData.weaponName.ToUpper()}: {currentAmmoInMagazine} / {currentTotalAmmo}";
    }

    public bool RefillAllWeaponsAmmo()
    {
        bool ammoWasAdded = false;

        foreach (var weaponKvp in weaponStates)
        {
            WeaponData weapon = weaponKvp.Key;
            WeaponAmmoState state = weaponKvp.Value;

            // Rellenar cargador al máximo
            if (state.ammoInMagazine < weapon.magazineSize)
            {
                state.ammoInMagazine = weapon.magazineSize;
                ammoWasAdded = true;
            }

            // Rellenar reserva al máximo
            if (state.totalAmmoReserve < weapon.maxTotalAmmo)
            {
                state.totalAmmoReserve = weapon.maxTotalAmmo;
                ammoWasAdded = true;
            }
        }

        // Actualizar variables del arma actual
        if (currentWeaponData != null && weaponStates.ContainsKey(currentWeaponData))
        {
            currentAmmoInMagazine = weaponStates[currentWeaponData].ammoInMagazine;
            currentTotalAmmo = weaponStates[currentWeaponData].totalAmmoReserve;
            UpdateAmmoUI();
        }

        return ammoWasAdded;
    }
}