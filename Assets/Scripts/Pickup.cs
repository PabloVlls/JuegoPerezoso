using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Pickup genérico. Al tocarlo el Player, ejecuta acciones y se destruye/desactiva.
/// </summary>
public class Pickup : MonoBehaviour
{
    [Header("Datos")]
    [Tooltip("Identificador del item (para inventario/score).")]
    public string itemId = "coin";
    public int amount = 1;

    [Header("Eventos")]
    [Tooltip("Acciones a ejecutar al recoger (sonido, partículas, sumar score, etc.).")]
    public UnityEvent onCollected;

    [Tooltip("Si es true, destruye el objeto; si es false, lo desactiva (pooling).")]
    public bool destroyOnCollect = true;

    bool _collected = false;

    void OnTriggerEnter(Collider other)
    {
        if (_collected) return;
        if (!other.CompareTag("Player")) return;

        onCollected?.Invoke();
        _collected = true;

        if (destroyOnCollect)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    void OnEnable()
    {
        _collected = false;
    }
}
