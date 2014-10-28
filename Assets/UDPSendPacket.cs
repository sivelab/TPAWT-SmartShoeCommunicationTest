using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

public class UDPSendPacket : MonoBehaviour {
	UdpClient udpClient;
	IPEndPoint remoteEndPointLeft;
	IPEndPoint remoteEndPointRight;
	private int port = 2000;
	public string IP;
	public string IPleft;
	public string IPright;
	private float time;
	public GUISkin skin;
	// Use this for initialization 
	void Start () {
		//send bytes will be used
		sendBytes = new Byte[3];
		port = 2000;
		udpClient = new UdpClient();
		IPright="192.168.0.102";//default ip address
		IPleft="192.168.0.101";//default ip address
		setupClient();//sets the end host ip and port for each shoe
		guiText.text = "";
		//Dictionaries are like hash tables, but quicker and more precise
		//in c#
		leftFootValveStatus = new Dictionary<int,bool>();
		rightFootValveStatus = new Dictionary<int,bool>();
		//initialate all 7 values for the valves to false
		//Note:  no commands are sent immediately, it just assumes all valves are closed
		//true = open, false = close
		for (int i = 1; i < 8; i ++)
		{
			leftFootValveStatus[i] = false;
			rightFootValveStatus[i] = false;
		}
	}
	private string displayChamber1;
	private Byte[] sendBytes; 
	private string oldIPLeft;
	private string oldIPRight;
	private int oldPort;
	//A style that doesn't have borders and a background, edit in Unity to change style
	public GUIStyle testStyle;
	private Dictionary<int, bool> leftFootValveStatus;//true = open, false = close
	private Dictionary<int, bool> rightFootValveStatus;//true = open, false = close
	public GUIContent shoeGUIContent;
	public enum shoeSide{left=1,right=2};
	public GUISkin tet;
	//edit this with the trials you want to change.  TODO: Working on way to make this editable in the GUI,
	//but it's a bit tricky.
	int[] trialCommands = new int[] {5,6,7,8,9};

	private enum testingCondition{leftNothing=0, leftInversion=1, leftEversion=2, leftExtension=3, leftFlexion=4,
		rightNothing=5, rightInversion=6, rightEversion=7, rightExtension=8, rightFlexion=9};
	///private int [] list = {0,1,2,3,4,5,6,7,8,9};
	private int [] list = {5,6,7,8,9};//only right foot commands
	float testTime = 0;
	bool testing = false;
	bool testSetup = false;
	int waitTime = 0;
	int trialNumber = 0;
	bool signalSent = false;
	bool leftFootAir = false;
	bool rightFootAir = false;
	bool nextStep = false;
	float endTrialTime = 0;
	int numberOfTrials = 5;
	int numberOfSets = 1;

	string trialData;
	bool toggleLeftCloseAll = false;
	bool toggleLeftInversion = false;
	bool toggleLeftEversion = false;
	bool toggleLeftExtension = false;
	bool toggleLeftFlexion = false;
	
	bool toggleRightCloseAll = false;
	bool toggleRightInversion = false;
	bool toggleRightEversion = false;
	bool toggleRightExtension = false;
	bool toggleRightFlexion = false;
	
	string notes = "";
	string statusBar = "Status:  Have the user start walking in place then hit the start test button";
	string trialFile= "C:\\SmartShoe\\TrialNumber#.txt";
	string username = "username";
	bool resetPosition = false;//does nothing yet

