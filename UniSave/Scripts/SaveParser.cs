using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System.Reflection;
using System;
using UnityEngine.SceneManagement;
using System.Security.Cryptography;

public class SaveParser : MonoBehaviour
{

	/*
	 * # READ IMPORTANT #
	 * Add this script to your scene, then drag every prefab you want (loadable) into Prefabs List.
	 * 
	 * This is done so when we want to LOAD our data back - we can be like hey this is the object want to create - create it.
	 * Then apply whatever changes (if any) to the object - E.G. > New Scripts > New Script Values, etc.
	 */


	/*
	 * This file is ONLY for instantiating items based upon loading save file ONLY.
	 * E.G. - if it notices the Object name is "TreeGreen" it will instantiate Tree Green at the position and rotation of what was saved.
	 */
	

	public string savePath;


	// Where you store all your prefabs you want to LOAD back into the scene based on the save file.
	public List<GameObject> Prefabs = new List<GameObject> ();
	[HideInInspector]
	public List<AddTooSave> savePrefComponents = new List<AddTooSave> ();

	string newName = "";

	// Do we want to send our client save data to a Remote MYSQL Database?
	public bool isRemote;
	// Do we want to do some basic TripleDES Encryption?
	public bool encrypt;


	//Account stuff (For Saving Data Into A Database).
	public string userCreate;
	// The users Username
	public string passCreate;
	// The users Password.
	public string uri = "https://www.your-website-or-IP/";
	// The link to the PHP file to save on a remote database.
	private string endPointFile = "UploadSave.php";
	[HideInInspector]
	public string formText = "";
	// The data we send to the server to save remotely.

	// Internal Stuff. (DON'T TOUCH UNLESS YOU KNOW WHAT YOU ARE DOING).
	private List<string> dataStream = new List<string> ();
	private string finalStream;
	private string lastType = "";
	// We use this to check if the component name is the same as the last. If it was - placing the name down again (to optimize).
	private int idIndex;


	/*
	 * Modify your save path to your liking in here.
	 */
	void Awake ()
	{
		savePath = Application.dataPath + "/Save.Save";
	}


	/*
	 * THIS IS FOR TESTING.
	 * Remove the SpaceBar stuff, in a real game of course this would be automated through a timer
	 * or a button press.
	 */
	void Update ()
	{
		if (Input.GetKeyDown (KeyCode.Space)) {
			SaveGameNew ();
		}
	}


	// This works with the script AddTooSave (It allows us to insert objects into the save list).
	public void AddSaveGameComponentToList (GameObject obj)
	{
		savePrefComponents.Add (obj.GetComponent<AddTooSave> ());
	}

	// When we no longer need the data - we throw it out.
	// IF YOU DESTROY AN OBJECT ONCE IT'S BEEN CREATED (YOU HAVE HAVE HAVE HAVEEE TO CALL THIS MANUALLY).
	// SO IF YOU WANT TO REMOVE THE OBJECT THEN CALL THIS METHOD AND MAKE SURE YOU REFERENCE THE OBJECT YOU ARE DESTROYING. (BEFORE DESTROYING IT).
	public void RemoveSaveGameComponentFromList (GameObject obj)
	{
		savePrefComponents.Remove (obj.GetComponent<AddTooSave> ());
		savePrefComponents.Sort ();
	}


	// New Save will try and save component values, etc.
	// (c) = Start Component (for loading when we load).
	// (c/) = Stop component loading so it can cycle back to transform stuff.
	// The first bool inside of every component means - are we enabled or not.
	// (a) = active state area.
	// (a/) = end of active checking.

