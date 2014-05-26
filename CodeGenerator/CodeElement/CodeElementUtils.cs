// Created by Kay
// Copyright 2013 by SCIO System-Consulting GmbH & Co. KG. All rights reserved.
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace Scio.CodeGeneration
{
	public static class CodeElementUtils
	{
		public static string GetFormattedType (Type elementType) {
			if (elementType == typeof(bool)) {
				return "bool";
			} else if (elementType == typeof(int)) {
				return "int";
			} else if (elementType == typeof(float)) {
				return "float";
			} else if (elementType == typeof(double)) {
				return "double";
			} else if (elementType == typeof(string)) {
				return "string";
			}
			return elementType.Name;
		}
		
		public static string GetFormattedValue (object obj) {
			if (obj == null) {
				return "null";
			} else if (obj == typeof(bool)) {
				return obj.ToString ().ToLower ();
			} else if (obj == typeof(string)) {
				string s = obj.ToString ();
				if (s.StartsWith ("\"") && s.EndsWith ("\"")) {
					return s;
				}
				return "\"" + s + "\"";
			}
			return obj.ToString ();
		}
		
		static AccessType GetAccessType (MethodInfo method) {
			if (method == null) {
				return AccessType.Private;
			}
			AccessType access = AccessType.Public;
			if (method.IsFamily) {
				access = AccessType.Protected;
			} else if (method.IsPrivate) {
				access = AccessType.Private;
			}
			return access;
		}
		
		static AccessType GetAccessType (FieldInfo field) {
			if (field == null) {
				return AccessType.Private;
			}
			AccessType access = AccessType.Public;
			if (field.IsFamily) {
				access = AccessType.Protected;
			} else if (field.IsPrivate) {
				access = AccessType.Private;
			}
			return access;
		}
		
		public static int CleanupExistingClass (ClassCodeElement existingClass, ClassCodeElement newClass, bool keepObsolete) {
			int remaining = RemoveDuplicateElements (existingClass.Properties, newClass.Properties, keepObsolete);
			remaining += RemoveDuplicateElements (existingClass.Fields, newClass.Fields, keepObsolete);
			remaining += RemoveDuplicateElements (existingClass.Methods, newClass.Methods, keepObsolete);
			return remaining;
		}
		
		public static List<ClassMemberCompareElement> CompareClasses (ClassCodeElement existingClass, ClassCodeElement newClass, bool keepObsolete) {
			List<ClassMemberCompareElement> l = CompareElements (existingClass.Fields, newClass.Fields, keepObsolete);
			l.AddRange (CompareElements (existingClass.Properties, newClass.Properties, keepObsolete));
			l.AddRange (CompareElements (existingClass.Methods, newClass.Methods, keepObsolete));
			return l;
		}

		static List<ClassMemberCompareElement> CompareElements<T> (List<T> oldMembers, List<T> newMembers, bool keepObsolete) 
				where T : MemberCodeElement {
			List<ClassMemberCompareElement> result = new List<ClassMemberCompareElement> ();
			IEnumerable<T> newMinusOldList = newMembers.Except (oldMembers, new MemberNameComparer<T> ());
			foreach (T newElement in newMinusOldList) {
				result.Add (new ClassMemberCompareElement (newElement, ClassMemberCompareElement.Result.New));
			}
			IEnumerable<T> oldMinusNewList = oldMembers.Except (newMembers, new MemberNameComparer<T> ());
			foreach (T oldElement in oldMinusNewList) {
				if (keepObsolete || !oldElement.Obsolete) {
					result.Add (new ClassMemberCompareElement (oldElement, ClassMemberCompareElement.Result.Obsolete));
				} else {
					result.Add (new ClassMemberCompareElement (oldElement, ClassMemberCompareElement.Result.Remove));
				}
			}
			return result;
		}

		static int RemoveDuplicateElements<T> (List<T> oldMembers, List<T> newMembers, bool keepObsolete) 
				where T : MemberCodeElement {
			if (!keepObsolete) {
				oldMembers.RemoveAll ((element) => element.Obsolete );
			}
			oldMembers.RemoveAll ((oldElement) => {
				string name = oldElement.Name;
				return newMembers.FindIndex ( (newElement) => newElement.Name == name) != -1;
			});
			return oldMembers.Count;
		}

		public static List<string> GetCriticalNames (ClassCodeElement existingClass)
		{
			List<string> names = new List<string> ();
			existingClass.Properties.ForEach ((item) => names.Add (item.Name));
			existingClass.Methods.ForEach ((item) => names.Add (item.Name));
			return names;
		}

		public static void MergeElements<T> (List<T> target, List<T> other, Predicate<T> filter = null) {
			if (filter == null) {
				filter = new System.Predicate<T> ((t) => true );
			}
			List<T> filteredList = other.FindAll (filter);
			target.AddRange (filteredList);
		}

		public static void AddPropertyInfos (List<GenericPropertyCodeElement> properties, List<PropertyInfo> propertyInfos) {
			foreach (PropertyInfo propertyInfo in propertyInfos) {
				string name = propertyInfo.Name;
				Type type = propertyInfo.PropertyType;
				AccessType access = AccessType.Public;
				GenericPropertyCodeElement p = new GenericPropertyCodeElement (type, name, access);
				MethodInfo[] accessors = propertyInfo.GetAccessors (true);
				foreach (MethodInfo m in accessors) {
					AccessType acc = GetAccessType (m);
					if (m.Name.StartsWith ("get_")) {
						p.Getter.Access = acc;
						p.Getter.CodeLines.Add ("throw new System.NotImplementedException ();");
					} else if (m.Name.StartsWith ("set_")) {
						p.Setter.Access = acc;
						p.Setter.CodeLines.Add ("throw new System.NotImplementedException ();");
					}
				}
				AddAttributes (p.Attributes, propertyInfo.GetCustomAttributes (false));
//				AccessType access = (getterAccess <= setterAccess ? getterAccess : setterAccess);
				properties.Add (p);
			}
		}

		public static void AddMethodInfos (List<GenericMethodCodeElement> methods, List<MethodInfo> methodInfos) {
			foreach (MethodInfo m in methodInfos) {
				Type t = m.ReturnType;
				GenericMethodCodeElement methodElement = new GenericMethodCodeElement (t, m.Name, GetAccessType (m));
				foreach (ParameterInfo param in m.GetParameters ()) {
					if (param.DefaultValue != null) {
						// NOTE: There is no chance .NET 3.5 to distinguish between a null default value and NO default value
						// In 4.0 a new property HasDefaultValue was introduced
						methodElement.AddParameter (param.ParameterType, param.Name, param.DefaultValue);
					} else {
						methodElement.AddParameter (param.ParameterType, param.Name);
					}
				}
				methodElement.Code.Add ("throw new System.NotImplementedException ();");
				AddAttributes (methodElement.Attributes, m.GetCustomAttributes (false));
				methods.Add (methodElement);
			}
		}
		
		public static void AddFieldInfos (List<GenericFieldCodeElement> fields, List<FieldInfo> fieldInfos) {
			foreach (FieldInfo f in fieldInfos) {
				Type t = f.FieldType;
				GenericFieldCodeElement fieldElement = new GenericFieldCodeElement (t, f.Name, "", GetAccessType (f));
				AddAttributes (fieldElement.Attributes, f.GetCustomAttributes (false));
				fields.Add (fieldElement);
			}
		}
		
		public static void AddAttributes (List<AttributeCodeElement> attributeList, object[] attributeObjects) {
			foreach (object o in attributeObjects) {
				if (o is ObsoleteAttribute) {
					ObsoleteAttribute a = (ObsoleteAttribute)o;
					attributeList.Add (new ObsoleteAttributeCodeElement (a.Message, a.IsError));
				} else if (o is GeneratedClassAttribute) {
					GeneratedClassAttribute a = (GeneratedClassAttribute)o;
					attributeList.Add (new GeneratedClassAttributeCodeElement (a.CreationDate, a.LastVersionDate));
				} else if (o is GeneratedMemberAttribute) {
					GeneratedMemberAttribute a = (GeneratedMemberAttribute)o;
					attributeList.Add (new GeneratedMemberAttributeCodeElement (a.CreationDate));
				} else {
					Scio.CodeGeneration.Logger.Debug ("Attribute = " + o.ToString ());
				}
			}
		}

	}
}
