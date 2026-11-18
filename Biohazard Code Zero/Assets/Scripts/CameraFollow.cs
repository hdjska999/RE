using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Configuración de Seguimiento")]
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 0, -10);

    [Header("Fondo del Mapa (Opcional)")]
    public SpriteRenderer mapBackground; // Aquí arrastrarás tu mapa

    private Camera cam;
    private float camHeight;
    private float camWidth;

    [Header("Game Feel - Camera Shake")]
    private float shakeTimer = 0f;
    private float shakeTotalDuration = 0f;
    private float shakeCurrentIntensity = 0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        if (cam != null && cam.orthographic)
        {
            camHeight = cam.orthographicSize;
            camWidth = camHeight * cam.aspect;
        }

        // Si no se asignó target, buscar al Player automáticamente
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    public void Shake(float duracion, float intensidad)
    {
        shakeTotalDuration = duracion;
        shakeTimer = duracion;
        shakeCurrentIntensity = Mathf.Max(shakeCurrentIntensity, intensidad);
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Posición a la que quiere ir la cámara
        Vector3 desiredPosition = target.position + offset;
        Vector3 finalPosition = desiredPosition;

        if (mapBackground != null && cam != null)
        {
            // Calculamos los límites matemáticos basados en el tamaño de la imagen y de la cámara
            float minX = mapBackground.bounds.min.x + camWidth;
            float maxX = mapBackground.bounds.max.x - camWidth;
            float minY = mapBackground.bounds.min.y + camHeight;
            float maxY = mapBackground.bounds.max.y - camHeight;

            float clampedX = Mathf.Clamp(desiredPosition.x, minX, maxX);
            float clampedY = Mathf.Clamp(desiredPosition.y, minY, maxY);
            finalPosition = new Vector3(clampedX, clampedY, desiredPosition.z);
        }

        // Movimiento suavizado hacia la posición objetivo
        Vector3 smoothPos = Vector3.Lerp(transform.position, finalPosition, smoothSpeed);

        // Aplicar sacudida de cámara (Camera Shake)
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float factorDecay = shakeTotalDuration > 0f ? (shakeTimer / shakeTotalDuration) : 0f;
            Vector2 randomOffset = Random.insideUnitCircle * (shakeCurrentIntensity * factorDecay);
            smoothPos += new Vector3(randomOffset.x, randomOffset.y, 0f);

            if (shakeTimer <= 0f)
            {
                shakeCurrentIntensity = 0f;
            }
        }

        transform.position = smoothPos;
    }
}
