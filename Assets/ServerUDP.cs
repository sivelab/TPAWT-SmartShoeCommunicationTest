using UnityEngine;
using System.Collections;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading;
using System.Collections.Generic;

public class ServerUDP : MonoBehaviour {

	public UDPSendPacket udpSendPacket;
	string leftShoeLastCommand = "";
	string rightShoeLastCommand = "";
	Thread receiveThread;
	UdpClient client;
	public int port;
	IPEndPoint sender;
	private string stringData = "Waiting to receive data";
	string leftShoeValveStatus="-------";//0 = valve 1, 1 = valve 2, etc
	string rightShoeValveStatus="-------";//0 = valve 1, 1 = valve 2, etc

	// Use this for initialization
	void Start () {
		port = 2050;
		leftShoeProximityData = new float[7];
		rightShoeProximityData = new float[7];
		leftShoePressureData = new float[7];
		rightShoePressureData = new float[7];
		/*
		byte[] test = new Byte[]{129,255,16,8};
		//int test2 = ToInt32(test,0);
		bool test2 = isSet(test, 0);
		Debug.Log("test: " + test2);
*/
   }	
	
	void startServer()
	{

		if (!serverStarted)
		{
			receiveThread = new Thread(new ThreadStart(ReceiveData));
			receiveThread.IsBackground = true;
			receiveThread.Start ();
			serverStarted = true;
		}
	}
	// Update is called once per frame
	public float globalTime;
	private string debugString = "Debug String";
	void Update () {
		globalTime = Time.time;
		testing = udpSendPacket.getTesting();
		trialNumber = udpSendPacket.getTrialNumber();
		if (testing&&!serverStarted)
		{
			startServer();
		}
		//guiText.text = stringData;

		
		/*
      print("Message received from {0}:" + sender.ToString());
      print(Encoding.ASCII.GetString(data, 0, data.Length));

      string welcome = "Welcome to my test server";
      data = Encoding.ASCII.GetBytes(welcome);
      newsock.Send(data, data.Length, sender);

      while(true)
      {
         data = newsock.Receive(ref sender);
       
         Console.WriteLine(Encoding.ASCII.GetString(data, 0, data.Length));
         newsock.Send(data, data.Length, sender);
      }
      */
	}
	public string writeLocationLeft= "C:\\SmartShoe\\DataLeft.txt";
	public string writeLocationRight= "C:\\SmartShoe\\DataRight.txt";
	public GUIStyle testStyles;
	private bool startRecording = false;
	private bool serverStarted = false;

	private bool testing = false;
	private int trialNumber = -1;
	private string serverStatus = "";
	void OnGUI(){
		GUI.TextField(new Rect(450, 10, 100, 20), "Receiving Port", 25,testStyles);
		port = Convert.ToInt32(GUI.TextField(new Rect(550, 10, 60, 20), port.ToString(), 25));
		if (GUI.Button(new Rect(450, 30, 100, 20), "Start Server"))
		{
			startServer();
		}
		GUI.TextField(new Rect(450, 50, 200, 20), "Save Leftfoot to File", 50, testStyles);
		writeLocationLeft= GUI.TextField(new Rect(450, 70, 200, 20), writeLocationLeft, 50);
		GUI.TextField(new Rect(450, 90, 200, 20), "Save Rightfoot to File", 50, testStyles);
		writeLocationRight= GUI.TextField(new Rect(450, 110, 200, 20), writeLocationRight, 50);

		if (GUI.Button(new Rect(450, 130, 100, 20), "Start Saving"))
		{
			startRecording = true;
		}
		if (GUI.Button(new Rect(550, 130, 100, 20), "Stop Saving"))
		{
			startRecording = false;
		}
		if (!serverStarted && startRecording)//error, start server!
		{
			GUI.TextField(new Rect(450,150, 100, 20), "Error:  Server not Started",testStyles);
		}
		else if (serverStarted && startRecording)//error, start server!
		{
			GUI.TextField(new Rect(450, 150, 100, 20), "Recording!",testStyles);
		}
		else if (!serverStarted && !startRecording)//error, start server!
		{
			GUI.TextField(new Rect(450, 150, 100, 20), "Server Not Started",testStyles);
		}
		else if (serverStarted && !startRecording)//error, start server!
		{
			GUI.TextField(new Rect(450, 150, 100, 20), "Server Ready!",testStyles);
		}
		GUI.TextField(new Rect(450, 185, 100, 20), "left Valve : "+leftShoeValveStatus,testStyles);

	
		GUI.TextField(new Rect(450, 205, 100, 20), "right Valve: "+rightShoeValveStatus,testStyles);
		GUI.TextField(new Rect(450, 245, 100, 20), "packets received right: "+packetsReceivedRight,testStyles);
		GUI.TextField(new Rect(450, 225, 100, 20), "packets received  left: "+packetsReceivedLeft,testStyles);
		
		
		debugString = GUI.TextField(new Rect(580, 335, 20, 20), debugString,testStyles);
	}