	void OnGUI() {
		//
		GUI.TextField(new Rect(10, 10, 80, 20), "Left Shoe IP", 25,testStyle);
		GUI.TextField(new Rect(210, 10, 100, 20), "Right Shoe IP", 25,testStyle);
		oldIPLeft = IPleft;//used to check if the IP has changed
		oldIPRight = IPright;//used to check if the IP has changed
		IPleft = GUI.TextField(new Rect(10, 30, 200, 20), IPleft, 25);
		IPright = GUI.TextField(new Rect(210, 30, 200, 20), IPright, 25);
		oldPort = port;//used to check if the port has changed
		string portString= port.ToString();
		GUI.TextField(new Rect(10, 50, 40, 20), "Port", 25,testStyle);
		portString = GUI.TextField(new Rect(50, 50, 80, 20), portString, 25);
		port = Convert.ToInt32(portString);
		//Note: Sending multiple UDClient.send commands in a single frame is buggy
		// Would not recommend
		if (IPleft != oldIPLeft  || IPright != oldIPRight || oldPort != port)
		{
			setupClient();
		}
		GUI.TextArea(new Rect(10,90,60,20), "Shoe 1", testStyle);
		for (int i = 0; i < 7; i ++)
		{
			if(!testing)
			{
				if (leftFootValveStatus[i+1])//valve is open
				{
					GUI.backgroundColor = Color.green;
					GUI.color = Color.green;
				}
				else//valve is closed
				{
					GUI.backgroundColor = Color.red;
					GUI.color = Color.red;
				}
			}
			else
			{
				GUI.backgroundColor = Color.black;
				GUI.color = Color.black;
			}
			//This if statement both creates and checks the button
			if (GUI.Button(new Rect(10,110+(i*20),60,20), "Valve "+ (i+1)))
			{
				toggleValve(i,shoeSide.left);
			}
		}
		//reset the GUI colors
		GUI.backgroundColor = Color.black;
		GUI.color = Color.white;
		GUI.TextArea(new Rect(200,90,60,20), "Shoe 2", testStyle);
		for (int i = 0; i < 7; i ++)
		{
			if(!testing)
			{
				if (rightFootValveStatus[i+1])//valve is open
				{
					GUI.backgroundColor = Color.green;
					GUI.color = Color.green;
				}
				else//valve is closed
				{
					GUI.backgroundColor = Color.red;
					GUI.color = Color.red;
				}
			}
			else
			{
				GUI.backgroundColor = Color.black;
				GUI.color = Color.black;
			}
			if (GUI.Button(new Rect(200,110+(i*20),60,20), "Valve "+ (i+1)))
			{
				toggleValve(i,shoeSide.right);
			}
		}

		//Resets the GUI color
		GUI.backgroundColor = Color.black;
		GUI.color = Color.white;
		if (GUI.Button(new Rect(80,110,80,20), "OpenAll "))
		{
			openAll(shoeSide.left);
		}
		if (GUI.Button(new Rect(270,110,80,20), "OpenAll "))
		{
			openAll(shoeSide.right);
		}

		if (GUI.Button(new Rect(80,130,80,20), "CloseAll "))
		{
			closeAll(shoeSide.left);
		}
		if (GUI.Button(new Rect(270,130,80,20), "CloseAll "))
		{
			closeAll(shoeSide.right);
		}
		if (GUI.Button(new Rect(80,150,80,20), "Inversion"))
		{
			//ankle outward
			//left 1357
			int[] openArray = {2,4,5,6};
			int[] closeArray = {1,3,7};
			openValvecloseValve(openArray,closeArray,shoeSide.left);
		}
		if (GUI.Button(new Rect(270,150,80,20), "Inversion"))
		{
			//ankle outward
			//Right  = 2456
			int[] openArray = {2,4,5,6};
			int[] closeArray = {1,3,7};
			openValvecloseValve(openArray,closeArray,shoeSide.right);
		}
		if (GUI.Button(new Rect(80,170,80,20), "Eversion"))
		{
			//ankle inward
			//left  = 2456
			int[] openArray = {1,3,5,7};
			int[] closeArray = {2,4,6};
			openValvecloseValve(openArray,closeArray,shoeSide.left);
		}
		if (GUI.Button(new Rect(270,170,80,20), "Eversion"))
		{
			//ankle inward
			//right = 1357
			int[] openArray = {1,3,5,7};
			int[] closeArray = {2,4,6};
			openValvecloseValve(openArray,closeArray,shoeSide.right);
		}
		if (GUI.Button(new Rect(80,190,80,20), "Extension"))
		{
			//toe down
			//left  = 567
			int[] openArray = {1,2,3,4};
			int[] closeArray = {5,6,7};
			openValvecloseValve(openArray,closeArray,shoeSide.left);
		}
		if (GUI.Button(new Rect(270,190,80,20), "Extension"))
		{
			//tow down
			//right = 1234
			int[] openArray = {1,2,3,4};
			int[] closeArray = {5,6,7};
			openValvecloseValve(openArray,closeArray,shoeSide.right);
		}
		if (GUI.Button(new Rect(80,210,80,20), "Flexion"))
		{
			//toe upward
			//left  = 567
			int[] openArray = {5,6,7};
			int[] closeArray = {1,2,3,4};
			openValvecloseValve(openArray,closeArray,shoeSide.left);
		}
		if (GUI.Button(new Rect(270,210,80,20), "Flexion"))
		{
			//toe upward
			//left = 567
			int[] openArray = {5,6,7};
			int[] closeArray = {1,2,3,4};
			openValvecloseValve(openArray,closeArray,shoeSide.right);
		}
		
		GUI.TextArea(new Rect(10,260,80,40),"Have the user stand where they are going to test. Hit the Start Test button and have the" +
			" user start walking in place.  When the status bar \nsays the trial is done, have the user stop walking in place and ask " +
			"them what type of deformation they felt (if any). Additional notes (if they \nsay anything or you want to add something) can" +
			"be placed in the additional notes section.  Hitting next trial will save that information into \n" +
			"the trial file along with the username and what command was actually sent and reset the boxes and notes section.",testStyle);
		
		int y = 40;
		GUI.Box(new Rect(10,280+y,60,20), "TrialFile:",testStyle);
		trialFile = GUI.TextArea(new Rect(70,280+y,300,20), trialFile);
		if (Input.GetKeyDown("b"))
		{
			if (username == "username")
			{
				username = "testingName";
			}
		}
		if (GUI.Button(new Rect(10,300+y,100,20), "Start Test(b)", tet.button)||Input.GetKeyUp("b"))
		{
			if (username == "username")
			{
				statusBar = "Status: Change the username before starting the test!";
			}
			else
			{
				//reshuffle(list);
				waitTime = UnityEngine.Random.Range(3,6);
				trialNumber=0;
				endTrialTime = 0;
				statusBar = "Status: Testing is underway! Undergoing trial " + trialNumber + " of " + numberOfTrials;
				trialData = "Trial " + trialNumber + " of " + username + ". ";
				testTime = Time.time;
				testing = true;
				signalSent = false;
			}
		}
		if (GUI.Button(new Rect(300,300+y,80,20), "Retry Trial", tet.button))
		{

			waitTime = UnityEngine.Random.Range(3,6);
			//trialNumber=0;
			endTrialTime = 0;
			statusBar = "Status: Redoing Trial " +trialNumber + " is underway! Undergoing trial " + trialNumber + " of " + numberOfTrials;
			trialData = "Trial " + trialNumber + " of " + username + ". ";
			testTime = Time.time;
			testing = true;
			signalSent = false;

		}
		username = GUI.TextArea(new Rect(110,300+y,80,20), username);

		if (GUI.Button(new Rect(190,300+y,110,20), ("# of Sets =" + numberOfSets),tet.button))
		{
			numberOfSets=(numberOfSets + 1) % 120;
			numberOfTrials=numberOfSets * 5;
//			int iterations = numberOfTrials / 5;
			//
			/* old random way
			list = new int[numberOfTrials];
			for (int i = 0; i < numberOfTrials; i++)
			{
				list[i] = UnityEngine.Random.Range(5,9);
			}
			*/
			list = createNewList(numberOfSets,trialCommands);
		}
		if (testing)
		{
			/*
			1. wait a random time between 3-6 seconds
			2. send a signal to change the shoe on the next left/right step
			3. on the step immediately after the change while the shoe is in the air, close all the valves to reset the shoe
			4. end trial 3-4 seconds after step 2
			
			
			*/
			if (Time.time - testTime > waitTime)
			{
				if (!signalSent)
				{
					if (list[trialNumber]<= 4)//left foot
					{
						if (leftFootAir)
						{
							sendSignal(list[trialNumber]);//sends the randomly sorted 1st int of list
							signalSent = true;
							nextStep = false;
						}
						else
							statusBar = "Status: Wait Time Over! Waiting for leftfootair  "+"Undergoing trial " + trialNumber + " of "+numberOfTrials;
					}
					else //right foot
					{
						if (rightFootAir)
						{
							sendSignal (list[trialNumber]);
							signalSent = true;
							nextStep = false;
						}
						else
							statusBar = "Status: Wait Time Over! Waiting for rightfootair "+"Undergoing trial " + trialNumber + " of "+numberOfTrials;
					}
				}
				else//signalSent = true
				{
					//check if the foot that was previously in the air is now on the floor
					if (list[trialNumber]<= 4 &&!nextStep) //left foot
					{
						statusBar = "Status: Wait Time Over! leftfoorair got, waiting for left foot down"+"Undergoing trial " + trialNumber + " of "+numberOfTrials;
						if (!leftFootAir)
						{
							nextStep = true;
						}
					}
					else if (!nextStep)//right foot
					{
						statusBar = "Status: Wait Time Over! rightfootair got, waiting for right foot down "+"Undergoing trial " + trialNumber + " of "+numberOfTrials;
						if (!rightFootAir)
						{
							nextStep = true;
						}
					}
					//if the foot that was in the air was on the floor, wait for it to be in
					//the air again so we can send the close all signal to the shoe to close all the bladders
					if(nextStep)
					{
						if (list[trialNumber]<= 4)//left foot
						{
							statusBar = "Status: Wait Time Over! leftfootdown got, waiting for leftfootair to close to close valves"+"Undergoing trial " + trialNumber + " of "+numberOfTrials;
							if (leftFootAir)
							{
								closeAll(shoeSide.left);
								nextStep = false;
								endTrialTime = Time.time;
							}
						}
						else //right foot
						{
							statusBar = "Status: Wait Time Over! rightfootdowngot, waiting for rightfootair to close valves "+"Undergoing trial " + trialNumber + " of "+numberOfTrials;
							if (rightFootAir)
							{
								closeAll(shoeSide.right);
								nextStep = false;
								endTrialTime = Time.time;
							}
						}
					}
				}

				if(endTrialTime != 0)//the shoe has been sent the signal, wait some time to end the trial
				{
					statusBar = "Status: Wait Time Over! End Trial Time. Just waiting for the end. "+"Undergoing trial " + trialNumber + " of "+numberOfTrials;
					if (Time.time - endTrialTime > 3.0f)
					{
						
						statusBar = "Trial Complete!  Ask the user the questions then hit Next Trial";
						testing = false;
					}
				}


			}
			else
			{
				statusBar = "Status: In wait Time  "+"Undergoing trial " + trialNumber + " of "+numberOfTrials;

			}
		}
		GUI.Box(new Rect(10,320+y,80,20),"Left Foot",testStyle);
		GUI.Box(new Rect(210,320+y,80,20),"Right Foot",testStyle);
		toggleLeftCloseAll = GUI.Toggle(new Rect(10,340+y,160,20),toggleLeftCloseAll,"Nothing",tet.toggle);
		toggleLeftInversion = GUI.Toggle(new Rect(10,360+y,160,20),toggleLeftInversion,"Inversion (ankle outward)",tet.toggle);
		toggleLeftEversion = GUI.Toggle(new Rect(10,380+y,160,20),toggleLeftEversion,"Eversion (ankle inward)",tet.toggle);
		toggleLeftExtension = GUI.Toggle(new Rect(10,400+y,160,20),toggleLeftExtension,"Extension (toe down)",tet.toggle);
		toggleLeftFlexion = GUI.Toggle(new Rect(10,420+y,160,20),toggleLeftFlexion,"Flexion (toe upward)",tet.toggle);
		
		toggleRightCloseAll = GUI.Toggle(new Rect(210,340+y,160,20),toggleRightCloseAll,"Nothing",tet.toggle);
		toggleRightInversion = GUI.Toggle(new Rect(210,360+y,160,20),toggleRightInversion,"Inversion (ankle outward)",tet.toggle);
		toggleRightEversion = GUI.Toggle(new Rect(210,380+y,160,20),toggleRightEversion,"Eversion (ankle inward)",tet.toggle);
		toggleRightExtension = GUI.Toggle(new Rect(210,400+y,160,20),toggleRightExtension,"Extension (toe down)",tet.toggle);
		toggleRightFlexion = GUI.Toggle(new Rect(210,420+y,160,20),toggleRightFlexion,"Flexion (toe upward)",tet.toggle);
		
		GUI.Box(new Rect(10, 440+y, 200, 20), "Additional Notes", testStyle);
		notes = GUI.TextArea(new Rect(10, 460+y, 400, 60), notes, 200);
		statusBar = GUI.TextField(new Rect(10, 520+y, 400, 20), statusBar, 200,testStyle);

		if (GUI.Button(new Rect(10,540+y,90,20), "Next Trial", tet.button))
		{
			try{
				System.IO.File.AppendAllText(trialFile, "");
			}
			catch (Exception e){
				statusBar = "FAILED WRITING TO FILE, INVALID FILE NAME PERHAPS??????? Get a good name and try again, otherwize ???";
				Debug.LogError(e.ToString());
				testing = false;
				return;
			}

			if(!parseToggles())
			{
				statusBar = "Please select a toggle before hitting next trial!";
				testing = false;
				return;
			}
			trialData += ":  Actual signal was " + convertEnum(list[trialNumber]);
			trialData += "\n  Additional Notes for trial " + trialNumber + ": " + notes;
			trialData = trialData.Replace("\n", "");//remove all newlines
			try{
				System.IO.File.AppendAllText(trialFile, trialData + "\r\n");
			}
			catch (Exception e){
				Debug.LogError(e.ToString());
				statusBar = "It failed again??? HOW DID IT PAST THE FIRST TEST. SNEAKY HACKER.";
				testing = false;
				return;
			}
			Debug.Log (trialData);
			//write the data to file

			//reset variables for next trial

			waitTime = UnityEngine.Random.Range(3,6);
			trialNumber++;
			endTrialTime = 0;
			statusBar = "Status: Testing is underway! Undergoing trial " + trialNumber + " of "+numberOfTrials;
			trialData = "Trial " + trialNumber + " of " + username + ". ";
			testTime = Time.time;
			testing = true;
			signalSent = false;
			
			notes = "";
			toggleLeftCloseAll = false;
			toggleLeftInversion = false;
			toggleLeftEversion = false;
			toggleLeftExtension = false;
			toggleLeftFlexion = false;
			
			toggleRightCloseAll = false;
			toggleRightInversion = false;
			toggleRightEversion = false;
			toggleRightExtension = false;
			toggleRightFlexion = false;

			if(trialNumber>=numberOfTrials)
			{
				trialNumber = 0;
				statusBar="Last Test Complete!";
				testing= false;
				return;
			}
		}
		//list = new int[] {1,1,1,1,1,1,1};
		if (GUI.Button(new Rect(100,540+y,250,20), "End Trials Early (doesn't save anything)", tet.button)||Input.GetKeyDown("n"))
		{
			testing = false;
			statusBar = "Testing ending!  Restarting will probably work.";
		}

	}

