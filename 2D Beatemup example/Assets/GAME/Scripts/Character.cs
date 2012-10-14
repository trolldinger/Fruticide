using UnityEngine;
using System.Collections;

public enum PlayerStates{
	idle,
	walk,
	jump,
	attacking,
	specialattack,
	jumpattack,
	hit,
	ko,
	grabbed,
	grabbedhit,
	grabbedattack,
	grabbedthrow,
	grab,
	attackgrab,
	grabthrow,
	dead,
	rise
}

public enum AIBehaviours{
	none,
	gostraigth,
	gostraigthcarefully,
	goaround,
	boss1
}

public class Character : MonoBehaviour {
	
	public int maxLife=100;
	public int life=100;
	public int deadScore=500;
	public int throwDamage=25;
	public int specialAttackDamage=35;
	public string characterName;
	public bool isPlayer;
	public bool isBoss;
	public float velocity;
	public float jumpHigh;
	public float jumpVelocity=0.2f;
	public float curveDistance = 20f;
	public float jumpDamping = 2f;
	public float KOBackwardVelocity;
	public float KOThrowVelocity;
	public Vector2 strategyTimes;
	public GameObject grabZone;
	public Transform grabbedZone;
	public GameObject throwZone;
	public GameObject[] hitBoxes;
	public PlayerStates states;
	
	public float attackDistance;
	public float carefullDistance;
	
	public AIBehaviours behaviours;
	public tk2dSlicedSprite lifeBar;
	public tk2dTextMesh actorName;
	
	public bool jumpState=false;
	public bool koOthers=false;
	
	private bool rev=false;
	private bool way=false;
	private bool nextAttack=false;
	private float ground;
	private float yJumpPoint;
	private float actualJumpVelocity;
	private float carefulVelocity;
	private float thinkingTime=0f;
	private float strategyTime;
	private float time;
	private int waypoint=-1;
	
	public int grabbedHitCount=0;
	
	
	public Vector3 moveDirection = Vector3.zero;
	
	private Transform playerPos;
	private Transform pointPos;
	
	public tk2dAnimatedSprite player;
	
	GameManager manager;
	VCButtonBase abtn;
	VCButtonBase bbtn;
	VCButtonBase cbtn;
	VCDPadBase dpad;
	
	// Use this for initialization
	
	void OnEnable () {
		manager=FindObjectOfType(typeof(GameManager)) as GameManager;
		life = maxLife;
		player = GetComponent<tk2dAnimatedSprite>();
		player.animationEventDelegate = PlayerAnim;
		if(isPlayer){
			lifeBar = GameObject.Find("PlayerLifeFill").GetComponent<tk2dSlicedSprite>();
			actorName = GameObject.Find("PlayerName").GetComponent<tk2dTextMesh>();
			actorName.text = characterName;
			actorName.Commit();
		}
		else if(isBoss){
			playerPos = GameObject.FindWithTag("Player").transform;
			carefulVelocity = velocity*0.3f;
			GameObject.Find("BossLifeBar").renderer.enabled = true;
			GameObject.Find("BossLifeFill").renderer.enabled = true;
			GameObject.Find("BossPortrait").renderer.enabled = true;
			GameObject.Find("BossName").renderer.enabled=true;
		}
		else{
			playerPos = GameObject.FindWithTag("Player").transform;
			carefulVelocity = velocity*0.3f;
		}
		player.color = new Color(player.color.r,player.color.g,player.color.b,1f);
		states = PlayerStates.idle;
		player.Play("Idle");
		StartCoroutine("CharacterBehaviour");
	}
	
