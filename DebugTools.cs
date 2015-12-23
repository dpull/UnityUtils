using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DebugTools : MonoBehaviour
{ 
	List<GameObject> CacheParticleObjects = new List<GameObject>();
	
    void OnGUI()
	{
		GUI.skin.button.fontSize = 32;
		if (CacheParticleObjects.Count > 0)
		{
			if (GUILayout.Button("恢复粒子"))
				EnableParticle();
		}
		else
		{
			if (GUILayout.Button("禁用粒子"))
				DisableParticle();
		}
	}

	void DisableParticle()
	{
		CacheParticleObjects.Clear();

		var particles = Object.FindObjectsOfType(typeof(ParticleSystem)) as ParticleSystem[];
		foreach (var particle in particles)
		{
			var go = particle.gameObject;
			if (go.activeInHierarchy)
			{
				go.SetActive(false);
				CacheParticleObjects.Add(go);
			}
		}
	}

	void EnableParticle()
	{
		foreach (var go in CacheParticleObjects)
		{
			if (go != null)
				go.SetActive(true);
		}
		CacheParticleObjects.Clear();
	}
}