	//numTrials is the number of trials you want
	//trialValues contains the associated trial numbers for valve states. i.e. {0,1,2,3,4} for left foot
	// or {5,6,7,8,9} for the right foot
	int[] createNewList(int numTrial, int[] trialValues)
	{
		int numPermutation = trialValues.Length;
		if (numTrial > Factorial(numPermutation))
		{
			Debug.LogError("numTrial needs to be less than or equal to factorial(numPermutation) otherwise cannot make unique trial sets");
			return new int[] {1,2,3,4,5};
		}

		Dictionary<int,int[]> listDict = new Dictionary<int, int[]>();
		int[] finalList = new int[numPermutation*numTrial];
		//create numTrial amount of little unique lists
		for (int i = 0; i < numTrial; i++)
		{
			bool newList = true;
			int [] tmpList = new int[numPermutation];
			smallList(tmpList,trialValues);
			//printList(tmpList);

			//check if the list we just created is unique
			for (int k = 0; k < listDict.Count; k++)
			{

				if(listCompare(listDict[k], tmpList))
				{
					/*
					Debug.LogError("SAME LISTS START");
					printList(listDict[k]);
					printList (tmpList);
					Debug.LogError("SAME LISTS END");
					*/
					newList = false;
				}

				//check to see if the list is already saved
				/*
				if (listDict[k].Intersect(tmpList).Any())
				{
					Debug.Log ("intersection");
					newList = false;
				}
				*/
			}
			while(!newList)
			{
				smallList (tmpList, trialValues);
				newList = true;
				for (int k = 0; k < listDict.Count; k++)
				{

					if(listCompare(listDict[k], tmpList))
					{
						/*
						Debug.LogError("SAME LISTS START");
						printList(listDict[k]);
						printList (tmpList);
						Debug.LogError("SAME LISTS END");
						*/
						newList=false;
					}

					//check to see if the list is already saved
					/*
					if (listDict[k].Intersect(tmpList).Any())
					{
						Debug.Log ("intersection");
						newList = false;
					}
					*/
				}
			}
			printList(tmpList);
			listDict[i] = tmpList;
		}
		//reassemble one final list from the smaller ones
		for (int i = 0; i < listDict.Count; i++)
		{
			for (int j =0; j < numPermutation; j++)
			{
				finalList[j+5*i] = listDict[i][j];
			}
		}
		printList(finalList);
		Debug.Log ("fL.l "+finalList.Length);
		return finalList;
	}
	//returns true if the two lists are equal
	bool listCompare(int[] a, int[] b)
	{
		if (a.Length != b.Length)
		{
			Debug.LogError("lists not the same length");
			return false;
		}
		// if the two values are ever not equal, return false, otherwise all equal
		for (int i = 0; i < a.Length; i ++)
		{
			if (a[i] != b[i])
			{
				return false;
			}
		}
		return true;
	}

