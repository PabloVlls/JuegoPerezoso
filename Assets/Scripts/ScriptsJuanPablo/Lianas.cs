using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lianas : MonoBehaviour
{
    public float liftSpeed = 2f;
    public GameObject objetoActivar;

    void Start()
    {
        objetoActivar.SetActive(false);
    }
    
    //---Activar/Desactivar plataforma oculta---
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

    //--- Física del jugador para subir---
    private void OnTriggerStay(Collider other)
    {
        CharacterController controller = other.GetComponent<CharacterController>();
        if (controller != null)
        {
            Vector3 upwardMovement = Vector3.up * liftSpeed * Time.deltaTime;
            controller.Move(upwardMovement);
            Debug.Log("Entra");
        }
    }
}