	void PlayerAnim(tk2dAnimatedSprite sprite, tk2dSpriteAnimationClip clip, tk2dSpriteAnimationFrame frame, int frameNum){
		if(frame.eventInt == 2){
			SpecialAttack();
		}
		if(frame.eventFloat != 0f){
			if(frame.eventFloat == 5f){
				grabbedHitCount++;
				if(grabbedHitCount==3){
					hitBoxes[(int)(frame.eventFloat-1f)].GetComponent<mirrorCol>().collisionType = CollisionType.strong;
				}
			}
			hitBoxes[(int)(frame.eventFloat-1f)].GetComponent<mirrorCol>().Attack();
		}
		if(frame.eventInfo == "continue" && isPlayer){
			if(frame.eventInt==1){
				if(nextAttack){
					nextAttack=false;
				}
				else{
					states = PlayerStates.idle;
					player.Play("Idle");
				}
			}
		}
		if(frame.eventInfo == "returngrab"){
			if(grabbedHitCount==3){
				grabbedHitCount=0;
				player.Play("Idle");
				states = PlayerStates.idle;
			}
			else{
				player.Play("grab");
				states = PlayerStates.grab;
			}
		}
		if(frame.eventInfo == "returngrabbed"){
			if(transform.parent != null){
				player.Play("grabbed");
				states = PlayerStates.grabbed;
			}
			else{
				player.Play("Idle");
				states = PlayerStates.idle;
			}
		}
		if(frame.eventInfo == "preparethrow"){
			PrepareToThrow();
		}
		if(frame.eventInfo == "throw"){
			Throw();
		}
		if(frame.eventInfo == "return"){
			nextAttack=false;
			states = PlayerStates.idle;
			player.Play("Idle");
		}
		if(frame.eventInfo == "isdead?"){
			if(life==0){
				player.Play("knocked");
				states = PlayerStates.dead;
			}
		}
	}
	
	void LifeBarStatus(){
		if(lifeBar)
			lifeBar.scale = new Vector3((float)life/(float)maxLife,1f,1f);
	}
	
	IEnumerator EnemyLife(){
		GameObject[] e = GameObject.FindGameObjectsWithTag("Enemy");
		for(int i=0;i<e.Length;i++){
			if(e[i].GetComponent<Character>().lifeBar){
				e[i].GetComponent<Character>().lifeBar=null;
				e[i].GetComponent<Character>().actorName = null;
				break;
			}
		}
		GameObject.Find("EnemyLifeBar").renderer.enabled = true;
		GameObject.Find("EnemyLifeFill").renderer.enabled = true;
		GameObject.Find("EnemyPortrait").renderer.enabled = true;
		GameObject.Find("EnemyName").renderer.enabled = true;
		lifeBar = GameObject.Find("EnemyLifeFill").GetComponent<tk2dSlicedSprite>();
		actorName = GameObject.Find("EnemyName").GetComponent<tk2dTextMesh>();
		actorName.text = characterName;
		actorName.Commit();
		float t=0f;
		while(t< 3f && lifeBar){
			t+=Time.deltaTime;
			yield return null;
		}
		if(lifeBar){
			GameObject.Find("EnemyLifeBar").renderer.enabled = false;
			GameObject.Find("EnemyLifeFill").renderer.enabled = false;
			GameObject.Find("EnemyPortrait").renderer.enabled = false;
			GameObject.Find("EnemyName").renderer.enabled = false;
			actorName.text = "";
			actorName.Commit();
		}
		yield return null;
	}
	
	IEnumerator CharacterBehaviour(){
		while(states != PlayerStates.dead){
			dpad = VCDPadBase.GetInstance("dpad");
			if(isBoss && !lifeBar){
				lifeBar = GameObject.Find("BossLifeFill").GetComponent<tk2dSlicedSprite>();
				actorName = GameObject.Find("BossName").GetComponent<tk2dTextMesh>();
				actorName.text = characterName;
				actorName.Commit();
			}
			LifeBarStatus();
			if(isPlayer){
				switch(states){
					case PlayerStates.idle:Idle(dpad);break;
					case PlayerStates.walk: Walk(dpad);break;
					case PlayerStates.jump:Jumping(false);break;
					case PlayerStates.grab:
					if(grabbedZone.childCount == 0) {
						player.Play("Idle");
						states = PlayerStates.idle;
					}
					break;
					case PlayerStates.jumpattack:Jumping(true);break;
					case PlayerStates.ko: KO(koOthers); break;
				}
			}
			else{
				switch(states){
					case PlayerStates.idle:Idle(playerPos);break;
					case PlayerStates.walk: Walk(playerPos);break;
					case PlayerStates.jump:Jumping(false);break;
					case PlayerStates.jumpattack:Jumping(true);break;
					case PlayerStates.ko:KO(koOthers);break;
				}
			}
			yield return null;
		}
		StartCoroutine("Dead");
	}
	