	void smallList (int[] tinyList, int[] trialValues)
	{
		for (int j = 0; j < trialValues.Length; j++)
		{
			tinyList[j]=trialValues[j];
		}
		reshuffle(tinyList);

	}



	public bool getResetPosition()
	{
		return resetPosition;
	}
	void printList(int[] pList)
	{
		string tempStr= "";
		for (int i = 0; i < pList.Length; i++)
		{
			tempStr = tempStr + pList[i] + " ";
		}
		Debug.Log(tempStr);
	}
	void sendSignal(int switchNum)
	{
		switch(switchNum)
		{
		case 0:
			//DO NOTHING 

			break;
		case 1:
			// left inversion
		{
			int[] openArray = {2,4,5,6};
			int[] closeArray = {1,3,7};
			openValvecloseValve(openArray,closeArray,shoeSide.left);
		}
			break;
		case 2:
			// left eversion
		{
			int[] openArray = {1,3,5,7};
			int[] closeArray = {2,4,6};
			openValvecloseValve(openArray,closeArray,shoeSide.left);
		}
			break;
		case 3:
			//left extension
		{
			int[] openArray = {1,2,3,4};
			int[] closeArray = {5,6,7};
			openValvecloseValve(openArray,closeArray,shoeSide.left);
		}
			break;

		case 4:
			//left flexion
		{
			int[] openArray = {5,6,7};
			int[] closeArray = {1,2,3,4};
			openValvecloseValve(openArray,closeArray,shoeSide.left);
		}
			break;

		case 5:
			//NOTHING FOR RIGHT FOOT
		{
			//NOT ANY DIFFERENT THAN NOTHING FOR LEFT??!?!?
		}
			break;

		case 6:
			//right inversion
		{
			int[] openArray = {2,4,5,6};
			int[] closeArray = {1,3,7};
			openValvecloseValve(openArray,closeArray,shoeSide.right);
		}
			break;

		case 7:
			//right eversion
		{
			int[] openArray = {1,3,5,7};
			int[] closeArray = {2,4,6};
			openValvecloseValve(openArray,closeArray,shoeSide.right);
		}
			break;
		case 8:
			//right extension
		{
			int[] openArray = {1,2,3,4};
			int[] closeArray = {5,6,7};
			openValvecloseValve(openArray,closeArray,shoeSide.right);
		}
			break;

		case 9:
			//right flexion
		{
			int[] openArray = {5,6,7};
			int[] closeArray = {1,2,3,4};
			openValvecloseValve(openArray,closeArray,shoeSide.right);
		}
			break;
		}

	}
	private bool parseToggles()
	{
		bool atLeastOneTrue = false;

		 if(toggleLeftCloseAll) {
			trialData += "UserGuess = leftCloseAll";
			atLeastOneTrue = true;
		}
		 if(toggleLeftInversion) {
			trialData += "UserGuess = leftInversion";
			atLeastOneTrue = true;
		}
		 if(toggleLeftEversion ){
			trialData += "UserGuess = leftEversion";
			atLeastOneTrue = true;
		}
		 if(toggleLeftExtension ){
			trialData += "UserGuess = leftExtension";
			atLeastOneTrue = true;
		}
		 if(toggleLeftFlexion ){
			trialData += "UserGuess = leftFlexion";
			atLeastOneTrue = true;
		}

		 if(toggleRightCloseAll ){
			trialData += "UserGuess = rightCloseAll";
			atLeastOneTrue = true;
		}
		 if(toggleRightInversion ){
			trialData += "UserGuess = rightInversion";
			atLeastOneTrue = true;
		}
		 if(toggleRightEversion ){
			trialData += "UserGuess = rightEversion";
			atLeastOneTrue = true;
		}
		 if(toggleRightExtension){
			trialData += "UserGuess = rightExtension";
			atLeastOneTrue = true;
		}
		if(toggleRightFlexion ){
			trialData += "UserGuess = rightFlexion";
			atLeastOneTrue = true;
		}
		return atLeastOneTrue;

	}
	private string convertEnum(int tC)
	{
		int switcher = tC;
		string returnString="invalidstringtemporyplacement";
		switch (switcher)
		{
		case 0:
			returnString = "leftNothing";
			break;
		case 1:
			returnString = "leftInversion";
			break;
		case 2:
			returnString = "leftEversion";
			break;
		case 3:
			returnString = "leftExtension";
			break;
		case 4:
			returnString = "leftFlexion";
			break;
		case 5:
			returnString = "rightNothing";
			break;
		case 6:
			returnString = "rightInversion";
			break;
		case 7:
			returnString = "rightEversion";
			break;
		case 8:
			returnString = "rightExtension";
			break;
		case 9:
			returnString = "rightFlexion";
			break;
		}
		return returnString;
	}
	public void setRightFootAir(bool air)
	{
		rightFootAir = air;
	}
	public void setLeftFootAir(bool air)
	{
		leftFootAir = air;
	}

