using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class CoffeeBean : MonoBehaviour
{
    public int points = 1;
    public ParticleSystem collectVFX;
    public AudioClip collectSFX;
    public bool destroyOnCollect = true;
    private bool collected = false;

    void Reset()
    {
        var col = GetComponent<Collider>(); col.isTrigger = true;
        var rb = GetComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        if (ScoreSystem.Instance != null)
            ScoreSystem.Instance.AddCoffeeBeanPoints(points);

        if (collectVFX) Instantiate(collectVFX, transform.position, Quaternion.identity);
        if (collectSFX) AudioSource.PlayClipAtPoint(collectSFX, transform.position);

        collected = true;
        if (destroyOnCollect) Destroy(gameObject);
        else gameObject.SetActive(false);
    }
}