	/*
	 * Currently only saves (GameObjects, Transforms, Scripts)
	 * Can save (Ints, floats, bools, Vector3s, Vector2's, Color RGBA, strings, doubles, and GameObject/Transform References)
	 * 
	 * No matter what however, it will automatically save Transform Position and Transform Rotation.
	 * Which is mainly useful for things such as enemies and such, which was my full intent and purpose for this.
	 * So lets say we have 20 enemies in various updated locations after spawning them, if we save and quit,
	 * they will be exactly where they were before and keep the curent stats they were currently at, such as
	 * a bool called isAttack. if it's true or false - the state is saved, so when you load - it loads the current state it was last doing.
	 * 
	 * This will also store References to objects which was a big thing I wanted to incorporate into this system
	 * 
	 * This isn't tested with the Animator or anything, I didn't design it with that intention.
	 * If you want to control Animator stuff with things like Animator.SetBool("Key", false); kinda things, then do it with a saved bool value
	 * and it should work.
	 * 
	 * 
	 * 
	 * # Animator Example
	 * ////////////////////////////////////////////
	 * // > bool boolToLoad;					 //
	 * // > Animator.SetBool("Key", boolToLoad); //
	 * ////////////////////////////////////////////
	 */

	public void SaveGameNew ()
	{
		// We run through this loop first to SET Id's for every object! (This is only useful for Object References). ( HOWEVER - IS VERY IMPORTANT ).
		foreach (AddTooSave sg in savePrefComponents) {
			idIndex++;
			sg.objectId = idIndex;
		}

		foreach (AddTooSave sg in savePrefComponents) {
			// If the instantiated objects (Spawned enemies) contain (Clone), just remove it from the name, otherwise use the name as it is.
			if (sg.name.Contains ("(Clone)")) {
				newName = sg.name.Replace ("(Clone)", "");
			} else {
				newName = sg.gameObject.name;
			}

			// Add the name to the object to the saved data.
			dataStream.Add ("n:" + newName);
			// Save the position (X, Y and Z).
			dataStream.Add (sg.gameObject.transform.position.x.ToString ());
			dataStream.Add (sg.gameObject.transform.position.y.ToString ());
			dataStream.Add (sg.gameObject.transform.position.z.ToString ());
			// Save the Rotation (X, Y and Z).
			dataStream.Add (sg.gameObject.transform.rotation.eulerAngles.x.ToString ());
			dataStream.Add (sg.gameObject.transform.rotation.eulerAngles.y.ToString ());
			dataStream.Add (sg.gameObject.transform.rotation.eulerAngles.z.ToString ());
			// Is the object enabled or disabled? Save what it is.
			bool isActive = gameObject.activeSelf;
			dataStream.Add ("a:" + isActive.ToString ());

			dataStream.Add ("(c)"); // Begin Component Stream.
			// Now we will get a list of every component and save their values ( | ) = a temporary seperator for later when re-loading data.
			Component[] cs = (Component[])sg.gameObject.GetComponents (typeof(Component));
			foreach (Component c in cs) {

				// For every single component, get the fields using Reflection (E.G. public float health, public bool isAlive).
				foreach (FieldInfo info in c.GetType().GetFields()) {
					System.Object obj = (System.Object)c;
					string ignoreStat = info.Name;
					if (!ignoreStat.Contains (sg.name) || !ignoreStat.Contains ("transform")) {
						if (lastType != c.GetType ().ToString ()) {
							dataStream.Add ("c:" + c.GetType ().ToString ());
							if (c is Behaviour) {
								dataStream.Add ((c as Behaviour).enabled.ToString ());
							}
						}


						// Now we will get the type of variable we are working with and save it.
						string dataType = info.FieldType.ToString ();
						string varName = info.Name;

						if (dataType.Contains ("System.Int16")) {
							// Int16. 
							dataType = "4:";
							dataStream.Add (dataType + " " + info.Name + " | " + info.GetValue (obj));
							lastType = c.GetType ().ToString ();
						}

						if (dataType.Contains ("System.Int32")) {
							// Int32.
							dataType = "5:";
							dataStream.Add (dataType + " " + info.Name + " | " + info.GetValue (obj));
							lastType = c.GetType ().ToString ();
						}
						if (dataType.Contains ("System.Int64")) {
							// Int64. 
							dataType = "6:";
							dataStream.Add (dataType + " " + info.Name + " | " + info.GetValue (obj));
							lastType = c.GetType ().ToString ();
						}
						if (dataType.Contains ("System.Single")) {
							// Float.
							dataType = "f:";
							dataStream.Add (dataType + " " + info.Name + " | " + info.GetValue (obj));
							lastType = c.GetType ().ToString ();
						}
						if (dataType.Contains ("System.Double")) {
							// Double.
							dataType = "d:";
							dataStream.Add (dataType + " " + info.Name + " | " + info.GetValue (obj));
							lastType = c.GetType ().ToString ();
						}
						if (dataType.Contains ("System.Boolean")) {
							// Boolean.
							dataType = "b:";
							dataStream.Add (dataType + " " + info.Name + " | " + info.GetValue (obj));
							lastType = c.GetType ().ToString ();
						}
						if (dataType.Contains ("System.String")) {
							// String.
							dataType = "s:";
							dataStream.Add (dataType + " " + info.Name + " | " + info.GetValue (obj));
							lastType = c.GetType ().ToString ();
						}

						if (dataType.Contains ("System.UInt16")) {
							// UInt16.
							dataType = "7:";
							dataStream.Add (dataType + " " + info.Name + " | " + info.GetValue (obj));
							lastType = c.GetType ().ToString ();
						}

						if (dataType.Contains ("System.UInt32")) {
							// UInt32.
							dataType = "8:";
							dataStream.Add (dataType + " " + info.Name + " | " + info.GetValue (obj));
							lastType = c.GetType ().ToString ();
						}

						if (dataType.Contains ("System.UInt64")) {
							// UInt64.
							dataType = "9:";
							dataStream.Add (dataType + " " + info.Name + " | " + info.GetValue (obj));
							lastType = c.GetType ().ToString ();
						}
						if (dataType.Contains ("System.Byte")) {
							// Byte.
							dataType = "y:";
							dataStream.Add (dataType + " " + info.Name + " | " + info.GetValue (obj));
							lastType = c.GetType ().ToString ();
						}
						if (dataType.Contains ("System.SByte")) {
							// Byte.
							dataType = "f:";
							dataStream.Add (dataType + " " + info.Name + " | " + info.GetValue (obj));
							lastType = c.GetType ().ToString ();
						}
						if (dataType.Contains ("System.Decimal")) {
							// Byte.
							dataType = "l:";
							dataStream.Add (dataType + " " + info.Name + " | " + info.GetValue (obj));
							lastType = c.GetType ().ToString ();
						}



						// Unity Related Stuff Now.
						if (dataType.Contains ("UnityEngine.Quaternion")) {
							// Quaternions.
							dataType = "q:";
							dataStream.Add (dataType + " " + info.Name + " | " + info.GetValue (obj));
							lastType = c.GetType ().ToString ();
						}
						if (dataType.Contains ("UnityEngine.Vector3")) {
							// Vector3.
							dataType = "3:";
							dataStream.Add (dataType + " " + info.Name + " | " + info.GetValue (obj));
							lastType = c.GetType ().ToString ();
						}
						if (dataType.Contains ("UnityEngine.Vector2")) {
							// Vector2.
							dataType = "2:";
							dataStream.Add (dataType + " " + info.Name + " | " + info.GetValue (obj));
							lastType = c.GetType ().ToString ();
						}
						
						if (dataType.Contains ("UnityEngine.Color")) {
							// RGBA (COLOR).
							dataType = "z:";
							dataStream.Add (dataType + " " + info.Name + " | " + info.GetValue (obj));
							lastType = c.GetType ().ToString ();
						}

                        if (dataType.Contains ("UnityEngine.Animator")) {
                            // Animator.
                            dataType = "A:";

                            // Now lets remove "UnityEngine.GameObject" from the name.
                            GameObject inf = (GameObject)info.GetValue (obj);
                            if (inf != null) {
                                int id = inf.GetComponent<AddTooSave> ().objectId;
                                // Now lets add it to the dataStream.
                                dataStream.Add (dataType + " " + info.Name + " | " + id.ToString ());
                                // To prevent double placing.
                                lastType = c.GetType ().ToString ();
                            }
                        }


						if (dataType.Contains ("UnityEngine.GameObject")) {
							// GameObject.
							dataType = "g:";

							// Now lets remove "UnityEngine.GameObject" from the name.
							GameObject inf = (GameObject)info.GetValue (obj);
							if (inf != null) {
								int id = inf.GetComponent<AddTooSave> ().objectId;
								// Now lets add it to the dataStream.
								dataStream.Add (dataType + " " + info.Name + " | " + id.ToString ());
								// To prevent double placing.
								lastType = c.GetType ().ToString ();
							}
						}
						if (dataType.Contains ("UnityEngine.Transform")) {
							// GameObject.
							dataType = "t:";

							// Now lets remove "UnityEngine.GameObject" from the name.
							Transform inf = (Transform)info.GetValue (obj);
							if (inf != null) {
								int id = inf.GetComponent<AddTooSave> ().objectId;
								// Now lets add it to the dataStream.
								dataStream.Add (dataType + " " + info.Name + " | " + id.ToString ());
								// To prevent double placing.
								lastType = c.GetType ().ToString ();

							}
						}
					}
				}
			}
			dataStream.Add ("(c/)"); // End of component stream.
			lastType = null;
			cs = null; // Clear out components from memory.
		}

		// Now lets get all the stuff we want to save and put it into an array.
		finalStream = string.Join ("\n", dataStream.ToArray ());
		
		// You can disable this later, this just lets you view the info in the console for viewing.
		Debug.Log (finalStream);

		// # REMOTE ONLINE SAVING. (If you want to save to an Online database).
		if (isRemote) {
			StartCoroutine (UploadSaveToRemoteLocation ());
		} else {
			// # LOCAL SAVING. (If you want to save to local storage).
			SaveLocally ();
		}
	}

