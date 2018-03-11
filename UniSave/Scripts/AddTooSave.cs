using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Reflection;
using System;


public class AddTooSave : MonoBehaviour
{

	/*
	 * Add this script to EVERY (PREFAB) - PREEEEFAAAAABB you want to save.
	 */


	// This is used to keep track of references based on ID's so it's easier to distinguish what object is what during loading and linking References to objects.
	[HideInInspector] // Hide it in the inspector - nobody really needs to see this (it is for Internal Purposes).
	public int objectId;

	// Hide these from the Inspector, they don't matter. They are more for internal purposes, but we need to access them via other classes. (better than using Static).
	public bool animator = false;
	public bool audioSource = false;
	public bool audioListener = false;
	public bool rigidBody = false;
	public bool meshRenderer = false;
	public bool skinnedMeshRenderer = false;

	public Int64 inta;


	void Start ()
	{
		string newName = gameObject.name;
		if (newName.Contains ("(Clone)")) {
			newName = newName.Replace ("(Clone)", "");
		}
		gameObject.name = newName;
		SaveParser list = GameObject.FindObjectOfType<SaveParser> ();
		list.AddSaveGameComponentToList (gameObject);
	}

}


