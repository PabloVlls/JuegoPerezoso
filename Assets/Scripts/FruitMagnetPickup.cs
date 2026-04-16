using UnityEngine;

/// <summary>
/// Pickup que activa el imán de frutas al ser recogido por el Player.
///
/// Setup:
/// 1. Ponlo en el objeto coleccionable (el "power-up de imán").
/// 2. El objeto necesita un Collider (isTrigger = true) y Rigidbody (isKinematic = true).
/// 3. El Player necesita el tag "Player" y tener FruitMagnet en su jerarquía.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class FruitMagnetPickup : MonoBehaviour
{
    [Header("Magnet config")]
    [Tooltip("Segundos que estará activo el imán.")]
    [SerializeField] float duration = 6f;

    [Tooltip("Cuánto se expande el radio base del imán mientras dure el efecto.")]
    [SerializeField] float radiusBonus = 4f;

    [Header("FX (opcionales)")]
    [SerializeField] ParticleSystem collectVFX;
    [SerializeField] AudioClip collectSFX;

    [Tooltip("Si es true, destruye el objeto al recogerlo; si es false, lo desactiva (pooling).")]
    [SerializeField] bool destroyOnCollect = true;

    bool _collected = false;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_collected) return;
        if (!other.CompareTag("Player")) return;

        // Busca FruitMagnet en el player: primero en él mismo, luego hijos, luego padres
        FruitMagnet magnet = other.GetComponent<FruitMagnet>();
        if (magnet == null) magnet = other.GetComponentInChildren<FruitMagnet>(true);
        if (magnet == null) magnet = other.GetComponentInParent<FruitMagnet>();

        if (magnet != null)
        {
            magnet.Activate(duration, radiusBonus);
        }
        else
        {
            Debug.LogWarning($"FruitMagnetPickup: No se encontró FruitMagnet en '{other.name}' ni en su jerarquía.");
        }

        // FX
        if (collectVFX != null) Instantiate(collectVFX, transform.position, Quaternion.identity);
        if (collectSFX != null) AudioSource.PlayClipAtPoint(collectSFX, transform.position);

        _collected = true;

        if (destroyOnCollect)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    // Si se reutiliza con pooling, resetear el flag
    void OnEnable()
    {
        _collected = false;
    }
}
