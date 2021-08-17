using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerMode {
	IDLE,
	PRESSED,
	DEPLOYING,
	DEPLOYED,
	DRAG,
	DEAD
};


public class TriPlayer : MonoBehaviour, IPawn
{
	
	public static TriPlayer player;
	public static bool ready = false;

	public Vector3 bottom = new Vector3(0,0.28f,0);
	public float lengthForLongpress = 0.4f;
	public float secondsUntilHideMenu = 4f;
	public int maxSquaresPerTurn = 3;
	private PlayerMode mode = PlayerMode.IDLE;
	private float holdTime = 0;
	private Vector2 startPointerPos;
	private UnityEngine.AI.NavMeshAgent agent;
	private Vector3 priorPosition;
	private int hitPoints=10;
	
	private bool assessPath = false;
	private UnityEngine.AI.NavMeshPath tempPath;
	private ArrayList oldPath = new ArrayList();
	private GameObject lastExtremum;
	private Vector3 prospectiveEnd;
	
	public GameObject actionMenu;
	
	private Vector3 cameraDiff;
	private bool moving = false;
	public GridSquare currentSquare;
	public GridSquare enemySquare;
	
	private Animation animBox;
	
	public Dictionary <AttackType,int> baseDamage = new Dictionary<AttackType,int>{
		{AttackType.LIGHT,2},
		{AttackType.HEAVY,4},
		{AttackType.SPECIAL,3},
		{AttackType.BLOCK,0}
	};
	
	private AttackType[] map = new AttackType[]{AttackType.LIGHT,AttackType.HEAVY,AttackType.SPECIAL,AttackType.BLOCK};
	private AttackType chosenAttack= AttackType.NONE;
	
	private TurnMode turnPhase = TurnMode.BEGIN;
	
	
    // Start is called before the first frame update
    void Start()
    {
		player=this;
		OccupySquare();
		agent=GetComponent<UnityEngine.AI.NavMeshAgent>();
		agent.updateRotation = true;
		cameraDiff = Camera.main.transform.position - transform.position;
		animBox=GetComponentInChildren<Animation>();
		
    }

    // Update is called once per frame
    void Update()
    {
		if(HitPoints() == 0) {
			return; //no movement or combat actions for you
		}
		if(Attack.turn != GameStatus.PLAYER_TURN) {
			return;
		}
		if(turnPhase == TurnMode.END) {
			turnPhase = TurnMode.BEGIN;
			enemySquare = OpponentNearby();
			mode = PlayerMode.IDLE;
		}
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
		
		switch(turnPhase) {
			case TurnMode.COMBAT:
				DeployCommandMenu();
				break;
		}
		
		if(moving) {
			
			Camera.main.transform.position = transform.position + cameraDiff;
			moving = agent.remainingDistance > agent.stoppingDistance/2f;// && agent.velocity != Vector3.zero;
			if(!moving) {
				agent.updateRotation =false;
				SetHeading(agent.velocity);
				Debug.Log("stopped");
				OccupySquare();
				enemySquare = OpponentNearby();
				if(enemySquare !=null) {
					
					Debug.Log("Suspect "+enemySquare.transform.position.ToString("F2"));
					turnPhase=TurnMode.COMBAT;
				}
				
			}
		}
		
       
    }
    
    void SetHeading(Vector3 velocity) {
		Vector2 determinant = new Vector2(velocity.x,velocity.z);
		if(Mathf.Abs(velocity.x) > Mathf.Abs(velocity.z)) {
			determinant.y = 0;
		}
		else {
			determinant.x = 0;
		}	
		int angle = 90*Mathf.RoundToInt(Mathf.Rad2Deg*Mathf.Atan2(determinant.x,determinant.y)/90f);
		Vector3 currentRot = transform.eulerAngles;
		currentRot.y = angle;
		
		transform.eulerAngles=currentRot;
	}
    
    void OnMouseDown() {
		if(Attack.turn != GameStatus.PLAYER_TURN) {
			return;
		}
		
		if(mode == PlayerMode.IDLE) {
			startPointerPos = Input.mousePosition;
			holdTime = Time.time+lengthForLongpress;
			mode = PlayerMode.PRESSED;
		}
	}
	
	void OnMouseUp() {
		if(Attack.turn != GameStatus.PLAYER_TURN) {
			return;
		}
		if(mode == PlayerMode.PRESSED) {
			GameObject g= GetWhatsUnderIt(Input.mousePosition);
			if(g == gameObject) {
				turnPhase = TurnMode.COMBAT;
			//	mode = PlayerMode.DEPLOYING;
			}
			else
			{					
				mode = PlayerMode.IDLE;
			}
		}
		if(mode == PlayerMode.DRAG) {
			turnPhase = TurnMode.MOVE;
			ParkIt();
			holdTime = 0;
			mode = PlayerMode.IDLE;
		}
			
	}
	
