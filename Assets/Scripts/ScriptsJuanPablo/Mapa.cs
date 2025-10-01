using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mapa : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed;
    public float sectionSize = 4;
    private int sectionsCount = 0;

    [Header("Dificultad / Velocidad")]
    public float incrementoVelocidad = 0.2f; //cuanto sube cada cierto tiempo
    public float tiempoPorIncremento = 10f; //cada cuantos segundos sube
    private float tiempoAcumulado = 0f;
    private int dificultadActual = 0; // 0 = fácil, 1 = medio, 2 = difícil

    // Lista con los elemnntos sin flitrar
    public List<GameObject> allElementos = new List<GameObject>();

    // Lista filtrada según dificultad
    public List<GameObject> elementos = new List<GameObject>();

    // Guardamos la última elección por referencia, no por índice
    private static GameObject lastSelectedElement = null;

    void Start()
    {
        sectionsCount = GameObject.FindGameObjectsWithTag("Section").Length;

        allElementos.Clear();
        foreach (Transform child in transform)
        {
            if (child.tag == "Elementos")
            {
                allElementos.Add(child.gameObject);
            }
        }

        // Inicializamos la dificultad ANTES de construir la lista disponible
        dificultadActual = 0;
        tiempoAcumulado = 0f;


        // Construir la lista "elementos" filtrada según dificultadActual
        UpdateAvailableList();

        // Activar el primer obstáculo permitido
        EnableRandomObstacle();
    }

    void Update()
    {
        //---Aumentar dificultad con el tiempo---
        tiempoAcumulado += Time.deltaTime;

        if (tiempoAcumulado >= tiempoPorIncremento)
        {
            tiempoAcumulado = 0f;
            speed += incrementoVelocidad;

            //Subimos el nivel actual
            if (dificultadActual < 2)
            {
                dificultadActual++;
                UpdateAvailableList();
            }
        }

        // Movimiento del mapa
        transform.Translate(Vector3.down * speed * Time.deltaTime);

        if (transform.position.y <= -sectionSize)
        {
            transform.Translate(Vector3.up * sectionSize * sectionsCount);
            EnableRandomObstacle();
        }

    }

    // Reconstruye "elementos" a partir de "allElementos" respetando dificultadActual
    private void UpdateAvailableList()
    {
        elementos.Clear();
        foreach (GameObject el in allElementos)
        {
            DificultadSeccion dif = el.GetComponent<DificultadSeccion>();
            if (dif != null)
            {
                if (dif.dificultad <= dificultadActual)
                    elementos.Add(el);
            }
        }
    }

    public void EnableRandomObstacle()
    {
        // Desactivar todos los grupos para evitar solapamientos
        foreach (GameObject elemento in elementos)
        {
            elemento.SetActive(false);
        }

        // Seleccionar un GameObject random distinto al último (si es posible)
        GameObject seleccionado = lastSelectedElement;
        int intentos = 0;
        while (seleccionado == lastSelectedElement && intentos < 10)
        {
            seleccionado = elementos[Random.Range(0, elementos.Count)];
            intentos++;
        }

        lastSelectedElement = seleccionado;
        seleccionado.SetActive(true);
    }

}
