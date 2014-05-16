// Created by Kay
// Copyright 2013 by SCIO System-Consulting GmbH & Co. KG. All rights reserved.
using System;

namespace Scio.CodeGenerator
{
	public interface CodeGenerator
	{
		string Code {get;}

		CodeGeneratorResult Prepare (CodeGeneratorConfig inputConfig);

		CodeGeneratorResult GenerateCode (FileCodeElement classCodeElement);
	}

	public class CodeGeneratorConfig
	{
		public string Template;
		public CodeGeneratorConfig (string template) {
			this.Template = template;
		}
	}

	public class CodeGeneratorResult
	{
		public enum Feedback
		{
			Success = 0,
			AskUserInput = 1,
			Error = 2,
		}
		public Feedback feedback = Feedback.Success;

		public bool NoSuccess { get { return feedback > Feedback.Success; } }
		public bool Success { get { return feedback == Feedback.Success; } }
		public bool AskUser { get { return feedback == Feedback.AskUserInput; } }
		public bool Error { get { return feedback == Feedback.Error; } }

		public string ErrorTitle = "";
		
		public string ErrorText = "";
		
		public void Reset () {
			feedback = Feedback.Success;
			ErrorTitle = "";
			ErrorText = "";
		}
		public CodeGeneratorResult SetError (string title = null, string text = null) {
			Log.Temp ("Error: " + title + " - " + text);
			return Check (false, feedback, Feedback.Error, title, text);
		}
		
		public CodeGeneratorResult SetWarning (string title = null, string text = null) {
			Log.Temp ("Warning: " + title + " - " + text);
			return Check (false, feedback, Feedback.AskUserInput, title, text);
		}
		
		public CodeGeneratorResult Check (bool condition, Feedback ok, Feedback failed, 
		                   string title = null, string text = null) {
			if (condition) {
				feedback = ok;
			} else {
				feedback = failed;
				if (!String.IsNullOrEmpty (title)) {
					ErrorTitle = title;
				}
				if (!String.IsNullOrEmpty (text)) {
					ErrorText = text;
				}
			}
			return this;
		}

		public override string ToString () {
			return string.Format ("[CodeGeneratorResult: feedback={0}, ErrorTitle={1}, ErrorText={2}]", feedback, ErrorTitle, ErrorText);
		}
		
	}
}

