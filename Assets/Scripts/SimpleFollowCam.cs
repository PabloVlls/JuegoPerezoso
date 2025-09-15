using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleFollowCam : MonoBehaviour
{
    public Transform target;          // Player
    public float yOffset = 2f;        // cuánto encima del Player
    public float smoothTime = 0.12f;  // suavizado vertical

    float _yVel;                      // interno para SmoothDamp
    float _fixedX, _fixedZ;           // X/Z fijas
    Quaternion _fixedRotation;        // rotación fija (sin tilt)

    void Awake()
    {
        // Guarda la posición y rotación iniciales para fijarlas
        _fixedX = transform.position.x;
        _fixedZ = transform.position.z;
        _fixedRotation = transform.rotation;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Solo ajusta Y con suavizado
        float targetY = target.position.y + yOffset;
        float newY = Mathf.SmoothDamp(transform.position.y, targetY, ref _yVel, smoothTime);

        // Mantén X/Z y la rotación inicial
        transform.position = new Vector3(_fixedX, newY, _fixedZ);
        transform.rotation = _fixedRotation;
    }
}

