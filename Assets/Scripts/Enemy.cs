using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour, IPawn
{
	public Vector3 bottom = new Vector3(0,0.28f,0);
    // Start is called before the first frame update
    void Start()
    {
        
		OccupyInitialSquare();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void OccupyInitialSquare() {
		RaycastHit hitInfo;
		Physics.SphereCast(transform.position,0.05f,-Vector3.up,out hitInfo, 1.5f,1<<6);
		hitInfo.transform.GetComponent<GridSquare>().Occupy(this);
	}
}
