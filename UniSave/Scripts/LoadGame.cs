using System.Collections.Generic;
using System.Collections;
using UnityEngine;

// So we can save our save file.
using System.IO;

using System.Globalization;
using System.Text;
using System;

// So we can get the variable names, set values, etc.
using System.Reflection;

using System;

// For REGEX
using System.Text.RegularExpressions;

// For The Current Scene.
using UnityEngine.SceneManagement;

// For basic Encryption.
using System.Security.Cryptography;


public class LoadGame : MonoBehaviour
{

	[HideInInspector]
	public GameObject obj;

	string line = "";
	int lineNumber = 0;
	Vector3 pos;
	Vector3 rot;

	private bool countLines = true;
	private SaveParser list;

	// remote stuff.
	public string uri = "IP or site put a '/' at the end.";
	// The link to the PHP file to save on a remote database.
	private string endPointFile = "LoadGame.php";

	[HideInInspector]
	public string formText = "";
	private string dataStream;
	private bool initialComponent;
	// Is this the first run inside the component code?


	// You should have no reason whatsoever to change any of this.
	void Start ()
	{
		list = GameObject.FindObjectOfType<SaveParser> ();
		if (!list) {
			Debug.LogError ("SaveParser.cs was not found, please drag SaveParser.cs onto an empty gameObject.");
			//Application.Quit (); Maybe quit if it's not found?
		}
	}



	/*
	 * THIS IS FOR TESTING.
	 * Remove the L Button stuff, in a real game of course this would be automated through a timer
	 * or a button press.
	 */
	void Update ()
	{
		if (Input.GetKeyUp (KeyCode.L)) {
			if (!list.encrypt) {
				Load ();	// If it's not Encrypted - just go ahead and load it!
			} else {
				// UnEncrypt The data.
				byte[] key = new byte[8]{ 1, 2, 3, 4, 5, 6, 7, 8 };	// Pick and choose any Key and IV you want (this was for testing).
				byte[] iv = new byte[8]{ 1, 2, 3, 4, 5, 6, 7, 8 };
				string fle = File.ReadAllText (list.savePath);

				SymmetricAlgorithm alg = DES.Create ();
				ICryptoTransform trans = alg.CreateDecryptor (key, iv);
				byte[] input = Convert.FromBase64String (fle);
				byte[] output = trans.TransformFinalBlock (input, 0, input.Length);
				dataStream = Encoding.Unicode.GetString (output);
				// Now Load it!
				Load ();
			}
		}
	}


	public void Load ()
	{
		if (list.isRemote) {
			StartCoroutine (RemoteUplink ());	// if it's remote - then upload to MySQL Database.
		} else {
			NewLoadDate ();						// If it isn't Remote - just save locally.
		}
	}

	/*
	 * This is how we load our data
	 * from the remote database.
	 */
	IEnumerator RemoteUplink ()
	{
		uri = uri + endPointFile;
		WWWForm form = new WWWForm ();
		form.AddField ("usernamePost", PlayerPrefs.GetString ("un"));
		form.AddField ("passwordPost", PlayerPrefs.GetString ("pw"));

		WWW www = new WWW (uri, form);

		yield return www;
		Debug.Log (www.text);
		dataStream = www.text;
		NewLoadDate ();
	}


	/*
	 * I seperated these variables
	 * from the top, this way it's
	 * more contained to specific areas.
	 * 
	 * Figured it would be easier to read
	 * this way as everything that uses it
	 * is below the variables.
	 * 
	 * Please don't change anything unless
	 * you feel comfortable doing so.
	 * 
	 * This may be open-source, but you have
	 * to really know what things are
	 * or you will mess up the code.
	 * 
	 * I would say in order to change any of
	 * this, you should be at least an
	 * Intermediate to Advanced C# Coder.
	 */

	//Regex (Initialize it HERE, to prevent overhead of doing it in the loops.) (Removes Whitespaces, etc).
	Regex reg = new Regex (@"\s+", RegexOptions.Compiled);

