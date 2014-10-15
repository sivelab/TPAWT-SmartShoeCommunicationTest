using UnityEngine;
using System.Collections;
public class Navigation : MonoBehaviour {
	
	//Class created to deal with navigation in the virutal world.
	//Currently supports a couple of models, a Walking In Place (WIP) model, the TreadPort model, and the Head Mounted Display (HMD) model
	
	
	
	
	public enum simulationModel {WIPmodel = 0, WIPmodel2 = 1, TreadPortModel = 2, HMDModel = 3, nullModel = 4, WIPmodel3 = 5};
	public simulationModel sModel = simulationModel.WIPmodel;
	public enum turningModel {sideStepTurning = 0, liftArmTurning = 1, forwardBackArmTurning = 2, noTurning = 3}; 
	public turningModel tModel = turningModel.liftArmTurning;
	public enum headPositionModel {frontFoot = 0, frontFootInterpolate = 1, centerFeet = 2, centerFeetInterpolate = 3};
	public headPositionModel hPModel = headPositionModel.frontFootInterpolate;
	public enum foot {right = 0, left = 1, standstill = 2};
	
	//These Transforms set in the Unity Editor, used for positions of the objects
	public Transform leftHand;
	public Transform rightHand;
	public Transform leftFootReal;
	public Transform rightFootReal;
	public Transform spineBase;
	public Transform head;
	
	public Transform leftFootVirtual;
	public Transform rightFootVirtual;
	public Transform virtualCenterMass;
	public Transform screenParent;
	public Transform realFloor;
	public Transform virtualFloor;
	public static bool moveForward; // Used to check if the character should be moving or not
	public static float curVelocity = 0; //The velocity the character should be moving at
	
	
	public float paceDistance = .7f;
	public float stepFrequency = 2;
	
	
	private int looper=-1;  //Used to reset the Walking Model as well as to keep track of the state of things
	private bool enableWalkInPlace = false;
	private Vector3 leftFootInitial;//real position of the left foot at start, to be modified to keep track
	private Vector3 rightFootInitial;//real position of the right foot at start
	private Vector3 forward;
	// For fading the camera due to user positional drift, should only be used in WIP-model
	//turn off in the editor if you don't want the camera to fade in the WIP-model, should auto turn off if camFade is not
	//set in the editor.  
	public bool camFadeOn = true;  
	public CameraFade camFade;
	//WIP v2 Variables
	private Vector3 chestCenterWorld;
	private Transform virtualBodyParent;// The body parts being virtually moved should have the same parent
	private GameObject body;
	private GameObject globalPositionVariable;
	private Transform leftFootVirtualClone;
	private Transform rightFootVirtualClone;
	CharacterController leftFootVirtualCC;
	CharacterController rightFootVirtualCC;
	private bool rightFootVirtualGrounded = false;
	private bool leftFootVirtualGrounded = false;
	private float feetToSpineBase=0.6f;
	
	private float virtualCenterMassHeight=0;//the distance between the feet and the VCM initially
	private float leftFootRealHeight=0;//the distance between the floor and the foot initially
	private float rightFootRealHeight=0;//the distance between the floor and the foot initially
	//private float leftFootVirtualX =0;//used to avoid sliding along walls
	//private float rightFootVirtualX =0;//used to avoid sliding along walls
	
	//WIP v1 Variables
	public float height; // the height of the user
	public float[] heightleft;//Keeps track of the height of the left foot for the past 100 ticks
	public float[] heightright;//Keeps track of the height of the right foot for the past 100 ticks
	public int iterations = 500;
	public float leftFootVerticalVelocity=0;
	public float rightFootVerticalVelocity=0;
	private int currentWalkingStateLeft  = 0;//keeps track of the current state of the left foot
	private int currentWalkingStateRight = 0;//keeps track of the current state of the right foot
	private float timeStateStartLeft = 0; //how long we have been in the current state, to make sure we don't get stuck in one state for too long
	private float timeStateStartRight = 0; //how long we have been in the current state, to make sure we don't get stuck in one state for too long
	private int testInt = 0;
	
	//Used for smoothing the velocity
	static float[] xvVl;
	static float[] yvVl;
	static float[] xvVr;
	static float[] yvVr;
	
	private int currentSecond = 0;
	private int lastSecond = 0;
	private float penultimateStepCounter = 0;//2 seconds ago # of steps
	private float lastStepCounter = 0;//1 second ago # of steps
	private float thisStepCounter = 0;//this second # of steps
	private int totalSteps = 0;
	
	
	//Turning Variables
	public float rotationAmount;//The amount of rotation to be applied to the scene
	
	// Use this for initialization
	void Start () {
		
		leftFootVirtualCC = leftFootVirtual.GetComponent<CharacterController>();
		rightFootVirtualCC = rightFootVirtual.GetComponent<CharacterController>();
		heightleft = new float[iterations];
		heightright = new float[iterations];
		body = new GameObject("body");
		feet = new GameObject("feet");
		globalPositionVariable = new GameObject("globalPositionVariable");
		
		if (head != null && leftFootReal != null && rightFootReal !=null)
		{
			//Calcuate the height from the head position minus the feet position. Uses the global position of each
			// .115 is added to the head position since the headposition I generally use is the 
			// center of the head and this wants the top of the head
			///this is done every reiteration of toggle walking
			height = (head.position.y + .115f) - (leftFootReal.position.y + rightFootReal.position.y)/2;
			xvVl = new float[3 + 1];
			yvVl = new float[3 + 1];
			xvVr = new float[3 + 1];
			yvVr = new float[3 + 1];
		}
		else
		{
			if (sModel == simulationModel.WIPmodel)
			{
				Debug.LogError("The head or feet transforms are not set.  Please set them for the Walking In Place Model.");
				sModel = simulationModel.nullModel;
			}
		}
		
		if (leftHand == null || rightHand == null || spineBase == null)
		{
			Debug.LogError("The spineBase and hand transforms are not set.  Please set them if using turning Mekanisms.");
		}
		if (camFade == null)
			camFadeOn = false;
		if(leftFootVirtual.parent == rightFootVirtual.parent)
		{
			virtualBodyParent = leftFootVirtual.parent;
		}
		else
		{
			Debug.LogError("Feet should have the same parent Node");
		}
	}
	
