using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
	/// <summary>
	/// Responsible for drawing an 'Event 2.0 for Unity' attribute in Inspector
	/// </summary>
	[CustomPropertyDrawer(typeof(UnityEventBase2), true)]
	public class UnityEvent2Drawer : PropertyDrawer
	{
		class State
		{
			internal ReorderableList m_ReorderableList;
			public int lastSelectedIndex;
		}

		private const string kNoFunctionString = "No Function";

		// PersistentCall2 paths
		internal const string kCallStatePath = "m_CallState";
		internal const string kIsStaticPath = "m_IsStatic";
		internal const string kInstancePath = "m_Target";
		internal const string kIsStaticForInstancePath = "m_IsStaticForInstance";
		internal const string kMethodNamePath = "m_MethodName";
		internal const string kArgumentsPath = "m_Arguments";
		internal const string kModesPath = "m_Modes";

		//ArgumentCache2 paths
		internal const string kObjectArgument = "m_ObjectArgument";
		internal const string kObjectArgumentAssemblyTypeName = "m_ObjectArgumentAssemblyTypeName";
		internal const string kCharArgument = "m_CharArgument";
		internal const string kByteArgument = "m_ByteArgument";
		internal const string kSByteArgument = "m_SByteArgument";
		internal const string kShortArgument = "m_ShortArgument";
		internal const string kUShortArgument = "m_UShortArgument";
		internal const string kIntArgument = "m_IntArgument";
		internal const string kUIntArgument = "m_UIntArgument";
		internal const string kLongArgument = "m_LongArgument";
		internal const string kFloatArgument = "m_FloatArgument";
		internal const string kDoubleArgument = "m_DoubleArgument";
		internal const string kStringArgument = "m_StringArgument";
		internal const string kBoolArgument = "m_BoolArgument";
		internal const string kEnumArgument = "m_EnumArgument";
		internal const string kVector2Argument = "m_Vector2Argument";
		internal const string kVector2IntArgument = "m_Vector2IntArgument";
		internal const string kVector3Argument = "m_Vector3Argument";
		internal const string kVector3IntArgument = "m_Vector3IntArgument";
		internal const string kVector4Argument = "m_Vector4Argument";
		internal const string kLayerMaskArgument = "m_LayerMaskArgument";
		internal const string kColorArgument = "m_ColorArgument";
		internal const string kQuaternionArgument = "m_QuaternionArgument";
		internal const string kObjectArrayArgument = "m_ObjectArrayArgument";
		internal const string kCharArrayArgument = "m_CharArrayArgument";
		internal const string kByteArrayArgument = "m_ByteArrayArgument";
		internal const string kSByteArrayArgument = "m_SByteArrayArgument";
		internal const string kShortArrayArgument = "m_ShortArrayArgument";
		internal const string kUShortArrayArgument = "m_UShortArrayArgument";
		internal const string kIntArrayArgument = "m_IntArrayArgument";
		internal const string kUIntArrayArgument = "m_UIntArrayArgument";
		internal const string kLongArrayArgument = "m_LongArrayArgument";
		internal const string kFloatArrayArgument = "m_FloatArrayArgument";
		internal const string kDoubleArrayArgument = "m_DoubleArrayArgument";
		internal const string kStringArrayArgument = "m_StringArrayArgument";
		internal const string kBoolArrayArgument = "m_BoolArrayArgument";
		internal const string kEnumArrayArgument = "m_EnumArrayArgument";
		internal const string kVector2ArrayArgument = "m_Vector2ArrayArgument";
		internal const string kVector2IntArrayArgument = "m_Vector2IntArrayArgument";
		internal const string kVector3ArrayArgument = "m_Vector3ArrayArgument";
		internal const string kVector3IntArrayArgument = "m_Vector3IntArrayArgument";
		internal const string kVector4ArrayArgument = "m_Vector4ArrayArgument";
		internal const string kLayerMaskArrayArgument = "m_LayerMaskArrayArgument";
		internal const string kColorArrayArgument = "m_ColorArrayArgument";
		internal const string kQuaternionArrayArgument = "m_QuaternionArrayArgument";

		internal const string kIsDynamicArgument = "m_IsDynamicArgument";

		private const float kSpacing = 5f;
		private const float kButtonWidth = 15f;
		private const int kExtraSpacing = 9;

		private UnityEventBase2 m_DummyEvent;
		private SerializedProperty m_Prop;
		private SerializedProperty m_ListenersArray;
		private string m_Text;

		private ReorderableList m_ReorderableList;
		private int m_LastSelectedIndex;

		private GUIContent m_IconToolbarMinus;
		private Vector2 m_IconToolbarMinusSize;

		private int m_ToBeRemovedIndex = -1;

		private readonly Dictionary<string, State> m_States = new Dictionary<string, State>();

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			RestoreState(property);

			return m_ReorderableList == null ? 0f : m_ReorderableList.GetHeight();
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			m_Prop = property;
			m_Text = label.text;

			State state = RestoreState(property);

			OnGUI(position);

			if (m_ToBeRemovedIndex > -1)
			{
				m_ReorderableList.index = m_ToBeRemovedIndex;
				ReorderableList.defaultBehaviours.DoRemoveButton(m_ReorderableList);
			}

			m_ToBeRemovedIndex = -1;

			state.lastSelectedIndex = m_LastSelectedIndex;
		}

		private void OnGUI(Rect position)
		{
			if (m_ListenersArray == null || !m_ListenersArray.isArray)
				return;

			m_DummyEvent = GetDummyEvent(m_Prop);
			if (m_DummyEvent == null)
				return;

			if (m_ReorderableList != null)
			{
				var oldIdentLevel = EditorGUI.indentLevel;
				EditorGUI.indentLevel = 0;
				m_ReorderableList.DoList(position);
				EditorGUI.indentLevel = oldIdentLevel;
			}
		}

		private State RestoreState(SerializedProperty property)
		{
			State state = GetState(property);

			m_ListenersArray = state.m_ReorderableList.serializedProperty;
			m_ReorderableList = state.m_ReorderableList;
			m_LastSelectedIndex = Mathf.Clamp(state.lastSelectedIndex, 0, m_ListenersArray.arraySize);
			m_ReorderableList.index = m_LastSelectedIndex;

			return state;
		}

		private State GetState(SerializedProperty property)
		{
			string key = property.propertyPath;
			State state = null;
			if (m_States.ContainsKey(key))
				state = m_States[key];
			// ensure the cached SerializedProperty is synchronized (case 974069)
			if (state == null || state.m_ReorderableList.serializedProperty.serializedObject != property.serializedObject)
			{
				if (state == null)
					state = new State();

				SerializedProperty listenersArray = property.FindPropertyRelative("m_PersistentCalls.m_Calls");
				state.m_ReorderableList =
					new ReorderableList(property.serializedObject, listenersArray, true, true, true, true)
					{
						drawHeaderCallback = DrawEventHeader,
						drawElementCallback = DrawEventListener,
						elementHeightCallback = GetElementHeight,
						onSelectCallback = OnSelectEvent,
						onReorderCallback = OnReorderEvent,
						onAddCallback = OnAddEvent,
						onRemoveCallback = OnRemoveEvent
					};

				SetupReorderableList(state.m_ReorderableList);

				m_States[key] = state;
			}

			m_IconToolbarMinus = new GUIContent(EditorGUIUtility.IconContent("Toolbar Minus"));
			m_IconToolbarMinus.tooltip = "Remove this item.";

			m_IconToolbarMinusSize = GUIStyle.none.CalcSize(m_IconToolbarMinus);

			return state;
		}

		private void DrawEventHeader(Rect rect)
		{
			string label = (string.IsNullOrEmpty(m_Text) ? "Event" : m_Text) + GetEventParams(m_DummyEvent);

			rect.width -= kButtonWidth;
			rect.height = EditorGUIUtility.singleLineHeight;

			Rect trashButtonRect = new Rect(rect.x + rect.width, rect.y, kButtonWidth, rect.height);
			GUIContent trashButton = EditorGUIUtility.IconContent("TreeEditor.Trash");
			trashButton.tooltip = "Delete all events";

			var tooltipAttr = GetAttribute<TooltipAttribute>(fieldInfo);

			EditorGUI.LabelField(rect, EditorGUIUtility.TrTextContent(label, tooltipAttr != null ? tooltipAttr.tooltip : ""));
			if (GUI.Button(trashButtonRect, trashButton, ReorderableList.defaultBehaviours.preButton))
				RemoveAllEvents();
		}

		private void DrawEventListener(Rect rect, int index, bool isActive, bool isFocused)
		{
			var pListener = m_ListenersArray.GetArrayElementAtIndex(index);

			rect.y++;

			Rect[] subRects = GetRowRects(rect);

			Rect enabledRect = subRects[0];
			Rect gameObjectRect = subRects[1];
			Rect functionRect = subRects[2];
			Rect iconToolbarMinusRect = subRects[3];

			// find the current event target...
			var callState = pListener.FindPropertyRelative(kCallStatePath);
			var listenerTarget = pListener.FindPropertyRelative(kInstancePath);
			var isStaticForInstance = pListener.FindPropertyRelative(kIsStaticForInstancePath);
			var methodName = pListener.FindPropertyRelative(kMethodNamePath);
			var arguments = pListener.FindPropertyRelative(kArgumentsPath);
			var modes = pListener.FindPropertyRelative(kModesPath);

			Color c = GUI.backgroundColor;
			GUI.backgroundColor = Color.white;

			EditorGUI.PropertyField(enabledRect, callState, GUIContent.none);

			EditorGUI.BeginChangeCheck();
			{
				GUI.Box(gameObjectRect, GUIContent.none);
				EditorGUI.PropertyField(gameObjectRect, listenerTarget, GUIContent.none);
				if (EditorGUI.EndChangeCheck())
				{
					methodName.stringValue = null;
					arguments.arraySize = 0;
					modes.arraySize = 0;
				}
			}

			BuildArgumentsField(rect, pListener);

			var desiredTypes = new Type[arguments.arraySize];

			for (int i = 0; i < arguments.arraySize; i++)
			{
				var argument = arguments.GetArrayElementAtIndex(i);
				var objArgument = argument.FindPropertyRelative(kObjectArgumentAssemblyTypeName);

				if (objArgument != null)
				{
					var desiredArgTypeName = objArgument.stringValue;
					if (!string.IsNullOrEmpty(desiredArgTypeName))
						desiredTypes[i] = Type.GetType(desiredArgTypeName, false) ?? typeof(Object);
				}
				else
				{
					desiredTypes[i] = typeof(Object);
				}
			}

			using (new EditorGUI.DisabledScope(listenerTarget.objectReferenceValue == null))
			{
				EditorGUI.BeginProperty(functionRect, GUIContent.none, methodName);
				{
					GUIContent buttonContent;
					if (EditorGUI.showMixedValue)
					{
						buttonContent = EditorGUIUtility.TrTextContent("\u2014", "Mixed Values");
					}
					else
					{
						var buttonLabel = new StringBuilder();
						if (listenerTarget.objectReferenceValue == null || string.IsNullOrEmpty(methodName.stringValue))
						{
							buttonLabel.Append(kNoFunctionString);
						}
						else if (!IsListenerValid(listenerTarget.objectReferenceValue, methodName.stringValue, desiredTypes, isStaticForInstance.boolValue))
						{
							var instanceString = "UnknownComponent";
							var instance = listenerTarget.objectReferenceValue;
							if (instance != null)
								instanceString = instance.GetType().Name;

							buttonLabel.Append(string.Format("<Missing {0}.{1}>", instanceString, methodName.stringValue));
						}
						else
						{
							buttonLabel.Append(isStaticForInstance.boolValue ? "static " : string.Empty);
							buttonLabel.Append(listenerTarget.objectReferenceValue.GetType().Name);

							if (!string.IsNullOrEmpty(methodName.stringValue))
							{
								buttonLabel.Append(".");
								if (methodName.stringValue.StartsWith("set_"))
									buttonLabel.Append(methodName.stringValue.Substring(4));
								else
									buttonLabel.Append(methodName.stringValue);
							}
						}
						buttonContent = EditorGUIUtility.TrTextContent(buttonLabel.ToString());
					}

					if (GUI.Button(functionRect, buttonContent, EditorStyles.popup))
					{
						// figure out the signature of this delegate...
						// The property at this stage points to the 'container' and has the field name
						Type delegateType = m_DummyEvent.GetType();

						// check out the signature of invoke as this is the callback!
						MethodInfo delegateMethod = delegateType.GetMethod("Invoke");
						var delegateArgumentsTypes = delegateMethod.GetParameters().Select(x => x.ParameterType).ToArray();

						BuildPopupList(listenerTarget.objectReferenceValue, pListener, delegateArgumentsTypes).DropDown(functionRect);
					}
				}
				EditorGUI.EndProperty();
			}

			if (GUI.Button(iconToolbarMinusRect, m_IconToolbarMinus, GUIStyle.none))
				m_ToBeRemovedIndex = index;

			GUI.backgroundColor = c;
		}

		private float GetElementHeight(int index)
		{
			var pListener = m_ListenersArray.GetArrayElementAtIndex(index);
			var arguments = pListener.FindPropertyRelative(kArgumentsPath);
			var modes = pListener.FindPropertyRelative(kModesPath);

			var height = m_ReorderableList.elementHeight;
			var arrayArgumentsHeight = 0f;
			for (int i = 0; i < modes.arraySize; i++)
			{
				if (modes.GetArrayElementAtIndex(i).enumValueIndex == (int)PersistentListenerMode2.Array)
				{
					var argument = arguments.GetArrayElementAtIndex(i);

					if (argument.FindPropertyRelative(kIsDynamicArgument).boolValue)
						continue;

					var assembly = argument.FindPropertyRelative(kObjectArgumentAssemblyTypeName);

					var desiredArgTypeName = assembly == null ? string.Empty : assembly.stringValue;
					var desiredType = typeof(List<Object>);

					if (!string.IsNullOrEmpty(desiredArgTypeName))
						desiredType = Type.GetType(desiredArgTypeName, false) ?? typeof(List<Object>);

					argument = GetArrayPropertyFromType(argument, desiredType);

					if (argument.isExpanded && argument.arraySize > 0)
						arrayArgumentsHeight += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * argument.arraySize;
				}
			}

			if (arguments.arraySize > 0)
			{
				height += EditorGUIUtility.singleLineHeight;

				if (arguments.arraySize > 1)
				{
					var spacing = kExtraSpacing + (arguments.arraySize - 1) * 2;

					height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.singleLineHeight * (arguments.arraySize + 1) + EditorGUIUtility.standardVerticalSpacing + spacing;
				}

				for (int i = 0; i < arguments.arraySize; i++)
				{
					var argument = arguments.GetArrayElementAtIndex(i);

					if (argument.FindPropertyRelative(kIsDynamicArgument).boolValue)
					{
						var dynamicTarget = argument.FindPropertyRelative(kInstancePath);
						var dynamicIsStaticForInstance = argument.FindPropertyRelative(kIsStaticForInstancePath);
						var dynamicMethodName = argument.FindPropertyRelative(kMethodNamePath);
						var dynamicArguments = argument.FindPropertyRelative(kArgumentsPath);
						var dynamicModes = argument.FindPropertyRelative(kModesPath);

						height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * (dynamicArguments.arraySize + 1);

						if (dynamicTarget.objectReferenceValue == null
							|| string.IsNullOrEmpty(dynamicMethodName.stringValue))
							continue;

						var methodInfo = GetMethodInfo(dynamicTarget, dynamicMethodName, dynamicArguments, dynamicIsStaticForInstance);

						if (methodInfo == null)
							continue;

						var parameters = methodInfo.GetParameters();

						for (int j = 0; j < dynamicArguments.arraySize; j++)
						{
							var modeEnum = GetMode(dynamicModes.GetArrayElementAtIndex(j));

							if (modeEnum == PersistentListenerMode2.Array)
							{
								var dynamicArgument = GetArrayPropertyFromType(dynamicArguments.GetArrayElementAtIndex(j), parameters[j].ParameterType);

								if (dynamicArgument.isExpanded)
								{
									height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * dynamicArgument.arraySize;
								}
							}
						}
					}
				}
			}

			return height + arrayArgumentsHeight;
		}

		private void OnSelectEvent(ReorderableList list)
		{
			m_LastSelectedIndex = list.index;
		}

		private void OnReorderEvent(ReorderableList list)
		{
			if (m_LastSelectedIndex != list.index)
			{
				int from = m_LastSelectedIndex;
				int to = list.index;

				if (from < to)
				{
					while (from < to)
					{
						m_ListenersArray.MoveArrayElement(from, from++);
					}
				}
				else
				{
					while (from > to)
					{
						m_ListenersArray.MoveArrayElement(from, from--);
					}
				}
			}

			m_LastSelectedIndex = list.index;
		}

		private void OnAddEvent(ReorderableList list)
		{
			if (m_ListenersArray.hasMultipleDifferentValues)
			{
				//When increasing a multi-selection array using Serialized Property
				//Data can be overwritten if there is mixed values.
				//The Serialization system applies the Serialized data of one object, to all other objects in the selection.
				//We handle this case here, by creating a SerializedObject for each object.
				//Case 639025.
				foreach (var targetObject in m_ListenersArray.serializedObject.targetObjects)
				{
					var tempSerializedObject = new SerializedObject(targetObject);
					var listenerArrayProperty = tempSerializedObject.FindProperty(m_ListenersArray.propertyPath);
					listenerArrayProperty.arraySize += 1;
					tempSerializedObject.ApplyModifiedProperties();
				}
				m_ListenersArray.serializedObject.SetIsDifferentCacheDirty();
				m_ListenersArray.serializedObject.Update();
				list.index = list.serializedProperty.arraySize - 1;
			}
			else
			{
				ReorderableList.defaultBehaviours.DoAddButton(list);
			}

			m_LastSelectedIndex = list.index;

			var pListener = m_ListenersArray.GetArrayElementAtIndex(list.index);

			var callState = pListener.FindPropertyRelative(kCallStatePath);
			var listenerTarget = pListener.FindPropertyRelative(kInstancePath);
			var isStaticForInstance = pListener.FindPropertyRelative(kIsStaticForInstancePath);
			var methodName = pListener.FindPropertyRelative(kMethodNamePath);
			var modes = pListener.FindPropertyRelative(kModesPath);
			var arguments = pListener.FindPropertyRelative(kArgumentsPath);

			callState.enumValueIndex = (int)UnityEventCallState.RuntimeOnly;
			listenerTarget.objectReferenceValue = null;
			isStaticForInstance.boolValue = false;
			methodName.stringValue = null;
			modes.arraySize = 0;
			arguments.arraySize = 0;
		}

		private void OnRemoveEvent(ReorderableList list)
		{
			ReorderableList.defaultBehaviours.DoRemoveButton(list);
			m_LastSelectedIndex = list.index;
		}

		private void RemoveAllEvents()
		{
			if (m_ListenersArray == null || !m_ListenersArray.isArray)
				return;

			m_ListenersArray.arraySize = 0;
		}

		private UnityEventBase2 GetDummyEvent(SerializedProperty prop)
		{
			//Use the SerializedProperty path to iterate through the fields of the inspected targetObject
			Object tgtobj = prop.serializedObject.targetObject;
			if (tgtobj == null)
				return new UnityEvent2();

			string propPath = prop.propertyPath;
			Type ft = tgtobj.GetType();
			while (propPath.Length != 0)
			{
				//we could have a leftover '.' if the previous iteration handled an array element
				if (propPath.StartsWith("."))
					propPath = propPath.Substring(1);

				var splits = propPath.Split(new[] { '.' }, 2);
				var newField = ft.GetField(splits[0], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (newField == null)
					break;

				ft = newField.FieldType;

				if (ft.IsArray)
					ft = ft.GetElementType();

				if (ft.IsGenericType && typeof(List<>) == ft.GetGenericTypeDefinition())
					ft = ft.GetGenericArguments()[0];

				//the last item in the property path could have been an array element
				//bail early in that case
				if (splits.Length == 1)
					break;

				propPath = splits[1];
				if (propPath.StartsWith("Array.data["))
					propPath = propPath.Split(new[] { ']' }, 2)[1];
			}

			if (ft.IsSubclassOf(typeof(UnityEventBase2)))
				return Activator.CreateInstance(ft) as UnityEventBase2;

			return new UnityEvent2();
		}

		internal string GetEventParams(UnityEventBase2 evt)
		{
			var methodInfo = evt.FindMethod(evt, "Invoke", PersistentListenerMode2.EventDefined);
			var types = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray();

			var sb = new StringBuilder();

			sb.Append(" (");
			for (int i = 0; i < types.Length; i++)
			{
				sb.Append(GetTypeName(types[i]));
				if (i < types.Length - 1)
				{
					sb.Append(", ");
				}
			}
			sb.Append(")");

			return sb.ToString();
		}

		private void SetupReorderableList(ReorderableList list)
		{
			// Two standard lines with standard spacing between and extra spacing below to better separate items visually.
			list.elementHeight = EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing + kExtraSpacing;
		}

		private Rect[] GetRowRects(Rect rect)
		{
			rect.height = EditorGUIUtility.singleLineHeight;
			rect.y += 2;

			Rect enabledRect = rect;
			enabledRect.width *= 0.3f;

			Rect gameObjectRect = enabledRect;
			gameObjectRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			Rect functionRect = rect;
			functionRect.xMin = gameObjectRect.xMax + kSpacing;
			functionRect.width -= m_IconToolbarMinusSize.x + kSpacing;

			Rect iconToolbarMinusRect = functionRect;
			iconToolbarMinusRect.xMin = functionRect.xMax + kSpacing;
			iconToolbarMinusRect.width = m_IconToolbarMinusSize.x;

			Rect argNameRect = gameObjectRect;
			argNameRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			argNameRect.width -= kButtonWidth + kSpacing;

			Rect dynamicRect = argNameRect;
			dynamicRect.xMin = argNameRect.xMax + kSpacing;
			dynamicRect.width = kButtonWidth;

			Rect argRect = rect;
			argRect.xMin = gameObjectRect.xMax + kSpacing;
			argRect.y += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;

			return new Rect[]
			{
				enabledRect,
				gameObjectRect,
				functionRect,
				iconToolbarMinusRect,
				argNameRect,
				dynamicRect,
				argRect
			};
		}

		private void BuildArgumentsField(Rect rect, SerializedProperty listener)
		{
			// figure out the signature of this delegate...
			// The property at this stage points to the 'container' and has the field name
			Type delegateType = m_DummyEvent.GetType();

			// check out the signature of invoke as this is the callback!
			MethodInfo delegateMethod = delegateType.GetMethod("Invoke");
			var delegateArgumentsTypes = delegateMethod.GetParameters().Select(x => x.ParameterType).ToArray();

			Rect[] subRects = GetRowRects(rect);

			Rect argNameRect = subRects[4];
			Rect dynamicCheckboxRect = subRects[5];
			Rect argRect = subRects[6];

			var listenerTarget = listener.FindPropertyRelative(kInstancePath);
			var isStaticForInstance = listener.FindPropertyRelative(kIsStaticForInstancePath);
			var methodName = listener.FindPropertyRelative(kMethodNamePath);
			var arguments = listener.FindPropertyRelative(kArgumentsPath);
			var modes = listener.FindPropertyRelative(kModesPath);

			if (arguments.arraySize == 0
				|| modes.arraySize == 0
				|| (modes.arraySize == 1 && GetMode(modes.GetArrayElementAtIndex(0)) == PersistentListenerMode2.Void))
				return;

			MethodInfo method = null;
			if (listenerTarget.objectReferenceValue != null)
				method = GetMethodInfo(listenerTarget, methodName, arguments, isStaticForInstance);

			if (method == null) return;

			//var customInspectorAttr = GetAttribute<CustomInspectorAttribute>(method);

			//if (customInspectorAttr != null)
			//{
			//	var methodBuilder = UnityEventBase2.GetValidMethodInfo(listenerTarget.objectReferenceValue, customInspectorAttr.methodName, new Type[] { typeof(SerializedProperty), typeof(Rect), typeof(Rect) });

			//	if (methodBuilder != null)
			//	{
			//		methodBuilder.Invoke(listenerTarget.objectReferenceValue, new object[] { arguments, argNameRect, argRect });

			//		return;
			//	}
			//}

			ParameterInfo[] parameters = method.GetParameters();

			var hasMethodLayerAttr = GetAttribute<LayerAttribute>(method) != null;
			var methodSliderAttr = GetAttribute<SliderAttribute>(method);
			var methodIntSliderAttr = GetAttribute<IntSliderAttribute>(method);
			var hasMethodTagAttr = GetAttribute<TagAttribute>(method) != null;

			for (int i = 0; i < arguments.arraySize; i++)
			{
				var argument = arguments.GetArrayElementAtIndex(i);
				var modeEnum = GetMode(modes.GetArrayElementAtIndex(i));
				var assembly = argument.FindPropertyRelative(kObjectArgumentAssemblyTypeName);
				var isDynamicArgument = argument.FindPropertyRelative(kIsDynamicArgument);

				//only allow argument if we have a valid target / method
				if (listenerTarget.objectReferenceValue == null || string.IsNullOrEmpty(methodName.stringValue))
					modeEnum = PersistentListenerMode2.Void;

				//var attrs = parameters[i].GetCustomAttributes(false);
				//foreach (var attr in attrs)
				//{
				//if (attr is CustomInspectorAttribute customInspAttr)
				//	customInspectorAttr = customInspAttr;
				//}

				if (i < parameters.Length)
				{
					EditorGUI.LabelField(argNameRect, EditorGUIUtility.TrTextContent(string.Format("{0}:", parameters[i].Name)));

					if (modeEnum != PersistentListenerMode2.EventDefined)
						EditorGUI.PropertyField(dynamicCheckboxRect, isDynamicArgument, GUIContent.none);
				}

				if (isDynamicArgument.boolValue)
				{
					var height = BuildDynamicArgumentField(argument, argRect);

					argNameRect.y += height;
					argRect.y += height;
					dynamicCheckboxRect.y += height;

					continue;
				}

				argument.FindPropertyRelative(kInstancePath).objectReferenceValue = null;
				argument.FindPropertyRelative(kMethodNamePath).stringValue = null;
				argument.FindPropertyRelative(kIsStaticForInstancePath).boolValue = false;
				argument.FindPropertyRelative(kArgumentsPath).arraySize = 0;
				argument.FindPropertyRelative(kModesPath).arraySize = 0;

				var hasParamLayerAttr = parameters[i].IsDefined(typeof(LayerAttribute), false);
				var paramSliderAttr = parameters[i].GetCustomAttribute<SliderAttribute>();
				var paramIntSliderAttr = parameters[i].GetCustomAttribute<IntSliderAttribute>();
				var hasParamTagAttr = parameters[i].IsDefined(typeof(TagAttribute), false);

				switch (modeEnum)
				{
					case PersistentListenerMode2.Object:
						argument = argument.FindPropertyRelative(kObjectArgument);
						break;
					case PersistentListenerMode2.Char:
						argument = argument.FindPropertyRelative(kCharArgument);
						break;
					case PersistentListenerMode2.Byte:
						argument = argument.FindPropertyRelative(kByteArgument);
						break;
					case PersistentListenerMode2.SByte:
						argument = argument.FindPropertyRelative(kSByteArgument);
						break;
					case PersistentListenerMode2.Short:
						argument = argument.FindPropertyRelative(kShortArgument);
						break;
					case PersistentListenerMode2.UShort:
						argument = argument.FindPropertyRelative(kUShortArgument);
						break;
					case PersistentListenerMode2.Int:
						argument = argument.FindPropertyRelative(kIntArgument);
						break;
					case PersistentListenerMode2.UInt:
						argument = argument.FindPropertyRelative(kUIntArgument);
						break;
					case PersistentListenerMode2.Long:
						argument = argument.FindPropertyRelative(kLongArgument);
						break;
					case PersistentListenerMode2.Float:
						argument = argument.FindPropertyRelative(kFloatArgument);
						break;
					case PersistentListenerMode2.Double:
						argument = argument.FindPropertyRelative(kDoubleArgument);
						break;
					case PersistentListenerMode2.String:
						argument = argument.FindPropertyRelative(kStringArgument);
						break;
					case PersistentListenerMode2.Bool:
						argument = argument.FindPropertyRelative(kBoolArgument);
						break;
					case PersistentListenerMode2.Vector2:
						argument = argument.FindPropertyRelative(kVector2Argument);
						break;
					case PersistentListenerMode2.Vector2Int:
						argument = argument.FindPropertyRelative(kVector2IntArgument);
						break;
					case PersistentListenerMode2.Vector3:
						argument = argument.FindPropertyRelative(kVector3Argument);
						break;
					case PersistentListenerMode2.Vector3Int:
						argument = argument.FindPropertyRelative(kVector3IntArgument);
						break;
					case PersistentListenerMode2.Vector4:
						argument = argument.FindPropertyRelative(kVector4Argument);
						break;
					case PersistentListenerMode2.LayerMask:
						argument = argument.FindPropertyRelative(kLayerMaskArgument);
						break;
					case PersistentListenerMode2.Color:
						argument = argument.FindPropertyRelative(kColorArgument);
						break;
					case PersistentListenerMode2.Quaternion:
						argument = argument.FindPropertyRelative(kQuaternionArgument);
						break;
					case PersistentListenerMode2.Array:
						var desiredArgTypeName = assembly == null ? string.Empty : assembly.stringValue;
						var desiredType = typeof(List<Object>);

						if (!string.IsNullOrEmpty(desiredArgTypeName))
							desiredType = Type.GetType(desiredArgTypeName, false) ?? typeof(List<Object>);

						argument = GetArrayPropertyFromType(argument, desiredType);

						break;
					default:
						argument = argument.FindPropertyRelative(kIntArgument);
						break;
				}

				if (argument == null)
					continue;

				//if (customInspectorAttr != null)
				//{
				//	var methodBuilder = UnityEventBase2.GetValidMethodInfo(listenerTarget.objectReferenceValue, customInspectorAttr.methodName, new Type[] { typeof(SerializedProperty), typeof(Rect), typeof(Rect) });

				//	methodBuilder?.Invoke(listenerTarget.objectReferenceValue, new object[] { argument, argNameRect, argRect });
				//}
				//else
				//{
				//}

				if (modeEnum == PersistentListenerMode2.EventDefined)
				{
					if (i < delegateArgumentsTypes.Length)
					{
						var type = Type.GetType(assembly.stringValue, false) ?? typeof(Object);

						if (type == delegateArgumentsTypes[i])
							EditorGUI.LabelField(argRect, "Dynamic " + GetTypeName(type));
					}
				}
				else if (modeEnum == PersistentListenerMode2.Enum)
				{
					Type enumType = Type.GetType(assembly.stringValue, false);
					string[] names = Enum.GetNames(enumType);

					argument.intValue = EditorGUI.Popup(argRect, argument.intValue, names);
				}
				else if (modeEnum == PersistentListenerMode2.Char)
				{
					Rect fieldRect = argRect;

					fieldRect.width -= kButtonWidth;

					EditorGUI.PropertyField(fieldRect, argument, GUIContent.none);

					Rect resetButtonRect = new Rect(fieldRect.x + fieldRect.width, fieldRect.y, kButtonWidth, fieldRect.height);
					GUIContent resetButton = EditorGUIUtility.IconContent("TreeEditor.Trash");
					resetButton.tooltip = "Delete char value";

					if (GUI.Button(resetButtonRect, resetButton, ReorderableList.defaultBehaviours.preButton))
					{
						argument.intValue = char.MinValue;
						argument.boolValue = false;
					}
				}
				else if (modeEnum == PersistentListenerMode2.Int)
				{
					if (hasMethodLayerAttr || hasParamLayerAttr)
						argument.intValue = EditorGUI.LayerField(argRect, argument.intValue);
					else if (paramIntSliderAttr != null)
						EditorGUI.IntSlider(argRect, argument, paramIntSliderAttr.minValue, paramIntSliderAttr.maxValue, GUIContent.none);
					else if (methodIntSliderAttr != null)
						EditorGUI.IntSlider(argRect, argument, methodIntSliderAttr.minValue, methodIntSliderAttr.maxValue, GUIContent.none);
					else
						EditorGUI.PropertyField(argRect, argument, GUIContent.none);
				}
				else if (modeEnum == PersistentListenerMode2.Float)
				{
					if (paramSliderAttr != null)
						EditorGUI.Slider(argRect, argument, paramSliderAttr.minValue, paramSliderAttr.maxValue, GUIContent.none);
					else if (methodSliderAttr != null)
						EditorGUI.Slider(argRect, argument, methodSliderAttr.minValue, methodSliderAttr.maxValue, GUIContent.none);
					else
						EditorGUI.PropertyField(argRect, argument, GUIContent.none);
				}
				else if (modeEnum == PersistentListenerMode2.String)
				{
					if (hasMethodTagAttr || hasParamTagAttr)
						argument.stringValue = EditorGUI.TagField(argRect, GUIContent.none, argument.stringValue);
					else
						EditorGUI.PropertyField(argRect, argument, GUIContent.none);
				}
				else if (modeEnum == PersistentListenerMode2.Vector2)
				{
					EditorGUI.BeginChangeCheck();
					var result = EditorGUI.Vector2Field(argRect, GUIContent.none, argument.vector2Value);
					if (EditorGUI.EndChangeCheck())
						argument.vector2Value = result;
				}
				else if (modeEnum == PersistentListenerMode2.Vector2Int)
				{
					EditorGUI.BeginChangeCheck();
					var result = EditorGUI.Vector2IntField(argRect, GUIContent.none, argument.vector2IntValue);
					if (EditorGUI.EndChangeCheck())
						argument.vector2IntValue = result;
				}
				else if (modeEnum == PersistentListenerMode2.Vector3)
				{
					EditorGUI.BeginChangeCheck();
					var result = EditorGUI.Vector3Field(argRect, GUIContent.none, argument.vector3Value);
					if (EditorGUI.EndChangeCheck())
						argument.vector3Value = result;
				}
				else if (modeEnum == PersistentListenerMode2.Vector3Int)
				{
					EditorGUI.BeginChangeCheck();
					var result = EditorGUI.Vector3IntField(argRect, GUIContent.none, argument.vector3IntValue);
					if (EditorGUI.EndChangeCheck())
						argument.vector3IntValue = result;
				}
				else if (modeEnum == PersistentListenerMode2.Vector4)
				{
					EditorGUI.BeginChangeCheck();
					var result = EditorGUI.Vector4Field(argRect, GUIContent.none, argument.vector4Value);
					if (EditorGUI.EndChangeCheck())
						argument.vector4Value = result;
				}
				else if (modeEnum == PersistentListenerMode2.Quaternion)
				{
					EditorGUI.BeginChangeCheck();
					var result = EditorGUI.Vector4Field(argRect, GUIContent.none, new Vector4(argument.quaternionValue.x, argument.quaternionValue.y, argument.quaternionValue.z, argument.quaternionValue.w));
					if (EditorGUI.EndChangeCheck())
						argument.quaternionValue = new Quaternion(result.x, result.y, result.z, result.w);
				}
				else if (modeEnum == PersistentListenerMode2.Array)
				{
					var desiredArgTypeName = assembly == null ? string.Empty : assembly.stringValue;
					var desiredType = typeof(List<Object>);

					if (!string.IsNullOrEmpty(desiredArgTypeName))
						desiredType = Type.GetType(desiredArgTypeName, false) ?? typeof(List<Object>);

					var foldoutArgRect = argRect;
#if !UNITY_2022_2_OR_NEWER
					foldoutArgRect.x += 10f;
#endif
					var arraySizeArgRect = argRect;
					arraySizeArgRect.x += 15f;
					arraySizeArgRect.width -= 15f;

					var itemArgRect = arraySizeArgRect;
					itemArgRect.x += 10f;
					itemArgRect.width -= 10f;
					itemArgRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

					argument.isExpanded = EditorGUI.Foldout(foldoutArgRect, argument.isExpanded, GUIContent.none);

					argument.arraySize = EditorGUI.IntField(arraySizeArgRect, argument.arraySize);

					if (argument.isExpanded)
					{
						for (int j = 0; j < argument.arraySize; j++)
						{
							if (desiredType.IsArray && desiredType.GetElementType().IsEnum)
							{
								string[] names = Enum.GetNames(desiredType.GetElementType());

								argument.GetArrayElementAtIndex(j).intValue = EditorGUI.Popup(itemArgRect, argument.GetArrayElementAtIndex(j).intValue, names);
							}
							else if (desiredType.GetGenericArguments().Length > 0 && desiredType.GetGenericArguments()[0].IsEnum)
							{
								string[] names = Enum.GetNames(desiredType.GetGenericArguments()[0]);

								argument.GetArrayElementAtIndex(j).intValue = EditorGUI.Popup(itemArgRect, argument.GetArrayElementAtIndex(j).intValue, names);
							}
							else if (desiredType == typeof(List<byte>) || desiredType == typeof(byte[])
								|| desiredType == typeof(List<sbyte>) || desiredType == typeof(sbyte[])
								|| desiredType == typeof(List<short>) || desiredType == typeof(short[])
								|| desiredType == typeof(List<ushort>) || desiredType == typeof(ushort[])
								|| desiredType == typeof(List<int>) || desiredType == typeof(int[])
								|| desiredType == typeof(List<uint>) || desiredType == typeof(uint[])
								|| desiredType == typeof(List<long>) || desiredType == typeof(long[])
								|| desiredType == typeof(List<float>) || desiredType == typeof(float[])
								|| desiredType == typeof(List<double>) || desiredType == typeof(double[])
								|| desiredType == typeof(List<string>) || desiredType == typeof(string[])
								|| desiredType == typeof(List<bool>) || desiredType == typeof(bool[])
								|| desiredType == typeof(List<Vector2>) || desiredType == typeof(Vector2[])
								|| desiredType == typeof(List<Vector3>) || desiredType == typeof(Vector3[])
								|| desiredType == typeof(List<Vector2Int>) || desiredType == typeof(Vector2Int[])
								|| desiredType == typeof(List<Vector3Int>) || desiredType == typeof(Vector3Int[])
								|| desiredType == typeof(List<LayerMask>) || desiredType == typeof(LayerMask[])
								|| desiredType == typeof(List<Color>) || desiredType == typeof(Color[]))
							{
								EditorGUI.PropertyField(itemArgRect, argument.GetArrayElementAtIndex(j), GUIContent.none);
							}
							else if (desiredType == typeof(List<char>) || desiredType == typeof(char[]))
							{
								Rect fieldRect = itemArgRect;

								fieldRect.width -= kButtonWidth;

								EditorGUI.PropertyField(fieldRect, argument.GetArrayElementAtIndex(j), GUIContent.none);

								Rect resetButtonRect = new Rect(fieldRect.x + fieldRect.width, fieldRect.y, kButtonWidth, fieldRect.height);
								GUIContent resetButton = EditorGUIUtility.IconContent("TreeEditor.Trash");
								resetButton.tooltip = "Delete char value";

								if (GUI.Button(resetButtonRect, resetButton, ReorderableList.defaultBehaviours.preButton))
									argument.GetArrayElementAtIndex(j).intValue = char.MinValue;
							}
							else if (desiredType == typeof(List<Vector4>) || desiredType == typeof(Vector4[]))
							{
								EditorGUI.BeginChangeCheck();
								var result = EditorGUI.Vector4Field(itemArgRect, GUIContent.none, argument.GetArrayElementAtIndex(j).vector4Value);
								if (EditorGUI.EndChangeCheck())
									argument.GetArrayElementAtIndex(j).vector4Value = result;
							}
							else if (desiredType == typeof(List<Quaternion>) || desiredType == typeof(Quaternion[]))
							{
								var quaternionValue = argument.GetArrayElementAtIndex(j).quaternionValue;
								EditorGUI.BeginChangeCheck();
								var result = EditorGUI.Vector4Field(itemArgRect, GUIContent.none, new Vector4(quaternionValue.x, quaternionValue.y, quaternionValue.z, quaternionValue.w));
								if (EditorGUI.EndChangeCheck())
									argument.GetArrayElementAtIndex(j).quaternionValue = new Quaternion(result.x, result.y, result.z, result.w);
							}
							else if (IsValidListType(desiredType) || typeof(Object[]).IsAssignableFrom(desiredType))
							{
								Type type = desiredType.IsArray ? desiredType.GetElementType() : desiredType.GetGenericArguments()[0];

								EditorGUI.BeginChangeCheck();
								var result = EditorGUI.ObjectField(itemArgRect, GUIContent.none, argument.GetArrayElementAtIndex(j).objectReferenceValue, type, true);
								if (EditorGUI.EndChangeCheck())
									argument.GetArrayElementAtIndex(j).objectReferenceValue = result;
							}

							itemArgRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
							argNameRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
							dynamicCheckboxRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
							argRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
						}
					}
				}
				else if (modeEnum == PersistentListenerMode2.Object)
				{
					var desiredArgTypeName = assembly == null ? string.Empty : assembly.stringValue;
					var desiredType = typeof(Object);

					if (!string.IsNullOrEmpty(desiredArgTypeName))
						desiredType = Type.GetType(desiredArgTypeName, false) ?? typeof(Object);

					EditorGUI.BeginChangeCheck();
					var result = EditorGUI.ObjectField(argRect, GUIContent.none, argument.objectReferenceValue, desiredType, true);
					if (EditorGUI.EndChangeCheck())
						argument.objectReferenceValue = result;
				}
				else if (modeEnum != PersistentListenerMode2.Void)
					EditorGUI.PropertyField(argRect, argument, GUIContent.none);

				//customInspectorAttr = null;

				argNameRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
				argRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
				dynamicCheckboxRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			}
		}

		private float BuildDynamicArgumentField(SerializedProperty argument, Rect dynamicTargetRect)
		{
			float height = (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2f;

			var dynamicArgumentTarget = argument.FindPropertyRelative(kInstancePath);
			var isStaticForInstance = argument.FindPropertyRelative(kIsStaticForInstancePath);
			var methodName = argument.FindPropertyRelative(kMethodNamePath);
			var arguments = argument.FindPropertyRelative(kArgumentsPath);
			var modes = argument.FindPropertyRelative(kModesPath);

			EditorGUI.BeginChangeCheck();
			var result = EditorGUI.ObjectField(dynamicTargetRect, GUIContent.none, dynamicArgumentTarget.objectReferenceValue, typeof(Object), true);
			if (EditorGUI.EndChangeCheck())
			{
				dynamicArgumentTarget.objectReferenceValue = result;
				isStaticForInstance.boolValue = false;
				methodName.stringValue = null;
				arguments.arraySize = 0;
				modes.arraySize = 0;
			}

			Rect functionRect = dynamicTargetRect;
			functionRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			Rect dynamicArgNameRect = functionRect;
			dynamicArgNameRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			dynamicArgNameRect.width = functionRect.width * 0.2f;

			Rect dynamicArgRect = dynamicArgNameRect;
			dynamicArgRect.xMin = dynamicArgNameRect.xMax;
			dynamicArgRect.width = functionRect.width - dynamicArgNameRect.width;

			height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * arguments.arraySize;

			var desiredTypes = new Type[arguments.arraySize];

			MethodInfo method = null;
			if (dynamicArgumentTarget.objectReferenceValue != null)
				method = GetMethodInfo(dynamicArgumentTarget, methodName, arguments, isStaticForInstance);

			if (method != null)
			{
				var hasMethodLayerAttr = GetAttribute<LayerAttribute>(method) != null;
				var methodSliderAttr = GetAttribute<SliderAttribute>(method);
				var methodIntSliderAttr = GetAttribute<IntSliderAttribute>(method);
				var hasMethodTagAttr = GetAttribute<TagAttribute>(method) != null;

				ParameterInfo[] parameters = method.GetParameters();

				for (int i = 0; i < arguments.arraySize; i++)
				{
					var arg = arguments.GetArrayElementAtIndex(i);
					var modeEnum = GetMode(modes.GetArrayElementAtIndex(i));
					var objArgument = arg.FindPropertyRelative(kObjectArgumentAssemblyTypeName);

					if (objArgument != null)
					{
						var desiredArgTypeName = objArgument.stringValue;
						if (!string.IsNullOrEmpty(desiredArgTypeName))
							desiredTypes[i] = Type.GetType(desiredArgTypeName, false) ?? typeof(Object);
					}
					else
					{
						desiredTypes[i] = typeof(Object);
					}

					EditorGUI.LabelField(dynamicArgNameRect, EditorGUIUtility.TrTextContent(string.Format("{0}:", parameters[i].Name), parameters[i].Name));

					switch (modeEnum)
					{
						case PersistentListenerMode2.Object:
							arg = arg.FindPropertyRelative(kObjectArgument);

							EditorGUI.BeginChangeCheck();
							var objectResult = EditorGUI.ObjectField(dynamicArgRect, GUIContent.none, arg.objectReferenceValue, desiredTypes[i], true);
							if (EditorGUI.EndChangeCheck())
								arg.objectReferenceValue = objectResult;

							break;
						case PersistentListenerMode2.Char:
							arg = arg.FindPropertyRelative(kCharArgument);
							break;
						case PersistentListenerMode2.Byte:
							arg = arg.FindPropertyRelative(kByteArgument);
							break;
						case PersistentListenerMode2.SByte:
							arg = arg.FindPropertyRelative(kSByteArgument);
							break;
						case PersistentListenerMode2.Short:
							arg = arg.FindPropertyRelative(kShortArgument);
							break;
						case PersistentListenerMode2.UShort:
							arg = arg.FindPropertyRelative(kUShortArgument);
							break;
						case PersistentListenerMode2.Int:
							arg = arg.FindPropertyRelative(kIntArgument);

							var hasParamLayerAttr = parameters[i].GetCustomAttribute<LayerAttribute>() != null;
							var paramIntSliderAttr = parameters[i].GetCustomAttribute<IntSliderAttribute>();

							if (hasMethodLayerAttr || hasParamLayerAttr)
								arg.intValue = EditorGUI.LayerField(dynamicArgRect, GUIContent.none, arg.intValue);
							else if (paramIntSliderAttr != null)
								EditorGUI.IntSlider(dynamicArgRect, arg, paramIntSliderAttr.minValue, paramIntSliderAttr.maxValue, GUIContent.none);
							else if (methodIntSliderAttr != null)
								EditorGUI.IntSlider(dynamicArgRect, arg, methodIntSliderAttr.minValue, methodIntSliderAttr.maxValue, GUIContent.none);
							else
								EditorGUI.PropertyField(dynamicArgRect, arg, GUIContent.none);

							break;
						case PersistentListenerMode2.UInt:
							arg = arg.FindPropertyRelative(kUIntArgument);
							break;
						case PersistentListenerMode2.Long:
							arg = arg.FindPropertyRelative(kLongArgument);
							break;
						case PersistentListenerMode2.Float:
							arg = arg.FindPropertyRelative(kFloatArgument);

							var paramSliderAttr = parameters[i].GetCustomAttribute<SliderAttribute>();

							if (paramSliderAttr != null)
								EditorGUI.Slider(dynamicArgRect, arg, paramSliderAttr.minValue, paramSliderAttr.maxValue, GUIContent.none);
							else if (methodSliderAttr != null)
								EditorGUI.Slider(dynamicArgRect, arg, methodSliderAttr.minValue, methodSliderAttr.maxValue, GUIContent.none);
							else
								EditorGUI.PropertyField(dynamicArgRect, arg, GUIContent.none);

							break;
						case PersistentListenerMode2.Double:
							arg = arg.FindPropertyRelative(kDoubleArgument);
							break;
						case PersistentListenerMode2.String:
							arg = arg.FindPropertyRelative(kStringArgument);

							var hasParamTagAttr = parameters[i].GetCustomAttribute<TagAttribute>() != null;

							if (hasMethodTagAttr || hasParamTagAttr)
								arg.stringValue = EditorGUI.TagField(dynamicArgRect, GUIContent.none, arg.stringValue);
							else
								EditorGUI.PropertyField(dynamicArgRect, arg, GUIContent.none);

							break;
						case PersistentListenerMode2.Bool:
							arg = arg.FindPropertyRelative(kBoolArgument);
							break;
						case PersistentListenerMode2.Enum:
							arg = arg.FindPropertyRelative(kIntArgument);

							string[] enumNames = Enum.GetNames(desiredTypes[i]);

							arg.intValue = EditorGUI.Popup(dynamicArgRect, arg.intValue, enumNames);

							break;
						case PersistentListenerMode2.Vector2:
							arg = arg.FindPropertyRelative(kVector2Argument);
							break;
						case PersistentListenerMode2.Vector2Int:
							arg = arg.FindPropertyRelative(kVector2IntArgument);
							break;
						case PersistentListenerMode2.Vector3:
							arg = arg.FindPropertyRelative(kVector3Argument);
							break;
						case PersistentListenerMode2.Vector3Int:
							arg = arg.FindPropertyRelative(kVector3IntArgument);
							break;
						case PersistentListenerMode2.Vector4:
							arg = arg.FindPropertyRelative(kVector4Argument);

							EditorGUI.BeginChangeCheck();
							var vector4Result = EditorGUI.Vector4Field(dynamicArgRect, GUIContent.none, arg.vector4Value);
							if (EditorGUI.EndChangeCheck())
								arg.vector4Value = vector4Result;

							break;
						case PersistentListenerMode2.LayerMask:
							arg = arg.FindPropertyRelative(kLayerMaskArgument);
							break;
						case PersistentListenerMode2.Color:
							arg = arg.FindPropertyRelative(kColorArgument);
							break;
						case PersistentListenerMode2.Quaternion:
							arg = arg.FindPropertyRelative(kQuaternionArgument);

							EditorGUI.BeginChangeCheck();
							var quaternionResult = EditorGUI.Vector4Field(dynamicArgRect, GUIContent.none, new Vector4(arg.quaternionValue.x, arg.quaternionValue.y, arg.quaternionValue.z, arg.quaternionValue.w));
							if (EditorGUI.EndChangeCheck())
								arg.quaternionValue = new Quaternion(quaternionResult.x, quaternionResult.y, quaternionResult.z, quaternionResult.w);
							break;
						case PersistentListenerMode2.Array:
							var desiredType = desiredTypes[i];

							arg = GetArrayPropertyFromType(arg, desiredType);

							var foldoutArgRect = dynamicArgRect;
#if !UNITY_2022_2_OR_NEWER
							foldoutArgRect.x += 10f;
#endif
							var arraySizeArgRect = dynamicArgRect;
							arraySizeArgRect.x += 15f;
							arraySizeArgRect.width -= 15f;

							var itemArgRect = arraySizeArgRect;
							itemArgRect.x += 10f;
							itemArgRect.width -= 10f;
							itemArgRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

							arg.isExpanded = EditorGUI.Foldout(foldoutArgRect, arg.isExpanded, GUIContent.none);

							arg.arraySize = EditorGUI.IntField(arraySizeArgRect, arg.arraySize);

							if (arg.isExpanded)
							{
								for (int j = 0; j < arg.arraySize; j++)
								{
									if (desiredType.IsArray && desiredType.GetElementType().IsEnum)
									{
										string[] names = Enum.GetNames(desiredType.GetElementType());

										arg.GetArrayElementAtIndex(j).intValue = EditorGUI.Popup(itemArgRect, arg.GetArrayElementAtIndex(j).intValue, names);
									}
									else if (desiredType.GetGenericArguments().Length > 0 && desiredType.GetGenericArguments()[0].IsEnum)
									{
										string[] names = Enum.GetNames(desiredType.GetGenericArguments()[0]);

										arg.GetArrayElementAtIndex(j).intValue = EditorGUI.Popup(itemArgRect, arg.GetArrayElementAtIndex(j).intValue, names);
									}
									else if (desiredType == typeof(List<byte>) || desiredType == typeof(byte[])
										|| desiredType == typeof(List<sbyte>) || desiredType == typeof(sbyte[])
										|| desiredType == typeof(List<short>) || desiredType == typeof(short[])
										|| desiredType == typeof(List<ushort>) || desiredType == typeof(ushort[])
										|| desiredType == typeof(List<int>) || desiredType == typeof(int[])
										|| desiredType == typeof(List<uint>) || desiredType == typeof(uint[])
										|| desiredType == typeof(List<long>) || desiredType == typeof(long[])
										|| desiredType == typeof(List<float>) || desiredType == typeof(float[])
										|| desiredType == typeof(List<double>) || desiredType == typeof(double[])
										|| desiredType == typeof(List<string>) || desiredType == typeof(string[])
										|| desiredType == typeof(List<bool>) || desiredType == typeof(bool[])
										|| desiredType == typeof(List<Vector2>) || desiredType == typeof(Vector2[])
										|| desiredType == typeof(List<Vector3>) || desiredType == typeof(Vector3[])
										|| desiredType == typeof(List<Vector2Int>) || desiredType == typeof(Vector2Int[])
										|| desiredType == typeof(List<Vector3Int>) || desiredType == typeof(Vector3Int[])
										|| desiredType == typeof(List<LayerMask>) || desiredType == typeof(LayerMask[])
										|| desiredType == typeof(List<Color>) || desiredType == typeof(Color[]))
									{
										EditorGUI.PropertyField(itemArgRect, arg.GetArrayElementAtIndex(j), GUIContent.none);
									}
									else if (desiredType == typeof(List<char>) || desiredType == typeof(char[]))
									{
										Rect fieldRect = itemArgRect;

										fieldRect.width -= kButtonWidth;

										EditorGUI.PropertyField(fieldRect, arg.GetArrayElementAtIndex(j), GUIContent.none);

										Rect resetButtonRect = new Rect(fieldRect.x + fieldRect.width, fieldRect.y, kButtonWidth, fieldRect.height);
										GUIContent resetButton = EditorGUIUtility.IconContent("TreeEditor.Trash");
										resetButton.tooltip = "Delete char value";

										if (GUI.Button(resetButtonRect, resetButton, ReorderableList.defaultBehaviours.preButton))
											arg.GetArrayElementAtIndex(j).intValue = char.MinValue;
									}
									else if (desiredType == typeof(List<Vector4>) || desiredType == typeof(Vector4[]))
									{
										EditorGUI.BeginChangeCheck();
										var vector4ItemResult = EditorGUI.Vector4Field(itemArgRect, GUIContent.none, arg.GetArrayElementAtIndex(j).vector4Value);
										if (EditorGUI.EndChangeCheck())
											arg.GetArrayElementAtIndex(j).vector4Value = vector4ItemResult;
									}
									else if (desiredType == typeof(List<Quaternion>) || desiredType == typeof(Quaternion[]))
									{
										var quaternionValue = arg.GetArrayElementAtIndex(j).quaternionValue;
										EditorGUI.BeginChangeCheck();
										var quaternionItemResult = EditorGUI.Vector4Field(itemArgRect, GUIContent.none, new Vector4(quaternionValue.x, quaternionValue.y, quaternionValue.z, quaternionValue.w));
										if (EditorGUI.EndChangeCheck())
											arg.GetArrayElementAtIndex(j).quaternionValue = new Quaternion(quaternionItemResult.x, quaternionItemResult.y, quaternionItemResult.z, quaternionItemResult.w);
									}
									else if (IsValidListType(desiredType) || typeof(Object[]).IsAssignableFrom(desiredType))
									{
										Type type = desiredType.IsArray ? desiredType.GetElementType() : desiredType.GetGenericArguments()[0];

										EditorGUI.BeginChangeCheck();
										var objectItemResult = EditorGUI.ObjectField(itemArgRect, GUIContent.none, arg.GetArrayElementAtIndex(j).objectReferenceValue, type, true);
										if (EditorGUI.EndChangeCheck())
											arg.GetArrayElementAtIndex(j).objectReferenceValue = objectItemResult;
									}

									itemArgRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
									dynamicArgNameRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
									dynamicArgRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

									height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
								}
							}

							break;
						case PersistentListenerMode2.EventDefined:
						case PersistentListenerMode2.Void:
						default:
							break;
					}

					switch (modeEnum)
					{
						case PersistentListenerMode2.EventDefined:
						case PersistentListenerMode2.Void:
						case PersistentListenerMode2.Char:
						case PersistentListenerMode2.Object:
						case PersistentListenerMode2.Int:
						case PersistentListenerMode2.Float:
						case PersistentListenerMode2.String:
						case PersistentListenerMode2.Enum:
						case PersistentListenerMode2.Vector4:
						case PersistentListenerMode2.Quaternion:
						case PersistentListenerMode2.Array:
							break;
						default:
							EditorGUI.PropertyField(dynamicArgRect, arg, GUIContent.none, true);
							break;
					}

					dynamicArgNameRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
					dynamicArgRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
				}
			}

			using (new EditorGUI.DisabledScope(result == null))
			{
				EditorGUI.BeginProperty(functionRect, GUIContent.none, methodName);
				{
					GUIContent buttonContent;
					if (EditorGUI.showMixedValue)
					{
						buttonContent = EditorGUIUtility.TrTextContent("\u2014", "Mixed Values");
					}
					else
					{
						var buttonLabel = new StringBuilder();
						if (result == null || string.IsNullOrEmpty(methodName.stringValue))
						{
							buttonLabel.Append(kNoFunctionString);
						}
						else if (!IsListenerValid(result, methodName.stringValue, desiredTypes, isStaticForInstance.boolValue))
						{
							var instanceString = "UnknownComponent";
							if (result != null)
								instanceString = result.GetType().Name;

							buttonLabel.Append(string.Format("<Missing {0}.{1}>", instanceString, methodName.stringValue));
						}
						else
						{
							buttonLabel.Append(result.GetType().Name);

							if (!string.IsNullOrEmpty(methodName.stringValue))
							{
								buttonLabel.Append(".");
								if (methodName.stringValue.StartsWith("get_"))
									buttonLabel.Append(methodName.stringValue.Substring(4));
								else
									buttonLabel.Append(methodName.stringValue);
							}
						}
						buttonContent = EditorGUIUtility.TrTextContent(buttonLabel.ToString());
					}

					if (GUI.Button(functionRect, buttonContent, EditorStyles.popup))
					{
						var dynamicArgumentAssemblyTypeName = argument.FindPropertyRelative(kObjectArgumentAssemblyTypeName);
						Type desiredType = typeof(Object);

						if (dynamicArgumentAssemblyTypeName != null)
						{
							var desiredArgTypeName = dynamicArgumentAssemblyTypeName.stringValue;
							if (!string.IsNullOrEmpty(desiredArgTypeName))
								desiredType = Type.GetType(desiredArgTypeName, false) ?? typeof(Object);
						}

						//BuildPopupList(result, argument, new Type[] { desiredType }, false, true).DropDown(functionRect);
						BuildDynamicPopupList(result, argument, desiredType).DropDown(functionRect);
					}
				}

				EditorGUI.EndProperty();
			}

			return height;
		}

		private GenericMenu BuildPopupList(Object target, SerializedProperty listener, Type[] delegateArgumentsTypes)
		{
			//special case for components... we want all the game objects targets there!
			var targetToUse = target;
			if (targetToUse is Component component)
				targetToUse = component.gameObject;

			// find the current event target...
			var methodName = listener.FindPropertyRelative(kMethodNamePath);

			var menu = new GenericMenu();

			menu.AddItem(EditorGUIUtility.TrTextContent(kNoFunctionString),
				string.IsNullOrEmpty(methodName.stringValue),
				ClearEventFunction,
				new UnityEventFunction(listener, (Object)null, null, null));

			if (targetToUse == null)
				return menu;

			menu.AddSeparator("");

			GeneratePopUpForType(menu, targetToUse, false, listener, delegateArgumentsTypes);

			if (targetToUse is GameObject go)
			{
				Component[] comps = go.GetComponents<Component>();

				var duplicateNames = comps.Where(c => c != null)
					.Select(c => c.GetType().Name)
					.GroupBy(x => x)
					.Where(g => g.Count() > 1)
					.Select(g => g.Key)
					.ToList();

				foreach (Component comp in comps)
				{
					if (comp == null)
						continue;

					GeneratePopUpForType(menu, comp, duplicateNames.Contains(comp.GetType().Name), listener, delegateArgumentsTypes);
				}
			}

			return menu;
		}

		private GenericMenu BuildDynamicPopupList(Object target, SerializedProperty listener, Type desiredType)
		{
			//special case for components... we want all the game objects targets there!
			var targetToUse = target;
			if (targetToUse is Component component)
				targetToUse = component.gameObject;

			// find the current event target...
			var methodName = listener.FindPropertyRelative(kMethodNamePath);

			var menu = new GenericMenu();

			menu.AddItem(EditorGUIUtility.TrTextContent(kNoFunctionString),
				string.IsNullOrEmpty(methodName.stringValue),
				ClearEventFunction,
				new UnityEventFunction(listener, (Object)null, null, null));

			if (targetToUse == null)
				return menu;

			menu.AddSeparator("");

			if (targetToUse is GameObject go)
			{
				Component[] comps = go.GetComponents<Component>();

				var duplicateNames = comps.Where(c => c != null)
					.Select(c => c.GetType().Name)
					.GroupBy(x => x)
					.Where(g => g.Count() > 1)
					.Select(g => g.Key)
					.ToList();

				foreach (Component comp in comps)
				{
					if (comp == null)
						continue;

					GeneratePopUpForType(menu, comp, duplicateNames.Contains(comp.GetType().Name), listener, new Type[] { desiredType }, true);
				}
			}

			return menu;
		}

		private void GeneratePopUpForType(GenericMenu menu, Object target, bool useFullTargetName, SerializedProperty listener, Type[] delegateArgumentsTypes, bool isGetter = false)
		{
			var methods = new List<ValidMethodMap>();
			string targetName = useFullTargetName ? target.GetType().FullName : target.GetType().Name;

			bool didAddDynamic = false;

			if (delegateArgumentsTypes == null)
				delegateArgumentsTypes = new Type[0];

			// skip 'void' event defined on the GUI as we have a void prebuilt type!
			if (!isGetter && delegateArgumentsTypes.Length != 0)
			{
				GetMethodsForTargetAndMode(target, methods, delegateArgumentsTypes, true);
				if (methods.Count > 0)
				{
					menu.AddDisabledItem(EditorGUIUtility.TrTextContent(targetName + "/Dynamic " + string.Join(", ", delegateArgumentsTypes.Select(e => GetTypeName(e)).ToArray())));
					AddMethodsToMenu(menu, listener, methods, targetName, delegateArgumentsTypes);
					didAddDynamic = true;
				}
			}

			methods.Clear();

			GetMethodsForTargetAndMode(target, methods, delegateArgumentsTypes, false, isGetter);

			if (methods.Count > 0)
			{
				if (didAddDynamic)
					// AddSeparator doesn't seem to work for sub-menus, so we have to use this workaround instead of a proper separator for now.
					menu.AddItem(EditorGUIUtility.TrTextContent(targetName + "/ "), false, null);

				if (delegateArgumentsTypes.Length != 0)
					menu.AddDisabledItem(EditorGUIUtility.TrTextContent(targetName + "/Static Parameters"));

				AddMethodsToMenu(menu, listener, methods, targetName, isGetter: isGetter);
			}
		}

		private void GetMethodsForTargetAndMode(Object target, List<ValidMethodMap> methods, Type[] delegateArgumentsTypes = null, bool isDynamic = false, bool isGetter = false)
		{
			IEnumerable<ValidMethodMap> newMethods = CalculateMethodMap(target, delegateArgumentsTypes, isDynamic, isGetter);

			methods.AddRange(newMethods);
		}

		private IEnumerable<ValidMethodMap> CalculateMethodMap(Object target, Type[] types, bool isDynamic, bool isGetter)
		{
			var validMethods = new List<ValidMethodMap>();
			if (target == null || (isDynamic && types == null))
				return validMethods;

			if (types == null)
				types = new Type[0];

			if (isGetter && types.Length != 1)
				return validMethods;

			// find the methods on the behaviour that match the signature
			Type componentType = target.GetType();
			var componentMethods = componentType.GetMethods().Where(x => !x.IsSpecialName).ToList();

			var wantedProperties = componentType.GetProperties().AsEnumerable();
			wantedProperties = wantedProperties.Where(x => x.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0);
			wantedProperties = wantedProperties.Where(x => (isGetter && x.GetGetMethod() != null) || x.GetSetMethod() != null);

			componentMethods.AddRange(wantedProperties.Select(x => isGetter ? x.GetGetMethod() : x.GetSetMethod()));

			foreach (var componentMethod in componentMethods)
			{
				var componentParameters = componentMethod.GetParameters();

				// Don't show obsolete methods.
				if (componentMethod.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length > 0)
					continue;

				if (isGetter)
				{
					if (componentMethod.ReturnType != types[0]) continue;
				}
				else if (componentMethod.ReturnType != typeof(void))
				{
					continue;
				}

				if (isDynamic)
				{
					if (types.Length > componentParameters.Length)
						continue;

					var modes = new PersistentListenerMode2[componentParameters.Length];

					// if the argument types do not match, no match
					bool parametersMatch = true;
					for (int i = 0; i < types.Length; i++)
					{
						if (componentParameters[i].ParameterType.IsAssignableFrom(types[i]))
							modes[i] = PersistentListenerMode2.EventDefined;
						else
						{
							modes[i] = GetMode(componentParameters[i].ParameterType);
							parametersMatch = false;
						}
					}

					for (int i = types.Length; i < componentParameters.Length; i++)
					{
						modes[i] = GetMode(componentParameters[i].ParameterType);
					}

					// valid dynamic method
					if (parametersMatch)
					{
						var vmm = new ValidMethodMap
						{
							target = target,
							methodInfo = componentMethod,
							modes = modes,
							parameters = componentParameters,
							isDynamic = isDynamic
						};
						validMethods.Add(vmm);
					}
				}
				else
				{
					if (HasInvalidParameter(componentParameters))
						continue;

					var vmm = new ValidMethodMap
					{
						target = target,
						methodInfo = componentMethod,
						modes = GetModes(componentParameters),
						parameters = componentParameters,
						isDynamic = isDynamic
					};
					validMethods.Add(vmm);
				}
			}

			return validMethods;
		}

		public bool IsListenerValid(Object uObject, string methodName, Type[] argumentTypes, bool isStatic)
		{
			if (uObject == null || string.IsNullOrEmpty(methodName))
				return false;

			return UnityEventBase2.GetValidMethodInfo(uObject, methodName, argumentTypes, isStatic) != null;
		}

		private bool HasInvalidParameter(ParameterInfo[] parameters)
		{
			foreach (var parameter in parameters)
				if (!IsValidParameter(parameter))
					return true;

			return false;
		}

		private bool IsValidParameter(ParameterInfo parameter)
		{
			if (parameter.IsOut)
				return false;

			return IsValidType(parameter.ParameterType);
		}

		private bool IsValidType(Type type)
		{
			if (type.IsPrimitive
				|| type.IsEnum
				|| type.IsValueType
				|| type == typeof(string)
				|| type == typeof(GameObject)
				|| typeof(Component).IsAssignableFrom(type))
			{
				return true;
			}

			if (type.IsArray || IsValidListType(type))
				return true;

			if (type.IsGenericTypeDefinition
				|| type.IsGenericParameter
				|| type.IsInterface
				|| type == typeof(object)
				|| type == typeof(Object)
				|| type == typeof(IEnumerator)
				|| type == typeof(Coroutine)
				|| type == typeof(UnityAction)
				|| typeof(IList).IsAssignableFrom(type)
				|| !typeof(Object).IsAssignableFrom(type)
				)
			{
				return false;
			}

			return true;
		}

		private bool IsValidListType(Type type)
		{
			var genericArguments = type.GetGenericArguments();

			return type.IsGenericType
					&& genericArguments.Length == 1
					&& !genericArguments[0].IsGenericParameter
					&& typeof(List<>).MakeGenericType(genericArguments[0]).IsAssignableFrom(type);
		}

		private void AddMethodsToMenu(GenericMenu menu, SerializedProperty listener, List<ValidMethodMap> methods, string targetName, Type[] delegateArgumentsTypes = null, bool isGetter = false)
		{
			string value = isGetter ? "get_" : "set_";

			// Note: sorting by a bool in OrderBy doesn't seem to work for some reason, so using numbers explicitly.
			IEnumerable<ValidMethodMap> orderedMethods = methods.OrderBy(e => e.methodInfo.Name.StartsWith(value) ? 0 : 1).ThenBy(e => e.methodInfo.IsStatic ? 0 : 1).ThenBy(e => e.methodInfo.Name);

			foreach (var validMethod in orderedMethods)
				AddFunctionsForScript(menu, listener, validMethod, targetName, delegateArgumentsTypes);
		}

		private void AddFunctionsForScript(GenericMenu menu, SerializedProperty listener, ValidMethodMap method, string targetName, Type[] delegateArgumentsTypes)
		{
			// find the current event target...
			var listenerTarget = listener.FindPropertyRelative(kInstancePath).objectReferenceValue;

			var isStatic = listener.FindPropertyRelative(kIsStaticForInstancePath).boolValue;
			//var isStatic = false;
			//try
			//{
			//	isStatic = listener.FindPropertyRelative(kIsStaticForInstancePath).boolValue;
			//}
			//catch
			//{
			//	isStatic = listener.FindPropertyRelative(kIsStaticPath).boolValue;
			//}

			var methodName = listener.FindPropertyRelative(kMethodNamePath).stringValue;
			var modes = listener.FindPropertyRelative(kModesPath);

			var parameters = method.methodInfo.GetParameters();
			var args = string.Join(", ",
				new List<ParameterInfo>(parameters)
					.ConvertAll(parameter => parameter.ParameterType)
					.ConvertAll(parameterType => GetTypeName(parameterType)));

			var isCurrentlySet = listenerTarget == method.target
				&& isStatic == method.methodInfo.IsStatic
				&& methodName == method.methodInfo.Name
				&& CompareModes(modes, method.modes);

			string path = GetFormattedMethodName(targetName, method.methodInfo, args, method.isDynamic, delegateArgumentsTypes);

			menu.AddItem(EditorGUIUtility.TrTextContent(path),
				isCurrentlySet,
				SetEventFunction,
				new UnityEventFunction(listener, method.target, method.methodInfo, method.modes));
		}

		private bool CompareModes(SerializedProperty modes1, PersistentListenerMode2[] modes2)
		{
			if (modes1 == null || modes2 == null)
				return false;

			if (modes1.arraySize == 1 && modes2.Length == 0)
				if (modes1.GetArrayElementAtIndex(0).enumValueIndex == (int)PersistentListenerMode2.Void)
					return true;

			if (modes1.arraySize != modes2.Length)
				return false;

			for (int i = 0; i < modes1.arraySize; i++)
			{
				if (modes1.GetArrayElementAtIndex(i).enumValueIndex != (int)modes2[i])
					return false;
			}

			return true;
		}

		private PersistentListenerMode2 GetMode(SerializedProperty mode)
		{
			switch (mode.type)
			{
				case "bool":
					return PersistentListenerMode2.Bool;
				case "float":
					return PersistentListenerMode2.Float;
				case "int":
					return PersistentListenerMode2.Int;
				case "string":
					return PersistentListenerMode2.String;
				case "Enum":
					return (PersistentListenerMode2)mode.enumValueIndex;
				default:
					return PersistentListenerMode2.Void;
			}
		}

		private PersistentListenerMode2 GetMode(Type type)
		{
			if (type == typeof(char))
				return PersistentListenerMode2.Char;
			else if (type == typeof(byte))
				return PersistentListenerMode2.Byte;
			else if (type == typeof(sbyte))
				return PersistentListenerMode2.SByte;
			else if (type == typeof(short))
				return PersistentListenerMode2.Short;
			else if (type == typeof(ushort))
				return PersistentListenerMode2.UShort;
			else if (type == typeof(int))
				return PersistentListenerMode2.Int;
			else if (type == typeof(uint))
				return PersistentListenerMode2.UInt;
			else if (type == typeof(long))
				return PersistentListenerMode2.Long;
			else if (type == typeof(float))
				return PersistentListenerMode2.Float;
			else if (type == typeof(double))
				return PersistentListenerMode2.Double;
			else if (type == typeof(string))
				return PersistentListenerMode2.String;
			else if (type == typeof(bool))
				return PersistentListenerMode2.Bool;
			else if (type.IsEnum)
				return PersistentListenerMode2.Enum;
			else if (type == typeof(Vector2))
				return PersistentListenerMode2.Vector2;
			else if (type == typeof(Vector2Int))
				return PersistentListenerMode2.Vector2Int;
			else if (type == typeof(Vector3))
				return PersistentListenerMode2.Vector3;
			else if (type == typeof(Vector3Int))
				return PersistentListenerMode2.Vector3Int;
			else if (type == typeof(Vector4))
				return PersistentListenerMode2.Vector4;
			else if (type == typeof(LayerMask))
				return PersistentListenerMode2.LayerMask;
			else if (type == typeof(Color))
				return PersistentListenerMode2.Color;
			else if (type == typeof(Quaternion))
				return PersistentListenerMode2.Quaternion;
			else if (type.IsArray || IsValidListType(type))
				return PersistentListenerMode2.Array;
			else
				return PersistentListenerMode2.Object;
		}

		private PersistentListenerMode2[] GetModes(ParameterInfo[] parameters)
		{
			if (parameters == null)
				return new PersistentListenerMode2[0];

			PersistentListenerMode2[] modes = new PersistentListenerMode2[parameters.Length];

			for (int i = 0; i < parameters.Length; i++)
				modes[i] = GetMode(parameters[i].ParameterType);

			return modes;
		}

		private string GetTypeName(Type type)
		{
			if (type == typeof(char))
				return "char";
			if (type == typeof(byte))
				return "byte";
			if (type == typeof(sbyte))
				return "sbyte";
			if (type == typeof(short))
				return "short";
			if (type == typeof(ushort))
				return "ushort";
			if (type == typeof(int))
				return "int";
			if (type == typeof(uint))
				return "uint";
			if (type == typeof(long))
				return "long";
			if (type == typeof(float))
				return "float";
			if (type == typeof(double))
				return "double";
			if (type == typeof(string))
				return "string";
			if (type == typeof(bool))
				return "bool";
			if (type.IsArray)
				return GetArrayTypeName(type);
			if (IsValidListType(type))
				return GetListTypeName(type);
			return type.Name;
		}

		private string GetArrayTypeName(Type type)
		{
			if (type == typeof(char[]))
				return "char[]";
			if (type == typeof(byte[]))
				return "byte[]";
			if (type == typeof(sbyte[]))
				return "sbyte[]";
			if (type == typeof(short[]))
				return "short[]";
			if (type == typeof(ushort[]))
				return "ushor[]t";
			if (type == typeof(int[]))
				return "int[]";
			if (type == typeof(uint[]))
				return "uint[]";
			if (type == typeof(long[]))
				return "long[]";
			if (type == typeof(float[]))
				return "float[]";
			if (type == typeof(double[]))
				return "double[]";
			if (type == typeof(string[]))
				return "string[]";
			if (type == typeof(bool[]))
				return "bool[]";
			if (type == typeof(Vector2[]))
				return "Vector2[]";
			if (type == typeof(Vector2Int[]))
				return "Vector2Int[]";
			if (type == typeof(Vector3[]))
				return "Vector3[]";
			if (type == typeof(Vector3Int[]))
				return "Vector3Int[]";
			if (type == typeof(Vector4[]))
				return "Vector4[]";
			if (type == typeof(LayerMask[]))
				return "LayerMask[]";
			if (type == typeof(Color[]))
				return "Color[]";
			if (type == typeof(Quaternion[]))
				return "Quaternion[]";

			return string.Format("{0}[]", type.GetElementType().Name);
		}

		private string GetListTypeName(Type type)
		{
			if (type == typeof(List<char>))
				return "List<char>";
			if (type == typeof(List<byte>))
				return "List<byte>";
			if (type == typeof(List<sbyte>))
				return "List<sbyte>";
			if (type == typeof(List<short>))
				return "List<short>";
			if (type == typeof(List<ushort>))
				return "List<ushort>";
			if (type == typeof(List<int>))
				return "List<int>";
			if (type == typeof(List<uint>))
				return "List<uint>";
			if (type == typeof(List<long>))
				return "List<long>";
			if (type == typeof(List<float>))
				return "List<float>";
			if (type == typeof(List<double>))
				return "List<double>";
			if (type == typeof(List<string>))
				return "List<string>";
			if (type == typeof(List<bool>))
				return "List<bool>";
			if (type == typeof(List<Vector2>))
				return "List<Vector2>";
			if (type == typeof(List<Vector2Int>))
				return "List<Vector2Int>";
			if (type == typeof(List<Vector3>))
				return "List<Vector3>";
			if (type == typeof(List<Vector3Int>))
				return "List<Vector3Int>";
			if (type == typeof(List<Vector4>))
				return "List<Vector4>";
			if (type == typeof(List<LayerMask>))
				return "List<LayerMask>";
			if (type == typeof(List<Color>))
				return "List<Color>";
			if (type == typeof(List<Quaternion>))
				return "List<Quaternion>";

			return string.Format("List<{0}>", type.GetGenericArguments()[0].Name);
		}

		private string GetFormattedMethodName(string targetName, MethodInfo method, string args, bool dynamic, Type[] delegateArgumentsTypes)
		{
			if (dynamic)
			{
				if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
				{
					return string.Format("{0}/{1}{2}", targetName, method.IsStatic ? "static " : string.Empty, method.Name.Substring(4));
				}
				else
				{
					var builder = new StringBuilder();

					var count = delegateArgumentsTypes.Length;
					for (int i = 0; i < count; i++)
					{
						builder.Append("[dynamic]");

						if (i < count - 1)
							builder.Append(", ");
					}

					var parameters = method.GetParameters();
					count = parameters.Length;

					if (count - delegateArgumentsTypes.Length > 0)
						builder.Append(", ");

					for (int i = delegateArgumentsTypes.Length; i < count; i++)
					{
						builder.Append(GetTypeName(parameters[i].ParameterType));

						if (i < count - 1)
							builder.Append(", ");
					}

					return string.Format("{0}/{1}{2} ({3})", targetName, method.IsStatic ? "static " : string.Empty, method.Name, builder.ToString());
				}
			}
			else
			{
				if (method.Name.StartsWith("get_"))
					return string.Format("{0}/{1}{2}", targetName, method.IsStatic ? "static " : string.Empty, method.Name.Substring(4));
				if (method.Name.StartsWith("set_"))
					return string.Format("{0}/{1}{3} {2}", targetName, method.IsStatic ? "static " : string.Empty, method.Name.Substring(4), args);
				else
					return string.Format("{0}/{1}{2} ({3})", targetName, method.IsStatic ? "static " : string.Empty, method.Name, args);
			}
		}

		private MethodInfo GetMethodInfo(SerializedProperty listenerTarget, SerializedProperty methodName, SerializedProperty arguments, SerializedProperty isStatic = null)
		{
			var desiredTypes = new Type[arguments.arraySize];
			for (int i = 0; i < arguments.arraySize; i++)
			{
				var argument = arguments.GetArrayElementAtIndex(i);
				var objArgument = argument.FindPropertyRelative(kObjectArgumentAssemblyTypeName);

				if (objArgument != null)
				{
					var desiredArgTypeName = objArgument.stringValue;

					if (!string.IsNullOrEmpty(desiredArgTypeName))
						desiredTypes[i] = Type.GetType(desiredArgTypeName, false) ?? typeof(Object);
				}
				else
					desiredTypes[i] = typeof(Object);
			}

			return UnityEventBase2.GetValidMethodInfo(listenerTarget.objectReferenceValue, methodName.stringValue, desiredTypes, isStatic != null ? isStatic.boolValue : false);
		}

		private static T GetAttribute<T>(MemberInfo memberInfo, bool inherit = false)
		{
			if (memberInfo != null)
			{
				var attrs = memberInfo.GetCustomAttributes(typeof(T), inherit);

				if (attrs.Length > 0)
					return (T)attrs[0];
			}

			return default(T);
		}

		private static T GetAttribute<T>(ParameterInfo parameterInfo, bool inherit = false)
		{
			if (parameterInfo != null)
			{
				var attrs = parameterInfo.GetCustomAttributes(typeof(T), inherit);

				if (attrs.Length > 0)
					return (T)attrs[0];
			}

			return default(T);
		}

		private SerializedProperty GetArrayPropertyFromType(SerializedProperty argument, Type type)
		{
			if (type == typeof(List<char>) || type == typeof(char[]))
				return argument.FindPropertyRelative(kCharArrayArgument);
			else if (type == typeof(List<byte>) || type == typeof(byte[]))
				return argument.FindPropertyRelative(kByteArrayArgument);
			else if (type == typeof(List<sbyte>) || type == typeof(sbyte[]))
				return argument.FindPropertyRelative(kSByteArrayArgument);
			else if (type == typeof(List<short>) || type == typeof(short[]))
				return argument.FindPropertyRelative(kShortArrayArgument);
			else if (type == typeof(List<ushort>) || type == typeof(ushort[]))
				return argument.FindPropertyRelative(kUShortArrayArgument);
			else if (type == typeof(List<int>) || type == typeof(int[])
				|| type.IsArray && type.GetElementType().IsEnum
				|| type.GetGenericArguments().Length > 0 && type.GetGenericArguments()[0].IsEnum)
				return argument.FindPropertyRelative(kIntArrayArgument);
			else if (type == typeof(List<uint>) || type == typeof(uint[]))
				return argument.FindPropertyRelative(kUIntArrayArgument);
			else if (type == typeof(List<long>) || type == typeof(long[]))
				return argument.FindPropertyRelative(kLongArrayArgument);
			else if (type == typeof(List<float>) || type == typeof(float[]))
				return argument.FindPropertyRelative(kFloatArrayArgument);
			else if (type == typeof(List<double>) || type == typeof(double[]))
				return argument.FindPropertyRelative(kDoubleArrayArgument);
			else if (type == typeof(List<string>) || type == typeof(string[]))
				return argument.FindPropertyRelative(kStringArrayArgument);
			else if (type == typeof(List<bool>) || type == typeof(bool[]))
				return argument.FindPropertyRelative(kBoolArrayArgument);
			else if (type == typeof(List<Vector2>) || type == typeof(Vector2[]))
				return argument.FindPropertyRelative(kVector2ArrayArgument);
			else if (type == typeof(List<Vector2Int>) || type == typeof(Vector2Int[]))
				return argument.FindPropertyRelative(kVector2IntArrayArgument);
			else if (type == typeof(List<Vector3>) || type == typeof(Vector3[]))
				return argument.FindPropertyRelative(kVector3ArrayArgument);
			else if (type == typeof(List<Vector3Int>) || type == typeof(Vector3Int[]))
				return argument.FindPropertyRelative(kVector3IntArrayArgument);
			else if (type == typeof(List<Vector4>) || type == typeof(Vector4[]))
				return argument.FindPropertyRelative(kVector4ArrayArgument);
			else if (type == typeof(List<LayerMask>) || type == typeof(LayerMask[]))
				return argument.FindPropertyRelative(kLayerMaskArrayArgument);
			else if (type == typeof(List<Color>) || type == typeof(Color[]))
				return argument.FindPropertyRelative(kColorArrayArgument);
			else if (type == typeof(List<Quaternion>) || type == typeof(Quaternion[]))
				return argument.FindPropertyRelative(kQuaternionArrayArgument);
			else if (IsValidListType(type) || typeof(Object[]).IsAssignableFrom(type))
				return argument.FindPropertyRelative(kObjectArrayArgument);

			return null;
		}

		private void SetEventFunction(object source)
		{
			((UnityEventFunction)source).Assign();
		}

		private void ClearEventFunction(object source)
		{
			((UnityEventFunction)source).Clear();
		}

		struct ValidMethodMap
		{
			public Object target;
			public MethodInfo methodInfo;
			public PersistentListenerMode2[] modes;
			public ParameterInfo[] parameters;
			public bool isDynamic;
		}

		struct UnityEventFunction
		{
			public readonly SerializedProperty m_Listener;
			public readonly Object m_Target;
			public readonly MethodInfo m_Method;
			public readonly PersistentListenerMode2[] m_Modes;

			public UnityEventFunction(SerializedProperty listener, Object target, MethodInfo method, PersistentListenerMode2[] modes)
			{
				m_Listener = listener;
				m_Target = target;
				m_Method = method;
				m_Modes = modes;
			}

			public void Assign()
			{
				// find the current event target...
				var listenerTarget = m_Listener.FindPropertyRelative(kInstancePath);
				var isStatic = m_Listener.FindPropertyRelative(kIsStaticForInstancePath);
				var methodName = m_Listener.FindPropertyRelative(kMethodNamePath);
				var arguments = m_Listener.FindPropertyRelative(kArgumentsPath);
				var modes = m_Listener.FindPropertyRelative(kModesPath);

				listenerTarget.objectReferenceValue = m_Target;
				isStatic.boolValue = m_Method.IsStatic;
				methodName.stringValue = m_Method.Name;

				var argParams = m_Method.GetParameters();

				arguments.arraySize = argParams.Length;
				modes.arraySize = argParams.Length;

				if (modes.arraySize == 0)
				{
					modes.InsertArrayElementAtIndex(0);
					modes.GetArrayElementAtIndex(0).enumValueIndex = (int)PersistentListenerMode2.Void;
				}

				var methodLayerAttr = GetAttribute<LayerAttribute>(m_Method);
				var methodSliderAttr = GetAttribute<SliderAttribute>(m_Method);
				var methodIntSliderAttr = GetAttribute<IntSliderAttribute>(m_Method);
				var methodTagAttr = GetAttribute<TagAttribute>(m_Method);

				for (int i = 0; i < argParams.Length; i++)
				{
					modes.GetArrayElementAtIndex(i).enumValueIndex = (int)m_Modes[i];

					var argument = arguments.GetArrayElementAtIndex(i);

					var fullArgumentType = argument.FindPropertyRelative(kObjectArgumentAssemblyTypeName);

					fullArgumentType.stringValue = argParams[i].ParameterType.AssemblyQualifiedName;

					switch (m_Modes[i])
					{
						case PersistentListenerMode2.Object:
							argument.FindPropertyRelative(kObjectArgument).objectReferenceValue = null;
							break;
						case PersistentListenerMode2.Char:
							argument.FindPropertyRelative(kCharArgument).intValue = char.MinValue;
							break;
						case PersistentListenerMode2.Byte:
							argument.FindPropertyRelative(kByteArgument).intValue = 0;
							break;
						case PersistentListenerMode2.SByte:
							argument.FindPropertyRelative(kSByteArgument).intValue = 0;
							break;
						case PersistentListenerMode2.Short:
							argument.FindPropertyRelative(kShortArgument).intValue = 0;
							break;
						case PersistentListenerMode2.UShort:
							argument.FindPropertyRelative(kUShortArgument).intValue = 0;
							break;
						case PersistentListenerMode2.Int:
							var paramLayerAttr = GetAttribute<LayerAttribute>(argParams[i]);
							var paramIntSliderAttr = GetAttribute<IntSliderAttribute>(argParams[i]);

							if (paramLayerAttr != null)
								argument.FindPropertyRelative(kIntArgument).intValue = paramLayerAttr.defaultValue;
							else if (methodLayerAttr != null)
								argument.FindPropertyRelative(kIntArgument).intValue = methodLayerAttr.defaultValue;
							else if (paramIntSliderAttr != null)
								argument.FindPropertyRelative(kIntArgument).intValue = paramIntSliderAttr.defaultValue;
							else if (methodIntSliderAttr != null)
								argument.FindPropertyRelative(kIntArgument).intValue = methodIntSliderAttr.defaultValue;
							else
								argument.FindPropertyRelative(kIntArgument).intValue = 0;

							break;
						case PersistentListenerMode2.UInt:
							argument.FindPropertyRelative(kUIntArgument).longValue = 0;
							break;
						case PersistentListenerMode2.Long:
							argument.FindPropertyRelative(kLongArgument).longValue = 0;
							break;
						case PersistentListenerMode2.Float:
							var paramSliderAttr = GetAttribute<SliderAttribute>(argParams[i]);

							if (paramSliderAttr != null)
								argument.FindPropertyRelative(kFloatArgument).floatValue = paramSliderAttr.defaultValue;
							else if (methodSliderAttr != null)
								argument.FindPropertyRelative(kFloatArgument).floatValue = methodSliderAttr.defaultValue;
							else
								argument.FindPropertyRelative(kFloatArgument).floatValue = 0f;

							break;
						case PersistentListenerMode2.Double:
							argument.FindPropertyRelative(kDoubleArgument).doubleValue = 0d;
							break;
						case PersistentListenerMode2.String:
							var paramTagAttr = GetAttribute<TagAttribute>(argParams[i]);

							if (paramTagAttr != null)
								argument.FindPropertyRelative(kStringArgument).stringValue = paramTagAttr.defaultValue;
							else if (methodTagAttr != null)
								argument.FindPropertyRelative(kStringArgument).stringValue = methodTagAttr.defaultValue;
							else
								argument.FindPropertyRelative(kStringArgument).stringValue = null;
							break;
						case PersistentListenerMode2.Bool:
							argument.FindPropertyRelative(kBoolArgument).boolValue = false;
							break;
						case PersistentListenerMode2.Enum:
							argument.FindPropertyRelative(kIntArgument).intValue = 0;
							break;
						case PersistentListenerMode2.Vector2:
							argument.FindPropertyRelative(kVector2Argument).vector2Value = Vector2.zero;
							break;
						case PersistentListenerMode2.Vector2Int:
							argument.FindPropertyRelative(kVector2IntArgument).vector2IntValue = Vector2Int.zero;
							break;
						case PersistentListenerMode2.Vector3:
							argument.FindPropertyRelative(kVector3Argument).vector3Value = Vector3.zero;
							break;
						case PersistentListenerMode2.Vector3Int:
							argument.FindPropertyRelative(kVector3IntArgument).vector3IntValue = Vector3Int.zero;
							break;
						case PersistentListenerMode2.Vector4:
							argument.FindPropertyRelative(kVector4Argument).vector4Value = Vector4.zero;
							break;
						case PersistentListenerMode2.LayerMask:
							argument.FindPropertyRelative(kLayerMaskArgument).intValue = 0;
							break;
						case PersistentListenerMode2.Color:
							argument.FindPropertyRelative(kColorArgument).colorValue = Color.clear;
							break;
						case PersistentListenerMode2.Quaternion:
							argument.FindPropertyRelative(kQuaternionArgument).quaternionValue = Quaternion.Euler(Vector3.zero);
							break;
						case PersistentListenerMode2.Array:
							argument.FindPropertyRelative(kObjectArrayArgument).arraySize = 0;
							argument.FindPropertyRelative(kCharArrayArgument).arraySize = 0;
							argument.FindPropertyRelative(kByteArrayArgument).arraySize = 0;
							argument.FindPropertyRelative(kSByteArrayArgument).arraySize = 0;
							argument.FindPropertyRelative(kShortArrayArgument).arraySize = 0;
							argument.FindPropertyRelative(kUShortArrayArgument).arraySize = 0;
							argument.FindPropertyRelative(kIntArrayArgument).arraySize = 0;
							argument.FindPropertyRelative(kUIntArrayArgument).arraySize = 0;
							argument.FindPropertyRelative(kLongArrayArgument).arraySize = 0;
							argument.FindPropertyRelative(kFloatArrayArgument).arraySize = 0;
							argument.FindPropertyRelative(kDoubleArrayArgument).arraySize = 0;
							argument.FindPropertyRelative(kStringArrayArgument).arraySize = 0;
							argument.FindPropertyRelative(kBoolArrayArgument).arraySize = 0;
							argument.FindPropertyRelative(kVector2ArrayArgument).arraySize = 0;
							argument.FindPropertyRelative(kVector3ArrayArgument).arraySize = 0;
							argument.FindPropertyRelative(kVector2IntArrayArgument).arraySize = 0;
							argument.FindPropertyRelative(kVector3IntArrayArgument).arraySize = 0;
							argument.FindPropertyRelative(kVector4ArrayArgument).arraySize = 0;
							argument.FindPropertyRelative(kLayerMaskArrayArgument).arraySize = 0;
							argument.FindPropertyRelative(kColorArrayArgument).arraySize = 0;
							argument.FindPropertyRelative(kQuaternionArrayArgument).arraySize = 0;
							break;
						default:
							break;
					}

					var isDynamic = argument.FindPropertyRelative(kIsDynamicArgument);
					if (isDynamic != null)
					{
						isDynamic.boolValue = false;

						argument.FindPropertyRelative(kInstancePath).objectReferenceValue = null;
						argument.FindPropertyRelative(kMethodNamePath).stringValue = null;
						argument.FindPropertyRelative(kArgumentsPath).arraySize = 0;
						argument.FindPropertyRelative(kModesPath).arraySize = 0;
					}
				}

				m_Listener.serializedObject.ApplyModifiedProperties();
			}

			public void Clear()
			{
				// find the current event target...
				m_Listener.FindPropertyRelative(kIsStaticForInstancePath).boolValue = false;
				m_Listener.FindPropertyRelative(kMethodNamePath).stringValue = null;
				m_Listener.FindPropertyRelative(kArgumentsPath).arraySize = 0;
				m_Listener.FindPropertyRelative(kModesPath).arraySize = 0;

				m_Listener.serializedObject.ApplyModifiedProperties();
			}
		}
	}
}