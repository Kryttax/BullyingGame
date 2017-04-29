﻿using Isometra;
using Isometra.Sequences;
using NCalc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class ComputerSocialComment : EventManager
{

	public ComputerManager computer;
	private JSONObject jsonObj;
	public TextAsset jsonFile;

	public override void ReceiveEvent(IGameEvent ev)
	{
		if (ev.Name.Replace("\"", "") == "computer comment")
		{
			string publicationKey = ev.getParameter(SequenceGenerator.EVENT_VARIABLE_FIELD).ToString().Replace("\"", "");
			string message = ev.getParameter(SequenceGenerator.EVENT_VALUE_FIELD).ToString().Replace("\"", "");
			string author = ev.getParameter(SequenceGenerator.EVENT_KEY_FIELD).ToString().Replace("\"", "");

			float time = (float)ev.getParameter(SequenceGenerator.EVENT_TIME_FIELD);
			object otherObj = ev.getParameter(SequenceGenerator.EVENT_OTHER_FIELD);
			string other = otherObj != null ? otherObj.ToString().Replace("\"", "") : "";

			StartCoroutine(ExecuteAfterTime(time, publicationKey, author, message, other));
		}
	}

	public override void Tick()
	{
	}

	// Use this for initialization
	void Start()
	{
		if (jsonFile != null)
		{
			string fileContents = jsonFile.text;
			jsonObj = JSONObject.Create(fileContents);

			Sequence seq = SequenceGenerator.createSimplyDialog("default", jsonObj);
			computer.AddDefaultPublicationCommentSequence(seq);
		}
	}

	// Update is called once per frame
	void Update()
	{

	}

	private void GenerateAndSetSequence(string jsonKey, string publicationKey)
	{
		Sequence seq = SequenceGenerator.createSimplyDialog(jsonKey, jsonObj);
		computer.AddPublicationCommentSequence(publicationKey, seq);
	}

	private void RemoveSequence(string publicationKey)
	{
		
		computer.RemovePublicationCommentSequence(publicationKey);
		
	}

	IEnumerator ExecuteAfterTime(float time, string publicationKey, string author, string message, string other)
	{
		yield return new WaitForSeconds(time);

		if (jsonObj)
		{
			if (other == "empty")
			{
				RemoveSequence(publicationKey);
			}
			else if (other != "")
			{
				GenerateAndSetSequence(other, publicationKey);
			}
		}
		
		Regex r = new Regex(@"\$(\w+)", RegexOptions.IgnoreCase);
		Match m = r.Match(message);

		GlobalState gState = GlobalState.Instance;
		Type t = gState.GetType();

		while (m.Success)
		{
			string g = m.Groups[1].Value;
			message = message.Replace("$"+g, t.GetProperty(g).GetValue(gState, null).ToString());
			m = m.NextMatch();
		}

		message = message.Replace("True", "true");
		message = message.Replace("False", "false");

		Regex e = new Regex(@"<\$(.*)\$>", RegexOptions.IgnoreCase);
		m = e.Match(message);
		while (m.Success)
		{
			string g = m.Groups[1].Value;
			Expression exp = new Expression(g);
			message = message.Replace("<$" + g + "$>", exp.Evaluate().ToString());
			m = m.NextMatch();
		}


		computer.AddPublicationComment(publicationKey, author, message);
	}
}