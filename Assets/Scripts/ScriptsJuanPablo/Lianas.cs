using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lianas : MonoBehaviour
{
    public float liftSpeed = 2f;

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