	void Update(){
		if(isPlayer){
			abtn = VCButtonBase.GetInstance("A");
			abtn.OnPress = ButtonPress;
			bbtn = VCButtonBase.GetInstance("B");
			bbtn.OnPress = ButtonPress;
			cbtn = VCButtonBase.GetInstance("C");
			cbtn.OnPress = ButtonPress;
		}
	}
	
	# region Control Behaviours
	// Update is called once per frame
	void CheckMovementControl (VCDPadBase dpad) {
		if(dpad){
			moveDirection=Vector3.zero;
			if (dpad.Left)
				moveDirection += Vector3.left;
			if (dpad.Right)
				moveDirection += Vector3.right;
			if (dpad.Up)
				moveDirection += Vector3.up+Vector3.forward;
			if (dpad.Down)
				moveDirection += Vector3.down+Vector3.back;
		}
	}
	
	void ButtonPress(VCButtonBase button){
		if(button == abtn){
			switch(states){
				case PlayerStates.idle: player.Play("attack"); states = PlayerStates.attacking; break;
				case PlayerStates.walk: player.Play("attack"); states = PlayerStates.attacking; break;
				case PlayerStates.grab: 
				if(dpad && dpad.Left || dpad && dpad.Right){
					player.Play("throw");
					states = PlayerStates.grabthrow;
				}
				else{
					player.Play("attackgrab"); 
					states = PlayerStates.attackgrab;
				}
				break;
				case PlayerStates.attacking:nextAttack=true;break;
				case PlayerStates.jump:states = PlayerStates.jumpattack;break;
			}
		}
		if(button == bbtn){
			switch(states){
				case PlayerStates.idle: 
					PrepareJump();
					states = PlayerStates.jump; 
				break;
				case PlayerStates.walk:
					PrepareJump();
					states = PlayerStates.jump; 
				break;
				case PlayerStates.attacking:
					nextAttack=false;
					PrepareJump();
					states = PlayerStates.jump;
				break;
			}
		}
		if(button == cbtn && life>1){
			switch(states){
				case PlayerStates.idle: player.Play("specialattack");states = PlayerStates.specialattack;SpecialAttackLifePenalty();break;
				case PlayerStates.walk: player.Play("specialattack");states = PlayerStates.specialattack;SpecialAttackLifePenalty();break;
				case PlayerStates.hit: player.Play("specialattack");states = PlayerStates.specialattack;SpecialAttackLifePenalty();break;
				case PlayerStates.attacking: nextAttack=false; player.Play("specialattack");states = PlayerStates.specialattack;SpecialAttackLifePenalty();break;
				case PlayerStates.grab: DetachGrabbedEnemy(); player.Play("specialattack");states = PlayerStates.specialattack;SpecialAttackLifePenalty();break;
			}
		}
	}
	#endregion
	
	#region Normal Behaviours
	
	IEnumerator Dead(){
		if(lifeBar){
			GameObject.Find("EnemyLifeBar").renderer.enabled = false;
			GameObject.Find("EnemyLifeFill").renderer.enabled = false;
			GameObject.Find("EnemyPortrait").renderer.enabled = false;
			GameObject.Find("EnemyName").renderer.enabled = false;
			if(!isPlayer && !isBoss){
				actorName.text = "";
				actorName.Commit();
			}
		}
		if(isBoss)
			Time.timeScale=1f;
		lifeBar=null;
		while(player.color.a > 0f){
			player.color = new Color(player.color.r,player.color.g,player.color.b,player.color.a-Time.deltaTime);
			yield return null;
		}
		transform.position = new Vector3(-1000,-1000,-1000);
		if(isPlayer)
			PoolManager.Pools["Players"].Despawn(this.transform);
		else{
			manager.AddScore(deadScore);
			if(isBoss)
				manager.StageCleared();
			PoolManager.Pools["Enemies"].Despawn(this.transform);
		}
		yield return null;
	}
	
