using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateCube : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        cubo.transform.rotation = Quaternion.identity;
        cubo.transform.localScale = scale * (1f / 50f);

        Rigidbody cuboRigidbody = cubo.AddComponent<Rigidbody>();
        cuboRigidbody.useGravity = true;
        cuboRigidbody.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        BoxCollider cuboCollider = cubo.AddComponent<BoxCollider>();

        cubo.tag = "bache";

        float duracionCubo = 0.01f;
        Destroy(cubo, duracionCubo);
    }

}
