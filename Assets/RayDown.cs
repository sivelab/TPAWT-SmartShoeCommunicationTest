using UnityEngine;
using System.Collections;

public class RayDown : MonoBehaviour {
	public float[] distanceDown;
	// Use this for initialization
	void Start () {
		distanceDown = new float[transform.childCount];
	
	}
	
	// Update is called once per frame
	void Update () {
		int shoepart = 0;
		foreach (Transform child in transform)
		{
			var ray = new Ray(child.transform.position, -1*Vector3.up);
			RaycastHit hit;
			if (Physics.Raycast (ray,out hit, 100))
			{
				distanceDown[shoepart] = ray.origin.y - hit.point.y;
			}
			shoepart++;
		}
	}
}
