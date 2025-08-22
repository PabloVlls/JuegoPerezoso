using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
     [System.Serializable]
    public class Pool
    {
        public string nombre;
        public GameObject prefab;
        public int size;
    }

    public static PoolManager Instance;

    public List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.nombre, objectPool);
        }
    }

    public GameObject SpawnFromPool(string nombre, Vector3 posicion, Quaternion rotacion)
    {
        if (!poolDictionary.ContainsKey(nombre))
        {
            Debug.LogWarning("No existe el pool: " + nombre);
            return null;
        }

        GameObject objeto = poolDictionary[nombre].Dequeue();

        objeto.SetActive(true);
        objeto.transform.position = posicion;
        objeto.transform.rotation = rotacion;

        poolDictionary[nombre].Enqueue(objeto);

        return objeto;
    }
}
