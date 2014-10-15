using UnityEngine;
using System.Collections;

public class Getvariedheights : MonoBehaviour {
	public float[] yOfChildren;
	// Use this for initialization
	void Start () {
		yOfChildren = new float[transform.childCount];
		int shoepart = 0;
		foreach (Transform child in transform)
		{
			yOfChildren[shoepart] = 5f+child.transform.position.y;
			shoepart++;
		}
	}
	
	// Update is called once per frame
	void Update () {
		int shoepart = 0;
		foreach (Transform child in transform)
		{
			yOfChildren[shoepart] = 5f+child.transform.position.y;
			shoepart++;
		}	
	}
}
