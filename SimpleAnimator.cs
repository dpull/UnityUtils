using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

public class SimpleAnimatorEditor
{
	[MenuItem("Assets/Animator to SimpleAnimator")]
	static void DoSomething()
	{
		var activeObject = Selection.activeObject as GameObject;
		var animators = activeObject.GetComponentsInChildren<Animator>();

		foreach (var animator in animators)
		{
			SimpleAnimatorState[] states;
			string defaultState;
			
			if (!BuildSimpleAnimatorState(animator, out states, out defaultState))
				continue;
		
			BuildSimpleAnimator(animator.gameObject, states, defaultState);
			DestoryAnimator(animator);
		}
	}

	private static bool BuildSimpleAnimatorState(Animator animator, out SimpleAnimatorState[] stateArray, out string defaultState)
	{
		var states = new List<SimpleAnimatorState>();
		var layerCount = animator.layerCount;
		var runtimeAnimatorController = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
		var layers = runtimeAnimatorController.layers;

		stateArray = null;
		defaultState = null;

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

	private static void BuildSimpleAnimator(GameObject go, SimpleAnimatorState[] states, string defaultState)
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
		Play("CY_SCHS_B_CX_A");
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
