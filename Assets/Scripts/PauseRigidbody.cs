﻿using UnityEngine;
using System.Collections;

public class PauseRigidbody : MonoBehaviour {
	private Rigidbody rbody;
	private Vector3 previousVelocity;
	private CollisionDetectionMode previouscolDetMode;
	//public bool justUnPaused = false;
	//public bool justPaused = false;

	void Awake () {
		rbody = GetComponent<Rigidbody>();
		//justPaused = true;
	}
		
	public void Pause () {
		if (rbody != null) {
			previousVelocity = rbody.velocity;
			previouscolDetMode = rbody.collisionDetectionMode;
			rbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
			rbody.isKinematic = true;
		}
	}

	public void UnPause () {
		if (rbody != null) {
			rbody.isKinematic = false;
			rbody.collisionDetectionMode = previouscolDetMode;
			rbody.velocity = previousVelocity;
		}
	}
}