	// Update is called once per frame
	void Update () {
		
		checkGroundedFeet();
		keyPressInput();
		if (enableWalkInPlace)//ToggleWalking is on (gestural movement)
		{
			int previous;
			
			//if this is the first iteration after walking was toggled, reset all values
			if (looper == -1)
			{
				
				previous = 0;
				//.112m is the 50th percentile of the distance between center of head and top of head accordding
				//to http://upload.wikimedia.org/wikipedia/commons/thumb/6/61/HeadAnthropometry.JPG/640px-HeadAnthropometry.JPG
				height = (head.position.y + .112f) - (leftFootReal.position.y + rightFootReal.position.y)/2;
				
				chestCenterWorld = spineBase.position;//for sidestep turning
				
				//set initial points to compare when walking happens
				leftFootInitial = leftFootReal.position;
				rightFootInitial = rightFootReal.position;
				
				
				//create two clones of the old feet that can be used
				setupFeetPlusCenterMass();
				
				
				currentWalkingStateLeft = 0;//feet on the floor position
				thisStepCounter = 0;
				rotationAmount = 0;
				//lfr.p.y = rf.p.y - lfrh
				//lfrh = rf.p.y - lfr.p.y
				if (camFade!= null)
					camFade.SetScreenOverlayColor(new Color(0,0,0,0));
				moveForward = false;
				
			}
			else
			{
				previous = looper;//this prevent previous from being -1 on startup
			}
			looper++;
			
			setRotationAmount();
			
			
			/********** Positional Drift Reduction Method ************/
			if(camFadeOn)
			{
				Vector3 mag = spineBase.position - chestCenterWorld;
				float distanceSpineBaseMoved = mag.magnitude;
				if (distanceSpineBaseMoved > .5f)
				{
					camFade.StartFade(new Color(0,0,0,distanceSpineBaseMoved*2.5f),2);
				}
				else
				{
					camFade.StartFade(new Color(0,0,0,0),2);
				}
			}
			//Debug.Log (distanceSpineBaseMoved);
			
			
			//WALKING IN PLACE 
			
			if (sModel == simulationModel.WIPmodel)
			{
				WIPmodel1(previous);
				
			}
			else if (sModel == simulationModel.WIPmodel2)
			{
				WIPmodel2();
				
			}
			else if (sModel == simulationModel.WIPmodel3)
			{
				WIPmodel3 (previous);
			}
		}
		else //Togglewalking is off
		{		
			
			enableWalkInPlace = udpSendPacket.getTesting();
			//reset things that need to be reset, and reset looper for the next iteration
			if(looper != -1 )
			{
				if (camFade != null)
					camFade.SetScreenOverlayColor(new Color(0,0,0,0));
				//reset the parent relationship of the feet
				leftFootVirtual.parent = virtualBodyParent;
				rightFootVirtual.parent = virtualBodyParent;
				screenParent.parent = null;
				//leftFootVirtual.localPosition = new Vector3(-.1f,0f,0f);
				//rightFootVirtual.localPosition = new Vector3(.1f,0f,0f);
				leftFootVirtual.gameObject.GetComponent<StayRelative>().enabled=true;
				rightFootVirtual.gameObject.GetComponent<StayRelative>().enabled=true;
				Destroy(leftFootVirtualClone.gameObject);
				Destroy(rightFootVirtualClone.gameObject);
				looper = -1;  //reset 
			}
		}
		
		
		
		
	}
	
	/*		End Update Function
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * */
	
	
	
	
	//external function to set the walking in place
	public void setWalkInPlace(bool walking)
	{
		enableWalkInPlace = walking;
	}
	
	public void checkGroundedFeet()
	{
		if ((leftFootVirtualCC.collisionFlags & CollisionFlags.Below)!= 0)
		{
			leftFootVirtualGrounded = true;
			//Debug.Log("Touching ground leftFootVirtual!");
		}
		else
			leftFootVirtualGrounded = false;
		
		if ((rightFootVirtualCC.collisionFlags & CollisionFlags.Below)!= 0)
		{
			rightFootVirtualGrounded = true;
			//Debug.Log("Touching ground rightFootVirtual!");
		}
		else
			rightFootVirtualGrounded = false;
		/*
		if (leftFootVirtualCC.collisionFlags == CollisionFlags.None)
			Debug.Log("Free floating!");
		
		if ((leftFootVirtualCC.collisionFlags & CollisionFlags.Sides )!= 0)
			Debug.Log("Touching sides!");
		
		if (leftFootVirtualCC.collisionFlags == CollisionFlags.Sides)
			Debug.Log("Only touching sides, nothing else!");
		
		if ((leftFootVirtualCC.collisionFlags & CollisionFlags.Above) != 0)
			Debug.Log("Touching sides!");
		
		if (leftFootVirtualCC.collisionFlags == CollisionFlags.Above)
			Debug.Log("Only touching Ceiling, nothing else!");
		 */
		
		
		/*
		if (leftFootVirtualCC.collisionFlags == CollisionFlags.Below)
		{
			//Debug.Log("Only touching ground, nothing else!");
		}
*/
	}
	
	private void keyPressInput()
	{	
		if ( Input.GetAxis("ToggleWalking") > 0)
		{
			enableWalkInPlace = true;
		}
		else if (Input.GetAxis("ToggleWalking") < 0)
		{
			enableWalkInPlace = false;
		}
	}
	
	private void setupFeetPlusCenterMass()
	{
		//unchild the virtual feet from their parent, so when they tell their previous parent
		//to move forward, they also don't move forward
		//instead a new parent is given to them, with the same transform as the old parent
		//this new parent is needed so I can use their local position to determine
		//that they aren't too far apart and for relative movement
		//Also keep a record of the previous parent so they can be reparented
		
		
		
		
		//turn off the stay relative function that tracks where the virtual feet are
		//relative to the real feat
		leftFootVirtual.gameObject.GetComponent<StayRelative>().enabled=false;
		rightFootVirtual.gameObject.GetComponent<StayRelative>().enabled=false;
		
		//set forward as the screens forward
		forward = screenParent.forward;//Vector3.Cross((rightFootVirtual.position - leftFootVirtual.position), Vector3.up);
		forward.Normalize();
		//set the feet to have the same forward as the screen
		leftFootVirtual.forward = forward;
		rightFootVirtual.forward = forward;
		//take the feet out of the virtualcentermass so they aren't affected by its position anymore.  
		leftFootVirtual.parent = null;
		rightFootVirtual.parent = null;
		
		
		//set up the virtualcentermass and body's position 
		//before they are parents so they don't negatively affect their children
		Vector3 temp = (rightFootVirtual.position + leftFootVirtual.position) / 2;
		feetToSpineBase = spineBase.position.y - (leftFootReal.position.y + rightFootReal.position.y)/2;
		temp.y = temp.y + feetToSpineBase;
		virtualCenterMass.position = temp;
		virtualCenterMass.forward = forward;
		body.transform.position = temp;
		body.transform.forward = forward;
		
		
		//set the parents so we can use them for rotation/position (virtualcentermass) and
		//for relative location (body for virtualfeet)
		screenParent.parent = virtualCenterMass;
		leftFootVirtual.parent = body.transform;
		rightFootVirtual.parent = body.transform;
		
		virtualCenterMassHeight = virtualCenterMass.position.y - (rightFootVirtual.position.y + leftFootVirtual.position.y )/2;
		//sets a base height to be tested against for feet raising
		leftFootRealHeight = realFloor.position.y - leftFootReal.position.y ;
		rightFootRealHeight = realFloor.position.y - rightFootReal.position.y ;
		//leftFootVirtualX = leftFootVirtual.localPosition.x;
		//rightFootVirtualX = rightFootVirtual.localPosition.x;
		
		//Create clones of the virtual feet that will mimic where the real feet should be
		leftFootVirtualClone = (Transform)Instantiate(leftFootVirtual);
		rightFootVirtualClone= (Transform)Instantiate (rightFootVirtual);
		
		leftFootVirtualClone.renderer.material.color = Color.red;
		rightFootVirtualClone.renderer.material.color = Color.red;;
		leftFootVirtualClone.collider.enabled = false;
		rightFootVirtualClone.collider.enabled = false;
		leftFootVirtualClone.parent = body.transform;
		rightFootVirtualClone.parent = body.transform;
		leftFootVirtualClone.position = leftFootVirtual.position;
		rightFootVirtualClone.position = rightFootVirtual.position;
		leftFootVirtualClone.gameObject.GetComponent<StayRelative>().enabled=true;
		rightFootVirtualClone.gameObject.GetComponent<StayRelative>().enabled=true;
		Destroy (leftFootVirtualClone.rigidbody);
		Destroy (rightFootVirtualClone.rigidbody);
		CharacterController l = leftFootVirtualClone.GetComponent<CharacterController>();
		CharacterController r = rightFootVirtualClone.GetComponent<CharacterController>();
		Destroy (l);
		Destroy (r);
		
		
	}
	
