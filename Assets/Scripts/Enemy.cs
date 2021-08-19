using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour, IPawn
{
	public string species="Pigbot";
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
	public GridSquare currentSquare;
	
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
	private Animation animBox;
	
	public bool debug = false;
	public int powerFillupRatePerTurn = 25; //percentage
	protected int powerMeter=0;
	public int morale = 5; //1 means "will flee upon getting hit";10 means "will fight to the death" ; 0 implies "will only fight when cornered"
	//A full Power Meter will embolden us to use a Heavy attack on the player.
	private int criticalHealth; //calculated minimum health before fleeing 
	private bool fleeing = false;
	private bool clocked = false; //A flag to prevents approaching on the next turn after getting knocked back
    // Start is called before the first frame update
    protected virtual void Start()
    {
        OccupySquare();
		agent=GetComponent<UnityEngine.AI.NavMeshAgent>();
		agent.updateRotation = true;
		animBox=GetComponentInChildren<Animation>();
		species+="|";
		criticalHealth = (int)(hitPoints*(1f-morale/10f));
    }

    // Update is called once per frame
    void Update()
    {
		if(HitPoints() == 0) {
			//prepare for cleanup, and prevent any further combat/movement actions
			return;
		}
		if(turnPhase == TurnMode.TOOK_HIT) {
			GoBackwards();
		}
				
		if(Attack.turn != GameStatus.OPPONENT_TURN){
			return;
		}
		
		if(turnPhase == TurnMode.END) {
			turnPhase = TurnMode.BEGIN;
		}
		switch(turnPhase) {
			case TurnMode.BEGIN:
				fleeing=false;
				powerMeter=Mathf.Min(powerMeter+powerFillupRatePerTurn,100);
				if(!clocked) {
					ProposeMove();
				}
				else {
					clocked=false;
					turnPhase = TurnMode.COMBAT;
				}
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
					Debug.Log("CPU:"+chosenAttack);
					TriPlayer.ready = false;
					Attack.SetEnemyReady(this);
				}
				EndTurn();
				break;
		}
        
    }
    
    public void EndTurn() {
		Debug.Log("DONE");
		turnPhase= TurnMode.END;
		Attack.EndTurn();
	}
    
    public void ProposeMove() {
		/*
		 * We'll advance toward the player if one or both of these is the case:
		 * -we have a full Power Meter (and could therefore severely damage the player with a Heavy strike)
		 * -hitPoints > criticalHealth
		 */
		tempPath = new UnityEngine.AI.NavMeshPath();
		if(hitPoints > criticalHealth || powerMeter == 100) 
		{
			Debug.Log("EXAMINE:"+currentSquare.transform.position.ToString("F2")+" to "+TriPlayer.player.currentSquare.transform.position.ToString("F2"));
			agent.CalculatePath(TriPlayer.player.currentSquare.transform.position, tempPath);
			turnPhase = TurnMode.ASSESS_PATH;
		}
		else 
		{
			//Put maxSquaresPerTurn between us and him
			fleeing = true;
			Vector3 winner= currentSquare.transform.position;
			Vector3 playerPos = TriPlayer.player.currentSquare.transform.position;
			float bestDistance = Attack.GroundDistance(playerPos,currentSquare.transform.position);
			float testDistance;
			RaycastHit[] tiles;
		
			//Find the tile farthest away from the player
			tiles=Physics.SphereCastAll(currentSquare.transform.position,maxSquaresPerTurn*2f-0.5f,-Vector3.up, 1.5f,1<<6);
					
			for(int i=tiles.Length-1;i>-1;i--) {
				if(tiles[i].transform.GetComponent<GridSquare>().IsOccupied(this)) {
					continue;
				}
				testDistance = (playerPos - tiles[i].transform.position).magnitude;
				if(testDistance > bestDistance) {
					bestDistance=testDistance;
					winner=tiles[i].transform.position;
				}
			}
			//and figure out how to move in the general direction thereof
			agent.CalculatePath(winner, tempPath);
			turnPhase = TurnMode.ASSESS_PATH;
		}
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
			inMotion = Attack.GroundDistance(transform.position,agent.destination) > agent.stoppingDistance;
			if(!inMotion) {
				agent.updateRotation =false;
				SetHeading(agent.velocity);
				OccupySquare();
				if(!fleeing && OpponentNearby()) {
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
		else{
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
			
			Debug.Log("Leaving "+currentSquare.transform.position.ToString("F2"));
		}
		RaycastHit hitInfo;
		Physics.SphereCast(transform.position,0.05f,-Vector3.up,out hitInfo, 1.5f,1<<6);
		currentSquare = hitInfo.transform.GetComponent<GridSquare>();
		Debug.Log("Landed on "+currentSquare.transform.position.ToString("F2"));
		currentSquare.Occupy(this);
	}
	
	protected virtual void ChooseAttack() {
		//if we're here, at least one of our attacks is in range
		//consider the Special a last resort
		
		
		chosenAttack = AttackType.NONE;
		if(powerMeter == 100) {
			//use the Heavy!
			chosenAttack = AttackType.HEAVY;
			powerMeter = 0; //arm the reload
			return;
		}
		chosenAttack = AttackType.LIGHT;
		
	}
	
	public AttackType ChosenAttack() {
		return chosenAttack;
	}
	
	protected bool CanStrike() {
		//Is the player within striking distance of any of our attacks?
		return OpponentNearby(); //rhe "melee" scenario; when ranged attacks come in, this will be more interesting
		
	}
	
	protected bool SpecialCouldReach(int distance) { //holy (expletive) this is abstract
		return (distance < 6); //cheesy, but proof-of-concept
			
	}
	
	protected bool CanUseSpecial() {
		return charges > 0; 
	}
	
	public virtual bool OpponentNearby() {
		
		Vector3 opponentPos=TriPlayer.player.currentSquare.transform.position;
		if(opponentPos.x==currentSquare.transform.position.x) {
			return (Mathf.Abs(opponentPos.z-currentSquare.transform.position.z)==1);
		}
		if(opponentPos.z==currentSquare.transform.position.z) {
			return (Mathf.Abs(opponentPos.x-currentSquare.transform.position.x)==1);
		}
		
		return false;
	}
	
	public Vector2 GetPosition() {
		return new Vector2(currentSquare.transform.position.x,currentSquare.transform.position.z);
	}
	
	public int HitPoints() {
		return Mathf.Max(hitPoints,0);
	}
	public void RunBlockAnim(int damage = 0) {
		hitPoints -= damage;
		if(damage == 0) {
			animBox.Play(species+"Block");
			return;
		}
			
		Debug.Log("UGH!"+damage);
		if(hitPoints <=0) {
			Attack.NotifyDead(this);
		}
		else
		{
			animBox.Play(species+"Hit");
		}
	}
	
	public virtual int DealtDamage() {
		return baseDamage[chosenAttack];
	}
	
	public void RunLightAnim() {
		Debug.Log("ENEMY:Light");
		animBox.Play(species+"Light");
	}
	
	public void RunHeavyAnim() {
		Debug.Log("ENEMY:Heavy");
		animBox.Play(species+"Heavy");
	}
	
	public void RunSpecialAnim() {
		Debug.Log("ENEMY:Special");
		charges--;
	}
	
	public void Shutdown() {
		currentSquare.Vacate();
		//play death animation, then queue destruction
		animBox.Play(species+"Death");
		Destroy(gameObject, 3f+animBox[species+"Death"].length);
	}
	
	public string GetTag() {
		return gameObject.tag;
	}
	
	public void Shove() {
		Vector3 winner= currentSquare.transform.position;
		Vector3 playerPos = TriPlayer.player.currentSquare.transform.position;
		float bestDistance = (playerPos - currentSquare.transform.position).magnitude;
		float testDistance;
		RaycastHit[] tiles;
		
		tiles=Physics.SphereCastAll(currentSquare.transform.position,.5f,-Vector3.up, 1.5f,1<<6);
		
		for(int i=tiles.Length-1;i>-1;i--) {
			if(tiles[i].transform.GetComponent<GridSquare>().IsOccupied(this)) {
				continue;
			}
			testDistance = (playerPos - tiles[i].transform.position).magnitude;
			if(testDistance > bestDistance) {
				bestDistance=testDistance;
				winner=tiles[i].transform.position;
			}
		}
		prospectiveEnd = (winner);
		turnPhase = TurnMode.TOOK_HIT;
		inMotion = false;
	}
	
	void GoBackwards() {
		if(!inMotion) {
			chosenAttack = AttackType.NONE;
			BlankOutPriorPath();
			
			Vector3 endPosition = prospectiveEnd+bottom;
			agent.updateRotation =false;
			agent.SetDestination(endPosition);
			inMotion = true;
		}
		else {
			inMotion = Attack.GroundDistance(transform.position,agent.destination) > agent.stoppingDistance;
			SetHeading(-agent.velocity);
			if(!inMotion) {
				agent.updateRotation =false;
				OccupySquare();
				clocked = true; //Prevents fully advancing on the next turn
				EndTurn();
			}
		}
	}
	
	
}
/*
 1. Movement phase:

Pursue the player IFF OK on hit points, else flee
-danger point is a function of "morale" attribute;initial HP*(1-(morale/10))

2. Combat phase:


Is the Heavy attack gauge full?
-if no, Light attack, and done
-if yes, Heavy attack, clear the gauge, and done

Power meter fills x % per turn;
*/