	public bool getTesting()
	{
		return testing;
	}

	public int getTrialNumber()
	{
		return trialNumber;
	}
	void setupClient()
	{
		try{
			remoteEndPointLeft = new IPEndPoint(IPAddress.Parse(IPleft), port);
		}
		catch(Exception e ){
			Debug.LogError(e);
		}
		try{
			remoteEndPointRight = new IPEndPoint(IPAddress.Parse(IPright), port);
		}
		catch(Exception e ){
			Debug.LogError(e);
		}
	}
	void openAll(shoeSide side)
	{

		sendBytes[0] = 0xEE;
		sendBytes[1] = 0xEE;
		sendBytes[2] = 0x00;
		if (side == shoeSide.left)
		{
			for (int i = 1; i < 8; i ++)
			{
				leftFootValveStatus[i] = true;
			}
			udpClient.Send(sendBytes, sendBytes.Length, remoteEndPointLeft);
		}
		else
		{
			for (int i = 1; i < 8; i ++)
			{
				rightFootValveStatus[i] = true;
			}
			udpClient.Send(sendBytes, sendBytes.Length, remoteEndPointRight);
		}
	}
	void closeAll(shoeSide side)
	{

		Byte [] sendLargeByte = new Byte[6];
		sendLargeByte[0] = 0xFF;
		sendLargeByte[1] = 0xFF;
		sendLargeByte[2] = 0x00;
		sendLargeByte[3] = 0xFF;
		sendLargeByte[4] = 0xFF;
		sendLargeByte[5] = 0x00;
		if( side == shoeSide.left)
		{
			for (int i = 1; i < 8; i ++)
			{
				leftFootValveStatus[i] = false;
			}
			udpClient.Send(sendLargeByte, sendLargeByte.Length, remoteEndPointLeft);
		}
		else
		{
			for (int i = 1; i < 8; i ++)
			{
				rightFootValveStatus[i] = false;
			}
			udpClient.Send(sendLargeByte, sendLargeByte.Length, remoteEndPointRight);
		}
	}
	//Toggles the valve sent in with valve and the shoe corresponding to side
	void toggleValve(int valve, shoeSide side)
		//valve should be 0-6
	{
		valve = valve + 1;
		if (side == shoeSide.left)
		{
			if (leftFootValveStatus[valve])//left foot valve is open
			{
				leftFootValveStatus[valve] = false;
				//close valve
				string hex = valve.ToString ("X");
				Byte [] sendLargeByte = new Byte[6];
				sendLargeByte[0] = Convert.ToByte(hex,16);
				sendLargeByte[1] = 0xFF;
				sendLargeByte[2] = 0x00;
				sendLargeByte[3] = Convert.ToByte(hex,16);
				sendLargeByte[4] = 0xFF;
				sendLargeByte[5] = 0x00;
				udpClient.Send(sendLargeByte, sendLargeByte.Length, remoteEndPointLeft);
			}
			else //left foot valve is closed
			{
				leftFootValveStatus[valve] = true;
				string hex = valve.ToString ("X");
				sendBytes[0] = Convert.ToByte(hex,16);
				sendBytes[1] = 0xEE;
				sendBytes[2] = 0x00;
				udpClient.Send(sendBytes, sendBytes.Length, remoteEndPointLeft);
			}
		}
		else//implied right side
		{
			if (rightFootValveStatus[valve])//left foot valve is open
			{
				
				rightFootValveStatus[valve] = false;
				//close valve
				string hex = valve.ToString ("X");
				Byte [] sendLargeByte = new Byte[6];
				sendLargeByte[0] = Convert.ToByte(hex,16);
				sendLargeByte[1] = 0xFF;
				sendLargeByte[2] = 0x00;
				sendLargeByte[3] = Convert.ToByte(hex,16);
				sendLargeByte[4] = 0xFF;
				sendLargeByte[5] = 0x00;
				udpClient.Send(sendLargeByte, sendLargeByte.Length, remoteEndPointRight);
			}
			else //left foot valve is closed
			{
				rightFootValveStatus[valve] = true;
				string hex = valve.ToString ("X");
				sendBytes[0] = Convert.ToByte(hex,16);
				sendBytes[1] = 0xEE;
				sendBytes[2] = 0x00;
				udpClient.Send(sendBytes, sendBytes.Length, remoteEndPointRight);
			}
		}
	}
	//opens all valves in openarray, closes all vales in close array (like usual,
	//it closes them twice)
	void openValvecloseValve(int [] openArray, int [] closeArray, shoeSide sSide)
	{

		//3 bytes for opening, 6 for closings (since there are two close messages)
		int size = openArray.Length *3+ closeArray.Length*6;
		Byte [] sendLargeByte = new Byte[size];
		for (int i =0; i < openArray.Length; i++)
		{
			if (sSide == shoeSide.left)
			{
				leftFootValveStatus[openArray[i]] = true;
			}
			else
			{
				rightFootValveStatus[openArray[i]] = true;
			}
			string hex = openArray[i].ToString();
			sendLargeByte[(i*3)] = Convert.ToByte(hex,16);
			sendLargeByte[((i*3)+1)] = 0xEE;
			sendLargeByte[((i*3)+2)] = 0x00;
		}
		for (int i =0; i < closeArray.Length; i++)
		{
			if (sSide == shoeSide.left)
			{
				leftFootValveStatus[closeArray[i]] = false;
			}
			else
			{
				rightFootValveStatus[closeArray[i]] = false;
			}
			string hex = closeArray[i].ToString();
			sendLargeByte[openArray.Length*3 + (i*6)] = Convert.ToByte(hex,16);
			sendLargeByte[openArray.Length*3 + ((i*6)+1)] = 0xFF;
			sendLargeByte[openArray.Length*3 + ((i*6)+2)] = 0x00;
			sendLargeByte[openArray.Length*3 + ((i*6)+3)] = Convert.ToByte(hex,16);
			sendLargeByte[openArray.Length*3 + ((i*6)+4)] = 0xFF;
			sendLargeByte[openArray.Length*3 + ((i*6)+5)] = 0x00;
		}
		if (sSide == shoeSide.left)
		{
			udpClient.Send(sendLargeByte, sendLargeByte.Length, remoteEndPointLeft);
		}
		else if (sSide == shoeSide.right)
		{
			udpClient.Send(sendLargeByte, sendLargeByte.Length, remoteEndPointRight);
		}
	}

