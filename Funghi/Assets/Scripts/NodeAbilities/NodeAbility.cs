﻿using UnityEngine;

namespace NodeAbilities
{
    public abstract class NodeAbility: ScriptableObject
    {
        #region Presentation
        [Header("AudioVisual")]
        public Texture2D icon;
        public AudioClip executionSound;
        public AudioClip deathSound;
        #endregion
        [Header("Skill")]
        public float radius;
		public int cost;
		public float tickRate;

        public abstract bool Execute(FungusNode node);
    }
}