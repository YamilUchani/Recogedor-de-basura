using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Direccion : MonoBehaviour
{
    [SerializeField] WheelCollider delantederecha;
    [SerializeField] WheelCollider delanteizquierda;
    [SerializeField] WheelCollider atrasderecha;
    [SerializeField] WheelCollider atrasizquierda;

    [SerializeField] Transform delantederechamo;
    [SerializeField] Transform delanteizquierdamo;
    [SerializeField] Transform atrasderechamo;
    [SerializeField] Transform atrasizquierdamo;
    
    public float aceleracion=500f;
    public float fuerzaruptura=300f;
    public float angulo=15f;

    private float aceleracionactual=0f;
    private float fuerzaactual=0f;
    private float anguloactual=0f;
    private void FixedUpdate() {
        aceleracionactual=aceleracion*Input.GetAxis("Vertical");

        if(Input.GetKey(KeyCode.Space))
            fuerzaactual=fuerzaruptura;
        else
            fuerzaactual=0f;
        delantederecha.motorTorque=aceleracionactual;
        delanteizquierda.motorTorque=aceleracionactual;
        
        delantederecha.brakeTorque=fuerzaactual;
        delanteizquierda.brakeTorque=fuerzaactual;
        atrasderecha.brakeTorque=fuerzaactual;
        atrasizquierda.brakeTorque=fuerzaactual;
        anguloactual=angulo*Input.GetAxis("Horizontal");
        delanteizquierda.steerAngle=anguloactual;
        delantederecha.steerAngle=anguloactual;

        Actualizacionruedas(delanteizquierda,delanteizquierdamo);
        Actualizacionruedas(delantederecha,delantederechamo);
        Actualizacionruedas(atrasizquierda,atrasizquierdamo);
        Actualizacionruedas(atrasderecha,atrasderechamo);

    }
    void Actualizacionruedas(WheelCollider col,Transform trans){
        Vector3 posicion;
        Quaternion rotacion;
        col.GetWorldPose(out posicion, out rotacion);

        trans.position=posicion;
        trans.rotation=rotacion;
    }
 }