	private  int cycle = 0;
	private bool everyOther = false;
	//0 is left shoe, 1 is right shoe
	public string shoeValve(shoeSide sSide)
	{
		//returns a string with the valve state
		string valveStatus = "";
		for (int i = 7; i > 0; i --)
		{
	
	

			if (sSide == shoeSide.left)
			{
				if (leftFootValveStatus[i])
				{
					//append to string
					valveStatus = valveStatus + 1;
				}
				else
				{
					valveStatus = valveStatus + 0;
				}
			}
			else
			{
				if (rightFootValveStatus[i])
				{
					valveStatus = valveStatus + 1;
				}
				else
				{
					valveStatus = valveStatus + 0;
				}
			}
		}
		return valveStatus;
	}
	public  void cycleShoeStates(int cycleState, int whichShoe)
	{
		shoeSide sSide;
		if (whichShoe == 0)
		{
			sSide = shoeSide.left;
		}
		else
		{
			sSide = shoeSide.right;
		}
		//pass in a cyclestate of not -1 in order to set your own cycle elsewhere
		//otherwise, pass in -1 and it will cycle 1,2,3,4,5,6
		if (cycleState!=-1)
		{
			cycle = cycleState;
		}
		//openall closeall inversion eversion extension flexion
		//0 = openall
		//1 = closeall
		//2 = inversion
		//3 = eversion
		//4 = extension
		//5 = flexion
		switch(cycle)
		{
		case 0:
			openAll(sSide);

			break;
		case 1:
			closeAll(sSide);

			break;
		case 2:
		{
			//ankle outward
			//left 1357
			int[] openArray = {2,4,5,6};
			int[] closeArray = {1,3,7};
			openValvecloseValve(openArray,closeArray,sSide);
			break;
		}
		case 3:
		{
			//ankle inward
			//right = 1357
			int[] openArray = {1,3,5,7};
			int[] closeArray = {2,4,6};
			openValvecloseValve(openArray,closeArray,sSide);

			break;
		}
		case 4:
		{
			//tow down
			//right = 1234
			int[] openArray = {1,2,3,4};
			int[] closeArray = {5,6,7};
			openValvecloseValve(openArray,closeArray,sSide);

			break;
		}
		case 5:
		{
			//toe upward
			//left  = 567
			int[] openArray = {5,6,7};
			int[] closeArray = {1,2,3,4};
			openValvecloseValve(openArray,closeArray,sSide);

			break;

		}
		}
		if(everyOther)
		{
			cycle= (cycle+1) % 6;
			everyOther = false;
		}
		else
		{
			everyOther = true;
		}
		Debug.Log(cycle);
	}
	
	//from http://forum.unity3d.com/threads/randomize-array-in-c.86871/
	void reshuffle(int[] texts)
	{
		// Knuth shuffle algorithm :: courtesy of Wikipedia :)
		for (int t = 0; t < texts.Length; t++ )
		{
			int tmp = texts[t];
			int r = UnityEngine.Random.Range(t, texts.Length);
			texts[t] = texts[r];
			texts[r] = tmp;
		}
	}
	int Factorial(int i)
	{
		if (i <= 1)
			return 1;
		return i * Factorial(i - 1);
	}
}

