using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Player : MonoBehaviour
{
	public float factor = 0.5f;
	public float moveDuration = .5f;
	public float fps=60f;
	//the dimensions will help us stay fully onscreen
	public float height = 2f;
	public float width = 4f;
	private Vector3 position = new Vector3(0.5f,0.5f,10f);
	float augendLateral=0;
	float augendVertical=0;
	private float lerpPos = 0;
	Vector3 currentPos;
	Vector3 targetPos;
	bool moving = false;
	
    // Start is called before the first frame update
    void Start()
    {
		
    }

    // Update is called once per frame
    void Update()
    {
        if(moving) {
			InterpolateMovement();
		}
    }
    
    public void HorizontalMove(float nuval) {
		position.x=0.5f+nuval*factor;
		augendLateral = -nuval*width;
		Move();
	}
	
	public void VerticalMove(float nuval) {
		position.y=0.5f+nuval*factor;
		augendVertical = -nuval*height;
		Move();
		
	}
	
	void Move() {
		currentPos = transform.position;
		Vector3 worldPos = Camera.main.ViewportToWorldPoint(position);
		worldPos.x+=augendLateral;
		worldPos.y+=augendVertical;
		targetPos = worldPos;
		moving = true;
		Debug.Log(worldPos.ToString("F2"));
		
		lerpPos=0;
	}
	
	void InterpolateMovement() {
		lerpPos += Time.deltaTime;
		float t=Mathf.Min(1f,lerpPos/moveDuration);
		transform.position = Vector3.Lerp(currentPos,targetPos,t);
		if(t<1f) {
			return;
		}
		moving = false;
		
	}
}