	private bool isTransform;
	// If (c) and (a) are both (c/) and (a/).
	private bool isComponent;
	// Are we within (c) and (c/) in the stream?
	private bool isActiveCheck;
	// Are we within (a) and (a/) in the stream?
	private Type cType;
	// If there's a component, then we use this as the reference.
	private Component c;
	private Component[] compList;

	private int componentValues;
	// Get the amount of Variables each script has.
	private string tempString;
	// A temporary string (don't touch this).
	private string variableValue;
	// The value for the variable.
	private string scriptType;
	// E.G. - Health, EnemyAI, Ammo, etc... (Pretty much the name of the script).
	private bool isTrue;
	// If any boolean values are true.

	private List<string> streamedData = new List<string> ();
	private string tempStr;
	private GameObject gObj;
	private Transform transObj;

	#region Hidden But Important Public Variables.

	// Referenced { GAME OBJECT } COMPONENTS. ( NOT NOT NOT TRANSFORM! )
	[HideInInspector]
	public List<Component> referencedC = new List<Component> ();

	// Referenced Game objects
	[HideInInspector]
	public List<GameObject> referencedG = new List<GameObject> ();

	// References to every object that needs to be loaded.
	[HideInInspector]
	public AddTooSave[] saves;

	// Reference ID's.
	[HideInInspector]
	public List<string> refId = new List<string> ();

	// The variables.
	[HideInInspector]
	public List<string> fields = new List<string> ();

	// List of GameObjects for linking;
	[HideInInspector]
	public List<GameObject> refedObs = new List<GameObject> ();

	// List of Transforms for linking.
	[HideInInspector]
	public List<Transform> refedTra = new List<Transform> ();

	// Referenced Transforms
	[HideInInspector]
	public List<Transform> referencedT = new List<Transform> ();

	// Transform Variables.
	[HideInInspector]
	public List<string> transFields = new List<string> ();

	// Transform REFERENCE ID's.
	[HideInInspector]
	public List<string> tRefId = new List<string> ();

	// Referenced { TRANSFORM } COMPONENTS. ( NOT NOT NOT GAME OBJECT! ).
	[HideInInspector]
	public List<Component> referenceTC = new List<Component> ();

	#endregion

	// This step is sort of redundant, but it reads the file again, only this time it just cares about building references to objects (GO and Transform).
	public void ReferenceBuilder ()
	{
		
		saves = FindObjectsOfType<AddTooSave> ();
		Array.Reverse (saves);	// < This is REQUIRED or you will get undesired results.

		// If no gameObjects or Transforms are found, then it will just skip any reference building.
		bool isGo = false;		// Create these two bools and make them false.
		bool isTr = false;

		using (StringReader reader = new StringReader (dataStream)) {
			while ((line = reader.ReadLine ()) != null) {
				tempString = line;
				// CHECK FOR GAMEOBJECTS.
				if (tempString.Contains ("g:")) {
					isGo = true;
					//We want to REMOVE the first THREE characters (they are only to identify what they are).
					tempString = tempString.Remove (0, 3);

					// Isolate the VALUE then REMOVE the seperator. (ADD 3) that way we skip any white spaces or anything.
					int indexPos = tempString.LastIndexOf (" | ") + 3;
					variableValue = tempString.Substring (indexPos, tempString.Length - indexPos);

					// Remove ALL White Spaces
					tempString = tempString.Substring (0, tempString.LastIndexOf (" "));
					tempString = reg.Replace (tempString, "");

					// Remove the Seperator
					if (tempString.Contains ("|")) {
						tempString = tempString.Replace ("|", "");
					}
					int id = Convert.ToInt32 (variableValue);
					GameObject tempOb = saves [id - 1].gameObject;
					refedObs.Add (tempOb);
				}

				// CHECK FOR TRANSFORMS.
				if (tempString.Contains ("t:")) {
					isTr = true;			// isTr (Is Transform), meaning there ARE transforms.
					//We want to REMOVE the first THREE characters (they are only to identify what they are).
					tempString = tempString.Remove (0, 3);

					// Isolate the VALUE then REMOVE the seperator. (ADD 3) that way we skip any white spaces or anything.
					int indexPos = tempString.LastIndexOf (" | ") + 3;
					variableValue = tempString.Substring (indexPos, tempString.Length - indexPos);

					// Remove ALL White Spaces
					tempString = tempString.Substring (0, tempString.LastIndexOf (" "));
					tempString = reg.Replace (tempString, "");

					// Remove the Seperator
					if (tempString.Contains ("|")) {
						tempString = tempString.Replace ("|", "");
					}
					int id = Convert.ToInt32 (variableValue);
					Transform tempOb = saves [id - 1].transform;
					refedTra.Add (tempOb);	// Add the transform to refedTra (Referenced Transforms).
				}
			}
		}

		// Did we find a GameObject?
		if (isGo) {
			ReferenceGO ();
		}

		// Did we find a Transform?
		if (isTr) {
			ReferenceTR ();
		}
			
		// When we are done loading, call this method.
		LoadingFinished ();
	}

