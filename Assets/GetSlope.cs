using UnityEngine;
using System.Collections;

public class GetSlope : MonoBehaviour {
	public Vector3 slope;
	public float DotProductTest;
	public float test2;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		RaycastHit hit;
		var ray = new Ray(transform.position, -1*Vector3.up);
		if (Physics.Raycast (ray,out hit, 100))
		{
			slope = hit.normal;
			slope.Normalize();
			DotProductTest = Mathf.Acos(Vector3.Dot (slope,Vector3.up));
			DotProductTest = Mathf.Rad2Deg*DotProductTest;
				//distanceDown[shoepart] = ray.origin.y - hit.point.y;
		}
	}
}
