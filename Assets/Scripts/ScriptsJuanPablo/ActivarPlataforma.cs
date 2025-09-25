using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivarPlataforma : MonoBehaviour
{
    public GameObject objetoActivar;

    void Start()
    {
        objetoActivar.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            objetoActivar.SetActive(false);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            objetoActivar.SetActive(true);
        }
    }
}
