using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mapa : MonoBehaviour
{
    public Transform player;
    public string[] bloquesDisponibles; // nombres de los pools
    public int bloquesActivosMax = 5;   // cuántos bloques mantener siempre activos

    private float siguientePosicionY = 0f;
    private Queue<GameObject> bloquesActivos = new Queue<GameObject>();

    void Start()
    {
        // Inicializamos con los primeros bloques
        for (int i = 0; i < bloquesActivosMax; i++)
        {
            SpawnBloque();
        }
    }

    void Update()
    {
        // Cuando el jugador se acerque al final de los bloques → mover uno arriba
        if (player.position.y + 20f > siguientePosicionY)
        {
            ReciclarBloque();
        }
    }

    void SpawnBloque()
    {
        // Elegir bloque aleatorio
        string nombre = bloquesDisponibles[Random.Range(0, bloquesDisponibles.Length)];
        GameObject bloque = PoolManager.Instance.SpawnFromPool(nombre, new Vector3(0, siguientePosicionY, 0), Quaternion.identity);

        // Buscar punto de referencia para la próxima posición
        Transform nextPoint = bloque.transform.Find("NextSpawnPoint");
        if (nextPoint != null) 
        {
            siguientePosicionY = bloque.transform.position.y + nextPoint.localPosition.y;
        } 
        else
        {
            Debug.LogWarning($"El prefab {bloque.name} no tiene un NextSpawnPoint. Agrega uno en la parte superior del bloque.");
        }
           
        bloquesActivos.Enqueue(bloque);

        // Generación procedural de ítems dentro del bloque
        GenerarObjetosAleatorios(bloque);
    }

    void ReciclarBloque()
    {
        // Tomamos el bloque más viejo (el que quedó abajo)
        GameObject bloqueViejo = bloquesActivos.Dequeue();

        // Lo recolocamos arriba
        bloqueViejo.transform.position = new Vector3(0, siguientePosicionY, 0);

        // Actualizamos el siguiente punto de spawn
        Transform nextPoint = bloqueViejo.transform.Find("NextSpawnPoint");
        if (nextPoint != null)
        {
            siguientePosicionY = bloqueViejo.transform.position.y + nextPoint.localPosition.y;
        }  
        else
        {
            Debug.LogWarning($"El prefab {bloqueViejo.name} no tiene un NextSpawnPoint. Agrega uno en la parte superior del bloque.");
        }
            
        // Limpiar ítems viejos antes de regenerar
        LimpiarObjetos(bloqueViejo);

        // Vuelve a generar contenido aleatorio en este bloque
        GenerarObjetosAleatorios(bloqueViejo);

        // Se mete de nuevo a la cola de activos
        bloquesActivos.Enqueue(bloqueViejo);
    }

    void GenerarObjetosAleatorios(GameObject bloque)
    {
        Transform spawnPoints = bloque.transform.Find("SpawnPoints");
        if (spawnPoints == null) return;

        foreach (Transform punto in spawnPoints)
        {
            if (Random.value < 0.4f) // 40% de chance de spawnear algo
            {
                PoolManager.Instance.SpawnFromPool("PowerUp", punto.position, Quaternion.identity);
            }
        }
    }

    void LimpiarObjetos(GameObject bloque)
    {
        // Si tus ítems son hijos del bloque, puedes destruirlos o desactivarlos aquí
        Transform spawnPoints = bloque.transform.Find("SpawnPoints");
        if (spawnPoints == null) return;

        foreach (Transform punto in spawnPoints)
        {
            // Elimina cualquier objeto previo en este punto
            foreach (Transform hijo in punto)
            {
                Destroy(hijo.gameObject); // O mejor: usar pooling si los ítems también se reciclan
            }
        }
    }
}