	private void setRotationAmount()
	{
		/*  Turning Mechanisms
				 * need spineBase, lefthand, and righthand transforms defined
				 * which turning method used depends on what tModel is set to
				 * ToggleWalking is set in the input settings with
				 * gravity = 0
				 * dead = .001
				 * sensitivity = 1000
				 * The positive button will turn on the walking in place model, setting all initial values needed at that time
				 * The negative button will turn off the walking in place model, resetting the looper variable so that all the
				 * values will be reset when walking is toggled in the future.
				 * 
				 * 
				 * Currently have three times of turning implemented.  The type currently in use can be set in the editor
				 * using the tModel enum.  Each turning model does the same thing, in that it sets the rotation amount that will
				 * be used by (in my case) FPSInputController.  The turn rate was set by trial and error and finding a good value
				 * that seemed to work.  Is more than welcome to be modified if desired.
				 * 
				 * 		if (navi.rotationAmount > .15) //turn right
							{
								transform.Rotate(0, navi.rotationAmount * 30 * Time.deltaTime, 0);
							}
						else if (navi.rotationAmount < -0.15) // turn left
							{
								transform.Rotate(0, navi.rotationAmount * 30 * Time.deltaTime, 0);
							}
				 * 
				 * 
				 * -SideStep to turn:  Based off the old TPAWT method, if you take a step to either side, the screen will rotate in that 
				 * direction.  The farther you step, the more the rotation is.
				 * 
				 * -lift arms to turn:  an idea thought up by an associate of mine, Jerald Thomas, where you lift your left arm up 
				 * to turn left, and your right arm up to turn right.  Sort of similar to bicycle signalling.  The higher the hand is lifted,
				 * the more turning will be done.
				 * 
				 * -forwardbackarmturning :  Similar to actually riding a bike.  Imagine holding onto handle bars of a bike in front of you.
				 * Then, if the right hand is moved closer to you compared to the left hand, it rotates right.  
				 * If the left hand is moved closer to you compared to the right, it rotates left.
				 * 
				 */
		
		//*********** Sidestep to turn
		//spineBase.z is the current location in the left-right plane the pelvis is
		//chestCenterWorld.z is the start location in the left-right plane the pelvis is
		//chestCenterWorld.y = height of stomache at start  
		if(tModel == turningModel.sideStepTurning)
		{
			rotationAmount = chestCenterWorld.z - spineBase.position.z;//if positive, rotate right, if negative, rotate left
		}
		
		
		//********** lift arms to turn
		
		//Debug.Log ("h- " + height + " -rA- " + rotationAmount + " -leftHand.y- " + leftHand.transform.position.y + " -hepcenter.y- " + spineBase.position.y);
		if (tModel == turningModel.liftArmTurning)
		{
			float verticalThreshold = spineBase.position.y - (0*height/15);//is spinebase in kinect v2
			if (leftHand.transform.position.y > verticalThreshold && rightHand.transform.position.y > verticalThreshold)// Left Hand and Right hand
			{
				//both hands up, no rotate
				rotationAmount = 0;
			}
			// if the left hand is above the spineBase (spineBase adjusted to be a better spot than kinects spineBase) rotate left
			else if (leftHand.transform.position.y > verticalThreshold)// Left Hand
			{
				//rotate left
				rotationAmount = -1 * (leftHand.transform.position.y - verticalThreshold);
			}
			//similar with right hand
			else if (rightHand.transform.position.y > verticalThreshold)//Right Hand
			{
				//rotate right
				rotationAmount =  (rightHand.transform.position.y - verticalThreshold);
			}
			else
			{
				rotationAmount = 0;
			}
		}
		
		//************** Riding A Bike Style Turning
		// For this to work, forward has to be in an x direction.  There are ways to define
		// a better forward, but those require either another object to be known or the user
		// looking the right direction when walking is toggled.  Right now I have a simulated room
		// that moves objects in the virtual world, so it's not a big deal and works for me.  Will
		// likely change this so a "forward"  can be defined instead of just using x
		if (tModel == turningModel.forwardBackArmTurning)
		{
			//if both arms are above the hip Center
			if (leftHand.transform.position.y > spineBase.position.y && rightHand.transform.position.y > spineBase.position.y )
			{
				//both arms are up.....
				float diff = (leftHand.transform.position.x - spineBase.position.x)-(rightHand.transform.position.x  - spineBase.position.x);
				if(diff > .05)//if left hand is forward 5 or more cm from right hand, rotate left
				{
					rotationAmount = diff * 3;
				}
				else if (diff < .05) // if right hand is forward 5 or more cm from left hand, rotate right
				{
					rotationAmount = diff * 3;
				}
				
			}
		}
		//********** head gaze turning  WIP
		
		// this will rotate left if the head is rotated to the left, rotate right if the head is rotated to the right
		// don't have a good way to figure this out yet, working on it.
		
		//Debug.Log(rotationAmount);
		
		
		/********** END TURNING **************/
	}
	
	
	
