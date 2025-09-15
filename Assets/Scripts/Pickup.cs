using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Pickup : MonoBehaviour
{
    [Header("Opcional")]
    [Tooltip("Identificador simple por si luego quieres contar/guardar en inventario.")]
    public string itemId = "coin";
    public int amount = 1;

    [Tooltip("Acciones a ejecutar al recoger (sonido, partículas, sumar score, etc.).")]
    public UnityEvent onCollected;

    [Tooltip("Si está activo, destruye el objeto; si no, lo desactiva (útil para pooling).")]
    public bool destroyOnCollect = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Ejecuta acciones (sonido, VFX, sumar puntos, etc.)
        onCollected?.Invoke();

        // Elimina o desactiva el pickup
        if (destroyOnCollect)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }
}
