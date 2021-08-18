using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSquare : MonoBehaviour
{
	private bool marked = false;
	public Material highlight;
	private Material original;
	public IPawn occupant;
	//private Renderer renderer;
    // Start is called before the first frame update
    void Start()
    {
      //  renderer = GetComponent<Renderer>();
        original = GetComponent<Renderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public bool Marked() {
		return marked;
	}
	public void Mark() {
		GetComponent<Renderer>().material = highlight;
		marked = true;
	}
	public void Unmark() {
		GetComponent<Renderer>().material = original;
		marked = false;
	}
	
	public void Vacate() {
		occupant = null;
	}
	
	public virtual void Occupy(IPawn incoming) {
		occupant = incoming;
	}
	
	public bool IsOccupied(IPawn inquisitor) {
		return occupant != null && occupant != inquisitor;
	}
}