	/*
	 * Function Space
	 * 
	 * 
	 * 
	 * 

	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * This model is an extension of model two.  It uses very similar methods, buts
	 * adds the idea of ghost feet and stable feet.
	 * 
	 * Ghost feet are essentially model two feet locations.  These don't necasarily indicate
	 * what the real foot will feel underneath it.
	 * 
	 * This is where stable feet come in.  When a foot strike occurs, the stable foot will be
	 * set at that location.
	 * 
	 * 
	 * 
	 * 
	 * 
	 * */
	private float avgVel=0;
	private int avgVelCount = 0;
	private float holdTime = 0;
	private float signifcantVelocity = .2f;
	public int tempState =1;
	private int lastState=1;
	private Vector3 rightFootForwardVelocity;
	private Vector3 leftFootForwardVelocity;
	public Transform ghostFoot;
	private Transform pastGhostFeet;
	private GameObject feet;
	private float timeOutTimer=0;
	private foot currentFoot;
	private float speed;
	private bool sentShoeSignal = false;
	public UDPSendPacket udpSendPacket;
	private void WIPmodel3(int previous)
	{
		getVerticalVelocities(previous);
		//float stepOffset = height/72; // 2.5 cm for someone who is 180cm tall
		//Debug.Log (stepOffset);
		float stepOffset = height/108; // 5 cm for someone who is 180cm tall
		switch (tempState)
		{
		case 1:
			//Stable state, initial state
			avgVel = 0;
			avgVelCount = 0;
			rightFootForwardVelocity = Vector3.zero;
			leftFootForwardVelocity = Vector3.zero;
			speed =0;
			timeOutTimer = 0;
			//TODO  Should really use the velocity as well as the foot height for reduncancy purposes
			/*
			if (rightFootVerticalVelocity > signifcantVelocity){
				tempState = 2;
			}
			else if (leftFootVerticalVelocity >signifcantVelocity){
				tempState =6;
			}
			*/
			if (rightFootReal.position.y > rightFootInitial.y + stepOffset)
			{
				tempState = 2;
			}
			else if (leftFootReal.position.y > leftFootInitial.y + stepOffset)
			{
				tempState =6;
			}
			break;
			
			//Right foot movement
		case 2:
			//right foot rising
			avgVel += rightFootVerticalVelocity;
			avgVelCount++;
			
			//move the foot forward a speed related to verticalFootVelocity;
			rightFootForwardVelocity = forward*(avgVel / avgVelCount)*Time.deltaTime;
			speed = (avgVel / avgVelCount);
			//Debug.Log ("velocity: " + (100*velocity) + ":forward:" + forward + ":rfvv:" + rightFootVerticalVelocity + ":tdt:" + Time.deltaTime+":av/avc:"+(avgVel/avgVelCount));
			//if rightfoot is high enough, it enters the top foot state
			if(rightFootReal.position.y > rightFootInitial.y + 2*stepOffset)
			{
				tempState=3;
			}


			/*
			//Check for stable hovering foot
			//If the foot is hovering stable, switch to state 3 and st the avgVel

			if (rightFootVerticalVelocity < signifcantVelocity){
				if (holdTime == 0)
					holdTime = Time.time;
			}
			else{
				holdTime = 0;
			}
			//if the foot velocity is stable for long enough, assume the foot is at it's max height
			if(Time.time - holdTime > .07f && holdTime != 0){
				tempState=3;
				avgVel = avgVel / avgVelCount;
			}
*/

			break;
		case 3:
			if(!sentShoeSignal)
			{
				udpSendPacket.setRightFootAir(true);
				//udpSendPacket.cycleShoeStates(-1,1);
				sentShoeSignal = true;
			}
			//top of foot swing
			//A steady state that will move the foot forward at a speed
			//relative to the avgVel found of the rising foot
			rightFootForwardVelocity = forward*avgVel*Time.deltaTime;
			speed = avgVel;
			//Debug.Log ("rfvv:"+rightFootVerticalVelocity+":speed:"+(100*velocity)+":tdt:"+Time.deltaTime+":av:"+avgVel);
			//foot moved below the threshold.  simple and effective
			if(rightFootReal.position.y < rightFootInitial.y + 2*stepOffset)
			{
				tempState=4;
				sentShoeSignal = false;
			}
			/*
			//Check for the footdown event
			//if the foot starts falling

			if (-1* rightFootVerticalVelocity > signifcantVelocity){
				tempState = 4;
				//slow the avgVel to make hitting the target easier
				avgVel = avgVel / 2;
			}
			*/
			break;
		case 4:
			udpSendPacket.setRightFootAir(false);
			//terminal right foot swing
			//the foot should be falling at this point thanks to maintainFootHeight
			//waiting for the foot to hit the ground while still moving forward at the slowed speed
			rightFootForwardVelocity = forward*avgVel*Time.deltaTime;
			speed = avgVel;
			//close to floor
			if(rightFootReal.position.y < rightFootInitial.y + stepOffset)
			{
				tempState=5;
			}

			//foot has come to a stop
			/*
			if(-1*rightFootVerticalVelocity <  signifcantVelocity/2)
			{
				tempState = 5;
			}
			*/
			//if (right foot about to hit the floor)
			//go to case 5
			break;
		case 5:
			//temporary
			//ghost feet not important, neither is stable foot
			tempState = 10;
			/*
			if (rightFootVirtualGrounded){
				tempState = 10;
				rightFootForwardVelocity = Vector3.zero;
				leftFootForwardVelocity = forward;
				currentFoot = foot.left;
				speed = 0;
				pastGhostFeet = (Transform)Instantiate(ghostFoot);
				pastGhostFeet.renderer.material.color = Color.blue;
				pastGhostFeet.collider.enabled = false;
				pastGhostFeet.parent = feet.transform;
				pastGhostFeet.position = rightFootVirtual.position;
			}
			*/
			//right foot strike
			//setup and wait for left foot movement
			//setup ghost foot location and foot elevations of stable foot
			break;
			
			//Left foot movement
			
			//after right foot strike, waiting for left foot rise
		case 10:
			avgVel = 0;
			avgVelCount = 0;
			speed = 0;
			if(leftFootReal.position.y > leftFootInitial.y + stepOffset)
			{
				tempState=6;
			}
			/*
			//TODO  Should really use the velocity as well as the foot height for reduncancy purposes

			if (leftFootVerticalVelocity >signifcantVelocity){
				tempState =6;
			}
			*/
			break;
		case 6:
			//left foot rising
			avgVel += leftFootVerticalVelocity;
			avgVelCount++;
			
			//move the foot forward a speed related to verticalFootVelocity;
			speed = (avgVel / avgVelCount);
			leftFootForwardVelocity = forward*speed*Time.deltaTime;
			//top of swing
			if(leftFootReal.position.y > leftFootInitial.y + 2*stepOffset)
			{
				tempState=7;
			}


			//Debug.Log ("velocity: " + (100*velocity) + ":forward:" + forward + ":rfvv:" + leftFootVerticalVelocity + ":tdt:" + Time.deltaTime+":av/avc:"+(avgVel/avgVelCount));
			/*
			//Check for stable hovering foot
			//If the foot is hovering stable, switch to state 3 and st the avgVel
			if (leftFootVerticalVelocity < signifcantVelocity){
				if (holdTime == 0)
					holdTime = Time.time;
			}
			else{
				holdTime = 0;
			}
			//if the foot velocity is stable for long enough, assume the foot is at it's max height
			if(Time.time - holdTime > .07f && holdTime != 0){
				tempState=7;
				avgVel = avgVel / avgVelCount;
			}
			*/
			break;
		case 7:
			if(!sentShoeSignal)
			{
				udpSendPacket.setLeftFootAir(true);
				//udpSendPacket.cycleShoeStates(-1,0);
				sentShoeSignal = true;
			}
			//top of foot swing
			//A steady state that will move the foot forward at a speed
			//relative to the avgVel found of the rising foot
			leftFootForwardVelocity = forward*avgVel*Time.deltaTime;
			speed = avgVel;
			//Debug.Log ("rfvv:"+leftFootVerticalVelocity+":speed:"+(100*velocity)+":tdt:"+Time.deltaTime+":av:"+avgVel);
			if(leftFootReal.position.y < leftFootInitial.y + 2*stepOffset)
			{
				tempState=8;
				sentShoeSignal = false;
			}
			/*
			//Check for the footdown event
			//if the foot starts falling

			if (-1* leftFootVerticalVelocity > signifcantVelocity){
				tempState = 8;
				//slow the avgVel to make hitting the target easier
				avgVel = avgVel / 2;
			}
			*/
			break;
		case 8:
			udpSendPacket.setLeftFootAir(false);
			//terminal left foot swing
			//the foot should be falling at this point thanks to maintainFootHeight
			//waiting for the foot to hit the ground while still moving forward at the slowed speed
			leftFootForwardVelocity = forward*avgVel*Time.deltaTime;
			speed = avgVel;
			if(leftFootReal.position.y < leftFootInitial.y + stepOffset)
			{
				tempState=9;
			}
			//foot has come to a stop
			/*
			if(-1*leftFootVerticalVelocity <  signifcantVelocity/2)
			{
				tempState = 9;
			}
			*/
			//if (left foot about to hit the floor)
			//go to case 9
			break;
		case 9:
			//left foot strike
			//temporary
			tempState=11;
			/*
			if (leftFootVirtualGrounded){
				tempState = 11;
				leftFootForwardVelocity = Vector3.zero;
				rightFootForwardVelocity = forward;
				currentFoot= foot.right;
				speed = 0;
				pastGhostFeet = (Transform)Instantiate(ghostFoot);
				pastGhostFeet.renderer.material.color = Color.yellow;
				pastGhostFeet.collider.enabled = false;
				pastGhostFeet.parent = feet.transform;
				pastGhostFeet.position = leftFootVirtual.position;
*/
			//setup and wait for left foot movement
			//setup ghost foot location and foot elevations of stable foot
			break;
		case 11:
			avgVel = 0;
			avgVelCount = 0;
			speed = 0;
			//TODO  Should really use the velocity as well as the foot height for reduncancy purposes
			if(rightFootReal.position.y > rightFootInitial.y + stepOffset)
			{
				tempState=2;
			}
			/*
			if (rightFootVerticalVelocity > signifcantVelocity){
				tempState = 2;
			}
			*/
			break;
		}
		if(lastState!=tempState)
		{
			timeOutTimer=Time.time;
			lastState=tempState;
		}
		if(Time.time - timeOutTimer > 2.0f && timeOutTimer != 0)
		{
			tempState = 1;
			lastState = 1;
		}
		//Debug.Log (tempState);
		//Debug.Log("ts"+tempState+" rfvv:"+rightFootVerticalVelocity+ " lfvv:"+leftFootVerticalVelocity + " tdt:" + Time.deltaTime);
		//Velocity Calculations
		
		//moveFeet ();
		//control shoe

		
		//Debug.Log("totL:"+timeOutTimer+":ts:"+tempState+":lffv:"+(100*leftFootForwardVelocity)+":rffv:"+(100*rightFootForwardVelocity));
		//feetCorrection(2.0f);
		//maintainFootHeight();//sets the vertical position of the foot relative to the floor
		//modifyScreenHeight();//used when someone steps onto something to raise the screen
		//handleTurning ();//use the rotation amount to turn the screen
		//move forward the centermass when the feet have move forward, based on the
		//front foot position
		setNewCenterMassPosition(Vector3.zero);
		
	}
	private void moveFoot(foot currentFoot, Vector3 vel)
	{
		if (currentFoot == foot.left)
		{
			leftFootVirtualCC.Move(vel);
		}
		else if (currentFoot == foot.right)
		{
			rightFootVirtualCC.Move(vel);
		}
	}
	
