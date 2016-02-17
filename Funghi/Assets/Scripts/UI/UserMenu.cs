﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ModularBehaviour;

public class UserMenu : MonoBehaviour
{

	public enum UserMenuButtonType
	{
		Skill,
		Brain,
		Build,
		None
	}

	public enum AbilityType
	{
		Eat,
		Lure,
		Enslave,
		Spawn,
		Slow,
		Speedup
	}

	/// <summary>
	/// This event passes the player requested wind direction.
	/// </summary>
	public static event System.Action<float> OnRequestWindDirection;

	[SerializeField] AbilityFader abilityFader;
	[SerializeField] Anemometer anemo;

	[SerializeField] RectTransform[] buttons;
	[SerializeField] UserAbilityButton[] abilityButtons;

	readonly float[] _activeButtonPositions = new float[3];
	public float InactiveButtonOffset;
	float _activeMenuPosition;
	public float InactiveMenuOffset;

	public Color AbilityActiveColor;
	public Color AbilityInactiveColor;

	const float StandardFadeTimeAbilities = 0.1f;
	const float StandardFadeTimeButtons = 0.25f;

	Transform audioAnchor;
	public AudioClip interactionSound;

	bool _abilityRectActive;
	UserMenuButtonType _activeButton = UserMenuButtonType.None;

	public static UserMenu current;

	[SerializeField] MainMenu mainMenu;

	void Start ()
	{
		current = this;
		audioAnchor = Camera.main.transform.FindChild ("Listener");
		for (var i = 1; i < buttons.Length; i++) {
			_activeButtonPositions [i - 1] = buttons [i].anchoredPosition.x;
		}
		_activeMenuPosition = buttons [0].anchoredPosition.x;

		BlendOut ();
		OnMenuInactive ();
		Wind.OnWind += SetAnemometerDirection;

		FungusResources.Instance.beatneat.isUnlocked = true;
		FungusResources.Instance.attract.isUnlocked = true;
		FungusResources.Instance.growth.isUnlocked = false;
		FungusResources.Instance.zombies.isUnlocked = false;
		FungusResources.Instance.slowdown.isUnlocked = false;
		FungusResources.Instance.speedup.isUnlocked = false;

		UpdateEnabledAbilities ();

		OnBuildSelected ();
		OnMenuInactive ();

	}

	public void UpdateEnabledAbilities ()
	{
		EnableOrDisableAbility (AbilityType.Eat, FungusResources.Instance.beatneat.isUnlocked);
		EnableOrDisableAbility (AbilityType.Lure, FungusResources.Instance.attract.isUnlocked);
		EnableOrDisableAbility (AbilityType.Spawn, FungusResources.Instance.growth.isUnlocked);
		EnableOrDisableAbility (AbilityType.Enslave, FungusResources.Instance.zombies.isUnlocked);
		EnableOrDisableAbility (AbilityType.Slow, FungusResources.Instance.slowdown.isUnlocked);
		EnableOrDisableAbility (AbilityType.Speedup, FungusResources.Instance.speedup.isUnlocked);
	}

	void OnDestroy ()
	{
		Wind.OnWind -= SetAnemometerDirection;
	}

	public void OnAnemoRequestsWindDirection (float degree)
	{
		if (OnRequestWindDirection == null)
			return;
		OnRequestWindDirection (degree);
	}

	/// <summary>
	/// This opens the menu - TODO
	/// </summary>
	public void OnMenuSelected ()
	{
		mainMenu.OpenMenu ();
	}

	public void OnAbilitySelected (AbilityType type)
	{
		if (GameWorld.Instance.IsPaused)
			return;
		//ForceBlendOut ();
		GameInput.Instance.SelectSkill (type);
		AudioSource.PlayClipAtPoint (interactionSound, audioAnchor.position);
	}

	public void OnBrainSelected ()
	{
		if (GameWorld.Instance.IsPaused)
			return;
		//implement handling here
		GameInput.Instance.ActivateBrainMode ();
		AudioSource.PlayClipAtPoint (interactionSound, audioAnchor.position);
	}

	public void OnBuildSelected ()
	{
		if (GameWorld.Instance.IsPaused)
			return;
		//implement handling here
		GameInput.Instance.ActivateBuildMode ();
		AudioSource.PlayClipAtPoint (interactionSound, audioAnchor.position);
	}

	/// <summary>
	/// Use this to un/lock the ui ability buttons
	/// </summary>
	public void EnableOrDisableAbility (AbilityType type, bool state)
	{
		abilityButtons [(int)type].SetActivatedState (state, state ? AbilityActiveColor : AbilityInactiveColor);
	}

	/// <summary>
	/// Use this to set the ui anemometer
	/// </summary>
	/// <param name="degree"></param>
	public void SetAnemometerDirection (float degree)
	{
		anemo.SetOrientation (degree);
	}

	public void StartChangingWind ()
	{
		GameInput.Instance.ActivateWindMode ();
	}

