// Created by Kay
// Copyright 2013 by SCIO System-Consulting GmbH & Co. KG. All rights reserved.
using System.Collections.Generic;

namespace Scio.CodeGenerator
{
	public class AttributeCodeElement : CodeElement
	{
		public string Name;
	
		public List<string> Parameters = new List<string> ();
	
		public AttributeCodeElement (string name)
		{
			this.Name = name;
		}
	
		public void AddParameter (string parameter)
		{
			Parameters.Add (parameter);
		}
		
		public void AddStringParameter (string param)
		{
			AddParameter ("\"" + param + "\"");
		}
		public override string ToString ()
		{
			string str = "";
			Parameters.ForEach ((string s) => str += (str.Length > 0 ? ", " : "") + s);
			return string.Format ("[{0} ({1})]", Name, str);
		}
	}
	
	public class ObsoleteAttributeCodeElement : AttributeCodeElement
	{
		public ObsoleteAttributeCodeElement (string message, bool error) : base ("System.ObsoleteAttribute")
		{
			AddStringParameter (message);
			AddParameter (error.ToString ().ToLower ());
		}
	}
}