	float finalSpeed =0;
	private void moveFeet()
	{
		//too fast
		if(finalSpeed>speed)
		{
			finalSpeed = (speed - finalSpeed)/32*Time.deltaTime + finalSpeed ;
		}
		//too slow
		else
		{
			
			finalSpeed = (speed - finalSpeed)*Time.deltaTime + finalSpeed ;
		}
		if (currentFoot == foot.left)
		{
			leftFootForwardVelocity.Normalize();
			Debug.Log("fs:"+finalSpeed+":lffv:"+(leftFootForwardVelocity*1000) + ":rffv:" + (rightFootForwardVelocity*1000));
			leftFootForwardVelocity=leftFootForwardVelocity * finalSpeed*Time.deltaTime;
			leftFootVirtualCC.Move (leftFootForwardVelocity);
			rightFootVirtualCC.Move (leftFootForwardVelocity);
		}
		else if (currentFoot == foot.right)
		{
			rightFootForwardVelocity.Normalize();
			Debug.Log("fs:"+finalSpeed+":lffv:"+(leftFootForwardVelocity*1000) + ":rffv:" + (rightFootForwardVelocity*1000));
			rightFootForwardVelocity=rightFootForwardVelocity*finalSpeed*Time.deltaTime;
			rightFootVirtualCC.Move (rightFootForwardVelocity);
			leftFootVirtualCC.Move (rightFootForwardVelocity);
		}
		else if (currentFoot == foot.standstill)
		{}
	}
	
	/*
	 * 
	 * 
	 * 
	 *  This mode tries and creates a virtual body in the virtual world that semi-realisitically
	 *	moves as a person moves.
	 *	
	 *	When a foot is lifted, it's position in the virtual world will move forward at
	 *	walking speed.  If the virtual foot is a paces distance from the non-moving virtual foot
	 *	and the foot is still lifted, it will stop moving forward.  In order to move forward,
	 *	the other physical foot must be lifted.
	 * 
	 * 
	 * */
	
	private void WIPmodel2()
	{
		
		
		//Debug.Log("leftFootInitial: " + leftFootInitial + " leftFoot.position: " +  leftFoot.position);
		//reset the forward in case of rotations
		forward = screenParent.forward;
		
		/*	FOOT PLACEMENT CORRECTION
		 * 
		 * When the user decides that he didn't like where he was standing in the real world
		 * and physically moved about the space in addition to walking in place
		 * 
		 * */
		//Vector3 leftFootDistance = leftFootVirtualClone.position - leftFootVirtual.position;
		//Vector3 rightFootDistance = rightFootVirtualClone.position - rightFootVirtual.position;
		
		
		
		//leftFootDistance.z=0;
		//rightFootDistance.z=0;
		//leftFootDistance.x -= leftFootVirtual.localPosition.x;
		//rightFootDistance.x -= rightFootVirtual.localPosition.x;
		//leftFootDistance.z -= leftFootVirtual.localPosition.z;
		//rightFootDistance.z -= rightFootVirtual.localPosition.z;
		//Debug.Log ("lfd"+leftFootDistance+"lfvcp"+leftFootVirtualClone.position+"btp"+body.transform.position);
		//Debug.Log("rfd"+rightFootDistance+"rfvcp"+rightFootVirtualClone.position);
		
		//if the magnitude is too high,
		//try and move the virtual foot to where the real foot is
		if(testInt ==0)
		{
			//testInt++;
			//Debug.Log("lfvc.p"+leftFootVirtualClone.position+"lfv.p"+leftFootVirtual.position+"lfd:"+leftFootDistance.magnitude+":rfvc.p"+rightFootVirtualClone.position+"rfv.p"+rightFootVirtual.position+"rfd:"+rightFootDistance.magnitude);
		}
		
		//how far foward or backwards the foot is
		/*Some Pretty weird stuff
		 * Moves the screen forward when the real foot moves forward
		 * therefore actually walking doesn't do anything
		 * pretty weird
		 * Not Actually useful
		Vector3 centerMassDisplacementLeft = leftFootReal.position - leftFootInitial;
		if (centerMassDisplacementLeft.x != 0)
		{
			screenParent.parent = null;
			Vector3 newPos = screenParent.position - screenParent.forward*centerMassDisplacementLeft.x;
			screenParent.position = newPos;
			leftFootInitial.Set (leftFootReal.position.x, leftFootInitial.y, leftFootInitial.z);
			screenParent.parent = virtualCenterMass;
		}
		Vector3 centerMassDisplacementRight = rightFootReal.position - rightFootInitial;
		if (centerMassDisplacementRight.x != 0)
		{
			screenParent.parent = null;
			Vector3 newPos = screenParent.position - screenParent.forward*centerMassDisplacementRight.x;
			screenParent.position = newPos;
			rightFootInitial.Set (rightFootReal.position.x, rightFootInitial.y, rightFootInitial.z);
			screenParent.parent = virtualCenterMass;
		}
		*
		*
		*
		*/
		
		feetCorrection(2.0f);
		/*	MOVING THE FEET FORWARD
		 * 
		 * 
		 * 
		 * */
		//assumed a step frequency of 3 for testing
		//from wendt 2010 GUD WIP
		//stepFrequency Currently being set in editor, default 2
		float speed = ((float)Mathf.Pow(((stepFrequency/1.57f) * (height/1.72f)),2));
		
		//Debug.Log(rightFootHeight + " r - l " + leftFootHeight);
		float stepOffset = height/72; // 5 cm for someone who is 180cm tall
		if (leftFootReal.position.y > leftFootInitial.y + stepOffset)//left foot is raised
		{
			//left foot is within pace distance of the right foot
			if (leftFootVirtual.localPosition.z < rightFootVirtual.localPosition.z + paceDistance)
			{
				//leftFootVirtual.position += forward * speed * Time.deltaTime ;//move forward at 1 m/s
				//move forward
				leftFootVirtualCC.Move(forward * speed * Time.deltaTime);
			}
		}
		
		if (rightFootReal.position.y > rightFootInitial.y + stepOffset)//left foot is raised
		{
			//right foot is within pace distance of the left foot
			if (rightFootVirtual.localPosition.z < leftFootVirtual.localPosition.z + paceDistance)
			{
				//move forward
				//rightFootVirtual.position += forward * speed * Time.deltaTime;//move forward at 1 m/s
				
				rightFootVirtualCC.Move(forward * speed * Time.deltaTime);
			}
		}
		
		maintainFootHeight();//sets the vertical position of the foot relative to the floor
		modifyScreenHeight();//used when someone steps onto something to raise the screen
		handleTurning ();//use the rotation amount to turn the screen
		setNewCenterMassPosition(Vector3.zero);//centerMassDisplacementLeft + centerMassDisplacementRight);//move forward the centermass when the feet have move forward
		
		/* Reallign the feet for sliding
		 * Redacted for real foot horizontal positioning
		 * 
		 * */
		/*
		//if the left foot has moved horizontally, realign the right foot
		if (leftFootVirtual.localPosition.x != leftFootVirtualX)
		{
			//find the distance the feet should be apart
			float footDistance = rightFootVirtual.localPosition.x - leftFootVirtualX;
			//reset to the new position of the left foot now that we have the distance it should be
			leftFootVirtualX = leftFootVirtual.localPosition.x;
			//move the right foot to its new location
			Vector3 rightFootVirtualNewPosition = rightFootVirtual.localPosition;
			rightFootVirtualNewPosition.x = leftFootVirtual.localPosition.x + footDistance;
			rightFootVirtual.localPosition = rightFootVirtualNewPosition;
			//set the new right foot local x
			rightFootVirtualX = rightFootVirtual.localPosition.x;
		}
		//if the right foot has moved horizontally, realign the left foot
		if (rightFootVirtual.localPosition.x != rightFootVirtualX)
		{
			//find the distance the feet should be apart
			float footDistance = rightFootVirtualX - leftFootVirtual.localPosition.x;
			//reset to the new position of the left foot now that we have the distance it should be
			rightFootVirtualX = rightFootVirtual.localPosition.x;
			//move the left foot to its new location
			Vector3 leftFootVirtualNewPosition = leftFootVirtual.localPosition;
			leftFootVirtualNewPosition.x = rightFootVirtual.localPosition.x - footDistance;
			leftFootVirtual.localPosition = leftFootVirtualNewPosition;
			//set the new left foot local x
			leftFootVirtualX = leftFootVirtual.localPosition.x;
		}
		*/
		
	}
	