	void Idle(VCDPadBase dpad){
		if(player)
			CheckMovementControl(dpad);
		if(moveDirection != Vector3.zero){
			states = PlayerStates.walk;
			player.Play("walk");
		}
	}
	
	void Idle(Transform target){
		if(target){
			if(behaviours == AIBehaviours.none){
				if(thinkingTime >= 1f){
					thinkingTime=0;
					strategyTime = Random.Range(strategyTimes.x,strategyTimes.y);
					time = Time.time+strategyTime;
					player.Play("walk");
					states = PlayerStates.walk;
					behaviours = (AIBehaviours)Random.Range(1,4);
				}
				else{
					thinkingTime+=Time.deltaTime;
				}
			}
			else{
				if(target.GetComponent<Character>().states != PlayerStates.ko && 
					target.GetComponent<Character>().states != PlayerStates.rise &&
					target.GetComponent<Character>().states != PlayerStates.dead){
					strategyTime = Random.Range(strategyTimes.x,strategyTimes.y);
					time = Time.time+strategyTime;
					player.Play("walk");
					states = PlayerStates.walk;
					behaviours = (AIBehaviours)Random.Range(1,4);
				}
			}
		}
	}
	
	void Walk(VCDPadBase dpad){
		if(player)
			CheckMovementControl(dpad);
		if(moveDirection == Vector3.zero){
			states = PlayerStates.idle;
			player.Play("Idle");
		}
		else{
			Move(moveDirection);
			grabZone.GetComponent<mirrorCol>().Grab();
		}
	}
	
	void Walk(Transform target){
		LockPlayer(target);
		if(target.GetComponent<Character>().states == PlayerStates.ko || 
			target.GetComponent<Character>().states == PlayerStates.rise){
			player.Play("Idle");
			states = PlayerStates.idle;
		}
		else{
			if(Time.time > time)
				behaviours = AIBehaviours.none;
			switch(behaviours){
				case AIBehaviours.gostraigth:GoStraight(target);break;
				case AIBehaviours.gostraigthcarefully:GoStraightCarefully(target);break;
				case AIBehaviours.goaround:GoAround();break;
				case AIBehaviours.none:states = PlayerStates.idle;player.Play("Idle");break;
			}
		}
	}
	
	void Move(Vector3 direction){
		if(moveDirection.x == -1f)
			player.scale = new Vector3(-1f,1f,1f);
		if(moveDirection.x == 1f)
			player.scale = Vector3.one;
		transform.Translate(direction*velocity*Time.deltaTime);
	}
	
	public void PrepareJump(){
			moveDirection.y=0f;
			ground = transform.position.y;
			yJumpPoint = transform.position.y+jumpHigh;
	}
	
	void Jumping(bool isAttacking){
		if(!jumpState && transform.position.y<yJumpPoint){
			if(isAttacking)
				player.Play("jumpattack");
			else
				player.Play("jumpstart");
			if(transform.position.y>yJumpPoint-curveDistance)
				actualJumpVelocity = JumpCurve(false);
			else
				actualJumpVelocity = jumpVelocity;
			transform.position = new Vector3(transform.position.x,transform.position.y+actualJumpVelocity*Time.deltaTime,transform.position.z);
		}
		else if(jumpState && transform.position.y>ground){
			if(isAttacking)
				player.Play("jumpattack");
			else
				player.Play("jumpend");
			if(transform.position.y>yJumpPoint-curveDistance)
				actualJumpVelocity = JumpCurve(true);
			else
				actualJumpVelocity = jumpVelocity;
			transform.position = new Vector3(transform.position.x,transform.position.y-actualJumpVelocity*Time.deltaTime,transform.position.z);
		}
		if(!jumpState && transform.position.y >= yJumpPoint){
			jumpState=true;
		}
		else if(jumpState && transform.position.y <= ground){
			transform.position = new Vector3(transform.position.x,ground,transform.position.z);
			jumpState=false;
			states = PlayerStates.idle;
			player.Play("Idle");
		}
		transform.Translate(new Vector3(moveDirection.x,0f,0f)*velocity*Time.deltaTime);
	}
	
