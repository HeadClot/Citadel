using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPC_Autobomb : MonoBehaviour {
	public int index = -1; // NPC reference index for looking up constants in tables in Const.cs
	public int currentWaypoint = 0;
	public bool visitWaypointsRandomly = false;
	public Const.aiState currentState;
	public float walkSpeed = 0.8f;
	public float runSpeed = 0.8f;
	public float meleeRange = 2.56f;
	public float waypointChangeRange = 0.64f;
	public float rangeToEnemy = 0f;
	public float changeEnemyTime = 3f; // Time before enemy will switch to different attacker
	public float tickTime = 0.05f;
	public float yawSpeed = 50f;
	public float attack3Force = 8f;
	public float attack3Radius = 6f;
	public float verticalViewOffset = 0.42f;
	public Vector3 explosionOffset;
	public Vector3 navigationTargetPosition;
	public Vector3 idealTransformForward;
	public bool isDead = false;
	[HideInInspector]
	public GameObject attacker;
	public GameObject enemy;
	public Transform[] walkWaypoints; // point(s) for NPC to walk to when roaming or patrolling
	public AudioClip SFXFootstep;
	public AudioClip SFXSightSound;
	public AudioClip SFXAttack1;
	public AudioClip SFXInspect;
	public AudioClip SFXInteracting;

	// More or less constant
	public float fieldOfViewAngle = 160f;
	public float fieldOfViewAttack = 80f;
	public float distToSeeWhenBehind = 2.5f;
	public float sightRange = 51.2f;

	private bool hasSFX;
	private bool firstSighting;
	private bool hadEnemy;
	private float idleTime;
	private float tickFinished;
	private float lastHealth;
	private AudioSource SFX;
	private Animation anim;
	private Rigidbody rbody;
	private HealthManager healthManager;
	private BoxCollider boxCollider;

	void Start () {
		healthManager = GetComponent<HealthManager>();
		lastHealth = healthManager.health;
		rbody = GetComponent<Rigidbody>();
		SFX = GetComponent<AudioSource>();
		anim = GetComponent<Animation>();

		currentState = Const.aiState.Idle;
		idealTransformForward = transform.forward;
		enemy = null;
		attacker = null;
		firstSighting = true;
		tickFinished = Time.time + tickTime;

		if (SFX == null) {
			Debug.Log ("WARNING: No audio source for npc at: " + transform.position.x.ToString () + ", " + transform.position.y.ToString () + ", " + transform.position.z + ".");
			hasSFX = false;
		} else {
			hasSFX = true;
		}

		if (healthManager.health > 0) {
			if (walkWaypoints.Length > 0 && walkWaypoints[currentWaypoint] != null) {
				currentState = Const.aiState.Walk; // If waypoints are set, start walking to them
			} else {
				currentState = Const.aiState.Idle; // Default to idle
			}
		}
	}

	void FixedUpdate () {
		if (PauseScript.a != null && PauseScript.a.paused) {
			return; // don't do any checks or anything else...we're paused!
		}

		// Only think every tick seconds to save on CPU and prevent race conditions
		if (tickFinished < Time.time) {
			Think();
			tickFinished = Time.time + tickTime;
		}
	}

	void Think () {
		CheckAndUpdateState ();

		switch (currentState) {
		case Const.aiState.Idle: 			Idle(); 		break;
		case Const.aiState.Walk:	 		Walk(); 		break;
		case Const.aiState.Run: 			Run(); 			break;
		case Const.aiState.Attack1: 		Attack(); 		break;
		default: 							break;
		}
	}

	// All state changes go in here.  Check for stuff.  See what we need to be doing.  Need to change?
	void CheckAndUpdateState() {
		if (currentState == Const.aiState.Dead)	return;

		// Check to see if we got hurt
		if (healthManager.health < lastHealth) {
			if (healthManager.health <= 0) {
				currentState = Const.aiState.Dying;
				return;
			}

			lastHealth = healthManager.health;
			// Make initial sight sound if we aren't running, attacking, or already in pain
			if (currentState != Const.aiState.Run || currentState != Const.aiState.Attack1 || currentState != Const.aiState.Attack2 || currentState != Const.aiState.Attack3 || currentState != Const.aiState.Pain)
				SightSound ();

			enemy = healthManager.attacker;
			currentState = Const.aiState.Run;
			return;
		} else {
			lastHealth = healthManager.health;
		}

		// Take a look around for enemies
		if (enemy == null) {
			// we don't have an enemy yet so let's look to see if we can see one
			if (CheckIfPlayerInSight ()) {
				currentState = Const.aiState.Run; // quit standing around and start fighting
				return;
			}
		} else {
			// We have an enemy now what...
			if (CheckIfEnemyInSight ()) {
				float dist = Vector3.Distance (transform.position, enemy.transform.position);
				// See if we are close enough to attack
				if (dist < meleeRange) {
					if (CheckIfEnemyInFront (enemy)) {
						currentState = Const.aiState.Attack1;
						return;
					}
				}
			}
		}
	}

	void Idle() {
		// Set animation state
		anim["Default Take"].wrapMode = WrapMode.Loop;
		anim.Play("Default Take");
	}

	void SightSound() {
		if (firstSighting) {
			firstSighting = false;
			if (hasSFX) SFX.PlayOneShot(SFXSightSound);
		}
	}


	void Walk() {
		// Set animation state
		anim["Default State"].wrapMode = WrapMode.Loop;
		anim.Play ("Default State");

		AI_Face (walkWaypoints[currentWaypoint].gameObject);

		// move it move it
		rbody.AddForce (transform.forward * walkSpeed * tickTime, ForceMode.Impulse); // moving forward!
		// I'm walkin', and waitin'...on the edge of my seat anticipating.
	}

	void AI_Face(GameObject goalLocation) {
		Vector3 dir = (goalLocation.transform.position - transform.position).normalized;
		dir.y = 0f;
		Quaternion lookRot = Quaternion.LookRotation(dir,Vector3.up);
		transform.rotation = Quaternion.Slerp (transform.rotation, lookRot, tickTime * yawSpeed); // rotate as fast as we can towards goal position
	}

	void Run() {
		// Set animation state
		anim["Default Take"].wrapMode = WrapMode.Loop;
		anim.Play ("Default Take");

		AI_Face (enemy); // turn and face your executioner

		// move it move it
		rbody.AddForce (transform.forward * runSpeed * Time.fixedDeltaTime, ForceMode.VelocityChange); // moving forward!
		//Vector3 newPos = transform.forward * runSpeed * tickTime;
		//newPos = transform.position + newPos;
		//transform.position = newPos;
		//transform.position = idealTransformForward * runSpeed * tickTime;
	}



	void Attack() {
		Dying ();
	}

	void Dying() {
		if (!isDead) {
			isDead = true;
			ExplosionForce ef = GetComponent<ExplosionForce> ();
			DamageData ddNPC = Const.SetNPCDamageData (index, Const.aiState.Attack3, gameObject);
			float take = Const.a.GetDamageTakeAmount (ddNPC);
			ddNPC.other = gameObject;
			ddNPC.damage = take;
			//enemy.GetComponent<HealthManager>().TakeDamage(ddNPC); Handled by ExplodeInner
			if (ef != null)
				ef.ExplodeInner (transform.position + explosionOffset, attack3Force, attack3Radius, ddNPC);
			healthManager.ObjectDeath (SFXAttack1);
			GetComponent<MeshRenderer> ().enabled = false;
			currentState = Const.aiState.Dead;
		}
	}

	// Sub functions
	bool CheckIfEnemyInFront (GameObject target) {
		Vector3 vec = Vector3.Normalize(target.transform.position - transform.position);
		float dot = Vector3.Dot(vec,transform.forward);
		if (dot > 0.800) return true; // enemy is within 18 degrees of forward facing vector
		return false;
	}


	bool CheckIfEnemyInSight() {
		Vector3 checkline = enemy.transform.position - transform.position; // Get vector line made from enemy to found player
		RaycastHit hit;
		if(Physics.Raycast(transform.position + transform.up, checkline.normalized, out hit, sightRange)) {
			if (hit.collider.gameObject == enemy) {
				if (CheckIfEnemyInFront(enemy))
					return true;
			}
		}
		return false;
	}
		
	bool CheckIfPlayerInSight() {
		GameObject tempent = Const.a.player1.GetComponent<PlayerReferenceManager>().playerCapsule;
		Vector3 viewOffsetPoint = transform.position;
		viewOffsetPoint.y += verticalViewOffset;
		Vector3 checkline = Vector3.Normalize(tempent.transform.position - viewOffsetPoint); // Get vector line made from enemy to found player
		//drawMyLine(tempent.transform.position,viewOffsetPoint,Color.green,2f);

		RaycastHit hit;
		if(Physics.Raycast(viewOffsetPoint, checkline, out hit, sightRange)) {
			if (hit.collider.gameObject == tempent) {
				//drawMyLine (hit.point, viewOffsetPoint, Color.red, 3f);
				float dist = Vector3.Distance (tempent.transform.position, transform.position);  // Get distance between enemy and found player
				float dot = Vector3.Dot (checkline, transform.forward.normalized);
				if (dot > 0.10f) {
					// enemy is within 81 degrees of forward facing vector
					if (firstSighting) {
						firstSighting = false;
						if (hasSFX)
							SFX.PlayOneShot (SFXSightSound);
					}
					enemy = tempent;
					return true; // time to fight!
				} else {
					if (dist < distToSeeWhenBehind) {
						SightSound ();
						enemy = tempent;
						return true; // time to turn around and face your executioner!
					}
				}
			} else {
				//drawMyLine(hit.point,viewOffsetPoint,Color.blue,1f);
			}
		}
		return false;
	}

	void drawMyLine(Vector3 start , Vector3 end, Color color,float duration = 0.2f){
		StartCoroutine( drawLine(start, end, color, duration));
	}

	IEnumerator drawLine(Vector3 start , Vector3 end, Color color,float duration = 0.2f){
		GameObject myLine = new GameObject ();
		myLine.transform.position = start;
		myLine.AddComponent<LineRenderer> ();
		LineRenderer lr = myLine.GetComponent<LineRenderer> ();
		lr.material = new Material (Shader.Find ("Particles/Additive"));
		lr.startColor = color;
		lr.endColor = color;
		lr.startWidth = 0.1f;
		lr.endWidth = 0.1f;
		lr.SetPosition (0, start);
		lr.SetPosition (1, end);
		yield return new WaitForSeconds(duration);
		GameObject.Destroy (myLine);
	}
}
