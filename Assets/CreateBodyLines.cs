using UnityEngine;
using System.Collections;

public class CreateBodyLines : MonoBehaviour {
	
	public Transform leftFoot;
	public Transform rightFoot;
	public Transform spineCenter;
	private LineRenderer lr;
	
	// Use this for initialization
	void Start () {
		lr = GetComponent<LineRenderer>();
	}
	
	// Update is called once per frame
	void Update () {
		lr.SetPosition(0, leftFoot.position);
		lr.SetPosition(1, spineCenter.position);
		lr.SetPosition(2, rightFoot.position);
		
	}
}