	// When loading is finished, call this so you know.
	public void LoadingFinished ()
	{
		Debug.Log ("Loading DONE!");
		// Do something below if you want.
	}


	// Reference Transform Links.
	public void ReferenceTR ()
	{
		for (int i = 0; i < referencedT.Count; i++) {
			Transform tempOb = refedTra [i].transform;
			Component c = referenceTC [i];
			tempOb.GetComponent (c.ToString ());

			FieldInfo[] info = c.GetType ().GetFields ();
			foreach (FieldInfo inf in info) {
				FieldInfo name = c.GetType ().GetField (transFields [i]);
				Transform intVal = refedTra [i].transform;
				if (name != null) {
					name.SetValue (
						c,
						intVal);
				}
			}
		}

	}

	// Reference GameObject Links.
	public void ReferenceGO ()
	{
		for (int i = 0; i < referencedG.Count; i++) {
			GameObject tempOb = refedObs [i].gameObject;
			Component c = referencedC [i];
			tempOb.GetComponent (c.ToString ());

			FieldInfo[] info = c.GetType ().GetFields ();
			foreach (FieldInfo inf in info) {
				FieldInfo name = c.GetType ().GetField (fields [i]);
				GameObject intVal = refedObs [i];
				if (name != null) {
					name.SetValue (
						c,
						intVal);
				}
			}
		}
	}
		

