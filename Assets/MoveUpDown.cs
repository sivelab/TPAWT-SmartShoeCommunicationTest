using UnityEngine;
using System.Collections;

public class MoveUpDown : MonoBehaviour {
	public string up = "i";
	public string down = "k";
	private float initialY;
	// Use this for initialization
	void Start () {
		initialY = transform.position.y;
	}
	
	// Update is called once per frame
	void Update () {

		if (Input.GetKey(up))
		{
			Vector3 newPos = transform.position;
			newPos.y += .8f*Time.deltaTime;
			transform.position = newPos;
		}
		if (Input.GetKey(down)&&transform.position.y > initialY)
		{
			Vector3 newPos = transform.position;
			newPos.y -= .8f*Time.deltaTime;
			transform.position = newPos;
		}
	}
}
