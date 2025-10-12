using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class FruitMagnetPickup : MonoBehaviour
{
    [Header("Magnet config")]
    [Tooltip("Segundos que estará activo el imán.")]
    public float duration = 6f;

    [Tooltip("Cuánto se expande el radio base del imán mientras dure el efecto.")]
    public float radiusBonus = 4f;

    [Header("FX (opcionales)")]
    public ParticleSystem collectVFX;
    public AudioClip collectSFX;

    [Tooltip("Si es true, destruye el objeto al recogerlo; si es false, solo lo desactiva (útil para pooling).")]
    public bool destroyOnCollect = true;

    private bool _collected = false;

    void Reset()
    {
        // Config recomendado para que OnTriggerEnter funcione con CharacterController
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_collected) return;
        if (!other.CompareTag("Player")) return;

        // Busca el componente FruitMagnet en el jugador (o en un hijo)
        var magnet = other.GetComponentInChildren<FruitMagnet>(true);
        if (magnet != null)
        {
            magnet.Activate(duration, radiusBonus);
        }
        else
        {
            Debug.LogWarning("FruitMagnetPickup: No se encontró FruitMagnet en el Player.");
        }

        // FX locales
        if (collectVFX) Instantiate(collectVFX, transform.position, Quaternion.identity);
        if (collectSFX) AudioSource.PlayClipAtPoint(collectSFX, transform.position);

        _collected = true;

        if (destroyOnCollect) Destroy(gameObject);
        else gameObject.SetActive(false);
    }
}
