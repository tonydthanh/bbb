using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerMode {
	IDLE,
	PRESSED,
	DEPLOYING,
	DEPLOYED,
	DRAG
};



public class TriPlayer : MonoBehaviour, IPawn
{
	public Vector3 bottom = new Vector3(0,0.28f,0);
	public float lengthForLongpress = 0.4f;
	public float secondsUntilHideMenu = 4f;
	private PlayerMode mode = PlayerMode.IDLE;
	private float holdTime = 0;
	private Vector2 startPointerPos;
	private UnityEngine.AI.NavMeshAgent agent;
	private Vector3 priorPosition;
	
	private bool assessPath = false;
	private UnityEngine.AI.NavMeshPath tempPath;
	private ArrayList oldPath = new ArrayList();
	private GameObject lastExtremum;
	private Vector3 prospectiveEnd;
	
	public GameObject actionMenu;
	
	private Vector3 cameraDiff;
	private bool moving = false;
	
	private DistanceSorter sortoid = new DistanceSorter();
	
	public AttackType[] map = { AttackType.LIGHT,AttackType.HEAVY,AttackType.SPECIAL};
    // Start is called before the first frame update
    void Start()
    {
		agent=GetComponent<UnityEngine.AI.NavMeshAgent>();
		agent.updateRotation = false;
		cameraDiff = Camera.main.transform.position - transform.position;
		OccupyInitialSquare();
    }

    // Update is called once per frame
    void Update()
    {
		if(assessPath) {
			ShowTemporaryPath();
		}
        switch(mode) {
			case PlayerMode.PRESSED:
				break;
			case PlayerMode.DEPLOYING:
				DeployCommandMenu();
				mode = PlayerMode.DEPLOYED;
				break;
			case PlayerMode.DEPLOYED:
				if(Time.time >= holdTime) {
					RetractCommandMenu();
					mode = PlayerMode.IDLE;
				}
				break;
		}
		if(moving) {
			Camera.main.transform.position = transform.position + cameraDiff;
			moving = !agent.isStopped;
		}
		
       
    }
    
    void OnMouseDown() {
		if(mode == PlayerMode.IDLE) {
			Debug.Log("Down");
			startPointerPos = Input.mousePosition;
			holdTime = Time.time+lengthForLongpress;
			mode = PlayerMode.PRESSED;
		}
	}
	
	void OnMouseUp() {
		if(mode == PlayerMode.PRESSED) {
			Debug.Log("Released");
			holdTime = 0;
			mode = PlayerMode.IDLE;
		}
		if(mode == PlayerMode.DRAG) {
			ParkIt();
			holdTime = 0;
			mode = PlayerMode.IDLE;
		}
			
	}
	
	void OnMouseDrag() {
		Vector2 pointerPos = Input.mousePosition;
		if(mode == PlayerMode.DRAG) {
			MoveIt(pointerPos); 
			return;
		}
		if(mode == PlayerMode.PRESSED && (pointerPos - startPointerPos).magnitude > 10f) {
			priorPosition = transform.position;
			mode = PlayerMode.DRAG;
			return;
		}
		if(holdTime < Time.time) {
			mode = PlayerMode.DEPLOYING;
		}
	}
    
	
	GameObject GetWhatsUnderIt(Vector3 screenPoint) {
		Ray probe = Camera.main.ScreenPointToRay(screenPoint);
		RaycastHit hitInfo;
		if (Physics.Raycast(probe.origin,probe.direction, out hitInfo)) // If raycast hit a collider...
        {
			return hitInfo.transform.gameObject;
        }
        return null;
	}
	
	
	
	
	public void DeployCommandMenu() {
		actionMenu.SetActive(true);
		holdTime=Time.time+secondsUntilHideMenu;
	}
	
	public void RetractCommandMenu() {
		actionMenu.SetActive(false);
		holdTime = 0;
	}
	
	public void ChooseAttack(int chosen) {
		Debug.Log("Chose attack "+map[chosen].ToString());
		RetractCommandMenu();
	}
	
	void MoveIt(Vector2 screenPos) {
		Vector3 aha= Camera.main.ScreenPointToRay(screenPos).origin;
		
		GameObject g= GetWhatsUnderIt(screenPos);
		if(g==null||g.tag!="Tile"){
			assessPath = false;
			return;
		}
		if(g == lastExtremum) {
			assessPath = false;
			return;
		}
		BlankOutPriorPath();
		lastExtremum = g;
		GridSquare gs = g.GetComponent<GridSquare>();
		if(gs.Marked()) 
		{
			assessPath = false;
			return;
		}
		tempPath = new UnityEngine.AI.NavMeshPath();
		agent.CalculatePath(g.transform.position, tempPath);
		assessPath = true;
	}
	
	void ParkIt() {
		BlankOutPriorPath();
		GameObject g= GetWhatsUnderIt(Input.mousePosition);
		if(g==null||g.tag!="Tile"){
			
			return;
		}
		Vector3 endPosition = prospectiveEnd+bottom;
		agent.SetDestination(endPosition);
		moving = true;	
	}
	
	void BlankOutPriorPath() {
		//Tiles along path go back to prior color
		foreach(GridSquare gs in oldPath) {
			gs.Unmark();
		}
		oldPath.Clear();
	}
	
	void ShowTemporaryPath() {
		
		if(tempPath.status == UnityEngine.AI.NavMeshPathStatus.PathInvalid) {
			return;
		}
		
		assessPath = false;
		
		
		int numCorners = tempPath.corners.Length;
		Vector3 start=tempPath.corners[0];
		prospectiveEnd = tempPath.corners[0];
		for(int i=1;i<numCorners;i++) {
			Vector3 diff = (tempPath.corners[i]-start);
			RaycastHit[] hitSquares = Physics.CapsuleCastAll(start,tempPath.corners[i],0.05f,diff.normalized,0.1f,1<<6);
			sortoid.fromWhere = start;
			Array.Sort(hitSquares,sortoid);
			for(int j=0;j<hitSquares.Length;j++)
			{
				if(hitSquares[j].transform.GetComponent<GridSquare>().IsOccupied(this)) {
					return;
				}
				hitSquares[j].transform.GetComponent<GridSquare>().Mark();
				prospectiveEnd = hitSquares[j].transform.position;
				oldPath.Add(hitSquares[j].transform.GetComponent<GridSquare>());
			}
			prospectiveEnd = tempPath.corners[i];
			start = tempPath.corners[i];
		}
	}
	
	public void OccupyInitialSquare() {
		RaycastHit hitInfo;
		Physics.SphereCast(transform.position,0.05f,-Vector3.up,out hitInfo, 1.5f,1<<6);
		hitInfo.transform.GetComponent<GridSquare>().Occupy(this);
	}
	
	private class DistanceSorter : IComparer {
		public Vector3 fromWhere;
		public int Compare (object x, object y) {
			
			if(x == null) {
				return -1;
			}
			if(y == null) {
				return 1;
			}
			
			float a = (((RaycastHit)x).transform.position - fromWhere).magnitude;
			float b = (((RaycastHit)y).transform.position - fromWhere).magnitude;
			
			if(a < b) {
				return -1;
			}
			if(a > b) {
				return 1;
			}
			return 0;
		}
	}
}
