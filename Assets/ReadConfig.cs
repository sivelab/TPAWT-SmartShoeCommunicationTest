using UnityEngine;
using System.Collections;
using System;
using System.IO;

 

public class ReadConfig : MonoBehaviour

{

    protected FileInfo theSourceFile = null;
    protected StreamReader reader = null;
    protected string text = " "; // assigned to allow first line to be read below

 

    void Start () {
		/*
        theSourceFile = new FileInfo ("config.txt");
        reader = theSourceFile.OpenText();
		while (text != null) {
			guiText.text = text;
			text = reader.ReadLine();
			//Console.WriteLine(text);
			print (text);
			
        }
        */
    }

    

    void Update () {


    }

}