using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlataformasFalsas : MonoBehaviour
{
    public GameObject plataformaFalsa;

    private void OnTriggerEnter (Collider other)
    {
        if (other.CompareTag("Player"))
        {
            plataformaFalsa.SetActive(false);
        }
    }
}
