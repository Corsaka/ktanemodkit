﻿using UnityEngine;
using KModkit;
using KeepCoding;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class badbonesScript : ModuleScript {
	//system
	private bool _isSolved = false;
	//module
	private int[] correctSeq; //correct sequence of notes
	private List<int> sequence = new List<int>(); //player's input sequence
	private int seqLength,badBone,goodBone,midBone,highBone,lowNoteCount=0; //length, note values, variations on low note
	private bool _skullHeld = false; //whether the user is currently moving the skull
	private Dictionary<GameObject,int> boneNotes; //dictionary assigning object to note
	private Dictionary<Vector3,GameObject> bonesPos; //dictionary assigning position to object
	private Dictionary<GameObject,int> boneConverter; //dictionary assigning object to value
	private Vector3 posNorth,posEast,posSouth,posWest; //positions of sprites
	private Vector2 mouseStartPos; //position of mouse, to control skull
	private Quaternion skullStartRot; //initial rotation of skull
	//list of primes for use in one specific function
	//yes, this will break if you make a bomb with more than 500 modules on it and the sequence length is 0, but that's such a rare edge case that i don't care.
	private int[] primes = {2,3,5,7,11,13,17,19,23,29,31,37,41,43,47,53,59,61,67,71,73,79,83,89,97,101,103,107,109,113,127,131,137,139,149,151,157,163,167,173,179,181,191,193,197,199,211,223,227,229,233,239,241,251,257,263,269,271,277,281,283,293,307,311,313,317,331,337,347,349,353,359,367,373,379,383,389,397,401,409,419,421,431,433,439,443,449,457,461,463,467,479,487,491,499};
	//eyes
	[SerializeField]
	internal KMSelectable submit,reset; //eye selectables
	public GameObject red,blue; //eyes
	//skull
	[SerializeField]
	internal KMSelectable skull; //skull selectable
	public GameObject skullPivot; //thing that moves
	//sprites
	public GameObject one,two,three,four; //your bones
	//lights
	public Light topBlue,bottomBlue,topRed,bottomRed; //eye lights
	//sounds
	public AudioClip[] audioClips; //order: end, high, low1, low2, low3, mid, bad1, bad2, bad3, bad4

	//bombgen
	private void Start () {
		//fix lighting bug
		float scalar = transform.lossyScale.x;
		topBlue.range *= scalar;
		bottomBlue.range *= scalar;
		topRed.range *= scalar;
		bottomRed.range *= scalar;

		skullStartRot = skullPivot.transform.localRotation;
		reset.Assign(onInteract: resetSeq);
		submit.Assign(onInteract: submitSeq);
		skull.Assign(onInteract: skullHold);
		skull.Assign(onInteractEnded: skullRelease);
		assignBones();
		mixEyes();
		createSeq();
	}

	private void assignBones()
	{
		//actual value of each bone
		boneConverter = new Dictionary<GameObject,int>(){{one,1},{two,2},{three,3},{four,4}};
		//position of each bone - defined below
		bonesPos = new Dictionary<Vector3,GameObject>();
		//note of each bone - defined below
		boneNotes = new Dictionary<GameObject,int>();

		//list of possible positions
		posNorth = new Vector3(0,0,0.06f);
		posEast = new Vector3(0.06f,0,0);
		posSouth = new Vector3(0,0,-0.06f);
		posWest = new Vector3(-0.06f,0,0);
		Vector3[] positions = {posNorth,posEast,posSouth,posWest};

		int[] order = Enumerable.Range(0,4).ToArray().Shuffle().ToArray(); //creates a range of numbers and orders them randomly
		//positions of the sprites
		one.transform.localPosition = positions[order[0]];
		two.transform.localPosition = positions[order[1]];
		three.transform.localPosition = positions[order[2]];
		four.transform.localPosition = positions[order[3]];
		
		GameObject[] boneList = {one,two,three,four};
		foreach(GameObject bone in boneList)
		{
			//store them in a dictionary so they can be accurately referred to later
			//this dictionary links positions with values
			if(bone.transform.localPosition == posNorth)
			{
				bonesPos[posNorth] = bone;
				Log("North bone: {0}",bone);
			}
			if(bone.transform.localPosition == posEast)
			{
				bonesPos[posEast] = bone;
				Log("East bone: {0}",bone);
			}	
			if(bone.transform.localPosition == posSouth)
			{
				bonesPos[posSouth] = bone;
				Log("South bone: {0}",bone);
			}
			if(bone.transform.localPosition == posWest)
			{
				bonesPos[posWest] = bone;
				Log("West bone: {0}",bone);
			}
		}

		//which bones each note is assigned to
		int[] rndRange = Enumerable.Range(1,4).ToArray().Shuffle().ToArray(); //Range is (startPos,numbers)
		badBone = rndRange[0]; //both the same note
		goodBone = rndRange[1]; //both the same note
		midBone = rndRange[2];
		highBone = rndRange[3];
		Log("Bad Bone: {0}; Good Bone: {1};",badBone,goodBone);
		Log("Mid bone: {0}; High bone {1};",midBone,highBone);

		int[] notes = {badBone,goodBone,midBone,highBone}; //to iterate over
		foreach(int note in notes)
		{
			//store them in a dictionary so they can be accurately referred to later
			//this dictionary links values with notes
			switch(note)
			{
				case 1:
					boneNotes[one] = note;
					break;
				case 2:
					boneNotes[two] = note;
					break;
				case 3:
					boneNotes[three] = note;
					break;
				case 4:
					boneNotes[four] = note;
					break;
			}
		}
	}

	private void mixEyes()
	{
		Vector3 posLeft = new Vector3(-0.00038132f,0.00075f,0);
		Vector3 posRight = new Vector3(0.00038132f,0.00075f,0);

		if(badBone > goodBone) //be cheeky. randomly generate the badbone and position red/blue according to that.
		{
			red.transform.localPosition = posLeft;
			blue.transform.localPosition = posRight;
		}
	}

	//determine sequence
	private void createSeq()
	{
		string nums = "";
		var bombInfo = Get<KMBombInfo>();
		foreach(int num in bombInfo.GetSerialNumberNumbers()) //for every digit in serial number
		{
			seqLength += num; //add value of digit to seqLength
			nums += String.Format("{0}+",num);
		}
		Log("Sequence Length: [{0}]={1}",nums.Remove(nums.Length-1,1),seqLength);
		if(seqLength == 0) //if the sum of these digits is 0
		{
			seqLength++;
			foreach(string _ in bombInfo.GetSolvedModuleNames()) //instead iterate over solved modules
			{
				seqLength++; //for every one, add 1 to seqLength
			}
			Log("Sequence Length 0! Backup Sequence Length: 1 + {0} solved modules: {1}",--seqLength,++seqLength);
		}
		Log("Sequence Length: {0}",seqLength);

		correctSeq = seqRules(); //run the big ol rules determinator
		Log("Correct Sequence: {0}",correctSeq);
	}

	private void resetSeq()
	{
		if(_isSolved){return;} //if solved, end function immediately
		ButtonEffect(reset,1.0f,Sound.ButtonPress);
		Log("Sequence reset.");
		sequence = new List<int>(); //otherwise, clear sequence
		PlaySound(Sound.ButtonRelease);
	}

	private void submitSeq()
	{
		if (_isSolved) { return; } //if solved, end function immediately

		ButtonEffect(reset, 1.0f, Sound.ButtonPress);
		Log("Inputted Sequence: {0}", sequence.Join(""));
		Log("Correct Sequence: {0}", correctSeq.Join(""));
		bool match = true;
		if(sequence.Count != seqLength)
		{
			match = false;
		}
		else
		{
			for (int i = 0; i < sequence.Count; i++)
			{
				if (sequence[i] != correctSeq[i])
				{
					match = false;
					break;
				}
			}
		}
		if(sequence[0] == goodBone && sequence[1] == midBone && sequence[2] == highBone && match)
		{
			PlaySound("badBonesSpecial");
			Solve("SOLVE! Correct sequence!");
			_isSolved = true;
		}
		else
		{
			StartCoroutine(PlayFinal(match));
		}
	}

	private void answerCheck(bool match)
	{
		if (match) //if they match
		{
			Solve("SOLVE! Correct sequence!");
			_isSolved = true; //stop any further interactions
		}
		else
		{
			Strike("STRIKE! Incorrect sequence!");
			PlayBad();
			sequence = new List<int>(); //reset sequence after strike
		}
	}

	private void skullHold()
	{
		//no _isSolved check as moving is fun :) (and doesn't affect anything!)
		_skullHeld = true;
		mouseStartPos = Input.mousePosition;
	}

	private void skullRelease()
	{
		_skullHeld = false;
		if(_isSolved){return;} //if solved, end function immediately
		Quaternion skullRot = skullPivot.transform.localRotation; //get rotation
		Vector3 eulerSkullRot = skullRot.eulerAngles; //convert rotation to something that isn't bullshit difficult to understand

		int bone = 0;
		int note = 0;
		if(22.6f >= eulerSkullRot.x && eulerSkullRot.x >= 17.5f) //bound is 22.6f because Clamp doesn't perfectly clamp to 22.5f
		{
			GameObject boneObj = bonesPos[posNorth]; //bonesPos converts position to object
			bone = boneConverter[boneObj]; //boneConverter converts object to integer
			sequence.Add(bone); //add this integer to the input sequence
			note = boneNotes[boneObj]; //boneNotes converts object to note
			PlayNote(note); //plays the note
		}
		if(337.4f <= eulerSkullRot.x && eulerSkullRot.x <= 342.5f)
		{
			GameObject boneObj = bonesPos[posSouth];
			bone = boneConverter[boneObj];
			sequence.Add(bone);
			note = boneNotes[boneObj];
			PlayNote(note);
		}
		if(337.4f <= eulerSkullRot.z && eulerSkullRot.z <= 342.5f)
		{
			GameObject boneObj = bonesPos[posEast];
			bone = boneConverter[boneObj];
			sequence.Add(bone);
			note = boneNotes[boneObj];
			PlayNote(note);
		}
		if(22.6f >= eulerSkullRot.z && eulerSkullRot.z >= 17.5f)
		{
			GameObject boneObj = bonesPos[posWest];
			bone = boneConverter[boneObj];
			sequence.Add(bone);
			note = boneNotes[boneObj];
			PlayNote(note);
		}
		if(bone!=0) //if they've released it above a bone
		{
			Log("{0} inputted. Current input: {1}",bone,sequence.Join(""));
		}
	}

	private void PlayLow()
	{
		switch(lowNoteCount++%3)
		{
			case 0:
				PlaySound(skullPivot.transform,"boneLow1");
				break;
			case 1:
				PlaySound(skullPivot.transform,"boneLow2");
				break;
			case 2:
				PlaySound(skullPivot.transform,"boneLow3");
				break;
		}
	}

	private void PlayMiddle()
	{
		PlaySound(skullPivot.transform,"boneMid");
	}

	private void PlayHigh()
	{
		PlaySound(skullPivot.transform,"boneHigh");
	}

	private IEnumerator PlayFinal(bool match)
	{
		lowNoteCount = 0;
		foreach(int val in sequence)
		{
			if(val == goodBone||val == badBone)
			{
				PlayLow();
				switch(lowNoteCount++%3)
				{
					case 0:
						yield return new WaitForSecondsRealtime(audioClips[2].length*0.9f);
						break;
					case 1:
						yield return new WaitForSecondsRealtime(audioClips[3].length*0.9f);
						break;
					case 2:
						yield return new WaitForSecondsRealtime(audioClips[4].length*0.9f);
						break;
				}
			}
			if(val == midBone)
			{
				PlayMiddle();
				yield return new WaitForSecondsRealtime(audioClips[5].length*0.9f);
			}
			if(val == highBone)
			{
				PlayHigh();
				yield return new WaitForSecondsRealtime(audioClips[1].length*0.9f);
			}
		}
		PlaySound(skullPivot.transform,"boneEnd");
		answerCheck(match);
	}

	private void PlayNote(int note)
	{
		if(note == goodBone||note == badBone)
		{
			PlayLow();
		}
		if(note == midBone)
		{
			PlayMiddle();
		}
		if(note == highBone)
		{
			PlayHigh();
		}
		if(note == 0)
		{
			Log("Default note value accessed. This is a bug.");
			throw new Exception("DEFAULT ACCESSED");
		}
	}

	private void PlayBad()
	{
		int[] badRange = Enumerable.Range(0,4).ToArray().Shuffle().ToArray();
		int bad = badRange[0];
		switch(bad)
		{
			case 0:
				PlaySound("bad1");
				break;
			case 1:
				PlaySound("bad2");
				break;
			case 2:
				PlaySound("bad3");
				break;
			case 3:
				PlaySound("bad4");
				break;
		}
	}

	private int[] seqRules()
	{
		var bombInfo = Get<KMBombInfo>(); //get cached bomb info
		int[] buildSeq = new int[seqLength]; //create a build sequence for use later
		int bbCount = 0; //to count bad bones modules
		bool multiRuleBool = false, badFourRuleBool = false, serialRuleBool = false, goodPlateRuleBool = false, containTwoRuleBool = false, notContainOneRuleBool = false; //bools for each rule
		bool replaceTwos = false, replaceThrees = false; //in case we are updating all future 2s/3s
		string badFourRuleLog, serialRuleLog, goodPlateRuleLog, containTwoRuleLog, notContainOneRuleLog, otherwiseLog; //logs for each rule
		badFourRuleLog = serialRuleLog = goodPlateRuleLog = containTwoRuleLog = notContainOneRuleLog = otherwiseLog = "DEFAULT TEXT - THIS SHOULD NOT BE VISIBLE";

		//pre for multiRule
		foreach (var module in bombInfo.GetModuleNames()) //iterate over all modules
		{
			if (module == "Bad Bones") //if their name is "badbones"
			{
				bbCount += 1; //add 1 to bad bone counter
			}
		}
		//pre for serialRule
		bool vowel = false;
		string serial = bombInfo.GetSerialNumberLetters().ToArray().Join("");
		var res = serial.Where(c => "AEIOU".Contains(c));
		if (res.Any()) //check for vowels in serial number
		{
			vowel = true; //if there are, set the vowel bool
		}
		for (int priority = 0; priority < 4; priority++) //we have 4 priority layers
		{
			//multiple bad bones modules
			if ((bbCount > 1) && !multiRuleBool) //if there's 2+ bad bones modules and this rule hasn't been completed before
			{
				for (int i = 2; i < seqLength; i += 3) //find every 3rd digit
				{
					buildSeq[i] = 3; //replace with a 3
				}
				Log("Multiple Bad Bones Modules found. Priority: 1. Every 3rd digit set to 3");
				multiRuleBool = true; //set rule as completed
			}

			//bad bone is a 4
			else if ((badBone == 4) && !badFourRuleBool) //if badBone is a 4 and this rule hasn't been completed before
			{
				switch (priority)
				{
					case 0:
						buildSeq[0] = 4; //first
						buildSeq[seqLength - 1] = 4; //final
						badFourRuleLog = "First/Last digit of sequence set to 4.";
						break;
					case 1:
						for (int i = 1; i < seqLength; i += 2) //find every 2nd digit
						{
							if (buildSeq[i] == 0) //check that it's not already assigned
							{
								buildSeq[i] = 2; //replace with a 2
							}
						}
						badFourRuleLog = "Every 2nd digit set to 2.";
						break;
				}
				Log("Bad Bone is a 4. Priority: {0}. " + badFourRuleLog, priority + 1);
				badFourRuleBool = true; //set rule as completed
			}

			//serial number contains a vowel
			else if (vowel && !serialRuleBool) //if we have a vowel and this rule hasn't been completed before
			{
				switch (priority)
				{
					case 0:
						replaceTwos = true; //to be replaced later
						serialRuleLog = "Every future 2 will be set to 3.";
						break;
					case 1:
						for (int i = 4; i < seqLength; i += 5) //find every 2nd digit
						{
							if (buildSeq[i] == 0) //check that it's not already assigned
							{
								buildSeq[i] = 4; //replace with a 4
							}
						}
						serialRuleLog = "Every 2nd digit set to 3.";
						break;
					case 2:
						for (int i = 1; i < seqLength; i += 2) //we can ignore i=2 because the only way to access this rule is for every 2nd digit to already be set to 2
						{
							if (primes.Contains(i))
							{
								if (buildSeq[i] == 0) //check that it's not already assigned
								{
									buildSeq[i] = 1; //replace with a 1
								}
							}
						}
						serialRuleLog = "All prime digits set to 1.";
						break;
				}
				Log("Serial contains a vowel. Priority: {0}. " + serialRuleLog, priority + 1);
				serialRuleBool = true; //set rule as completed
			}

			//good bone exceed number of port plates
			else if ((goodBone > bombInfo.GetPortPlateCount()) && !goodPlateRuleBool)
			{
				switch (priority)
				{
					case 0:
						for (int i = 0; i < seqLength; i++) //iterate over entire thing
						{
							switch (i % 4)
							{
								//for each digit, set correctly
								case 0:
									buildSeq[i] = 3;
									break;
								case 1:
									buildSeq[i] = 1;
									break;
								case 2:
									buildSeq[i] = 2;
									break;
								case 3:
									buildSeq[i] = 4;
									break;
							}
						}
						goodPlateRuleLog = "Repeating '3124' until end of sequence.";
						break;
					case 1:
						for (int i = 0; i < seqLength; i += 2)
						{
							if (buildSeq[i] == 0) //check that it's not already assigned
							{
								buildSeq[i] = 1; //replace with a 1
							}
						}
						goodPlateRuleLog = "Every odd digit set to 1.";
						break;
					case 2:
						for (int i = 3; i < seqLength; i += 4) //find every 4th digit
						{
							if (buildSeq[i] == 0) //check that it's not already assigned
							{
								buildSeq[i] = 2; //replace with a 2
							}
						}
						goodPlateRuleLog = "Every 4th digit set to 2.";
						break;
					case 3:
						for (int i = 0; i < seqLength; i++) //find every remaining digit
						{
							if (buildSeq[i] == 0) //check that it's not already assigned
							{
								buildSeq[i] = goodBone; //replace with the good bone
							}
						}
						goodPlateRuleLog = String.Format("Every remaining digit set to {0}.", goodBone);
						break;
				}
				Log("Good Bone value ({0}) exceeds number of port plates ({1}). Priority: {2}. " + goodPlateRuleLog, goodBone, bombInfo.GetPortPlateCount(), priority + 1);
				goodPlateRuleBool = true; //set rule as completed
			}

			//sequence contains a 2
			else if (buildSeq.Contains(2) && !containTwoRuleBool)
			{
				switch (priority)
				{
					case 0:
						for (int i = 2; i < seqLength; i += 3) //find every 3rd digit
						{
							buildSeq[i] = 4; //replace with a 4
						}
						containTwoRuleLog = "Every 3rd digit set to 4.";
						break;
					case 1:
						replaceThrees = true; //replace all future 3s
						containTwoRuleLog = "Every future 3 will be set to 4.";
						break;
					case 2:
						buildSeq[seqLength - 1] = 1; //replace final digit with 1
						containTwoRuleLog = "Final digit replaced with 1.";
						break;
					case 3:
						for (int i = 0; i < seqLength; i++)
						{
							if (buildSeq[i] == 0) //check that it's not already assigned
							{
								buildSeq[i] = 4; //replace with a 4
							}
						}
						containTwoRuleLog = "Every remaining digit set to 4.";
						break;
				}
				Log("Sequence contains a 2. Priority: {0}. " + containTwoRuleLog, priority + 1);
				containTwoRuleBool = true; //set rule as completed
			}

			//sequence does not contain a 1
			else if (!buildSeq.Contains(1) && !notContainOneRuleBool)
			{
				switch (priority)
				{
					case 0:
						for (int i = 1; i < seqLength; i += 2) //find every 2nd digit
						{
							buildSeq[i] = 4; //set to 4
						}
						notContainOneRuleLog = "Every 2nd digit set to 4.";
						break;
					case 1:
						for (int i = 0; i < seqLength; i++)
						{
							if (buildSeq[i] == 3) //replace all 3s
							{
								buildSeq[i] = 4; //with 4s
							}
						}
						notContainOneRuleLog = "Every 3 replaced with 4.";
						break;
					case 2:
						for (int i = 0; i < seqLength; i++)
						{
							if (i < 4) //replace the first 4 digits
							{
								buildSeq[i] = 2; //with a 2
							}
						}
						notContainOneRuleLog = "First 4 digits replaced with a 2.";
						break;
					case 3:
						for (int i = 0; i < seqLength; i++) //iterate over remaining digits
						{
							if (buildSeq[i] == 0) //check that it's not already assigned
							{
								buildSeq[i] = 1; //replace with a 1
							}
						}
						notContainOneRuleLog = "Every remaining digit set to 1.";
						break;
				}
				Log("Sequence does not contain a 1. Priority: {0}. " + notContainOneRuleLog, priority + 1);
				notContainOneRuleBool = true; //set rule as completed
			}

			//otherwise
			else
			{
				switch (priority)
				{
					case 0:
						for (int i = 3; i < seqLength; i += 4) //find every 4th digit
						{
							buildSeq[i] = 4; //set to 4
						}
						otherwiseLog = "Every 4th digit set to 4.";
						break;
					case 1:
						for (int i = 2; i < seqLength; i += 3) //find every 3rd digit
						{
							if (buildSeq[i] == 0) //check that it's not already assigned
							{
								buildSeq[i] = 3; //set to 3
							}
						}
						otherwiseLog = "Every 3rd digit set to 3.";
						break;
					case 2:
						for (int i = 1; i < seqLength; i += 2) //find every 2nd digit
						{
							if (buildSeq[i] == 0) //check that it's not already assigned
							{
								buildSeq[i] = 2; //set to 2
							}
						}
						otherwiseLog = "Every 2nd digit set to 2.";
						break;
					case 3:
						for (int i = 0; i < seqLength; i++) //find every remaining digit
						{
							if (buildSeq[i] == 0) //check that it's not already assigned
							{
								buildSeq[i] = 1; //set to 1
							}
						}
						otherwiseLog = "Every remaining digit set to 1.";
						break;
				}
				Log("No other rules apply. Priority: {0}. " + otherwiseLog, priority + 1);
			}

			//log on every iteration
			Log("Current sequence: {0}", buildSeq.Join(""));
		}

		//replacements
		Log("Replacing all digits matching 'Replace future values of X with Y' rules:");
		for (int i = 0; i < seqLength; i++)
		{
			if (replaceTwos) //if we're replacing twos
			{
				if (buildSeq[i] == 2)
				{
					buildSeq[i] = 3;
					Log("Replaced 2 (digit {0}) with 3", i + 1);
					Log("Current sequence: {0}", buildSeq.Join(""));
				}
			}
			if (replaceThrees) //if we're replacing threes
			{
				if (buildSeq[i] == 3) //yes, this can happen straight after a 2 is replaced with a 3 - 3 -> 4 triggers afterwards, so if both are active, 2 -> 4
				{
					buildSeq[i] = 4;
					Log("Replaced 3 (digit {0}) with 4", i + 1);
					Log("Current sequence: {0}", buildSeq.Join(""));
				}
			}
			if (!(replaceTwos || replaceThrees))
			{
				Log("None! Current sequence: {0}", buildSeq.Join(""));
				break;
			}
		}

		//sequence mods
		buildSeq = sequenceMods(bombInfo, buildSeq);

		Log("Replacing the Bad Bone ({0}) with the Good Bone ({1}):", badBone, goodBone);
		for (int i = 0; i < seqLength; i++)
		{
			if (buildSeq[i] == badBone) //if value is the bad bone
			{
				buildSeq[i] = goodBone; //replace with the good bone
				Log("Replaced {0} (digit {1}) with {2}", badBone, i + 1, goodBone);
			}
		}

		return buildSeq;
	}

	private int[] sequenceMods(KMBombInfo bombInfo, int[] modSeq)
	{
		Log("Checking sequence modifiers:");
		//no ports
		if (bombInfo.GetPortCount() == 0)
		{
			string portLog = "DEFAULT TEXT - SHOULD NOT BE VISIBLE.";
			int count = badBone + goodBone; //preset count as being the good/bad bone values
			for (int i = 0; i < seqLength; i++)
			{
				if ((modSeq[i] == goodBone) || (modSeq[i] == badBone)) //check if they're good/bad bones
				{
					count += 1;
				}
			}
			if (count > seqLength)
			{
				modSeq = modSeq.Reverse();
				portLog = "Reversing entire sequence.";
			}
			else
			{
				portLog = "No action taken.";
			}
			Log("No ports found. Count: [{0}+{1}+{2}={3}] {4} {5} (sequence length); " + portLog, (count - badBone - goodBone), badBone, goodBone, count, (count > seqLength) ? ">" : "<", seqLength);
			Log("Current sequence: {0}", modSeq.Join(""));
		}

		//more letters than numbers
		if (bombInfo.GetSerialNumberLetters().Count() > bombInfo.GetSerialNumberNumbers().Count())
		{
			for (int i = 0; i < seqLength; i++)
			{
				if (IsPowerOfTwo(i + 1))
				{
					switch (modSeq[i])
					{
						case 1:
							modSeq[i] = 4;
							break;
						case 2:
							modSeq[i] = 3;
							break;
						case 3:
							modSeq[i] = 2;
							break;
						case 4:
							modSeq[i] = 1;
							break;
					}
				}
			}
			Log("More letters than numbers in serial number. Replacing power of 2 positions.");
			Log("Current sequence: {0}", modSeq.Join(""));
		}

		//more than 3 batteries
		if (bombInfo.GetBatteryCount() > 3)
		{
			int tempVal;
			for (int i = 0; i < seqLength; i++)
			{
				if (i < 4)
				{
					tempVal = (modSeq[i] + 2) % 5;
					if (tempVal == 0)
					{
						tempVal = goodBone;
					}
					modSeq[i] = tempVal;
				}
			}
			Log("More than 3 batteries. Adjusting positions 1-4: add 2, modulo 5, replace 0s with Good Bone ({0}).", goodBone);
			Log("Current sequence: {0}", modSeq.Join(""));
		}

		//bad bone even
		if (badBone % 2 == 0)
		{
			if (seqLength > 3)
			{
				int startIndex = 2;
				int endIndex = Math.Min(seqLength,8);
				while(startIndex < endIndex)
				{
					int temp = modSeq[startIndex];
					modSeq[startIndex] = modSeq[endIndex-1];
					modSeq[endIndex-1] = temp;
					startIndex++;
					endIndex--;
				}
				Log("Bad Bone is even. Reversing digits 3-{0}", Math.Min(seqLength, 8));
				Log("Current sequence: {0}", modSeq.Join(""));
			}
		}

		//BAAAAD TO THE BONE
		if (seqLength == 5 && bonesPos[posNorth] == one && bonesPos[posEast] == two && bonesPos[posSouth] == three && bonesPos[posWest] == four)
		{
			modSeq = new int[5] { goodBone, goodBone, highBone, goodBone, midBone }; //
			Log("North bone is 1 & bone order is clockwise & sequence length is 5. We're Bad to the Bone!");
			Log("Current sequence: {0}", modSeq.Join(""));
		}

		//BOB???????????
		if (seqLength == 3 && bombInfo.IsIndicatorPresent("BOB"))
		{
			modSeq = new int[3] { goodBone, midBone, highBone }; //generate the smoke on the water riff
			Log("Sequence length is 3 and Indicator BOB present. This riff sounds familiar...");
			Log("Current sequence: {0}", modSeq.Join(""));
		}

		return modSeq;
	}

	private bool IsPowerOfTwo(int x) //for use above
	{
		return (x & (x - 1)) == 0;
	}

	// Update is called once per frame
	void Update () {
		Quaternion skullRot = skullPivot.transform.localRotation;
		if(!_skullHeld && !(skullRot == skullStartRot))
		{
			//return skull to center
			skullPivot.transform.localRotation = Quaternion.Lerp(skullRot,skullStartRot,20.0f*Time.deltaTime);
		}
		if(_skullHeld)
		{
			float xMouse = mouseStartPos.x - Input.mousePosition.x; //mousePos x needs to be inverted - +x = left. apparently.
			float yMouse = Input.mousePosition.y - mouseStartPos.y; //mousePos y does not - +y = up. for some reason.
			Vector3 currentRot = new Vector3(yMouse,0,xMouse);
			Vector3 clampedRot = Vector3.ClampMagnitude(currentRot,22.5f);
			skullPivot.transform.localRotation = Quaternion.Euler(clampedRot);
		}
	}
}
