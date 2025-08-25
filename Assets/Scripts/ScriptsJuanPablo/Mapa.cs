using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mapa : MonoBehaviour
{
    public List<GameObject> elementos;
    public float speed;
    public float sectionSize = 4;
    private int sectionsCount = 0;
    private static int lastRandomIndex = -1;

    void Start()
    {
        sectionsCount = GameObject.FindGameObjectsWithTag("Section").Length;

        elementos = new List<GameObject>();
        foreach (Transform child in transform)
        {
            if (child.tag == "Elementos")
            {
                elementos.Add(child.gameObject);
            }
        }

        EnableRandomObstacle();
    }

    void Update()
    {
        transform.Translate(Vector3.down * speed * Time.deltaTime);

        if (transform.position.y <= -sectionSize)
        {
            transform.Translate(Vector3.up * sectionSize * sectionsCount);

            EnableRandomObstacle();
        }
    }

    public void EnableRandomObstacle()
    {
        foreach (GameObject elemento in elementos)
        {
            elemento.SetActive(false);
        }

        int randomIndex = lastRandomIndex;
        while (randomIndex == lastRandomIndex)
        {
            randomIndex = Random.Range(0, elementos.Count);
        }

        lastRandomIndex = randomIndex;
        elementos[randomIndex].SetActive(true);
    }

}
