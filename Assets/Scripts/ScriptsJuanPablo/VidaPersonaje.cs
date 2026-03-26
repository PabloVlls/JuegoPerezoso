using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VIdaPersonaje : MonoBehaviour
{
    public int vidaMax = 3;

    void Update()
    {
        Debug.Log(vidaMax);

        if (vidaMax == 0)
        {
            Destroy(this.gameObject);
            Time.timeScale = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Muerte"))
        {
            vidaMax --;
            Destroy(other.gameObject);
        }
    }
}