	void OnMouseDrag() {
		if(Attack.turn != GameStatus.PLAYER_TURN) {
			return;
		}
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
		/*
		if(holdTime < Time.time) {
			mode = PlayerMode.DEPLOYING;
		}*/
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
		chosenAttack = map[chosen];
		Debug.Log("Player:"+chosenAttack);
		RetractCommandMenu();
		Attack.SetPlayerReady(this);
		EndTurn();
	}
	
	public void CancelAttack() {
		turnPhase = TurnMode.BEGIN;
		RetractCommandMenu();
	}
	
	public void EndTurn() {
		RetractCommandMenu();
		enemySquare = OpponentNearby();
		turnPhase= TurnMode.END;
		Attack.EndTurn(true);
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
		TriPlayer.ready = true;
		chosenAttack = AttackType.NONE;
		BlankOutPriorPath();
		GameObject g= GetWhatsUnderIt(Input.mousePosition);
		if(g==null||g.tag!="Tile"){
			
			return;
		}
		Vector3 endPosition = prospectiveEnd+bottom;
		agent.updateRotation =true;
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
		GridSquare inTransit;
		for(int i=1;i<numCorners;i++) {
			Vector3 diff = (tempPath.corners[i]-start);
			RaycastHit[] hitSquares = Physics.CapsuleCastAll(start,tempPath.corners[i],0.05f,diff.normalized,0.1f,1<<6);
			//sortoid.fromWhere = start;
			//Array.Sort(hitSquares,sortoid);
			DistanceSorter.Sort(start,hitSquares);
			for(int j=0;j<hitSquares.Length;j++)
			{
				inTransit = hitSquares[j].transform.GetComponent<GridSquare>();
				if(oldPath.IndexOf(inTransit) > -1) { //we processed this one already
					continue;
				}
				if(inTransit.IsOccupied(this)) {
					return;
				}
				inTransit.Mark();
				prospectiveEnd = inTransit.transform.position;
				oldPath.Add(inTransit);
				
				if(oldPath.Count >= maxSquaresPerTurn) {
					return;
				}
			}
			prospectiveEnd = tempPath.corners[i];
			start = tempPath.corners[i];
		}
	}
	
	
	
	public void OccupySquare() {
		if(currentSquare!=null) {
			currentSquare.Vacate();
		}
		Vector3 position;
		if(prospectiveEnd == null)
		{
			position = transform.position;
		}
		else 
		{
			position = prospectiveEnd+bottom;
		}
			
		RaycastHit hitInfo;
		Physics.SphereCast(position,0.25f,-Vector3.up,out hitInfo, 1.5f,1<<6);
		currentSquare = hitInfo.transform.GetComponent<GridSquare>();
		//Debug.Log(currentSquare.transform.position.ToString("F2"));
		currentSquare.Occupy(this);
	}
	
	public Vector2 GetPosition() {
		return new Vector2(currentSquare.transform.position.x,currentSquare.transform.position.z);
	}
	
	public AttackType ChosenAttack() {
		return chosenAttack;
	}
	
	public int HitPoints() {
		return Mathf.Max(hitPoints,0);
	}
	public void RunBlockAnim(int damage = 0) {
		hitPoints -= damage;
		if(damage == 0) {
			animBox.Play("Block");
			return;
		}
		Debug.Log("SUSTAINED "+damage);
		if(hitPoints <=0) {
			mode = PlayerMode.DEAD; //to prevent response to user activity.
			Attack.NotifyDead(this);
		}
		else
		{
			animBox.Play("Hit");
		}
		
	}
	
	public int DealtDamage() {
		return baseDamage[chosenAttack];
	}
	
	public void RunLightAnim() {
		Debug.Log("PLAYER:Light");
		animBox.Play("Light");
	}
	
	public void RunHeavyAnim() {
		Debug.Log("Aimed at "+enemySquare.transform.position.ToString("F2"));
		enemySquare.occupant.Shove();
		Debug.Log("PLAYER:Heavy");
		animBox.Play("Heavy");
	}
	
	public void RunSpecialAnim() {
		Debug.Log("PLAYER:Special");
	}
	
	public void Shutdown() {
		currentSquare.Vacate();
		//play death animation, then queue destruction
		animBox.Play("Death");
		Destroy(gameObject, 3f+animBox["Death"].length);
	}
	
	public string GetTag() {
		return gameObject.tag;
	}
	
	public GridSquare OpponentNearby() {
		RaycastHit[] tiles;
		
		tiles=Physics.SphereCastAll(currentSquare.transform.position,1f,-Vector3.up, 1.5f,1<<6);
		
		for(int i=tiles.Length-1;i>-1;i--) {
			if(tiles[i].transform.GetComponent<GridSquare>().IsOccupied(this)) {
				SetHeading(tiles[i].transform.position - currentSquare.transform.position);
				return tiles[i].transform.GetComponent<GridSquare>();
			}
		}
		return null;
	}
		
	public void Shove() {
	}
}