	private void maintainFootHeight()
	{
		/*	MAINTAINING THE VERTICAL POSITION OF THE FEET
		 * 
		 * 
		 * 
		 * */
		
		//Set the y position of the virtual foot relative off the ground compared to
		//the real foots elevation
		
		
		//how high each real foot is off the real ground
		float leftFootHeight = leftFootReal.position.y - realFloor.position.y;
		float rightFootHeight = rightFootReal.position.y - realFloor.position.y;
		
		
		//determine which foot is the lower foot
		int feetCloseEnough = 0; //0 is both feet are close, 1 is left foot is too low, 2 is right foot is too low
		float feetDistance = 1; //the maximum vertical distance the feet are allowed to be apart
		if (leftFootVirtual.position.y - rightFootVirtual.position.y > feetDistance)
		{
			feetCloseEnough = 2;
		}
		
		if (leftFootVirtual.position.y - rightFootVirtual.position.y < -feetDistance)
		{
			feetCloseEnough = 1;
		}
		
		//if real foot is on the ground, move the virtual foot down because of gravity (not actually "accelerating"  like gravity yet)
		//floor is set to approximately .04 below resting foot
		if (realFloor.position.y - leftFootRealHeight - 0.04f < leftFootReal.position.y && 
		    leftFootReal.position.y < realFloor.position.y - leftFootRealHeight + 0.04f &&
		    feetCloseEnough!= 1)
		{
			leftFootVirtualCC.Move(Vector3.down *Time.deltaTime * 2f);
		}
		// if the left virtualfoot is lower than the real foot (compared to the floor) by .04, then move virtual foot up
		else if (leftFootVirtual.position.y < virtualFloor.position.y + leftFootHeight - .04f)
		{
			leftFootVirtualCC.Move (Vector3.up * Time.deltaTime *2f);
		}
		// if the left virtual foot is higher than the real foot is off the ground, move it down
		else if (leftFootVirtual.position.y > virtualFloor.position.y + leftFootHeight  +.04f)
		{
			leftFootVirtualCC.Move(Vector3.down *Time.deltaTime * 2f);
			
		}
		
		//do the same for the right foot
		if (realFloor.position.y - rightFootRealHeight - 0.04f < rightFootReal.position.y &&
		    rightFootReal.position.y < realFloor.position.y - rightFootRealHeight + 0.04f &&
		    feetCloseEnough!=2)
		{
			rightFootVirtualCC.Move(Vector3.down *Time.deltaTime * 2f);
		}
		else if (rightFootVirtual.position.y < virtualFloor.position.y + rightFootHeight - 0.04f)//global y position of virtual foot
		{
			rightFootVirtualCC.Move (Vector3.up * Time.deltaTime *2);
		}
		else  if (rightFootVirtual.position.y > virtualFloor.position.y + rightFootHeight + .04f)
		{
			rightFootVirtualCC.Move(Vector3.down *Time.deltaTime * 2f);
		}
		
	}
	private void handleTurning()
	{
		/*
		 * TURNING
		 * 
		 * 
		 * 
		 * 
		 * 
		 * 
		 * */
		//uses the rotationAmount found from the turning mode to rotate the screen
		
		
		//turning right
		if (rotationAmount > 0)
		{
			Vector3 right = leftFootVirtual.right;
			Vector3 old = rightFootVirtual.position;
			//move the right foot a little to the right ot check for collisions
			rightFootVirtualCC.Move (right*.1f*Time.deltaTime);
			//move the foot back to the it's old position
			rightFootVirtual.position = old;
			//if there are collisions
			if (( rightFootVirtualCC.collisionFlags & CollisionFlags.Sides)!=0)
			{
				//reset the rotationAmount to not rotate
				rotationAmount = 0;
			}
			//if still rotation, check the other foot
			if (rotationAmount !=0)
			{
				old = leftFootVirtual.position;
				leftFootVirtualCC.Move( -1* right * .1f * Time.deltaTime);
				//move the foot back to the it's old position
				leftFootVirtual.position = old;
				if (( leftFootVirtualCC.collisionFlags & CollisionFlags.Sides)!=0)
				{
					//reset the rotationAmount to not rotate
					rotationAmount = 0;
				}
			}
		}
		//turning left
		else if (rotationAmount < 0)
		{
			Vector3 left = -1*leftFootVirtual.right;
			Vector3 old = leftFootVirtual.position;
			//move the right foot a little to the right ot check for collisions
			leftFootVirtualCC.Move (left*.1f*Time.deltaTime);
			//move the foot back to the it's old position
			leftFootVirtual.position = old;
			//if there are collisions
			if (( leftFootVirtualCC.collisionFlags & CollisionFlags.Sides)!=0)
			{
				//reset the rotationAmount to not rotate
				rotationAmount = 0;
			}
			//if still rotation, check the other foot
			if (rotationAmount !=0)
			{
				old = rightFootVirtual.position;
				rightFootVirtualCC.Move( -1* left * .1f * Time.deltaTime);
				//move the foot back to the it's old position
				rightFootVirtual.position = old;
				if (( rightFootVirtualCC.collisionFlags & CollisionFlags.Sides)!=0)
				{
					//reset the rotationAmount to not rotate
					rotationAmount = 0;
				}
			}
			
		}
		
		//First, parent the body to the virtualcentermass, so everything has the same parent
		body.transform.parent = virtualCenterMass;
		body.transform.parent = virtualCenterMass;
		
		//Then, rotate everything about that parent with the rotation amount
		
		virtualCenterMass.transform.Rotate(Vector3.up * rotationAmount);
		
		//Then, unparent the body from the virtualBodyParent the feet to the body
		body.transform.parent = null;
		forward = virtualCenterMass.transform.forward;
	}
	private void modifyScreenHeight()
	{
		/*	MODIFYING THE SCREEN POSITION TO MATCH THE NEW POSITION OF THE FEET
		 * 
		 * 
		 * 
		 * 
		 * 
		 * */
		
		
		//TODO :  make this next part a function, since I essentially just do it twice for each foot
		//if front foot is lower than the back foot, move screen down
		//if front foot is higher and grounded, move screen up
		//if left foot is in front of right
		
		//left foot in front
		if (leftFootVirtual.localPosition.z > rightFootVirtual.localPosition.z)
		{
			//left foot lower that right
			if (leftFootVirtual.position.y < rightFootVirtual.position.y)
			{
				//move screen down
				//if virtualCenterMass.position if more than .04 away from front foot position
				if (leftFootVirtual.position.y - .04 > virtualCenterMass.position.y - virtualCenterMassHeight|| 
				    virtualCenterMass.position.y - virtualCenterMassHeight> leftFootVirtual.position.y + .04)
				{
					Vector3 t1 = virtualCenterMass.position;
					t1.y = leftFootVirtual.position.y + virtualCenterMassHeight;
					virtualCenterMass.position = t1;
				}
			}
			//left foot is higher than right, then if left foot is grounded raise the screen
			else if (leftFootVirtualGrounded)
			{
				if (leftFootVirtual.position.y - .04 > virtualCenterMass.position.y - virtualCenterMassHeight|| 
				    virtualCenterMass.position.y - virtualCenterMassHeight> leftFootVirtual.position.y + .04)
				{
					Vector3 t1 = virtualCenterMass.position;
					t1.y = leftFootVirtual.position.y + virtualCenterMassHeight;
					virtualCenterMass.position = t1;
				}
			}
		}
		//right foot in front
		
		if (leftFootVirtual.localPosition.z < rightFootVirtual.localPosition.z)
		{
			//right foot lower that right
			if (leftFootVirtual.position.y > rightFootVirtual.position.y)
			{
				//move screen down
				//if virtualCenterMass.position if more than .04 away from front foot position
				if (rightFootVirtual.position.y - .04 > virtualCenterMass.position.y - virtualCenterMassHeight|| 
				    virtualCenterMass.position.y - virtualCenterMassHeight> rightFootVirtual.position.y + .04)
				{
					Vector3 t1 = virtualCenterMass.position;
					t1.y = rightFootVirtual.position.y + virtualCenterMassHeight;
					virtualCenterMass.position = t1;
				}
			}
			//left foot is higher than right, then if left foot is grounded raise the screen
			else if (rightFootVirtualGrounded)
			{
				if (rightFootVirtual.position.y - .04 > virtualCenterMass.position.y - virtualCenterMassHeight|| 
				    virtualCenterMass.position.y - virtualCenterMassHeight> rightFootVirtual.position.y + .04)
				{
					Vector3 t1 = virtualCenterMass.position;
					t1.y = rightFootVirtual.position.y + virtualCenterMassHeight;
					virtualCenterMass.position = t1;
				}
			}
		}
	}
	private void setNewCenterMassPosition(Vector3 correctionVector)
	{
		/*	SETTING THE CORRECT HEAD POSITION
			 * 
			 * 
			 * 
			 * 
			 * */
		
		//Find the new location of where the body should be
		//since the feet are moving
		
		//Move the camera to above the front most foot, but still in the center
		//of the two feet for left/right
		
		Vector3 temp = (rightFootVirtual.localPosition + leftFootVirtual.localPosition) / 2;
		//create a new transform so I can use the localPosition of the transforms
		//in order to find the forwardmost spot
		//set the parent so the local positions match up
		globalPositionVariable.transform.parent = rightFootVirtual.parent;
		
		//set the z local position to be the front foots
		//globalPositionVariable.transform.localPosition = temp;
		if (rightFootVirtual.localPosition.z >= leftFootVirtual.localPosition.z)
		{
			temp.z = rightFootVirtual.localPosition.z;
		}
		else
		{
			temp.z = leftFootVirtual.localPosition.z;
		}
		temp.x =0;
		//set globalPositionVariable localposition to the right spot for its x and z components
		//still have to set the Y
		globalPositionVariable.transform.localPosition = temp;
		//To set the y, we find globalPositionVariable's global position replacing temps now useless values
		temp = globalPositionVariable.transform.position;
		//set temps new y
		temp.y = virtualCenterMass.position.y;
		temp += correctionVector;
		//set globalPositionVariable's global position with the now correct y value
		globalPositionVariable.transform.position = temp;
		
		
		// To change between interpolate and absolute
		if (hPModel == headPositionModel.frontFootInterpolate)
		{
			//Interpelate the distance in order to smooth the experience of walking
			virtualCenterMass.position = Vector3.Lerp(virtualCenterMass.position, globalPositionVariable.transform.position, 1f * Time.deltaTime);
		}
		else if (hPModel == headPositionModel.frontFoot)
		{
			virtualCenterMass.position = temp;
		}
		
	}
	
