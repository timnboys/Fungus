﻿using UnityEngine;
using NodeAbilities;
using UnityEngine.SceneManagement;

public class LevelGoal : MonoBehaviour
{
	public NodeAbility unlockedAbility;
	[SerializeField]
	string nextLevelName = "";
	[SerializeField]
	float radius;

	void Update ()
	{
		if (GameWorld.Instance.Core) {
			if (Vector3.Distance (transform.position, GameWorld.Instance.Core.transform.position) < radius) {
				GameWorld.Instance.OnCoreTouchedGoal (this);
			}
		}
	}

	void OnDrawGizmos ()
	{
		Gizmos.color = new Color (0, 1f, 0, 0.33f);
		Gizmos.DrawSphere (transform.position, radius);
	}

	public void LoadConfiguredLevel ()
	{
		Scene s = SceneManager.GetSceneByName (nextLevelName);
		if (s.IsValid () && !s.isLoaded) {
			SceneManager.LoadScene (s.name, LoadSceneMode.Single);
		}
	}

}
