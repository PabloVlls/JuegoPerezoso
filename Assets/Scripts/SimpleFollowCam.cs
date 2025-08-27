using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleFollowCam : MonoBehaviour
{
    [Header("Follow target")]
    public Transform target;                 // Asigna tu Player aquí

    [Header("Framing")]
    public Vector3 offset = new Vector3(0f, 2f, -10f);
    public float smoothTime = 0.15f;         // suavizado (0.1–0.3 suele ir bien)
    Vector3 velocity = Vector3.zero;

    [Header("Bloqueos de ejes (útil para centrar el tronco)")]
    public bool lockXToZero = true;          // deja la X fija para que el tronco quede centrado
    public bool lockZToOffset = true;        // mantiene el Z del offset (distancia fija)

    void LateUpdate()
    {
        if (!target) return;

        // Posición objetivo básica
        Vector3 wanted = target.position + offset;

        // Opcional: mantener el tronco centrado en X y la distancia Z estable
        if (lockXToZero) wanted.x = offset.x;      // normalmente 0
        if (lockZToOffset) wanted.z = target.position.z + offset.z;

        // Suavizado con SmoothDamp (suele sentirse mejor que Lerp)
        transform.position = Vector3.SmoothDamp(transform.position, wanted, ref velocity, smoothTime);

        // Mantén un ligero look hacia arriba del jugador para encuadre
        var lookPoint = target.position + Vector3.up * 1f;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(lookPoint - transform.position, Vector3.up), 0.25f);
    }
}

