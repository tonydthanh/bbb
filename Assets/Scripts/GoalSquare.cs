using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoalSquare : GridSquare
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public override void Occupy(IPawn incoming) {
		base.Occupy(incoming);
		if(incoming.GetTag()=="Player") {
			Attack.NotifyWon();
		}
	}
}
