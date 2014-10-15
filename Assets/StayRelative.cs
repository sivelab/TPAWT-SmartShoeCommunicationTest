using UnityEngine;
using System.Collections;

public class StayRelative : MonoBehaviour {
	
	public Transform copyPosition;//FPC TPAWT MOVABLE
	public Transform copyParent;//TPAWTMOCKERY
	//public Transform itself;//FPC TPAWT
	public Transform selfParent;//TPAWT
	public float test;
	// itself.position = (copyPosition.position - copyParent.position) + selfParent.position
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		test =selfParent.transform.eulerAngles.y;
		//transform.eulerAngles.y = test;
		//transform.position = (copyParent.position - copyPosition.position) + itself.position
		
		
		//takes the vector from copyParent to copyPosition and adds it to the position of selfParent
		//this makes the position of itself the same relative to the position of copyPosition from copyParent
		transform.position = ((copyPosition.position - copyParent.position) + selfParent.position);
		
		//rotate itself around the rotation of selfParent, so that if selfParent was not aligned the same
		//as copyParent, itself is still in the right spot
		
		transform.RotateAround(selfParent.position,Vector3.up,selfParent.transform.eulerAngles.y);
		transform.eulerAngles = selfParent.eulerAngles; 
		//itself.eulerAngles.Set(0,0,0);
	}
}