	private void getVerticalVelocities(int previous)
	{
		looper = looper % iterations;
		heightleft[looper] = leftFootReal.position.y;
		heightright[looper] = rightFootReal.position.y;
		leftFootVerticalVelocity = (heightleft[looper] - heightleft[previous]) * Time.deltaTime;
		rightFootVerticalVelocity =  (heightright[looper] - heightright[previous]) * Time.deltaTime;
		
	}
	
	private void feetCorrection(float speed)
	{
		Vector3 leftFootDistance = body.transform.InverseTransformPoint(leftFootVirtualClone.position);
		Vector3 rightFootDistance = body.transform.InverseTransformPoint (rightFootVirtualClone.position);
		leftFootDistance.y = 0;
		rightFootDistance.y=0;
		//Debug.Log("lFD: "+leftFootDistance+"|| rFD: "+rightFootDistance);
		if ((leftFootVirtual.position - leftFootVirtualClone.position).magnitude > .1f )
		{
			//Debug.Log("testing");
			
			Vector3 displacement = leftFootDistance - leftFootVirtual.localPosition;
			displacement.y = 0;
			//displacement.z = 0;
			displacement = leftFootVirtual.TransformDirection(displacement);
			leftFootVirtualCC.Move(displacement*Time.deltaTime*speed);
		}
		if ((rightFootVirtual.position - rightFootVirtualClone.position).magnitude > .1f)
		{
			//Debug.Log("testing");
			Vector3 displacement = rightFootDistance - rightFootVirtual.localPosition;
			displacement.y = 0;
			//displacement.z = 0;
			displacement = rightFootVirtual.TransformDirection(displacement);
			rightFootVirtualCC.Move(displacement*Time.deltaTime*speed);
		}
		
		//Debug.Log("lfvcc"+leftFootVirtualCC.velocity.x+" "+leftFootVirtualCC.velocity.y+" "+leftFootVirtualCC.velocity.z+" rfvcc"+rightFootVirtualCC.velocity.x +" "+rightFootVirtualCC.velocity.x +" "+ rightFootVirtualCC.velocity.z);
		
	}
	
