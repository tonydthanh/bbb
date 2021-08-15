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
    // Start is called before the first frame update
    void Start()
    {
        
		OccupySquare();
    }

    // Update is called once per frame
    void Update()
    {
		if(HitPoints() == 0) {
			//prepare for cleanup, and prevent any further combat/movement actions
			return;
		}
		if(!TriPlayer.ready){
			return;
		}
        if(CanStrike()) {
			ChooseAttack();
			TriPlayer.ready = false;
			Attack.SetEnemyReady(this);
		}
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
		if(distance > range[AttackType.HEAVY])
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
		//play death animation, then
		Destroy(gameObject);
	}
}
