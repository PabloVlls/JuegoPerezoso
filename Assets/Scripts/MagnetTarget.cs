using UnityEngine;

/// <summary>
/// Marca un objeto como atraíble por el FruitMagnet.
///
/// Setup:
/// 1. Colócalo en cada fruta/coleccionable que deba ser atraído.
/// 2. El objeto debe estar en el layer "Collectible" (el mismo que targetMask del FruitMagnet).
/// 3. (Opcional) Asigna un attractionPoint si quieres que el imán tire desde un punto específico.
/// </summary>
public class MagnetTarget : MonoBehaviour
{
    [Tooltip("Punto desde el que se calcula la atracción. Si es null, usa el transform del objeto.")]
    public Transform attractionPoint;

    [Tooltip("Si es false, el imán ignora este objeto.")]
    public bool eligible = true;

    public Vector3 GetWorldPosition()
    {
        if (attractionPoint != null && attractionPoint.gameObject.activeInHierarchy)
            return attractionPoint.position;

        return transform.position;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
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
