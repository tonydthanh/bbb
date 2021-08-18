using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AttackType {
	NONE,
	BLOCK,
	LIGHT,
	HEAVY,
	SPECIAL
}

public enum GameStatus {
	OPPONENT_TURN,
	PLAYER_TURN,
	PROCESSING,
	GAME_OVER
}
public enum TurnMode {
	BEGIN,
	ASSESS_PATH,
	MOVE,
	COMBAT,
	TOOK_HIT,
	END
}
//This is where (I suspect) the rock-paper-scissors implementation goes (FLJ, 8/14/2021)
public class Attack : MonoBehaviour
{
	private static IPawn readyPlayer;
	private static IPawn readyEnemy;
	public static GameStatus turn = GameStatus.PLAYER_TURN;
	private static bool opponentDead = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public static void SetEnemyReady(IPawn who) {
		readyEnemy=who;
	}
	
	public static void SetPlayerReady(IPawn who) {
		readyPlayer=who;
	}
    public static void BeginCombatRound() {
		if(readyPlayer == null) {
			Debug.Log("Free Hit:Enemy");
			FreeHit(readyEnemy,TriPlayer.player);
			return;
		}
		if(readyEnemy == null){
			//make sure there's something to swing at
			if(TriPlayer.player.enemySquare != null && TriPlayer.player.enemySquare.occupant !=null) {
				Debug.Log("Free Hit:Player");
				FreeHit(readyPlayer,TriPlayer.player.enemySquare.occupant);
			}
			return;
		}
		Debug.Log(readyPlayer.ChosenAttack()+" "+readyEnemy.ChosenAttack());
		Rochambeau(readyPlayer,readyEnemy);
		
		
		TriPlayer.ready=true;
		
	}
    //who wins between rock, paper, and scissors
    private static void Rochambeau(IPawn inquisitor, IPawn opponent) { //That's what the M7 trio of "Mythbusters" called it
		AttackType a = inquisitor.ChosenAttack();
		AttackType b = opponent.ChosenAttack();
		if(a == b) {
			//a draw
			inquisitor.RunBlockAnim();
			opponent.RunBlockAnim();
			return;
		}
		
		if(Blocked(inquisitor,opponent) || Blocked(opponent,inquisitor))
		{
			return;
		}
		
		//scissors cuts paper
		if(a == AttackType.LIGHT && b == AttackType.HEAVY) 
		{
			//b wins
			opponent.RunHeavyAnim();
			inquisitor.RunBlockAnim(opponent.DealtDamage());
			return;
		}
		
		if(b == AttackType.LIGHT && a == AttackType.HEAVY) 
		{
			//a wins
			inquisitor.RunHeavyAnim();
			opponent.RunBlockAnim(inquisitor.DealtDamage());
			return;
		}
		
		//rock dulls scissors 
		if(a == AttackType.SPECIAL && b == AttackType.HEAVY) 
		{
			//a wins
			inquisitor.RunSpecialAnim();
			opponent.RunBlockAnim(inquisitor.DealtDamage());
			return;
		}
		
		if(b == AttackType.SPECIAL && a == AttackType.HEAVY) 
		{
			//b wins
			opponent.RunSpecialAnim();
			inquisitor.RunBlockAnim(opponent.DealtDamage());
			return;
		}
		
		//paper wraps rock
		if(a == AttackType.SPECIAL && b == AttackType.LIGHT) 
		{
			//b wins
			opponent.RunLightAnim();
			inquisitor.RunBlockAnim(opponent.DealtDamage());
			return;
		}
		
		if(b == AttackType.SPECIAL && a == AttackType.LIGHT) 
		{
			//a wins
			inquisitor.RunLightAnim();
			opponent.RunBlockAnim(inquisitor.DealtDamage());
			return;
		}
	}
	
	private static void FreeHit(IPawn a,IPawn b) {
		switch(a.ChosenAttack()) {
			case AttackType.HEAVY:
				a.RunHeavyAnim();
				break;
			case AttackType.LIGHT:
				a.RunLightAnim();
				break;
			case AttackType.SPECIAL:
				a.RunSpecialAnim();
				break;
		
		}
		b.RunBlockAnim(a.DealtDamage());
		
		
		//if both are dead, declare a mutual kill
		if(a.HitPoints() == 0 && b.HitPoints() == 0) {
			Debug.Log("Nobody walked away");
		}
		
	}
	
	private static bool Blocked(IPawn a,IPawn b) {
		if(a.ChosenAttack() == AttackType.BLOCK) {
			a.RunBlockAnim();
			switch(b.ChosenAttack()){
				case AttackType.HEAVY:
					b.RunHeavyAnim();
					break;
				case AttackType.LIGHT:
					b.RunLightAnim();
					break;
				case AttackType.SPECIAL:
					b.RunSpecialAnim();
					break;
			}
			return true;
		}
		return false;
	}
	public static void EndTurn(bool playerInput = false) {
		if(!playerInput) {
			if(readyEnemy != null || readyPlayer != null) {
				BeginCombatRound();
			}
			//reset for the next bout
			readyEnemy = null;
			readyPlayer = null;
		}
		if(turn == GameStatus.OPPONENT_TURN||opponentDead) {
			turn = GameStatus.PLAYER_TURN;
			Debug.Log("YOUR TURN");
			return;
		}
		if(turn == GameStatus.PLAYER_TURN && !opponentDead) {
			turn = GameStatus.OPPONENT_TURN;
			Debug.Log("THEIR TURN");
			return;
		}
	}
	
	public static void NotifyDead(IPawn who) {
		if(who.GetTag() == "Player") {
			turn = GameStatus.GAME_OVER;
			Debug.Log("Game over, man!");	
		}
		else
		{
			opponentDead = true;
		}
		//if both are dead, declare a mutual kill
		if(opponentDead && turn == GameStatus.GAME_OVER) {
			Debug.Log("Nobody walked away");
		}
		who.Shutdown();
	}
}
/* Pre-"after action report": 
 * I suspect the IPawns should inform the central class of their positions (FLJ, 8/17/21)
 */