	float JumpCurve(bool isFalling){
		float vel=0f;
		if(isFalling){
			vel = Mathf.Lerp(actualJumpVelocity,jumpVelocity,jumpDamping);
		}
		else{
			vel = Mathf.Lerp(actualJumpVelocity,60f,jumpDamping);
		}
		return vel;
	}
	
	void KO(bool throwed){
		player.Play("falling");
		if(throwed){
			Collider[] cols = Physics.OverlapSphere(transform.position,50f);
			for(int i = 0; i < cols.Length;i++){
				if(cols[i].GetComponent<Character>() && cols[i].gameObject.layer == gameObject.layer && 
					cols[i].GetComponent<Character>().states != PlayerStates.ko){
					cols[i].GetComponent<Character>().koOthers = false;
					cols[i].GetComponent<Character>().PrepareJump();
					cols[i].GetComponent<Character>().states = PlayerStates.ko;
					if(!cols[i].GetComponent<Character>().isPlayer)
						manager.AddScore((int)((float)throwDamage*0.33f*10f));
					cols[i].GetComponent<Character>().RecieveDamage((int)((float)throwDamage * 0.33f));
				}
			}
		}
		if(!jumpState && transform.position.y<yJumpPoint){
			if(transform.position.y>yJumpPoint-curveDistance)
				actualJumpVelocity = JumpCurve(false);
			else
				actualJumpVelocity = jumpVelocity;
			transform.position = new Vector3(transform.position.x,transform.position.y+actualJumpVelocity*Time.deltaTime,transform.position.z);
		}
		else if(jumpState && transform.position.y>ground){
			if(transform.position.y>yJumpPoint-curveDistance)
				actualJumpVelocity = JumpCurve(true);
			else
				actualJumpVelocity = jumpVelocity;
			transform.position = new Vector3(transform.position.x,transform.position.y-actualJumpVelocity*Time.deltaTime,transform.position.z);
		}
		if(!jumpState && transform.position.y >= yJumpPoint){
			jumpState=true;
		}
		else if(jumpState && transform.position.y <= ground){
			transform.position = new Vector3(transform.position.x,ground,transform.position.z);
			koOthers=false;
			jumpState=false;
			states = PlayerStates.rise;
			player.Play("raise");
		}
		if(throwed)
			transform.Translate(new Vector3(-player.scale.x,0f,0f) * KOThrowVelocity*Time.deltaTime);
		else
			transform.Translate(new Vector3(-player.scale.x,0f,0f) * KOBackwardVelocity*Time.deltaTime);
	}
	
	void SpecialAttack(){
		Collider[] cols = Physics.OverlapSphere(transform.position,100f);
		for(int i = 0; i<cols.Length;i++){
			if(cols[i].gameObject.layer != gameObject.layer && cols[i].GetComponent<Character>()){
				cols[i].GetComponent<Character>().PrepareJump();
				cols[i].GetComponent<Character>().states = PlayerStates.ko;
				if(!cols[i].GetComponent<Character>().isPlayer)
					manager.AddScore(throwDamage*10);
				cols[i].GetComponent<Character>().RecieveDamage(specialAttackDamage);
			}
		}
	}
	
	void SpecialAttackLifePenalty(){
		life-=specialAttackDamage;
		if(life<=0)
			life=1;
	}
	
	void LockPlayer(Transform target){
		if(transform.position.x>target.position.x){
			player.scale = new Vector3(-1f,1f,1f);
		}
		else{
			player.scale = Vector3.one;
		}
		if(transform.position.z != transform.position.y)
			transform.position = new Vector3(transform.position.x,transform.position.y,transform.position.y);
	}
	
