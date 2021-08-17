using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPawn 
{
	public int HitPoints();
	public void OccupySquare();
	
	public AttackType ChosenAttack();
	
	public void RunBlockAnim(int damage = 0);
	
	public int DealtDamage();
	
	public void RunLightAnim();
	
	public void RunHeavyAnim();
	
	public void RunSpecialAnim();
	
	public void Shutdown();
	
	public string GetTag();
	
	public void Shove();
}
