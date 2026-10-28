using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Configuración de Seguimiento")]
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 0, -10);

    [Header("Fondo del Mapa")]
    public SpriteRenderer mapBackground; // Aquí arrastrarás tu mapa

    private Camera cam;
    private float camHeight;
    private float camWidth;

    void Start()
    {
        cam = Camera.main;
        
        // Calculamos cuánto mide la cámara en la pantalla
        camHeight = cam.orthographicSize;
        camWidth = camHeight * cam.aspect;
    }

    void LateUpdate()
    {
        if (target == null || mapBackground == null) return;

        // Posición a la que quiere ir la cámara
        Vector3 desiredPosition = target.position + offset;

        // Calculamos los límites matemáticos basados en el tamaño de la imagen y de la cámara
        float minX = mapBackground.bounds.min.x + camWidth;
        float maxX = mapBackground.bounds.max.x - camWidth;
        float minY = mapBackground.bounds.min.y + camHeight;
        float maxY = mapBackground.bounds.max.y - camHeight;

        // Evitamos que la cámara pase de esos límites
        float clampedX = Mathf.Clamp(desiredPosition.x, minX, maxX);
        float clampedY = Mathf.Clamp(desiredPosition.y, minY, maxY);

        Vector3 clampedPosition = new Vector3(clampedX, clampedY, desiredPosition.z);
        
        // Aplicamos el movimiento suavizado
        transform.position = Vector3.Lerp(transform.position, clampedPosition, smoothSpeed);
    }
}