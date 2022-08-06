using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movimiento : MonoBehaviour
{ 
    public int movementSpeed;
    public int rotateSpeed;
    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.A))
            transform.Rotate(Vector3.up,rotateSpeed*-1*Time.deltaTime);
        if (Input.GetKey(KeyCode.D))
            transform.Rotate(Vector3.up,rotateSpeed*1*Time.deltaTime);
        if (Input.GetKey(KeyCode.W))
            GetComponent<Rigidbody>().AddForce(transform.forward*movementSpeed);
        if (Input.GetKey(KeyCode.S))
            GetComponent<Rigidbody>().AddForce(transform.forward*-movementSpeed);
    }

}