	/* 
	 * Load the actual object and set up basic stuff such as Instantiate, position, health, etc.
	 * This method is the bread and butter of Variable connections and such.
	 * 
	 * Please stay cautioned when changing any of these values, only do so if you know what you
	 * are doing. Changing something may result in undesired or not working logic.
	 */
	public void NewLoadDate ()
	{

		using (StringReader reader = new StringReader (dataStream)) {
			while ((line = reader.ReadLine ()) != null) {


				if (!isComponent && !isActiveCheck) {
					#region Check if (C) or (C/) or (a) or (a/) is present
					// First lets check if it's a specific kind of line first.
					if (line.Contains ("(c)")) {
						isComponent = true;
						lineNumber = 0;
						pos = Vector3.zero;
						rot = Vector3.zero;
					}
					#endregion
					if (line == null) {
						lineNumber = 0;
						pos = Vector3.zero;
						rot = Vector3.zero;
					}
					if (lineNumber == 0 && !isComponent && !isActiveCheck) {
						string obName = line;

						if (obName.Contains ("n:")) {
							obName = obName.Replace ("n:", "");
							for (int i = 0; i < list.Prefabs.Count; i++) {
								if (list.Prefabs [i].name == obName) {
									// Before CREATING the object, let's see if it's ALREADY a SCENE OBJECT (IF SO, DO NOT CREATE IT), Instead - reference it!
									obj = list.Prefabs [i].gameObject;
									obj = Instantiate (obj, transform.position, Quaternion.identity);
									obj.name = obName;
								}
							}
						}
						countLines = true;
					}

					#region DO NOT TOUCH THESE UNLESS YOU KNOW WHAT YOU ARE DOING. (Loads the Initial Position and Rotation based on LINE COUNT).
					if (lineNumber == 1) {
						pos.x = (float)double.Parse (line, NumberStyles.Any, CultureInfo.InvariantCulture);
					} 
					if (lineNumber == 2) {
						pos.y = (float)double.Parse (line, NumberStyles.Any, CultureInfo.InvariantCulture);
					} 
					if (lineNumber == 3) {
						pos.z = (float)double.Parse (line, NumberStyles.Any, CultureInfo.InvariantCulture);
						obj.transform.position = pos;
					} 
					if (lineNumber == 4) {
						rot.z = (float)double.Parse (line, NumberStyles.Any, CultureInfo.InvariantCulture);
					}
					if (lineNumber == 5) {
						rot.y = (float)double.Parse (line, NumberStyles.Any, CultureInfo.InvariantCulture);
					} 
					if (lineNumber == 6) {
						rot.z = (float)double.Parse (line, NumberStyles.Any, CultureInfo.InvariantCulture);
						obj.transform.rotation = Quaternion.Euler (rot);

					}
					if (countLines) {
						lineNumber++;
					}
					#endregion
				}

				if (line.Contains ("(c/)")) {
					isComponent = false;
					cType = null;
					c = null;
					tempStr = string.Empty;
					isTrue = false;
					scriptType = string.Empty;
					tempString = string.Empty;
					tempStr = string.Empty;
				}


				#region Active Check (Whether the actual object is SetActive (false or true).
				if (line == "a:True") {
					obj.SetActive (true);
				}  
				if (line == "a:False") {
					obj.SetActive (false);
				}

				#endregion


				// Now IF it IS a component, lets do some modifications from the load file!
				if (isComponent) {
					// Go ahead and zero everything out and such.
					lineNumber = 0;
					countLines = false;
					pos = Vector3.zero;
					rot = Vector3.zero;

					// If the line contains "c:" then it's a component item to load. (if the prefab doesn't have the component - then ADD IT).
					if (line.Contains ("c:")) {
						tempStr = line.Replace ("c:", "");
						if (obj.GetComponent (Type.GetType (tempStr))) {
							c = obj.GetComponent (Type.GetType (tempStr));
						} else {
							c = obj.AddComponent (Type.GetType (tempStr));
						}
					}


					// Is the component enabled or disabled?
					if (line == "True") {
						if (c is Behaviour) {
							(c as Behaviour).enabled = true;
						}
					} else if (line == "False") {
						if (c is Behaviour) {
							(c as Behaviour).enabled = false;
						}
					}


					/* 
					 * tempString - Is initially the entire line that it reads, it contains the entire block of information.
					 * So from here, we begin to manipulate the string to do exactly what we want - stripping away of un-needed stuff,
					 * seperating values from the variable names, and then using FieldInfo to get the actual variable and then setting
					 * the value to it.
					 */

					tempString = line;


					// Now we check each line for the following if conditions, then we do the loading.

					// { QUATERNIONS }
					if (tempString.Contains ("q:")) {
						//We want to REMOVE the first THREE characters (they are only to identify what they are).
						tempString = tempString.Remove (0, 3);

						// Isolate the VALUE then REMOVE the seperator. (ADD 3) that way we skip any white spaces or anything.
						int indexPos = tempString.LastIndexOf (" | ") + 3;
						variableValue = tempString.Substring (indexPos, tempString.Length - indexPos);
						tempString = tempString.Substring (0, indexPos);

						// Remove ALL White Spaces Again using Regex (Just a backup just to make sure).
						tempString = tempString.Substring (0, tempString.LastIndexOf (" "));
						tempString = reg.Replace (tempString, "");

						// Remove the Seperator
						if (tempString.Contains ("|")) {
							tempString = tempString.Replace ("|", "");
						}

						// Remove the Vector Brackets. (From both Variable Value and tempString).
						if (variableValue.Contains ("(")) {
							variableValue = variableValue.Replace ("(", "");
							tempString = tempString.Replace ("(", "");
						}
						if (variableValue.Contains (")")) {
							variableValue = variableValue.Replace (")", "");
							tempString = tempString.Replace ("(", "");
						}

						// Now remove the COMMAS ONLY FOR THE TEMPSTRING (E.G. - the variable name string).
						if (tempString.Contains (",")) {
							tempString = tempString.Replace (",", "");
						}

						// Now we split the values of this final string and convert each one to a float.
						string[] vecs = variableValue.Split ("," [0]);
						float x = (float)double.Parse (vecs [0], NumberStyles.Any);
						float y = (float)double.Parse (vecs [1], NumberStyles.Any);
						float z = (float)double.Parse (vecs [2], NumberStyles.Any);
						float w = (float)double.Parse (vecs [3], NumberStyles.Any);
						Quaternion qt = new Quaternion (x, y, z, w);

						// We get the variable name with TempString
						// then we set the value using variableValue.
						FieldInfo[] info = c.GetType ().GetFields ();
						foreach (FieldInfo inf in info) {
							FieldInfo name = c.GetType ().GetField (tempString);
							if (name != null) {
								name.SetValue (
									c,
									qt);
							}
						}
					}

					// { VECTOR 3's }
					if (tempString.Contains ("3:")) {
						//We want to REMOVE the first THREE characters (they are only to identify what they are).
						tempString = tempString.Remove (0, 3);

						// Isolate the VALUE then REMOVE the seperator. (ADD 3) that way we skip any white spaces or anything.
						int indexPos = tempString.LastIndexOf (" | ") + 3;
						variableValue = tempString.Substring (indexPos, tempString.Length - indexPos);
						tempString = tempString.Substring (0, indexPos);

						// Remove ALL White Spaces Again using Regex (Just a backup just to make sure).
						tempString = tempString.Substring (0, tempString.LastIndexOf (" "));
						tempString = reg.Replace (tempString, "");

						// Remove the Seperator
						if (tempString.Contains ("|")) {
							tempString = tempString.Replace ("|", "");
						}

						// Remove the Vector Brackets. (From both Variable Value and tempString).
						if (variableValue.Contains ("(")) {
							variableValue = variableValue.Replace ("(", "");
							tempString = tempString.Replace ("(", "");
						}
						if (variableValue.Contains (")")) {
							variableValue = variableValue.Replace (")", "");
							tempString = tempString.Replace ("(", "");
						}

						// Now remove the COMMAS ONLY FOR THE TEMPSTRING (E.G. - the variable name string).
						if (tempString.Contains (",")) {
							tempString = tempString.Replace (",", "");
						}

						// Now we split the values of this final string and convert each one to a float.
						string[] vecs = variableValue.Split ("," [0]);
						float x = (float)double.Parse (vecs [0], NumberStyles.Any);
						float y = (float)double.Parse (vecs [1], NumberStyles.Any);
						float z = (float)double.Parse (vecs [2], NumberStyles.Any);

						Vector3 newVector = new Vector3 (x, y, z);

						// We get the variable name with TempString
						// then we set the value using variableValue.
						FieldInfo[] info = c.GetType ().GetFields ();
						foreach (FieldInfo inf in info) {
							FieldInfo name = c.GetType ().GetField (tempString);
							if (name != null) {
								name.SetValue (
									c,
									newVector);
							}
						}
					}


					// { VECTOR 2's }
					if (tempString.Contains ("2:")) {
						//We want to REMOVE the first THREE characters (they are only to identify what they are).
						tempString = tempString.Remove (0, 3);

						// Isolate the VALUE then REMOVE the seperator. (ADD 3) that way we skip any white spaces or anything.
						int indexPos = tempString.LastIndexOf (" | ") + 3;
						variableValue = tempString.Substring (indexPos, tempString.Length - indexPos);
						tempString = tempString.Substring (0, indexPos);

						// Remove ALL White Spaces Again using Regex (Just a backup just to make sure).
						tempString = tempString.Substring (0, tempString.LastIndexOf (" "));
						tempString = reg.Replace (tempString, "");

						// Remove the Seperator
						if (tempString.Contains ("|")) {
							tempString = tempString.Replace ("|", "");
						}

						// Remove the Vector Brackets. (From both Variable Value and tempString).
						if (variableValue.Contains ("(")) {
							variableValue = variableValue.Replace ("(", "");
							tempString = tempString.Replace ("(", "");
						}
						if (variableValue.Contains (")")) {
							variableValue = variableValue.Replace (")", "");
							tempString = tempString.Replace ("(", "");
						}

						// Now remove the COMMAS ONLY FOR THE TEMPSTRING (E.G. - the variable name string).
						if (tempString.Contains (",")) {
							tempString = tempString.Replace (",", "");
						}

						// Now we split the values of this final string and convert each one to a float.
						string[] vecs = variableValue.Split ("," [0]);
						float x = (float)double.Parse (vecs [0], NumberStyles.Any);
						float y = (float)double.Parse (vecs [1], NumberStyles.Any);

						Vector2 newVector = new Vector2 (x, y);

						// We get the variable name with TempString
						// then we set the value using variableValue.
						FieldInfo[] info = c.GetType ().GetFields ();
						foreach (FieldInfo inf in info) {
							FieldInfo name = c.GetType ().GetField (tempString);
							if (name != null) {
								name.SetValue (
									c,
									newVector);
							}
						}
					}


					// { COLOR RGBA }
					if (tempString.Contains ("z:")) {
						//We want to REMOVE the first THREE characters (they are only to identify what they are).
						tempString = tempString.Remove (0, 3);

						// Saves with RGBA for some reason, well - remove that!
						if (tempString.Contains ("RGBA")) {
							tempString = tempString.Replace ("RGBA", "");
						}

						// Isolate the VALUE then REMOVE the seperator. (ADD 3) that way we skip any white spaces or anything.
						int indexPos = tempString.LastIndexOf (" | ") + 3;
						variableValue = tempString.Substring (indexPos, tempString.Length - indexPos);
						tempString = tempString.Substring (0, indexPos);

						// Remove ALL White Spaces Again using Regex (Just a backup just to make sure).
						tempString = tempString.Substring (0, tempString.LastIndexOf (" "));
						tempString = reg.Replace (tempString, "");

						// Remove the Seperator
						if (tempString.Contains ("|")) {
							tempString = tempString.Replace ("|", "");
						}

						// Remove the Vector Brackets. (From both Variable Value and tempString).
						if (variableValue.Contains ("(")) {
							variableValue = variableValue.Replace ("(", "");
							tempString = tempString.Replace ("(", "");
						}
						if (variableValue.Contains (")")) {
							variableValue = variableValue.Replace (")", "");
							tempString = tempString.Replace ("(", "");
						}

						// Now remove the COMMAS ONLY FOR THE TEMPSTRING (E.G. - the variable name string).
						if (tempString.Contains (",")) {
							tempString = tempString.Replace (",", "");
						}

						// Now we split the values of this final string and convert each one to a float.
						string[] vecs = variableValue.Split ("," [0]);
						float r = (float)double.Parse (vecs [0], NumberStyles.Any);
						float g = (float)double.Parse (vecs [1], NumberStyles.Any);
						float b = (float)double.Parse (vecs [2], NumberStyles.Any);
						float a = (float)double.Parse (vecs [3], NumberStyles.Any);
						Color col = new Color (r, g, b, a);

						// We get the variable name with TempString
						// then we set the value using variableValue.
						FieldInfo[] info = c.GetType ().GetFields ();
						foreach (FieldInfo inf in info) {
							FieldInfo name = c.GetType ().GetField (tempString);
							if (name != null) {
								name.SetValue (
									c,
									col);
							}
						}
					}


					// { INTEGERS }
					if (tempString.Contains ("i:")) {
						//We want to REMOVE the first THREE characters (they are only to identify what they are).
						tempString = tempString.Remove (0, 3);

						// Isolate the VALUE then REMOVE the seperator. (ADD 3) that way we skip any white spaces or anything.
						int indexPos = tempString.LastIndexOf (" | ") + 3;
						variableValue = tempString.Substring (indexPos, tempString.Length - indexPos);

						// Remove ALL White Spaces Again using Regex (Just a backup just to make sure).
						tempString = tempString.Substring (0, tempString.LastIndexOf (" "));
						tempString = reg.Replace (tempString, "");

						// Remove the Seperator
						if (tempString.Contains ("|")) {
							tempString = tempString.Replace ("|", "");
						}


						// We get the variable name with TempString
						// then we set the value using variableValue.
						FieldInfo[] info = c.GetType ().GetFields ();
						foreach (FieldInfo inf in info) {
							FieldInfo name = c.GetType ().GetField (tempString);
							int intVal = Convert.ToInt32 (variableValue);
							if (name != null) {
								name.SetValue (
									c,
									intVal);
							}
						}
					}

					// { FLOATS }
					if (tempString.Contains ("f:")) {
						//We want to REMOVE the first THREE characters (they are only to identify what they are).
						tempString = tempString.Remove (0, 3);

						// Isolate the VALUE then REMOVE the seperator. (ADD 3) that way we skip any white spaces or anything.
						int indexPos = tempString.LastIndexOf (" | ") + 3;
						variableValue = tempString.Substring (indexPos, tempString.Length - indexPos);

						// Remove ALL White Spaces Again using Regex (Just a backup just to make sure).
						tempString = tempString.Substring (0, tempString.LastIndexOf (" "));
						tempString = reg.Replace (tempString, "");

						// Remove the Seperator
						if (tempString.Contains ("|")) {
							tempString = tempString.Replace ("|", "");
						}


						// We get the variable name with TempString
						// then we set the value using variableValue.
						FieldInfo[] info = c.GetType ().GetFields ();
						foreach (FieldInfo inf in info) {
							FieldInfo name = c.GetType ().GetField (tempString);
							float flVal = (float)double.Parse (variableValue, NumberStyles.Any);
							if (name != null) {
								name.SetValue (
									c,
									flVal);
							}
						}
					}


					// { DOUBLES }
					if (tempString.Contains ("d:")) {
						//We want to REMOVE the first THREE characters (they are only to identify what they are).
						tempString = tempString.Remove (0, 3);

						// Isolate the VALUE then REMOVE the seperator. (ADD 3) that way we skip any white spaces or anything.
						int indexPos = tempString.LastIndexOf (" | ") + 3;
						variableValue = tempString.Substring (indexPos, tempString.Length - indexPos);

						// Remove ALL White Spaces Again using Regex (Just a backup just to make sure).
						tempString = tempString.Substring (0, tempString.LastIndexOf (" "));
						tempString = reg.Replace (tempString, "");

						// Remove the Seperator
						if (tempString.Contains ("|")) {
							tempString = tempString.Replace ("|", "");
						}


						// We get the variable name with TempString
						// then we set the value using variableValue.
						FieldInfo[] info = c.GetType ().GetFields ();
						foreach (FieldInfo inf in info) {
							FieldInfo name = c.GetType ().GetField (tempString);
							double dblVal = (double)double.Parse (variableValue, NumberStyles.Any);
							if (name != null) {
								name.SetValue (
									c,
									dblVal);
							}
						}
					}

					// { BOOLEANS }
					if (tempString.Contains ("b:")) {
						//We want to REMOVE the first THREE characters (they are only to identify what they are).
						tempString = tempString.Remove (0, 3);

						// Isolate the VALUE then REMOVE the seperator. (ADD 3) that way we skip any white spaces or anything.
						int indexPos = tempString.LastIndexOf (" | ") + 3;
						variableValue = tempString.Substring (indexPos, tempString.Length - indexPos);

						// Remove ALL White Spaces Again using Regex (Just a backup just to make sure).
						tempString = tempString.Substring (0, tempString.LastIndexOf (" "));
						tempString = reg.Replace (tempString, "");

						// Remove the Seperator
						if (tempString.Contains ("|")) {
							tempString = tempString.Replace ("|", "");
						}


						// We get the variable name with TempString
						// then we set the value using variableValue.
						FieldInfo[] info = c.GetType ().GetFields ();
						foreach (FieldInfo inf in info) {
							FieldInfo name = c.GetType ().GetField (tempString);
							if (variableValue == "True") {
								isTrue = true;
							}
							if (variableValue == "False") {
								isTrue = false;
							}
							if (name != null) {
								name.SetValue (
									c,
									isTrue);
							}
						}
					}

					// { STRINGS }
					if (tempString.Contains ("s:")) {
						//We want to REMOVE the first THREE characters (they are only to identify what they are).
						tempString = tempString.Remove (0, 3);

						// Isolate the VALUE then REMOVE the seperator. (ADD 3) that way we skip any white spaces or anything.
						int indexPos = tempString.LastIndexOf (" | ") + 3;
						variableValue = tempString.Substring (indexPos, tempString.Length - indexPos);
						tempString = tempString.Substring (0, indexPos);

						// Remove ALL White Spaces Again using Regex (Just a backup just to make sure).
						tempString = tempString.Substring (0, tempString.LastIndexOf (" "));
						tempString = reg.Replace (tempString, "");

						// Remove the Seperator
						if (tempString.Contains ("|")) {
							tempString = tempString.Replace ("|", "");
						}

						// We get the variable name with TempString
						// then we set the value using variableValue.
						FieldInfo[] info = c.GetType ().GetFields ();
						foreach (FieldInfo inf in info) {
							FieldInfo name = c.GetType ().GetField (tempString);
							if (name != null) {
								name.SetValue (
									c,
									variableValue);
							}
						}
					}
						
					// { GAME OBJECTS }

					if (tempString.Contains ("g:")) {
						//We want to REMOVE the first THREE characters (they are only to identify what they are).
						tempString = tempString.Remove (0, 3);

						// Isolate the VALUE then REMOVE the seperator. (ADD 3) that way we skip any white spaces or anything.
						int indexPos = tempString.LastIndexOf (" | ") + 3;
						variableValue = tempString.Substring (indexPos, tempString.Length - indexPos);

						// Remove ALL White Spaces Again using Regex (Just a backup just to make sure).
						tempString = tempString.Substring (0, tempString.LastIndexOf (" "));
						tempString = reg.Replace (tempString, "");

						// Remove the Seperator
						if (tempString.Contains ("|")) {
							tempString = tempString.Replace ("|", "");
						}
						referencedG.Add (obj);		// Add the gameObject.
						referencedC.Add (c);		// Add the component.
						fields.Add (tempString);	// Add the Variable.
						refId.Add (variableValue);	// Add the GameObject refernce id.
					}

					// { TRANSFORMS }
					if (tempString.Contains ("t:")) {
						//We want to REMOVE the first THREE characters (they are only to identify what they are).
						tempString = tempString.Remove (0, 3);

						// Isolate the VALUE then REMOVE the seperator. (ADD 3) that way we skip any white spaces or anything.
						int indexPos = tempString.LastIndexOf (" | ") + 3;
						variableValue = tempString.Substring (indexPos, tempString.Length - indexPos);

						// Remove ALL White Spaces Again using Regex (Just a backup just to make sure).
						tempString = tempString.Substring (0, tempString.LastIndexOf (" "));
						tempString = reg.Replace (tempString, "");

						// Remove the Seperator
						if (tempString.Contains ("|")) {
							tempString = tempString.Replace ("|", "");
						}
						referencedT.Add (obj.transform);	// Add the Transform.
						referenceTC.Add (c);				// Add the Component.
						transFields.Add (tempString);		// Add the Variable.
						tRefId.Add (variableValue);			// Add the Transform Reference ID.
					}
				}
			}
		}

		/*
		 * Finally when we get done loading variable values,
		 * we now begin setting up Object reference connections (ONLY GAMEOBJECTS AND TRANSFORMS).
		 */ 
		ReferenceBuilder ();
	}
}
