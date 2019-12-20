using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToolBox.Extensions
{
	internal static class ReflectionExtensions
	{
		public static void SetPrivateExplicit<T>(this T self, string name, object value)
		{
			ReflectionExtensions.MemberKey memberKey = new ReflectionExtensions.MemberKey(typeof(T), name);
			FieldInfo field;
			if (!ReflectionExtensions._fieldCache.TryGetValue(memberKey, out field))
			{
				field = memberKey.type.GetField(memberKey.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
				ReflectionExtensions._fieldCache.Add(memberKey, field);
			}
			field.SetValue(self, value);
		}

		public static void SetPrivate(this object self, string name, object value)
		{
			ReflectionExtensions.MemberKey memberKey = new ReflectionExtensions.MemberKey(self.GetType(), name);
			FieldInfo field;
			if (!ReflectionExtensions._fieldCache.TryGetValue(memberKey, out field))
			{
				field = memberKey.type.GetField(memberKey.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
				ReflectionExtensions._fieldCache.Add(memberKey, field);
			}
			field.SetValue(self, value);
		}

		public static object GetPrivateExplicit<T>(this T self, string name)
		{
			ReflectionExtensions.MemberKey memberKey = new ReflectionExtensions.MemberKey(typeof(T), name);
			FieldInfo field;
			if (!ReflectionExtensions._fieldCache.TryGetValue(memberKey, out field))
			{
				field = memberKey.type.GetField(memberKey.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
				ReflectionExtensions._fieldCache.Add(memberKey, field);
			}
			return field.GetValue(self);
		}

		public static object GetPrivate(this object self, string name)
		{
			ReflectionExtensions.MemberKey memberKey = new ReflectionExtensions.MemberKey(self.GetType(), name);
			FieldInfo field;
			if (!ReflectionExtensions._fieldCache.TryGetValue(memberKey, out field))
			{
				field = memberKey.type.GetField(memberKey.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
				ReflectionExtensions._fieldCache.Add(memberKey, field);
			}
			return field.GetValue(self);
		}

		public static void SetPrivateProperty(this object self, string name, object value)
		{
			ReflectionExtensions.MemberKey memberKey = new ReflectionExtensions.MemberKey(self.GetType(), name);
			PropertyInfo property;
			if (!ReflectionExtensions._propertyCache.TryGetValue(memberKey, out property))
			{
				property = memberKey.type.GetProperty(memberKey.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
				ReflectionExtensions._propertyCache.Add(memberKey, property);
			}
			property.SetValue(self, value, null);
		}

		public static object GetPrivateProperty(this object self, string name)
		{
			ReflectionExtensions.MemberKey memberKey = new ReflectionExtensions.MemberKey(self.GetType(), name);
			PropertyInfo property;
			if (!ReflectionExtensions._propertyCache.TryGetValue(memberKey, out property))
			{
				property = memberKey.type.GetProperty(memberKey.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
				ReflectionExtensions._propertyCache.Add(memberKey, property);
			}
			return property.GetValue(self, null);
		}

		public static void SetPrivateProperty(this Type self, string name, object value)
		{
			ReflectionExtensions.MemberKey memberKey = new ReflectionExtensions.MemberKey(self, name);
			PropertyInfo property;
			if (!ReflectionExtensions._propertyCache.TryGetValue(memberKey, out property))
			{
				property = memberKey.type.GetProperty(memberKey.name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
				ReflectionExtensions._propertyCache.Add(memberKey, property);
			}
			property.SetValue(null, value, null);
		}

		public static object GetPrivateProperty(this Type self, string name)
		{
			ReflectionExtensions.MemberKey memberKey = new ReflectionExtensions.MemberKey(self, name);
			PropertyInfo property;
			if (!ReflectionExtensions._propertyCache.TryGetValue(memberKey, out property))
			{
				property = memberKey.type.GetProperty(memberKey.name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
				ReflectionExtensions._propertyCache.Add(memberKey, property);
			}
			return property.GetValue(null, null);
		}

		public static object CallPrivate(this object self, string name, params object[] p)
		{
			return self.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).Invoke(self, p);
		}

		public static object CallPrivate(this Type self, string name, params object[] p)
		{
			return self.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).Invoke(null, p);
		}

		public static void LoadWith<T>(this T to, T from)
		{
			foreach (FieldInfo fieldInfo in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (fieldInfo.FieldType.IsArray)
				{
					Array array = (Array)fieldInfo.GetValue(from);
					Array array2 = Array.CreateInstance(fieldInfo.FieldType.GetElementType(), array.Length);
					for (int j = 0; j < array.Length; j++)
					{
						array2.SetValue(array.GetValue(j), j);
					}
				}
				else
				{
					fieldInfo.SetValue(to, fieldInfo.GetValue(from));
				}
			}
		}

		public static void ReplaceEventsOf(this object self, object obj)
		{
			foreach (Button button in Resources.FindObjectsOfTypeAll<Button>())
			{
				for (int j = 0; j < button.onClick.GetPersistentEventCount(); j++)
				{
					if (button.onClick.GetPersistentTarget(j) == obj)
					{
						(button.onClick.GetPrivateExplicit("m_PersistentCalls").GetPrivate("m_Calls") as IList)[j].SetPrivate("m_Target", self);
					}
				}
			}
			foreach (Slider slider in Resources.FindObjectsOfTypeAll<Slider>())
			{
				for (int k = 0; k < slider.onValueChanged.GetPersistentEventCount(); k++)
				{
					if (slider.onValueChanged.GetPersistentTarget(k) == obj)
					{
						(slider.onValueChanged.GetPrivateExplicit("m_PersistentCalls").GetPrivate("m_Calls") as IList)[k].SetPrivate("m_Target", self);
					}
				}
			}
			foreach (InputField inputField in Resources.FindObjectsOfTypeAll<InputField>())
			{
				for (int l = 0; l < inputField.onEndEdit.GetPersistentEventCount(); l++)
				{
					if (inputField.onEndEdit.GetPersistentTarget(l) == obj)
					{
						(inputField.onEndEdit.GetPrivateExplicit("m_PersistentCalls").GetPrivate("m_Calls") as IList)[l].SetPrivate("m_Target", self);
					}
				}
				for (int m = 0; m < inputField.onValueChanged.GetPersistentEventCount(); m++)
				{
					if (inputField.onValueChanged.GetPersistentTarget(m) == obj)
					{
						(inputField.onValueChanged.GetPrivateExplicit("m_PersistentCalls").GetPrivate("m_Calls") as IList)[m].SetPrivate("m_Target", self);
					}
				}
				if (inputField.onValidateInput != null && inputField.onValidateInput.Target == obj)
				{
					inputField.onValidateInput.SetPrivate("_target", obj);
				}
			}
			foreach (Toggle toggle in Resources.FindObjectsOfTypeAll<Toggle>())
			{
				for (int n = 0; n < toggle.onValueChanged.GetPersistentEventCount(); n++)
				{
					if (toggle.onValueChanged.GetPersistentTarget(n) == obj)
					{
						(toggle.onValueChanged.GetPrivateExplicit("m_PersistentCalls").GetPrivate("m_Calls") as IList)[n].SetPrivate("m_Target", self);
					}
				}
			}
			EventTrigger[] array5 = Resources.FindObjectsOfTypeAll<EventTrigger>();
			for (int i = 0; i < array5.Length; i++)
			{
				foreach (EventTrigger.Entry entry in array5[i].triggers)
				{
					for (int num = 0; num < entry.callback.GetPersistentEventCount(); num++)
					{
						if (entry.callback.GetPersistentTarget(num) == obj)
						{
							(entry.callback.GetPrivateExplicit("m_PersistentCalls").GetPrivate("m_Calls") as IList)[num].SetPrivate("m_Target", self);
						}
					}
				}
			}
		}

		public static MethodInfo GetCoroutineMethod(this Type objectType, string name)
		{
			Type type = null;
			name = "+<" + name + ">";
			foreach (Type type2 in objectType.GetNestedTypes(BindingFlags.NonPublic))
			{
				if (type2.FullName.Contains(name))
				{
					type = type2;
					break;
				}
			}
			if (type != null)
			{
				return type.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return null;
		}

		private static readonly Dictionary<ReflectionExtensions.MemberKey, FieldInfo> _fieldCache = new Dictionary<ReflectionExtensions.MemberKey, FieldInfo>();

		private static readonly Dictionary<ReflectionExtensions.MemberKey, PropertyInfo> _propertyCache = new Dictionary<ReflectionExtensions.MemberKey, PropertyInfo>();

		private struct MemberKey
		{
			public MemberKey(Type inType, string inName)
			{
				this.type = inType;
				this.name = inName;
				this._hashCode = (this.type.GetHashCode() ^ this.name.GetHashCode());
			}

			public override int GetHashCode()
			{
				return this._hashCode;
			}

			public readonly Type type;

			public readonly string name;

			private readonly int _hashCode;
		}
	}
}
