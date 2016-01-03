﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NodeAbilities
{
	[AbilityIdentifier("Attract")]
	[CreateAssetMenuAttribute()]
	public class Attract: NodeAbility
	{		
		public GameObject attractSpores;
		GameObject spores;
		public float influenceRadius;

		public override void Execute (FungusNode node)
		{
			if (spores == null) {
				spores = Instantiate (attractSpores, node.transform.position, Quaternion.Euler (Vector3.zero)) as GameObject;
				spores.GetComponent<ParticleSystem> ().Play ();
			}

			if (!spores.GetComponent<ParticleSystem> ().isPlaying) {
				spores.GetComponent<ParticleSystem> ().Play ();
			}

			Quaternion rotation = Quaternion.Euler (Wind.Instance.arrowTrans.transform.rotation.eulerAngles - Vector3.up * 90);
			spores.transform.rotation = Quaternion.Euler (spores.transform.rotation.eulerAngles.x, 
			                                              Wind.Instance.arrowTrans.transform.rotation.eulerAngles.y - 90,  
			                                              spores.transform.rotation.eulerAngles.z);
			Vector3 rotatedVector = (rotation * Vector3.up * radius);

			InfluenceEnemiesInArea (node, rotatedVector);

		}

		public override void StopExecution (FungusNode node)
		{
			if (spores.GetComponent<ParticleSystem> ().isPlaying) {
				spores.GetComponent<ParticleSystem> ().Stop ();
			}

		}

		private void InfluenceEnemiesInArea (FungusNode node, Vector3 rotatedVector)
		{

			Vector3 dir = Vector3.Normalize (rotatedVector);
			Vector3 tempVector = new Vector3 (0, 0, 0); 
			int i = 0;
			while (Vector3.Magnitude(tempVector) < Vector3.Magnitude(rotatedVector)) {
				List<Human> enemiesInRadius = GameWorld.Instance.GetEnemies (node.transform.position + tempVector, influenceRadius);
				//TODO: Change Behaviour of Enemies
				i++;
				tempVector = dir * (i * influenceRadius);
			}



		}
	}
}