	public void StopChangingWind ()
	{
		GameInput.Instance.DeactivateMode (UserMenu.UserMenuButtonType.Build);
	}



	#region Fading

	void ForceBlendOut ()
	{
		_activeButton = UserMenuButtonType.None;
		_abilityRectActive = false;
		BlendOut ();
	}

	public void OnMenuButtonActive (UserMenuButtonType type)
	{
		_activeButton = type;
		BlendIn ();
		if (type != UserMenuButtonType.Skill & !_abilityRectActive) {
			abilityFader.FadeOut (StandardFadeTimeAbilities);
		}

		GameInput.Instance.ActivateMode (type);

	}

	public void OnMenuButtonInactive (UserMenuButtonType type)
	{
		_activeButton = UserMenuButtonType.None;
		BlendOut ();


	}

	public void OnAbilityRectActive ()
	{
		_abilityRectActive = true;
		BlendIn ();
	}

	public void OnAbilityRectInactive ()
	{
		_abilityRectActive = false;
		BlendOut ();
		//Debug.Log ("OnAbilityRectInactive");
		//GameInput.Instance.DeactivateMode (UserMenuButtonType.Skill);
	}

	public void OnMenuActive ()
	{
		if (menuFadeRoutine != null)
			StopCoroutine (menuFadeRoutine);
		menuFadeRoutine = StartCoroutine (FadeMenu (StandardFadeTimeButtons, true));
	}

	public void OnMenuInactive ()
	{
		if (menuFadeRoutine != null)
			StopCoroutine (menuFadeRoutine);
		menuFadeRoutine = StartCoroutine (FadeMenu (StandardFadeTimeButtons, false));
	}

	public void BlendOut ()
	{
		if (delayedCheckRoutine != null)
			StopCoroutine (delayedCheckRoutine);
		delayedCheckRoutine = StartCoroutine (InactiveCheckTimer (0.25f));
	}

	public void BlendIn ()
	{
		if (delayedCheckRoutine != null)
			StopCoroutine (delayedCheckRoutine);
		if (_activeButton == UserMenuButtonType.Skill) {
			abilityFader.FadeIn (StandardFadeTimeAbilities);
		}
		if (routine != null)
			StopCoroutine (routine);
		routine = StartCoroutine (FadeRoutine (StandardFadeTimeButtons, true));
	}

	Coroutine routine;
	Coroutine delayedCheckRoutine;
	Coroutine menuFadeRoutine;

	IEnumerator FadeMenu (float duration, bool inOut)
	{
		float t = 0;
		while (t < 1f) {
			t += Time.deltaTime / duration;
			t = Mathf.Clamp01 (t);
			var pos = buttons [0].anchoredPosition;
			pos.x = Mathf.Lerp (pos.x, inOut ? _activeMenuPosition : _activeMenuPosition + InactiveMenuOffset, t);
			buttons [0].anchoredPosition = pos;
			yield return null;
		}
	}

	IEnumerator FadeRoutine (float duration, bool inOut)
	{
		GameInput.Instance.ActivateNoMode ();
		float t = 0;
		while (t < 1f) {
			t += Time.deltaTime / duration;
			t = Mathf.Clamp01 (t);
			for (var i = 1; i < buttons.Length; i++) {
				var pos = buttons [i].anchoredPosition;
				pos.x = Mathf.Lerp (pos.x, inOut ? _activeButtonPositions [i - 1] : _activeButtonPositions [i - 1] + InactiveButtonOffset, t);
				buttons [i].anchoredPosition = pos;
			}
			yield return null;
		}

	}

	IEnumerator InactiveCheckTimer (float duration)
	{
		while (duration > 0) {
			duration -= Time.deltaTime;
			yield return null;
		}
		if (_activeButton == UserMenuButtonType.Skill | _abilityRectActive) {
			yield break;
		}
		abilityFader.FadeOut (StandardFadeTimeAbilities);
		if (routine != null)
			StopCoroutine (routine);
		routine = StartCoroutine (FadeRoutine (StandardFadeTimeButtons, false));
	}

	Coroutine pingRoutine;

	public void PingSkillButton ()
	{
		if (pingRoutine != null) {
			StopCoroutine (pingRoutine);
		}
		pingRoutine = StartCoroutine (PingSkill ());
	}

	IEnumerator PingSkill ()
	{
		float timer = 0.0f;
		float duration = .5f;
		Vector3 startSize = buttons [2].localScale;
		while (timer < 1.0f) {
			buttons [2].localScale = Mathf.Lerp (1.0f, 1.5f, Mathf.SmoothStep (0f, 1f, timer)) * startSize;
			timer += Time.deltaTime / duration;
			yield return null;
		}

		while (timer > 0f) {
			buttons [2].localScale = Mathf.Lerp (1.0f, 1.5f, Mathf.SmoothStep (0f, 1f, timer)) * startSize;
			timer -= Time.deltaTime / duration;
			yield return null;
		}
	}

	#endregion
}
