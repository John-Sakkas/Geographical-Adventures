using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

public class RebindManager : MonoBehaviour
{
	public event Action<InputAction, int> rebindComplete;
	public event Action<InputAction, int> rebindCancelled;
	public event System.Action onBindingsSaved;

	// State
	List<InputAction> modifiedActions;
	InputActionRebindingExtensions.RebindingOperation activeRebindOperation;
	InputAction activeRebindAction;
	int activeRebindIndex;

	public static RebindManager _instance;

	void Awake()
	{
		modifiedActions = new List<InputAction>();

	}

	public void LoadSavedBindings(PlayerAction playerActions)
	{
		foreach (var action in playerActions)
		{
			LoadBindingOverride(action);
		}
	}


	public void StartRebind(InputAction actionToRebind, int bindingIndex)
	{
		//	Cancel if trying to rebind same binding multiple times
		if (actionToRebind == activeRebindAction && activeRebindIndex == bindingIndex)
		{
			Cancel();
			return;
		}

		Cancel();
		activeRebindIndex = bindingIndex;
		activeRebindAction = actionToRebind;
		activeRebindAction.Disable();

		activeRebindOperation = actionToRebind.PerformInteractiveRebinding(bindingIndex);
		activeRebindOperation.WithCancelingThrough("<Keyboard>/"); // For some obscure reason, putting nothing or <Keyboard>/escape block the key e
		activeRebindOperation.WithControlsExcluding("Mouse");

		activeRebindOperation.OnComplete(operation => OnRebindComplete());
		activeRebindOperation.OnCancel(operation => OnRebindCanceled());

		activeRebindOperation.Start();

	}

	void OnRebindComplete()
	{
		if (activeRebindAction != null)
		{
			activeRebindAction.Enable();
			activeRebindOperation.Dispose();

			modifiedActions.Add(activeRebindAction);
			rebindComplete?.Invoke(activeRebindAction, activeRebindIndex);
			activeRebindAction = null;
		}
	}

	void OnRebindCanceled()
	{
		if (activeRebindAction != null)
		{
			activeRebindAction.Enable();
			activeRebindOperation?.Dispose();
			rebindCancelled?.Invoke(activeRebindAction, activeRebindIndex);
			activeRebindAction = null;
		}
	}


	public void SaveChangedBindings()
	{
		foreach (var t in modifiedActions)
		{
			SaveBindingOverride(t);
		}
		modifiedActions.Clear();

		PlayerPrefs.Save();
		onBindingsSaved?.Invoke();

		void SaveBindingOverride(InputAction action)
		{
			for (int i = 0; i < action.bindings.Count; i++)
			{
				PlayerPrefs.SetString(action.actionMap + action.name + i, action.bindings[i].overridePath);
			}
		}
	}

	public void ReloadBindingsOnExit()
	{
		foreach (var t in modifiedActions)
		{
			LoadBindingOverride(t);
		}

		modifiedActions.Clear();

	}



	public static void LoadBindingOverride(InputAction action)
	{
		//Debug.Log("Load override");
		for (int i = 0; i < action.bindings.Count; i++)
		{
			if (!string.IsNullOrEmpty(PlayerPrefs.GetString(action.actionMap + action.name + i)))
			{
				action.ApplyBindingOverride(i, PlayerPrefs.GetString(action.actionMap + action.name + i));
			}
		}
	}

	public static void ResetBinding(InputAction action, int bindingIndex)
	{

		if (action.bindings[bindingIndex].isComposite)
		{
			for (int i = bindingIndex; i < action.bindings.Count && action.bindings[i].isComposite; i++)
			{
				action.RemoveBindingOverride(i);
			}
		}
		else
		{
			action.RemoveBindingOverride(bindingIndex);
		}

		Instance.modifiedActions.Add(action);
	}

	public void Cancel()
	{
		activeRebindOperation?.Cancel();
		activeRebindAction = null;
		activeRebindOperation = null;
	}

	public static RebindManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = FindObjectOfType<RebindManager>(includeInactive: true);
			}
			return _instance;
		}
	}


}