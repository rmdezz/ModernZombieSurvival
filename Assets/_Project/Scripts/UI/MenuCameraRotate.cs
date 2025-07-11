using UnityEngine;

public class MenuCameraRotate : MonoBehaviour
{
    [Tooltip("Punto alrededor del cual rotará la cámara (ej. el centro del diorama).")]
    [SerializeField] private Transform pivotPoint;

    [Tooltip("Velocidad de rotación en grados por segundo.")]
    [SerializeField] private float rotationSpeed = 5f;

    void Awake()
    {
        if (pivotPoint == null)
        {
            Debug.LogWarning("Pivot Point no asignado en MenuCameraRotate. La cámara no rotará.", this);
            enabled = false;
        }
    }

    void Update()
    {
        // Rotar la cámara alrededor del punto pivote en el eje Y (horizontalmente)
        transform.RotateAround(pivotPoint.position, Vector3.up, rotationSpeed * Time.deltaTime);

        // (Opcional) Hacer que la cámara siempre mire al punto pivote
        // Esto es útil si la cámara se mueve además de rotar.
        // transform.LookAt(pivotPoint); 
    }
}