	// Save where you like, for testing it will save in project folder.
	public void SaveLocally ()
	{

		if (encrypt) {
			byte[] key = new byte[8]{ 1, 2, 3, 4, 5, 6, 7, 8 };
			byte[] iv = new byte[8]{ 1, 2, 3, 4, 5, 6, 7, 8 };

			SymmetricAlgorithm alg = DES.Create ();
			ICryptoTransform trans = alg.CreateEncryptor (key, iv);
			byte[] input = Encoding.Unicode.GetBytes (finalStream);
			byte[] output = trans.TransformFinalBlock (input, 0, input.Length);
			string outt = Convert.ToBase64String (output);
			File.WriteAllText (savePath, outt);
		} else {
			File.WriteAllText (savePath, finalStream);
		}
		Debug.Log ("SAVE COMPLETED");
	}

		
	/*
	 * If you want to update the saves to a remote database.
	 * Then this is what get's called via a Co-Routine.
	 */
	IEnumerator UploadSaveToRemoteLocation ()
	{
		// Do we want to encrypt it? If so, then convert finalstream into the encryption.
		if (encrypt) {
			byte[] key = new byte[8]{ 1, 2, 3, 4, 5, 6, 7, 8 };
			byte[] iv = new byte[8]{ 1, 2, 3, 4, 5, 6, 7, 8 };

			SymmetricAlgorithm alg = DES.Create ();
			ICryptoTransform trans = alg.CreateEncryptor (key, iv);
			byte[] input = Encoding.Unicode.GetBytes (finalStream);
			byte[] output = trans.TransformFinalBlock (input, 0, input.Length);
			finalStream = Convert.ToBase64String (output);
		}

		// You can comment these out if you want, this is if you have more than one scene and want to load your data based on the current scene.
		Scene scene = SceneManager.GetActiveScene ();
		string sName = scene.name;

		uri = uri + endPointFile;
		WWWForm form = new WWWForm ();
		form.AddField ("usernamePost", userCreate);
		form.AddField ("passwordPost", passCreate);
		form.AddField ("savePost", finalStream);
		// scenePost not required if it's always the same scene in the game. (This is mainly only useful if you want to have a save for each level to load from on database).
		form.AddField ("scenePost", sName);
		WWW www = new WWW (uri, form);

		yield return www;
		Debug.Log (www.text);

		dataStream.Clear ();
		Debug.Log ("SAVE COMPLETED");
	}
}
