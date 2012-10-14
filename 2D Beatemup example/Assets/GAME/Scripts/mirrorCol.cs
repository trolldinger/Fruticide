using UnityEngine;
using System.Collections;

public enum CollisionType{
	weak,
	strong,
	grab
}

public class mirrorCol : MonoBehaviour {
	
	public int Damage;
	public CollisionType collisionType;
	
	public Transform grabbedPos;
	
	Character thisCharacter;
	
	GameManager manager;
	Vector3 pos;
	Vector3 pos2;
	Vector3 gPos;
	Vector3 gPos2;
	
	// Use this for initialization
	void Start () {
		manager=FindObjectOfType(typeof(GameManager)) as GameManager;
		thisCharacter = transform.parent.GetComponent<Character>();
		pos= transform.localPosition;
		pos2 = new Vector3(-transform.localPosition.x,transform.localPosition.y,transform.localPosition.z);
		if(collisionType == CollisionType.grab){
			gPos = grabbedPos.localPosition;
			gPos2 = new Vector3(-grabbedPos.localPosition.x,grabbedPos.localPosition.y,grabbedPos.localPosition.z);
		}
	}
	
	public void Attack(){
		if(thisCharacter.player.scale.x == -1)
			transform.localPosition = pos2;
		else
			transform.localPosition = pos;
		Vector3 p1 = transform.position;
        Vector3 p2 = Vector3.right;
		Vector3 p2alt = Vector3.left;
		if(thisCharacter.player.scale.x == -1f)
			AttackCast(p1,p2alt);
		else
			AttackCast(p1,p2);
	}
	
	public void Grab(){
		if(thisCharacter.player.scale.x == -1){
			transform.localPosition = pos2;
			grabbedPos.localPosition = gPos2;
		}
		else{
			transform.localPosition = pos;
			grabbedPos.localPosition = gPos;
		}
		Vector3 p1 = transform.position;
        Vector3 p2 = Vector3.right;
		Vector3 p2alt = Vector3.left;
		if(thisCharacter.player.scale.x == -1f)
			GrabCast(p1,p2alt);
		else
			GrabCast(p1,p2);
	}
	
	void GrabCast(Vector3 p1, Vector3 p2){
		RaycastHit hit;
		if(Physics.Raycast(p1,p2,out hit,transform.localScale.x)){
			if(hit.collider.gameObject.layer != gameObject.layer && hit.collider.GetComponent<Character>()){
					if(hit.collider.GetComponent<Character>().player.scale.x == thisCharacter.player.scale.x){
						hit.collider.GetComponent<Character>().player.scale = 
						new Vector3(hit.collider.GetComponent<Character>().player.scale.x*-1f,1f,1f);
					}
					hit.collider.GetComponent<Character>().player.Play("grabbed");
					hit.collider.GetComponent<Character>().states = PlayerStates.grabbed;
					hit.collider.transform.parent = grabbedPos;					
					thisCharacter.states = PlayerStates.grab;
					thisCharacter.player.Play("grab");
					hit.collider.transform.localPosition = Vector3.zero;
					hit.collider.GetComponent<Character>().RecieveDamage(Damage);
			}
		}
	}
	
	void AttackCast(Vector3 p1, Vector3 p2){
		Debug.DrawRay(p1,p2,Color.red,3f);
		RaycastHit[] hits = Physics.RaycastAll(p1,p2,transform.localScale.x);
		int i = 0;
		while(i<hits.Length){
			if(hits[i].collider.gameObject.layer != gameObject.layer && hits[i].collider.GetComponent<Character>()){
				switch(collisionType){
					case CollisionType.weak: 
					if(hits[i].collider.GetComponent<Character>().states == PlayerStates.jump ||
						hits[i].collider.GetComponent<Character>().states == PlayerStates.jumpattack){
						hits[i].collider.GetComponent<Character>().states = PlayerStates.ko;
						if(!hits[i].collider.GetComponent<Character>().isPlayer)
							manager.AddScore(Damage*10);
						hits[i].collider.GetComponent<Character>().RecieveDamage(Damage);
					}
					else if(thisCharacter.states == PlayerStates.attackgrab){
						if(hits[i].collider.GetComponent<Character>().states == PlayerStates.walk || 
							hits[i].collider.GetComponent<Character>().states == PlayerStates.idle){
							hits[i].collider.GetComponent<Character>().states = PlayerStates.hit;
							hits[i].collider.GetComponent<Character>().player.Play("hurt");
						}
						else{
							hits[i].collider.GetComponent<Character>().states = PlayerStates.grabbedhit;
							hits[i].collider.GetComponent<Character>().player.Play("grabbedhit");
						}
						if(!hits[i].collider.GetComponent<Character>().isPlayer)
							manager.AddScore(Damage*10);
						hits[i].collider.GetComponent<Character>().RecieveDamage(Damage);
						if(hits[i].collider.GetComponent<Character>().life <=0){
							hits[i].transform.parent = null;
							thisCharacter.grabbedHitCount=0;
						}
					}
					else{
						if(hits[i].collider.GetComponent<Character>().states == PlayerStates.attackgrab || 
						hits[i].collider.GetComponent<Character>().states == PlayerStates.grab){
							DetachGrabbedEnemy(hits[i].collider.gameObject);
						}
						if(hits[i].collider.GetComponent<Character>().states != PlayerStates.grabthrow){
							if(hits[i].collider.GetComponent<Character>().states != PlayerStates.hit){
								hits[i].collider.GetComponent<Character>().states = PlayerStates.hit;
								hits[i].collider.GetComponent<Character>().player.Play("hurt");
								if(!hits[i].collider.GetComponent<Character>().isPlayer)
									manager.AddScore(Damage*10);
								hits[i].collider.GetComponent<Character>().RecieveDamage(Damage);
							}
						}
					}
					break;
					case CollisionType.strong: 
					if(thisCharacter.grabbedHitCount == 3){
						hits[i].transform.parent = null;
						collisionType = CollisionType.weak;
					}
					if(hits[i].collider.GetComponent<Character>().states == PlayerStates.attackgrab || 
						hits[i].collider.GetComponent<Character>().states == PlayerStates.grab){
						DetachGrabbedEnemy(hits[i].collider.gameObject);
					}
					if(hits[i].collider.GetComponent<Character>().states != PlayerStates.grabthrow){
						hits[i].collider.GetComponent<Character>().PrepareJump();
						hits[i].collider.GetComponent<Character>().states = PlayerStates.ko;
						if(!hits[i].collider.GetComponent<Character>().isPlayer)
							manager.AddScore(Damage*10);
						hits[i].collider.GetComponent<Character>().RecieveDamage(Damage);
					}
					break;
				}
			}
			i++;
		}
	}
	
	void DetachGrabbedEnemy(GameObject player){
		player.GetComponent<Character>().grabbedHitCount=0;
		GameObject[] children = GameObject.FindGameObjectsWithTag("Enemy");
		for(int i = 0;i<children.Length;i++){
			if(children[i].transform.parent != null){
				children[i].transform.parent = null;
				children[i].GetComponent<Character>().states = PlayerStates.idle;
			}
		} 
	}
}