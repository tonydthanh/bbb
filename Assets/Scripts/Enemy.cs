using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour, IPawn
{
	public Vector3 bottom = new Vector3(0,0.28f,0);
	public  Dictionary<AttackType,int> range = new Dictionary<AttackType,int>{
		{AttackType.LIGHT,1},
		{AttackType.HEAVY,3}
	};
	public Dictionary <AttackType,int> baseDamage = new Dictionary<AttackType,int>{
		{AttackType.LIGHT,2},
		{AttackType.HEAVY,4},
		{AttackType.SPECIAL,3},
	};
	private GridSquare currentSquare;
	
	private AttackType chosenAttack= AttackType.NONE;
	private int charges = 1;
	private int hitPoints=10;
	
	private TurnMode turnPhase = TurnMode.BEGIN;
	
	
	private UnityEngine.AI.NavMeshAgent agent;
	
	private UnityEngine.AI.NavMeshPath tempPath;
	private ArrayList oldPath = new ArrayList();
	public int maxSquaresPerTurn = 2;
	private Vector3 prospectiveEnd;
	private bool inMotion = false;
	
    // Start is called before the first frame update
    void Start()
    {
        OccupySquare();
		agent=GetComponent<UnityEngine.AI.NavMeshAgent>();
		agent.updateRotation = true;
    }

    // Update is called once per frame
    void Update()
    {
		if(HitPoints() == 0) {
			//prepare for cleanup, and prevent any further combat/movement actions
			return;
		}
		if(Attack.turn != GameStatus.OPPONENT_TURN){
			return;
		}
		if(turnPhase == TurnMode.END) {
			turnPhase = TurnMode.BEGIN;
		}
		switch(turnPhase) {
			case TurnMode.BEGIN:
				ProposeMove();
				break;
			case TurnMode.ASSESS_PATH:
				AssessPath();
				break;
			case TurnMode.MOVE:
				Move();
				break;
			case TurnMode.COMBAT:
				if(CanStrike()) {
					ChooseAttack();
					TriPlayer.ready = false;
					Attack.SetEnemyReady(this);
				}
				EndTurn();
				break;
		}
        
    }
    
    public void EndTurn() {
		turnPhase= TurnMode.END;
		Attack.EndTurn();
	}
    
    public void ProposeMove() {
		
		tempPath = new UnityEngine.AI.NavMeshPath();
		agent.CalculatePath(TriPlayer.player.currentSquare.transform.position, tempPath);
		turnPhase = TurnMode.ASSESS_PATH;
	}
	
	void BlankOutPriorPath() {
		//Tiles along path go back to prior color
		foreach(GridSquare gs in oldPath) {
			gs.Unmark();
		}
		oldPath.Clear();
	}
	
	public void AssessPath() {
		if(tempPath.status == UnityEngine.AI.NavMeshPathStatus.PathInvalid) {
			return;
		}
		
		turnPhase = TurnMode.MOVE;
		
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
	
	
	public void Move() {
		if(!inMotion) {
			chosenAttack = AttackType.NONE;
			BlankOutPriorPath();
			
			Vector3 endPosition = prospectiveEnd+bottom;
			agent.updateRotation =true;
			agent.SetDestination(endPosition);
			inMotion = true;
		}
		else {
			inMotion = agent.remainingDistance > agent.stoppingDistance/2f;// && agent.velocity != Vector3.zero;
			if(!inMotion) {
				agent.updateRotation =false;
				SetHeading(agent.velocity);
				OccupySquare();
				if(OpponentNearby()) {
					turnPhase=TurnMode.COMBAT;
				}
				else
				{
					EndTurn();
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
	
	public void OccupySquare() {
		if(currentSquare!=null) {
			currentSquare.Vacate();
		}
		RaycastHit hitInfo;
		Physics.SphereCast(transform.position,0.05f,-Vector3.up,out hitInfo, 1.5f,1<<6);
		currentSquare = hitInfo.transform.GetComponent<GridSquare>();
		currentSquare.Occupy(this);
	}
	
	protected void ChooseAttack() {
		int distance = DistToPlayer();
		//if we're here, at least one of our attacks is in range
		//consider the Special a last resort
		chosenAttack = AttackType.NONE;
		if(distance <= range[AttackType.HEAVY]) {
			chosenAttack = AttackType.HEAVY;		}
		if(distance <= range[AttackType.LIGHT]) {
			//depending on how aggressive we are, we could probably deal more damage with the HEAVY at close range
			//chosenAttack = AttackType.LIGHT;
		}
		if(chosenAttack == AttackType.NONE) {
			if(CanUseSpecial() && SpecialCouldReach(distance))
			{
				chosenAttack = AttackType.SPECIAL;
			}
		}
		Debug.Log("CPU:"+chosenAttack);
	}
	
	public AttackType ChosenAttack() {
		return chosenAttack;
	}
	
	protected bool CanStrike() {
		//Is the player within striking distance of any of our attacks?
		int distance = DistToPlayer();
		if(distance < 1) {
			return false;
		}
		if(CanUseSpecial() && SpecialCouldReach(distance))
		{
			return true;
		}
		if(distance > range[AttackType.LIGHT])
		{
			return false;
		}
		return true;
	}
	
	protected bool SpecialCouldReach(int distance) { //holy (expletive) this is abstract
		return (distance < 6); //cheesy, but proof-of-concept
			
	}
	
	protected bool CanUseSpecial() {
		return charges > 0; 
	}
	
	public bool OpponentNearby() {
		RaycastHit[] tiles;
		
		tiles=Physics.SphereCastAll(currentSquare.transform.position,1f,-Vector3.up, 1.5f,1<<6);
		Debug.Log(tiles.Length);
		for(int i=tiles.Length-1;i>-1;i--) {
			if(tiles[i].transform.GetComponent<GridSquare>().IsOccupied(this)) {
				return true;
			}
		}
		return false;
	}
	
	public Vector2 GetPosition() {
		return new Vector2(currentSquare.transform.position.x,currentSquare.transform.position.z);
	}
	
	int DistToPlayer() {
		int dx = (int)Mathf.Abs(TriPlayer.player.GetPosition().x - GetPosition().x);
		int dz = (int)Mathf.Abs(TriPlayer.player.GetPosition().y - GetPosition().y);
	//	Debug.Log((TriPlayer.player.GetPosition()-GetPosition()).ToString("F1"));
		//exclude diagonals
		if(dx == 0) {
			return dz;
		}
		if(dz == 0) {
			return dx;
		}
		return 0; //can't hit
	}
	
	public int HitPoints() {
		return Mathf.Max(hitPoints,0);
	}
	public void RunBlockAnim(int damage = 0) {
		hitPoints -= damage;
		Debug.Log("UGH!"+damage);
	}
	
	public int DealtDamage() {
		return baseDamage[chosenAttack];
	}
	
	public void RunLightAnim() {
		Debug.Log("ENEMY:Light");
	}
	
	public void RunHeavyAnim() {
		Debug.Log("ENEMY:Heavy");
	}
	
	public void RunSpecialAnim() {
		Debug.Log("ENEMY:Special");
		charges--;
	}
	
	public void Shutdown() {
		currentSquare.Vacate();
		//play death animation, then
		Destroy(gameObject);
	}
}
