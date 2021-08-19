using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedEnemy :Enemy {
	public int attackRange = 2;
	
	protected override void Start() {
		powerMeter=100;
		base.Start();
	}
	
	public override bool OpponentNearby() {
		
		Vector3 opponentPos=TriPlayer.player.currentSquare.transform.position;
		/*
		if(Mathf.Abs(opponentPos.z-currentSquare.transform.position.z)==attackRange) {
			Debug.Log("Zs");
			Debug.Break();
		} 
		if(Mathf.Abs(opponentPos.x-currentSquare.transform.position.x)==attackRange) {
			Debug.Log("Xs");
			Debug.Break();
		}*/
		if(opponentPos.x==currentSquare.transform.position.x) {
			return (Mathf.Abs(opponentPos.z-currentSquare.transform.position.z)<=attackRange);
		}
		if(opponentPos.z==currentSquare.transform.position.z) {
			return (Mathf.Abs(opponentPos.x-currentSquare.transform.position.x)<=attackRange);
		}
		
		return false;
	}
	
	public override int DealtDamage() {
		return baseDamage[ChosenAttack()];
	}
}
