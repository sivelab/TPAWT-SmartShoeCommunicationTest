using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kinect = Windows.Kinect;

public class KinectPointManV2 : MonoBehaviour {

	public GameObject BodySourceManager;

	private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong,GameObject>();
	private BodySourceManager _BodyManager;

	public GameObject[] bodyList;//set up in the unity editor
	public Dictionary<GameObject,ulong> _bodyList = new Dictionary<GameObject,ulong>();
	//_BoneMap maps which joints are connected to each other
	private Dictionary<Kinect.JointType, Kinect.JointType> _BoneMap = new Dictionary<Kinect.JointType, Kinect.JointType>()
	{
		{ Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft },
		{ Kinect.JointType.AnkleLeft, Kinect.JointType.KneeLeft },
		{ Kinect.JointType.KneeLeft, Kinect.JointType.HipLeft },
		{ Kinect.JointType.HipLeft, Kinect.JointType.SpineBase },
		
		{ Kinect.JointType.FootRight, Kinect.JointType.AnkleRight },
		{ Kinect.JointType.AnkleRight, Kinect.JointType.KneeRight },
		{ Kinect.JointType.KneeRight, Kinect.JointType.HipRight },
		{ Kinect.JointType.HipRight, Kinect.JointType.SpineBase },
		
		{ Kinect.JointType.HandTipLeft, Kinect.JointType.HandLeft },
		{ Kinect.JointType.ThumbLeft, Kinect.JointType.HandLeft },
		{ Kinect.JointType.HandLeft, Kinect.JointType.WristLeft },
		{ Kinect.JointType.WristLeft, Kinect.JointType.ElbowLeft },
		{ Kinect.JointType.ElbowLeft, Kinect.JointType.ShoulderLeft },
		{ Kinect.JointType.ShoulderLeft, Kinect.JointType.SpineShoulder },
		
		{ Kinect.JointType.HandTipRight, Kinect.JointType.HandRight },
		{ Kinect.JointType.ThumbRight, Kinect.JointType.HandRight },
		{ Kinect.JointType.HandRight, Kinect.JointType.WristRight },
		{ Kinect.JointType.WristRight, Kinect.JointType.ElbowRight },
		{ Kinect.JointType.ElbowRight, Kinect.JointType.ShoulderRight },
		{ Kinect.JointType.ShoulderRight, Kinect.JointType.SpineShoulder },
		
		{ Kinect.JointType.SpineBase, Kinect.JointType.SpineMid },
		{ Kinect.JointType.SpineMid, Kinect.JointType.SpineShoulder },
		{ Kinect.JointType.SpineShoulder, Kinect.JointType.Neck },
		{ Kinect.JointType.Neck, Kinect.JointType.Head },
	};

	// Use this for initialization
	void Start () {


		foreach (var body in bodyList)
		{
			_bodyList[body] = 0;
		}
	}
	
	// Update is called once per frame
	void Update () {

		if (BodySourceManager == null)
		{
			return;
		}
		
		_BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
		if (_BodyManager == null)
		{
			return;
		}
		
		Kinect.Body[] data = _BodyManager.GetData();
		if (data == null)
		{
			return;
		}
		
		List<ulong> trackedIds = new List<ulong>();
		foreach(var body in data)
		{
			if (body == null)
			{
				continue;
			}
			
			if(body.IsTracked)
			{
				trackedIds.Add (body.TrackingId);
			}
		}
		
		List<ulong> knownIds = new List<ulong>(_Bodies.Keys);
		
		// First unassign untracked bodies
		foreach(ulong trackingId in knownIds)
		{
			if(!trackedIds.Contains(trackingId))
			{
				foreach ( var body in bodyList){
					if (_bodyList[body] == trackingId){
						_bodyList[body]=0;
					}
				}
				//Destroy(_Bodies[trackingId]);
				_Bodies.Remove(trackingId);
			}
		}
		
		foreach(var body in data)
		{
			if (body == null)
			{
				continue;
			}
			
			if(body.IsTracked)
			{
				if(!_Bodies.ContainsKey(body.TrackingId))
				{
					//if the body isn't set yet
					//set it to a premade unused body
					_Bodies[body.TrackingId] = AssignBodyObject(body.TrackingId);
				}
				
				RefreshBodyObject(body, _Bodies[body.TrackingId]);
			}
		}


	}
	//Returns the body that will be placed into the _Bodies list if the list is empty
	private GameObject AssignBodyObject(ulong id)
	{
		for(int i = 0; i < bodyList.Length; i++){
			// if the body list is not in use, return that body otherwise try the next one
			// this check may not be necessary
			bool alreadyMade = false;
			for(int j = 0; j < bodyList.Length; j++){
				if (_bodyList[bodyList[j]]==id){
					alreadyMade = true;
				}
			}
			if (!alreadyMade&&_bodyList[bodyList[i]]==0){
				//set the body to being in use
				_bodyList[bodyList[i]] = id;
				return bodyList[i];
				        	
			}
			// if the body has already been assigned, return that body
			else if (_bodyList[bodyList[i]]==id){
				return bodyList[i];
			}

		}
		Debug.LogError("Error:  No body was found to be availabe.  Make sure bodyList is set in the Unity Editor");
		GameObject body = new GameObject("Body:" + id);
		return body;
	}

	private void RefreshBodyObject(Kinect.Body body, GameObject bodyObject)
	{
		for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
		{
			Kinect.Joint sourceJoint = body.Joints[jt];
			Kinect.Joint? targetJoint = null;
			if(_BoneMap.ContainsKey(jt))
			{
				targetJoint = body.Joints[_BoneMap[jt]];
			}
			
			Transform jointObj = bodyObject.transform.FindChild(jt.ToString());
			jointObj.localPosition = GetVector3FromJoint(sourceJoint);



			LineRenderer lr = jointObj.GetComponent<LineRenderer>();
			if(targetJoint.HasValue)
			{
				lr.SetPosition(0, jointObj.position);
				//lr.SetPosition(1, jointTarget.position);
				lr.SetPosition(1, bodyObject.transform.TransformPoint(GetVector3FromJoint(targetJoint.Value)));
				lr.SetColors(GetColorForState (sourceJoint.TrackingState), GetColorForState(targetJoint.Value.TrackingState));
			}
			else
			{
				lr.enabled = false;
			}
		}
	}

	private static Color GetColorForState(Kinect.TrackingState state)
	{
		switch (state)
		{
		case Kinect.TrackingState.Tracked:
			return Color.green;
			
		case Kinect.TrackingState.Inferred:
			return Color.red;
			
		default:
			return Color.black;
		}
	}
	private static Vector3 GetVector3FromJoint(Kinect.Joint joint)
	{
		return new Vector3(joint.Position.X * 1, joint.Position.Y * 1, joint.Position.Z * -1);
	}
}
