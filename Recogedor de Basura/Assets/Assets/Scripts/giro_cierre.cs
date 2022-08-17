using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class giro_cierre : MonoBehaviour
{
    public bool arriba;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if(arriba)
        {
            if(transform.localEulerAngles.x<=313 || transform.localEulerAngles.x>=317)
            {
                transform.Rotate(new Vector3(-45, 0, 0) * Time.deltaTime);
            }
        }
        else if(!arriba)
        {
            if(transform.localEulerAngles.x>=300 )
            {
                transform.Rotate(new Vector3(45, 0, 0) * Time.deltaTime);
            }
        }
        if (Input.GetKeyDown(KeyCode.Space) )
        {
            if(!arriba)
            {
                arriba=true;
            }
            else if(arriba)
            {
                arriba=false;
            }
        }
    }
}