	//float pastTime = 0;
	private int packetsReceivedRight = 0;
	private int packetsReceivedLeft = 0;
	private int leftShoeProximityIterator=0;
	public  float[] leftShoeProximityData;
	public  float[] rightShoeProximityData;
	public  float[] leftShoePressureData;
	public  float[] rightShoePressureData;
	private void ReceiveData()
	{
		client = new UdpClient(port);
		float pastTime = 0;
		while (true)
		{
			try{
				IPEndPoint anyIP = new IPEndPoint (IPAddress.Any, 0);
      			byte[] data = new byte[81];
				data = client.Receive(ref anyIP);

				stringData = ByteArrayToString(data);//for data saving purposes
				//test to see if the split work

				//parse data

				for (int i = 0; i < data.Length; i++)
				{
					Debug.Log (i+" "+data[i]);
				}
				if (isSet(data, 24))//check the bit for which shoe, if true it's right foot
				{
					//right shoe
					//Debug.Log("RIGHT SHOE!");
					packetsReceivedRight++;
					if (data.Length > 15 && (packetsReceivedRight%5 == 0))//only process every fifth packet
					{
						if (data[0]+data[1]+data[2] ==381)//7f+7f+7f
						{
							//takes the 4th byte received and converts it to a string of 8 values
							string testStr = System.Convert.ToString(data[3],2);
							testStr = testStr.PadLeft(8,'0');
							Debug.Log ("first bit: " + testStr[0] + " wholething: " + testStr);
							rightShoePressureData[0] = ToInt32(data,6);
							rightShoePressureData[1] = ToInt32(data,18);
							rightShoePressureData[2] = ToInt32(data,30);
							rightShoePressureData[3] = ToInt32(data,42);
							rightShoePressureData[4] = ToInt32(data,54);
							rightShoePressureData[5] = ToInt32(data,66);
							rightShoePressureData[6] = ToInt32(data,78);
							
							
							debugString = "";
							rightShoeValveStatus = testStr.Substring(1);
							//takes the 4th and 5th bytes and converts them to a 2 byte int
							rightShoeProximityData[0] = ToInt16(data,4);
							rightShoeProximityData[1] = ToInt16(data,16);
							rightShoeProximityData[2] = ToInt16(data,28);
							rightShoeProximityData[3] = ToInt16(data,40);
							rightShoeProximityData[4] = ToInt16(data,52);
							rightShoeProximityData[5] = ToInt16(data,64);
							rightShoeProximityData[6] = ToInt16(data,76);
							// [0] - 24 header
							// [3] - valve state
							// [4] - 16 prox. 1
							// [6] - 32 pres. 1
							//6 bytes acc

							// [16] - 16 prox. 1
							// [18] - 32 pres. 1
							//6 bytes acc


							// [28] - 16 prox. 1
							// [30] - 32 pres. 1
							//6 bytes acc


							// [40] - 16 prox. 1
							// [42] - 32 pres. 1
							//6 bytes acc


							// [52] - 16 prox. 1
							// [54] - 32 pres. 1
							//6 bytes acc


							// [64] - 16 prox. 1
							// [66] - 32 pres. 1
							//6 bytes acc


							// [76] - 16 prox. 1
							// [78] - 32 pres. 1
							//6 bytes acc
							//85 bytes totes
							/*

							*/
							//if the first bit is 1, it is the right foot
							//else//right
						}

					}
				}
				else //left shoe
				{
					packetsReceivedLeft++;
					if (data.Length > 15 && (packetsReceivedLeft%5 == 0))//only process every fifth packet
					{
						if (data[0]+data[1]+data[2] ==381)//7f+7f+7f
						{
							string testStr = System.Convert.ToString(data[3],2);
							testStr = testStr.PadLeft(8,'0');
							if (testStr[0] == '0')//left
							{
								leftShoeValveStatus = testStr.Substring(1);
								//takes the 4th and 5th bytes and converts them to a 2 byte int
								leftShoeProximityData[0] = ToInt16(data,4);
								leftShoeProximityData[1] = ToInt16(data,16);
								leftShoeProximityData[2] = ToInt16(data,28);
								leftShoeProximityData[3] = ToInt16(data,40);
								leftShoeProximityData[4] = ToInt16(data,52);
								leftShoeProximityData[5] = ToInt16(data,64);
								leftShoeProximityData[6] = ToInt16(data,76);
							}
						}
					}
				}
				/*
				for (int i =0; i < stringArray.Length; i ++)
				{
					Debug.Log (i+ " " + stringArray[i]);
				}


				if (stringArray.Length > 15)
				{
					foreach(string str in stringArray)
					{
						str.Trim();//removes all whitespace
					}
					Debug.Log (stringArray[0].CompareTo("7D"));
					//Check that the header is right
					//if (stringArray[0].CompareTo("7F")==0&&stringArray[1].CompareTo("7F")==0&&stringArray[2].CompareTo("7F")==0)
					if (stringArray[0].CompareTo("7F")==0)//==0&&stringArray[1].CompareTo("7F")==0&&stringArray[2].CompareTo("7F")==0)
					{
						string testStr = HexStringToBinary(stringArray[3]);
						if (testStr.Length == 8)
						{
							if (testStr[0]==0)//left shoe
							{
								leftShoeValveStatus = testStr.Substring(1);	
								testStr = stringArray[4] +  stringArray[5];
								//Debug.Log (testStr);
								leftShoeProximity = System.Convert.ToInt32(testStr,16).ToString();//proximity data
								testStr = stringArray[6] +  stringArray[7]+  stringArray[8];
								leftShoePressure = System.Convert.ToInt32(testStr,16).ToString();//Pressure data

							}
							else //1 is right shoe
							{
								rightShoeValveStatus = testStr.Substring(1);
								testStr = stringArray[4] +  stringArray[5];
								//Debug.Log (testStr);
								rightShoeProximity = System.Convert.ToInt32(testStr,16).ToString ();
								testStr = stringArray[6] +  stringArray[7]+  stringArray[8];
								rightShoePressure = System.Convert.ToInt32(testStr,16).ToString();//Pressure data
							}
						}
						else
						{
							Debug.LogError("Malformed Byte?? How did that happen? testStr = "+ testStr);
						}
					}
					else
					{
						Debug.Log ("Received invalid header, ignoring packet");
					}

				}
				else
				{
					Debug.LogError ("Received packet was too small, ignore!");
				}

				//string[] splitString = stringData.Split (stringData, char.Parse("_"));

				//Debug.Log("dl:"+data.Length + " " + (globalTime-pastTime));
				//pastTime = globalTime;
				*/
				if (startRecording)
				{
					System.IO.File.AppendAllText(writeLocationLeft, "globalTime:"+globalTime+"  timeDifference:"+(globalTime-pastTime)+"   "+stringData+ "\r\n");
					pastTime = globalTime;
				}
				if(testing)
				{
					try{
						System.IO.File.AppendAllText(writeLocationLeft, "globalTime:"+globalTime + "   trialNumber:"+trialNumber+": Data:"+stringData+ "\r\n");
						pastTime = globalTime;

						serverStatus = "Saving Data from the test!";
					}
					catch (Exception e){
						serverStatus = "tried and failed to save testing data.  Perhaps bad file location?";
					}
				}

			}
			catch(Exception e){
				Debug.LogError(e);
				//guiText.text = e.ToString ();
			}
		}
	}
	public static string ByteArrayToString(byte[] ba)
	{
		string hex = BitConverter.ToString(ba);
		return hex.Replace("-"," ");
	}

	private void OnDisable() 
	{ 
		if ( receiveThread!= null) 
			receiveThread.Abort(); 
		if(client != null)
		client.Close(); 
	} 
	//converts a byte array at position start index and the next byte after it into a short. 
	//Assumes data is big endian
	public static short ToInt16(byte[] value, int startIndex)
	{
		short result = 0;
		result = (short) value[startIndex];
		result = (short)(result << 8);
		result += (short) value[startIndex + 1];
		return result;
	}
	//converts a byte array at position start index and the next 3 bytes after it into an int. 
	//Assumes data is big endian
	public static int ToInt32(byte[] value, int startIndex)
	{
		int result = 0;
		result = (int) value[startIndex];
		result = (int)(result << 8);
		result += (int) value[startIndex + 1];
		result = (int)(result << 8);
		result += (int) value[startIndex + 2];
		result = (int)(result << 8);

		result += (int) value[startIndex + 3];
		return result;
	}

	public bool isSet(byte[] arr, int bit) {
		int index = bit / 8;  // Get the index of the array for the byte with this bit
		int bitPosition = bit % 8;  // Position of this bit in a byte
		
		return (arr[index] >> bitPosition & 1) == 1;
	}
}