	void PrepareToBeThrowed(){
		GameObject thrower;
		Vector3 p1 = transform.position;
        Vector3 p2 = Vector3.right;
		Vector3 p2alt = Vector3.left;
		if(player.scale.x == -1f)
			thrower = SearchThrower(p1,p2alt);
		else
			thrower = SearchThrower(p1,p2);
		player.Play("grabbedthrow");
		states = PlayerStates.grabbedthrow;
		transform.parent = thrower.GetComponent<Character>().throwZone.transform;
		koOthers=true;
		moveDirection.y=0f;
		transform.position = thrower.GetComponent<Character>().throwZone.transform.position;
		player.scale = new Vector3(-thrower.GetComponent<Character>().player.scale.x,
			thrower.GetComponent<Character>().player.scale.y,
			thrower.GetComponent<Character>().player.scale.z);
		ground = thrower.transform.position.y;
		yJumpPoint = thrower.transform.position.y+jumpHigh;
	}
	
	GameObject SearchThrower(Vector3 position, Vector3 direction){
		GameObject thrower = null;
		RaycastHit[] hits = Physics.RaycastAll(position,direction,150f);
		int i = 0;
		while(i<hits.Length){
			if(hits[i].collider.gameObject.layer != gameObject.layer && hits[i].collider.GetComponent<Character>()){
				if(hits[i].collider.GetComponent<Character>().states == PlayerStates.grab ||
					hits[i].collider.GetComponent<Character>().states == PlayerStates.attackgrab){
					thrower = hits[i].collider.gameObject;
					if(dpad.Right)
						hits[i].collider.GetComponent<Character>().player.scale = Vector3.one;
					if(dpad.Left)
						hits[i].collider.GetComponent<Character>().player.scale = new Vector3(-1f,1f,1f);
					break;
				}
			}
			i++;
		}
		return thrower;
	}
	
	void PrepareToThrow(){
		Vector3 p1 = transform.position;
        Vector3 p2 = Vector3.right;
		Vector3 p2alt = Vector3.left;
		RaycastHit[] hits = null;
		if(player.scale.x == 1f)
			hits = Physics.RaycastAll(p1,p2,100f);
		else
			hits = Physics.RaycastAll(p1,p2alt,100f);
		int i = 0;
		while(i<hits.Length){
			if(hits[i].collider.gameObject.layer != gameObject.layer && hits[i].collider.GetComponent<Character>()){
				if(hits[i].collider.GetComponent<Character>().states == PlayerStates.grabbed || 
					hits[i].collider.GetComponent<Character>().states == PlayerStates.grabbedhit){
					hits[i].collider.gameObject.GetComponent<Character>().PrepareToBeThrowed();
					break;
				}
			}
			i++;
		}
	}
	
	void Throw(){
		grabbedHitCount=0;
		Transform enemy = throwZone.transform.GetChild(0);
		enemy.parent = null;
		enemy.GetComponent<Character>().states = PlayerStates.ko;
		if(!enemy.GetComponent<Character>().isPlayer)
			manager.AddScore(throwDamage*10);
		enemy.GetComponent<Character>().RecieveDamage(throwDamage);
	}
	
	void DetachGrabbedEnemy(){
		grabbedHitCount=0;
		GameObject[] children = GameObject.FindGameObjectsWithTag("Enemy");
		for(int i = 0;i<children.Length;i++){
			if(children[i].transform.parent != null){
				children[i].transform.parent = null;
				children[i].GetComponent<Character>().states = PlayerStates.idle;
			}
		} 
	}
	
	public void RecieveDamage(int damage){
		if(!isPlayer && !isBoss){
			lifeBar=null;
			StartCoroutine("EnemyLife");
		}
		life-=damage;
		if(life <= 0){
			life = 0;
			if(koOthers == false)
				PrepareJump();
			if(isBoss){
				Time.timeScale=0.33f;
				GameObject[] children = GameObject.FindGameObjectsWithTag("Enemy");
				for(int i = 0;i<children.Length;i++){
					if(!children[i].GetComponent<Character>().isBoss){
						children[i].GetComponent<Character>().life=0;
						children[i].GetComponent<Character>().RecieveDamage(1);
					}
				}
			}
			states = PlayerStates.ko;
		}
	}
	
