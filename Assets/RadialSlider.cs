using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ludiq;
using Bolt;


public class RadialSlider : MonoBehaviour
{
    Vector3 mousePos;
    public GameObject robot;
    public string variableName;
    Quaternion rotation;
    float angle = 0.0f;

    private void Start()
    {
        rotation = transform.rotation;
    }

    public void onHandleDrag() {
        mousePos = Input.mousePosition;
        Vector2 dir = mousePos - transform.position;
        angle = (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        angle = (angle <= 0) ? (360 + angle) : angle;

        rotation = Quaternion.AngleAxis(angle, Vector3.forward);

    }

    private void Update()
    {
        Quaternion val = Quaternion.Lerp(transform.rotation, rotation, .2f);
        transform.rotation = val;

        Variables.Object(robot).Set(variableName, val);
    }


}
