using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

public class SimpleAnimatorEditor
{
	[MenuItem("Assets/Select Animator to SimpleAnimator")]
	private static void Convert()
	{
		var activeObject = Selection.activeObject as GameObject;
		var animators = activeObject.GetComponentsInChildren<Animator>();

		foreach (var animator in animators)
		{
			Process(animator);
		}
	}
	
	public static SimpleAnimator Process(Animator animator)
	{
		SimpleAnimatorState[] states;
		string defaultState;
			
		if (!BuildSimpleAnimatorState(animator, out states, out defaultState))
			return null;
		
		var simpleAnimator = BuildSimpleAnimator(animator.gameObject, states, defaultState);
		DestoryAnimator(animator);

		return simpleAnimator;
	}

	private static bool BuildSimpleAnimatorState(Animator animator, out SimpleAnimatorState[] stateArray, out string defaultState)
	{
		stateArray = null;
		defaultState = null;
		
		var states = new List<SimpleAnimatorState>();
		var runtimeAnimatorController = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
		if (runtimeAnimatorController == null)
		{
			if (animator.runtimeAnimatorController != null)
				Debug.LogFormat("Not support. {0} runtimeAnimatorController type:{1}", animator.runtimeAnimatorController.name, animator.runtimeAnimatorController.GetType());
			return false;
		}
		
		var layers = runtimeAnimatorController.layers;
		if (layers.Length != 1)
		{
			Debug.LogFormat("Not support. {0} layer count:{1}", runtimeAnimatorController.name, layers.Length);
			return false;
		}
	
		foreach (var layer in layers)
		{
			defaultState = layer.stateMachine.defaultState.name;
			
			foreach (var state in layer.stateMachine.states)
			{
				if (Mathf.Abs(state.state.speed - 1) > float.Epsilon)
				{
					Debug.LogFormat("Not support. {0}:{1} state speed:{2}", runtimeAnimatorController.name, state.state.name, state.state.speed);
					return false;
				}
				
				if (state.state.transitions.Length > 1)
				{
					Debug.LogFormat("Not support. {0}:{1} transitions count:{2}", runtimeAnimatorController.name, state.state.name, state.state.transitions.Length);
					return false;
				}
				
				var simpleState = new SimpleAnimatorState();
				states.Add(simpleState);

				simpleState.name = state.state.name;
				simpleState.clip = state.state.motion as AnimationClip;
				
				if (simpleState.clip == null)
				{
					if (state.state.motion != null)
						Debug.LogFormat("Not support. {0}:{1} motion type:{2}", runtimeAnimatorController.name, state.state.name, state.state.motion.GetType());
					return false;
				}
				
				simpleState.hasTransition = state.state.transitions.Length == 1;
				if (simpleState.hasTransition)
				{
					var transition = state.state.transitions[0];
					if (transition.offset > float.Epsilon)
					{
						Debug.LogFormat("Not support. {0}:{1} transitions offset:{2}", runtimeAnimatorController.name, state.state.name, transition.offset);
						return false;
					}
				
					simpleState.destinationState = transition.destinationState.name;
				
					simpleState.hasExitTime = transition.hasExitTime;
					simpleState.exitTime = transition.exitTime;
				
					simpleState.hasFixedDuration = transition.hasFixedDuration;
					simpleState.duration = transition.duration;
				
					simpleState.offset = transition.offset;
				}
			}
		}
		
		stateArray = states.ToArray();
		return true;
	}

	private static SimpleAnimator BuildSimpleAnimator(GameObject go, SimpleAnimatorState[] states, string defaultState)
	{
		var animation = go.AddComponent<Animation>();
		var simpleAnimator = go.AddComponent<SimpleAnimator>();

		foreach (var state in states)
		{
			state.clip.legacy = true;
			animation.AddClip(state.clip, state.clip.name);
		}

		simpleAnimator.states = states;
		simpleAnimator.defaultState = defaultState;
		simpleAnimator.animation = animation;

		return simpleAnimator;
	}

	private static void DestoryAnimator(Animator animator)
	{
		var go = animator.gameObject;
		UnityEngine.Object.DestroyImmediate(animator, true);
	}
}
#endif

[Serializable]
public class SimpleAnimatorState
{
	public string name;
	public AnimationClip clip;
	
	public bool hasTransition;
	public string destinationState;

	public bool hasExitTime;
	public float exitTime;
	
	public bool hasFixedDuration;
	public float duration;
	
	public float offset;
}

public class SimpleAnimator : MonoBehaviour
{
	public SimpleAnimatorState[] states;
	public Animation animation;
	public string defaultState;
	public string currentState;
	private Coroutine transitionCoroutine = null;

	void Start()
	{
		foreach (var state in states)
		{
			state.clip.legacy = true;
		}
		
		if (!string.IsNullOrEmpty(defaultState))
			Play(defaultState);
	}

	public bool Play(string stateName)
	{
		foreach (var state in states)
		{
			if (stateName == state.name)
			{
				if (!animation.Play(state.clip.name))
					return false;

				ChangeState(state);
				return true;
			}
		}
		return false;
	}

	private bool CrossFade(string stateName, float fadeLength)
	{
		foreach (var state in states)
		{
			if (stateName == state.name)
			{
				animation.CrossFade(state.clip.name, fadeLength);
				ChangeState(state);
				
				return true;
			}
		}
		return false;
	}

	private void ChangeState(SimpleAnimatorState state)
	{
		if (transitionCoroutine != null)
		{
			StopCoroutine(transitionCoroutine);
			transitionCoroutine = null;
		}

		transitionCoroutine = StartCoroutine(Transition(state));
		currentState = state.name;
	}

	private IEnumerator Transition(SimpleAnimatorState state)
	{
		var time = state.clip.length;
		var exitTime = state.exitTime;
		
		while (exitTime > 1)
		{
			yield return new WaitForSeconds(time);
			
			exitTime -= 1;
			animation.Play(state.clip.name);
		}

		exitTime *= time;
		yield return new WaitForSeconds(exitTime);

		transitionCoroutine = null;
		
		var duration = state.duration;
		if (!state.hasFixedDuration)
			duration *= time;
		
		CrossFade(state.destinationState, duration);
	}

	private IEnumerator StopPlay(SimpleAnimatorState state)
	{
		yield return new WaitForSeconds(state.clip.length);
		animation.Stop(state.clip.name);
	}
}
