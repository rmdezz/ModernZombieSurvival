using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 6.0f;
    [SerializeField] private float sprintSpeedMultiplier = 1.5f;
    [SerializeField] private float gravity = -9.81f * 2;
    // --- NUEVO: Parámetros de Salto ---
    [SerializeField] private float jumpHeight = 1.5f; // Altura aproximada del salto

    [Header("Look")]
    [SerializeField] private float lookSensitivityX = 0.2f;

    [Header("Footsteps")]
    [SerializeField] private List<AudioClip> footstepSounds;
    [SerializeField] private float timeBetweenFootstepsWalking = 0.5f;
    [SerializeField] private float timeBetweenFootstepsSprinting = 0.3f;
    [SerializeField] private float footstepVolume = 0.7f;
    // --- NUEVO: Sonido de Salto (Opcional) ---
    [SerializeField] private AudioClip jumpSound;
    // [SerializeField] private AudioClip landSound; // Podríamos añadir sonido de aterrizaje después

    private CharacterController controller;
    private AudioSource audioSource;
    private Vector3 verticalVelocity; // Esto ya maneja la gravedad
    private Vector2 moveInput;
    private PlayerInputActions playerInputActions;

    private float nextFootstepTime = 0f;
    private bool isSprinting = false;
    // --- NUEVO: Variable para rastrear la solicitud de salto ---
    private bool jumpRequested = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        playerInputActions = new PlayerInputActions();

        // Movimiento
        playerInputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        playerInputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        // Sprint
        playerInputActions.Player.Sprint.performed += ctx => isSprinting = true;
        playerInputActions.Player.Sprint.canceled += ctx => isSprinting = false;

        // --- NUEVO: Suscripción al Input de Salto ---
        playerInputActions.Player.Jump.performed += ctx => RequestJump();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (audioSource == null && (footstepSounds.Count > 0 || jumpSound != null))
            Debug.LogWarning("PlayerMovement tiene sonidos asignados pero no AudioSource.", this);
    }

    void OnEnable()
    {
        playerInputActions.Player.Move.Enable();
        playerInputActions.Player.Look.Enable();
        playerInputActions.Player.Sprint.Enable();
        playerInputActions.Player.Jump.Enable(); // ¡Activar la acción de salto!
    }

    void OnDisable()
    {
        playerInputActions.Player.Move.Disable();
        playerInputActions.Player.Look.Disable();
        playerInputActions.Player.Sprint.Disable();
        playerInputActions.Player.Jump.Disable(); // ¡Desactivar la acción de salto!
    }

    // --- NUEVO: Método llamado por el evento de Input ---
    private void RequestJump()
    {
        // Solo permitir solicitar un salto si estamos en el suelo
        if (controller.isGrounded)
        {
            jumpRequested = true;
            Debug.Log("Salto solicitado!");
        }
    }

    void Update()
    {
        // Leer input de Look aquí ya que HandleLook no se llama si el script está desactivado
        Vector2 lookDelta = playerInputActions.Player.Look.ReadValue<Vector2>();
        HandleLook(lookDelta); // Pasar el delta

        HandleMovement();
        HandleGravityAndJump(); // Combinar gravedad y salto
        HandleFootsteps();
    }

    private void HandleMovement()
    {
        float currentSpeed = isSprinting ? speed * sprintSpeedMultiplier : speed;
        Vector3 moveDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
        Vector3 move = moveDirection * currentSpeed; // No multiplicar por Time.deltaTime aquí
                                                     // CharacterController.Move lo hace internamente con el vector de velocidad

        // Aplicar movimiento horizontal (sin Time.deltaTime porque lo aplicaremos globalmente con verticalVelocity)
        // controller.Move(move * Time.deltaTime); // Haremos el Move una sola vez en HandleGravityAndJump
    }

    // --- MODIFICADO: Renombrado y lógica de salto añadida ---
    private void HandleGravityAndJump()
    {
        // Aplicar gravedad
        if (controller.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f; // Pequeña fuerza hacia abajo para mantenerlo pegado
        }

        // Aplicar Salto
        if (jumpRequested)
        {
            // Fórmula para calcular la velocidad vertical necesaria para alcanzar jumpHeight:
            // v = sqrt(h * -2 * g)
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity); // 'gravity' ya es negativa
                                                                         // o usa Physics.gravity.y si quieres el global

            // Reproducir sonido de salto (opcional)
            if (jumpSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(jumpSound, footstepVolume); // Reusar footstepVolume o añadir jumpVolume
            }
            Debug.Log("Saltando con velocidad Y: " + verticalVelocity.y);
            jumpRequested = false; // Consumir la solicitud de salto
        }

        // Aplicar gravedad constantemente
        verticalVelocity.y += gravity * Time.deltaTime;

        // --- Mover el personaje ---
        // Combinar movimiento horizontal (calculado en HandleMovement) con el vertical
        float currentSpeed = isSprinting ? speed * sprintSpeedMultiplier : speed;
        Vector3 horizontalMove = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized * currentSpeed;
        Vector3 finalMove = (horizontalMove + verticalVelocity) * Time.deltaTime; // Aplicar Time.deltaTime aquí a la velocidad total

        controller.Move(finalMove);
    }


    // --- MODIFICADO: Pasar lookDelta como parámetro ---
    private void HandleLook(Vector2 lookDelta) // Aceptar el delta como parámetro
    {
        float mouseX = lookDelta.x * lookSensitivityX; // Ya no multiplicamos por Time.deltaTime aquí
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleFootsteps()
    {
        if (audioSource == null || footstepSounds == null || footstepSounds.Count == 0) return;

        bool isMovingOnGround = controller.isGrounded && moveInput != Vector2.zero;

        if (isMovingOnGround)
        {
            if (Time.time >= nextFootstepTime)
            {
                int randomIndex = Random.Range(0, footstepSounds.Count);
                audioSource.PlayOneShot(footstepSounds[randomIndex], footstepVolume);
                float timeBetweenSteps = isSprinting ? timeBetweenFootstepsSprinting : timeBetweenFootstepsWalking;
                nextFootstepTime = Time.time + timeBetweenSteps;
            }
        }
    }
}