	#endregion
	
	#region AI Behaviours
	
	void GoStraight(Transform target){
		if(Vector3.Distance(target.position,transform.position) <= attackDistance){
			player.Play("attack");
			states = PlayerStates.attacking;
		}
		if(target.GetComponent<Character>().states != PlayerStates.jump &&
			target.GetComponent<Character>().states != PlayerStates.jumpattack){
			moveDirection = Vector3.Normalize(target.position-transform.position);
			moveDirection.z = moveDirection.y;
		}
		transform.Translate(moveDirection * velocity*Time.deltaTime);
	}
	
	void GoStraightCarefully(Transform target){
		if(target.GetComponent<Character>().states != PlayerStates.jump &&
			target.GetComponent<Character>().states != PlayerStates.jumpattack){
			moveDirection = Vector3.Normalize(target.position-transform.position);
			moveDirection.z = moveDirection.y;
		}
		if(Vector3.Distance(target.position,transform.position) <= carefullDistance){
			if(target.GetComponent<Character>().player.scale.x == 1f && player.scale.x == -1f ||
				target.GetComponent<Character>().player.scale.x == -1f && player.scale.x == 1f){
				if(target.GetComponent<Character>().moveDirection.x > 0 || 
					target.GetComponent<Character>().moveDirection.x < 0)
					transform.Translate(-moveDirection * velocity*Time.deltaTime);
				else
					transform.Translate(moveDirection * carefulVelocity*Time.deltaTime);
			}
			else
				transform.Translate(moveDirection * velocity*Time.deltaTime);
			if(Vector3.Distance(target.position,transform.position) <= attackDistance){
				player.Play("attack");
				states = PlayerStates.attacking;
			}
		}
		else{
			transform.Translate(moveDirection * velocity*Time.deltaTime);
		}
		
	}
	
	void GoAround(){
		if(waypoint == -1){
			int b = Random.Range(0,2);
			if(b==1)
				way = true;
			else
				way = false;
			if(SearchNearest()){
				if(way)
					waypoint = 1;
				else
					waypoint= 5;
				rev=false;
			}
			else{
				if(way)
					waypoint = 4;
				else
					waypoint= 8;
				rev=true;
			}
			pointPos = GameObject.Find("pathPoint"+waypoint).transform;
		}
		else{
			moveDirection = Vector3.Normalize(pointPos.position-transform.position);
			moveDirection.z = moveDirection.y;
			transform.Translate(moveDirection * velocity*Time.deltaTime);
			if(Vector3.Distance(transform.position,pointPos.position) <= 30f){
				if(rev && way && waypoint == 1 || rev && !way && waypoint == 5 ||
					!rev && way && waypoint == 4 || !rev && !way && waypoint == 8){
					waypoint=-1;
					behaviours = (AIBehaviours)Random.Range(1,4);
				}
				else{
					if(rev)
						waypoint--;
					else
						waypoint++;
					pointPos = GameObject.Find("pathPoint"+waypoint).transform;
				}
			}
		}
	}
	
	bool SearchNearest(){
		float near1 = Vector3.Distance(transform.position, GameObject.Find("pathPoint1").transform.position);
		float near2 = Vector3.Distance(transform.position, GameObject.Find("pathPoint5").transform.position);
		float near3 = Vector3.Distance(transform.position, GameObject.Find("pathPoint4").transform.position);
		float near4 = Vector3.Distance(transform.position, GameObject.Find("pathPoint8").transform.position);
		float[] near = {near1,near2,near3,near4};
		float nearest = float.PositiveInfinity;
		for(int i = 0; i< near.Length;i++){
			if(near[i]<nearest)
				nearest=near[i];
		}
		if( nearest == near1 || nearest == near2)
			return true;
		else
			return false;
	}
	#endregion
}