	/*  Old Walking In Place Model
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * 
	 * Old Walking In Place Model
	 * 
	 * */
	private void WIPmodel1(int previous)
	{
		getVerticalVelocities(previous);
		
		
		//butterworth lowpass filter, third order, 60 samples per second, corner frequency of 3 Hz
		//calculated with help from http://www-users.cs.york.ac.uk/~fisher/mkfilter/trad.html
		
		//left foot
		xvVl[0] = xvVl[1]; xvVl[1] = xvVl[2]; xvVl[2] = xvVl[3];
		xvVl[2] = (leftFootVerticalVelocity)/ 345.0423889f;//input / GAIN
		yvVl[0] = yvVl[1]; yvVl[1] = yvVl[2]; yvVl[2] = yvVl[3]; 
		yvVl[3] =   (xvVl[0] + xvVl[3]) + 3 * (xvVl[1] + xvVl[2])
			+ (  0.5320753683f * yvVl[0]) + ( -1.9293556691f * yvVl[1])
				+ (  2.3740947437f * yvVl[2]);
		leftFootVerticalVelocity = -1*yvVl[3];
		
		//right foot
		xvVr[0] = xvVr[1]; xvVr[1] = xvVr[2]; xvVr[2] = xvVr[3];
		xvVr[2] = (rightFootVerticalVelocity)/ 345.0423889f;//input / GAIN
		yvVr[0] = yvVr[1]; yvVr[1] = yvVr[2]; yvVr[2] = yvVr[3]; 
		yvVr[3] =   (xvVr[0] + xvVr[3]) + 3 * (xvVr[1] + xvVr[2])
			+ (  0.5320753683f * yvVr[0]) + ( -1.9293556691f * yvVr[1])
				+ (  2.3740947437f * yvVr[2]);
		rightFootVerticalVelocity = -1*yvVr[3];
		//WALKING STATE MACHINE MODEL
		/*-----------------
					 * Walking States
					 * 0 = both feet on the floor
					 * 1 = right foot rising
					 * 2 = right foot at the top
					 * 3 = right foot falling
					 * 4 = right foot has fallen
					 * 5 = left foot rising
					 * 6 = left foot at top
					 * 7 = left foot falling
					 * repeat
					 * 
					 * 
					 */
		
		/*Left Foot State Machine
					 * 
					 */ 
		
		if(currentWalkingStateLeft==4)
		{
			
			if (leftFootVerticalVelocity > 0.1f)//beginning left foot rise/fall cycle
			{
				
				timeStateStartLeft = Time.time;
				currentWalkingStateLeft = 5;
				thisStepCounter+=0.25f;//NEW
			}
		}
		else if(currentWalkingStateLeft == 5 )//find the fall
		{
			if (leftFootVerticalVelocity < 0.1f) //velocity has slowed, downward movement is coming soon which means maxheight is about to be reached)
			{
				timeStateStartLeft = Time.time;
				currentWalkingStateLeft = 6;
				thisStepCounter+=0.25f;//NEW
			}
		}
		else if(currentWalkingStateLeft == 6 )//max height was just reached
		{
			//if (leftFootVerticalVelocity < -0.5f)//foot is falling, possible unneeded step
			{
				timeStateStartLeft = Time.time;
				currentWalkingStateLeft = 7;
				thisStepCounter+=0.25f;//NEW
			}
		}
		else if(currentWalkingStateLeft == 7 )//foot is falling, find when it hits the floor
		{
			if (leftFootVerticalVelocity > -0.1f)
			{
				timeStateStartLeft = Time.time;
				currentWalkingStateLeft = 4;
				thisStepCounter+=0.25f;//NEW
				//thisStepCounter++;//OLD
				totalSteps++; 
			}
		}
		/* Right Foot State Machine
					 * 
					 */
		if (currentWalkingStateRight==0)
		{
			if (rightFootVerticalVelocity > 0.1f) //beginning right foot rise/fall cycle
			{
				timeStateStartRight = Time.time;
				currentWalkingStateRight = 1;
				thisStepCounter+=0.25f;//NEW
			}
		}
		
		else if(currentWalkingStateRight == 1)
		{
			if (rightFootVerticalVelocity < 0.1f) //velocity has slowed, downward movement is coming soon which means maxheight is about to be reached)
			{
				timeStateStartRight = Time.time;
				currentWalkingStateRight = 2;
				thisStepCounter+=0.25f;//NEW
				
			}/*
						else
						{
							Debug.Log("vRF "+rightFootVerticalVelocity);
						}*/
			
		}
		
		else if (currentWalkingStateRight == 2)
		{
			//if (rightFootVerticalVelocity < -0.5f)//foot is falling, possible unneeded step
			{
				timeStateStartRight = Time.time;
				currentWalkingStateRight = 3;
				thisStepCounter+=0.25f;//NEW
			}
			
		}
		
		else if (currentWalkingStateRight == 3)
		{
			if (rightFootVerticalVelocity > -0.1f)
			{
				timeStateStartRight = Time.time;
				currentWalkingStateRight = 0;
				
				thisStepCounter+=0.25f;//NEW
				//thisStepCounter++;//OLD
				
				totalSteps++; 
				
			}
			
		}
		
		
		if (Time.time - timeStateStartLeft>1.0f)//if the state gets stuck somewhere for too long, reset back to initial state
		{
			currentWalkingStateLeft = 4;
			timeStateStartLeft = Time.time;
		}
		if (Time.time - timeStateStartRight>1.0f)//if the state gets stuck somewhere for too long, reset back to initial state
		{
			currentWalkingStateRight = 0;
			timeStateStartRight = Time.time;
		}
		
		currentSecond = (int)Mathf.Floor(Time.time);
		//Debug.Log("cS"+currentSecond);
		//Debug.Log("lS"+currentSecond);
		if (currentSecond != lastSecond)  // a new second has started
		{
			Debug.Log("entered " + lastStepCounter + " " + thisStepCounter);
			lastSecond = currentSecond;
			penultimateStepCounter = lastStepCounter;
			lastStepCounter = thisStepCounter;
			thisStepCounter = 0;
			
		}
		stepFrequency = (lastStepCounter + thisStepCounter)/2;
		
		//Debug.Log ("lSC" + lastStepCounter);
		//Debug.Log ("tSC" + thisStepCounter);
		
		
		
		
		
		//if new height >= initialheight + 5cm, move forward
		//Debug.Log(heightleft[looper]);
		//Debug.Log (heightright[looper]);
		
		if ((leftFootVerticalVelocity + rightFootVerticalVelocity) >=  .15f)//if the foot is going at .15f or above
		{
			//from wendt2010 GUD WIP
			// V = (frequency/1.57 * height/1.72)^2
			// if there has been at least 1 recorded step in the last second, use that for speed, else
			// use the velocity of the feet to get the initial jump in speed
			if (stepFrequency >=.5)
			{
				if (stepFrequency > 2)
					curVelocity = ((float)Mathf.Pow(((2/1.57f) * (height/1.72f)),2));
				else
					curVelocity = ((float)Mathf.Pow(((stepFrequency/1.57f) * (height/1.72f)),2));
			}
			else
			{
				//This first velocity is if I want the speed for initial/final
				//curVelocity = (leftFootVerticalVelocity + rightFootVerticalVelocity);
				curVelocity = 0;
			}
			Debug.Log(curVelocity);
			//Debug.Log("cV "+curVelocity);
			moveForward = true;
		}
		else if (stepFrequency >=.5)
		{
			if (stepFrequency > 2)
				curVelocity = ((float)Mathf.Pow(((2/1.57f) * (height/1.72f)),2));
			else
				curVelocity = ((float)Mathf.Pow(((stepFrequency/1.57f) * (height/1.72f)),2));
		}
		else
		{
			curVelocity = 0;
			moveForward = false;
		}
		/*
					if (heightleft[looper] >= initialheightleft + .05)//if the foot is up 5 cm from initial height
					{
						moveForward = true;
					}
					else if (heightright[looper] >= initialheightright + .05)
					{
						moveForward = true;
					}
					else
					{
						moveForward = false;
					}
					*/
		
	}
	
	
	
	
}

/*
 * 
 * for every 11 bytes in a chamber, first 2 are distance with a value around 3000 (uncalibrated)

3 to 5th are pressure

last six, each 2 are accelerarions//*
*/