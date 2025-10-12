using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnetTarget : MonoBehaviour
{
    public Transform attractionPoint;
    public bool eligible = true;

    public Vector3 GetWorldPosition()
    {
        // Si el attractionPoint no está seteado o está desactivado, usa el propio transform
        if (attractionPoint == null || !attractionPoint.gameObject.activeInHierarchy)
            return transform.position;

        return attractionPoint.position;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Evita asignar por error un punto que esté en el Player (típico fallo)
        if (attractionPoint != null)
        {
            var root = attractionPoint.root;
            if (root.CompareTag("Player"))
            {
                Debug.LogWarning($"{name}: AttractionPoint no debe ser del Player. Limpiando referencia.");
                attractionPoint = null;
            }
        }
    }
#endif
}
