﻿using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;
using UnityEngine;
using System;
using ModularBehaviour;
using Pathfinding;

[RequireComponent (typeof(SlimeHandler))]
public class GameWorld : MonoBehaviour
{
	#region Global

	[Header ("Updating")]
	Coroutine humanUpdater;
	public float humanTickInterval = 0.1f;
	float humanDelta = 0;
	System.Diagnostics.Stopwatch humanStopWatch = new System.Diagnostics.Stopwatch ();
	Coroutine nodeUpdater;
	public float nodeTickInterval = 0.1f;
	float nodeDelta = 0;
	System.Diagnostics.Stopwatch nodeStopWatch = new System.Diagnostics.Stopwatch ();
	public float coreTickInterval = 0.1f;
	float lastCoreUpdate = 0;

	public static event Action<Message> OnMessageBroadcast;
	public static event Action<Entity> OnNodeDestroyed;
	public static event Action<Entity> OnCoreKilled;

	public const float nodeConnectionDistance = 3f;

	static GameWorld instance;

	public static GameWorld Instance {
		get {
			if (instance == null) {
				instance = FindObjectOfType<GameWorld> ();
			}
			return instance;
		}
	}

	static float levelStartTime;
	public static float LevelTime;

	#endregion

	List<FungusNode> nodes = new List<FungusNode> ();

	public ReadOnlyCollection<FungusNode> Nodes { get { return nodes.AsReadOnly (); } }

	List<Human> humans = new List<Human> ();

	public ReadOnlyCollection<Human> Humans { get { return humans.AsReadOnly (); } }

	List<PoliceStation> policeStations = new List<PoliceStation> ();

	public ReadOnlyCollection<PoliceStation> PoliceStations { get { return policeStations.AsReadOnly (); } }

	List<PoliceCar> policeCars = new List<PoliceCar> ();

	public ReadOnlyCollection<PoliceCar> PoliceCars { get { return policeCars.AsReadOnly (); } }

	FungusCore core;

	public bool disableDeltaCompensation = false;

	public FungusCore Core { get { return core; } }

	public bool destroyDisconnectedNodes = true;

	public bool IsPaused;

	[SerializeField]
	SlimeHandler slimeHandler;

	[SerializeField]
	Transform entityHolder;

	[Header ("Prefabs")]
	[SerializeField]
	FungusCore corePrefab;
	[SerializeField]
	FungusNode fungusNodePrefab;

	[Header ("Masks")]
	public LayerMask ObstacleLayer;

	LevelEventDispatcher eventDispatcher;

	SpawnerTriggerCollection triggerCollection;

	[RuntimeInitializeOnLoadMethod]
	static void InitApplication ()
	{
	    Application.targetFrameRate = 30;
#if DEBUG
		new GameObject ("DebugHelper").AddComponent<DebugHelper> ();
#endif
#if UNITY_ANDROID
		Screen.orientation = StandardGameSettings.Get.androidScreenOrientation;
#endif
	}

	void Awake ()
	{
		instance = this;
		slimeHandler = GetComponent<SlimeHandler> ();
		PlayerRecord.Initialize ("SomeOne");
	}

	void Start ()
	{
		if (triggerCollection == null) {
			triggerCollection = FindObjectOfType<SpawnerTriggerCollection> ();
		}
		GameInput.ReleaseSpawnFungusCallback (SpawnFungusNode);
		GameInput.RegisterSpawnFungusCallback (SpawnFungusNode);
		/*if (nodeUpdater == null) {
			nodeUpdater = StartCoroutine (NodeUpdate ());
		}
		if (humanUpdater == null) {
			humanUpdater = StartCoroutine (HumanUpdate ());
		}*/
		eventDispatcher = FindObjectOfType<LevelEventDispatcher> ();
		if (eventDispatcher == null) {
			throw new NullReferenceException ("No EventDispatcher found in Level");
		}
		eventDispatcher.FireEvent (LevelEventType.Start);
	}

	void OnLevelWasLoaded ()
	{
		levelStartTime = Time.time;
	}

	bool gameShuttingDown = false;

	void OnApplicationQuit ()
	{
		gameShuttingDown = true;
	}

	void Update ()
	{
		if (IsPaused) {
			GameInput.Instance.enabled = false;
		} else {
			GameInput.Instance.enabled = true;
		}
		LevelTime = Time.time - levelStartTime;
		slimeHandler.UpdateSlimeConnections ();
		if (core) {
			if (Time.time - lastCoreUpdate >= coreTickInterval) {
				core.UpdateEntity (coreTickInterval);
				if (triggerCollection) {
					triggerCollection.EvaluateForCore (core.transform.position);
				}
				lastCoreUpdate = Time.time;
			}
		}
	}

	IEnumerator HumanUpdate ()
	{
		RESTART:
		while (IsPaused)
			yield return null;
		humanDelta = humanStopWatch.ElapsedMilliseconds / 1000f;
		humanStopWatch.Reset ();
		humanStopWatch.Start ();
		for (int i = humans.Count; i-- > 0;) {
			humans [i].UpdateEntity (humanDelta);
		}
		for (int i = 0; i < policeCars.Count; i++) {
			policeCars [i].UpdateEntity (humanDelta);
		}
		yield return new WaitForSeconds (humanTickInterval - (disableDeltaCompensation ? 0 : humanStopWatch.ElapsedMilliseconds / 1000f));
		goto RESTART;
	}

	IEnumerator NodeUpdate ()
	{
		RESTART:
		while (IsPaused)
			yield return null;
		nodeDelta = nodeStopWatch.ElapsedMilliseconds / 1000f;
		nodeStopWatch.Reset ();
		nodeStopWatch.Start ();
		for (int i = nodes.Count; i-- > 0;) {
			nodes [i].UpdateEntity (nodeDelta);
		}
		yield return new WaitForSeconds (nodeTickInterval - (disableDeltaCompensation ? 0 : nodeStopWatch.ElapsedMilliseconds / 1000f));
		goto RESTART;
	}

	public void SpawnNodeAndCenter ()
	{
		FungusStart start = FindObjectOfType<FungusStart> ();
		if (!start) {
			Debug.LogError ("No Startpoint defined!");
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPaused = true;
#endif
			return;
		}
		SpawnFungusNode (start.transform.position);
		SpawnCore (start.transform.position);
		deathEventCalled = false;
	    goalReachedCalled = false;
	}

	public bool SpawnCore (Vector3 position)
	{
		if (core != null) {
			Debug.LogError ("A core is already registered!");
			return false;
		}
		Instantiate (corePrefab, position, Quaternion.Euler (90.0f, 0.0f, 0.0f));
		return true;
	}

	public FungusNode SpawnFungusNode (Vector3 position)
	{
		//TODO limit?
		var fn = Instantiate (fungusNodePrefab, position, Quaternion.identity) as FungusNode;
		return fn;
	}

	public void RestartCurrentLevel ()
	{
		Debug.Log ("Restarting Level, TODO: reset resources, abilities etc.");
		RemoveAllEntities ();
		slimeHandler.ClearAllConnections ();
		RemoveAllSlimeTags ();
		FungusResources.Instance.Reset ();
		UserMenu.current.UpdateEnabledAbilities ();
	    foreach (var spawner in FindObjectsOfType<EntitySpawner>())
	    {
	        spawner.CancelWorkers();
	    }
	    foreach (var spt in FindObjectsOfType<SpawnerTriggerCollection>())
	    {
	        spt.Reset();
	    }
		var t = FindObjectOfType<Tutorials.Tutorial> ();
		if (t) {
			t.Reset ();
		}
		Start ();
	}

	void RemoveAllEntities ()
	{
		if (core) {
			Destroy (core.gameObject);
			core = null;
		}
		for (int i = 0; i < nodes.Count; i++) {
			Destroy (nodes [i].gameObject);
		}
		nodes.Clear ();
		for (int i = 0; i < policeCars.Count; i++) {
			Destroy (policeCars [i].gameObject);
		}
		policeCars.Clear ();
		//for (int i = 0; i < policeStations.Count; i++)
		//{
		//    Destroy(policeStations[i].gameObject);
		//}
		//policeStations.Clear();
		for (int i = 0; i < humans.Count; i++) {
			Destroy (humans [i].gameObject);
		}
		humans.Clear ();
	}

	#region Helper

	public List<Vector3> SmoothPath (List<Vector3> vectorPath, bool likeSlime, SimpleSmoothModifier.SmoothType smoothType = SimpleSmoothModifier.SmoothType.Simple)
	{
		if (likeSlime) {
			return slimeHandler.SmoothLikeSlime (vectorPath);
		}
		return slimeHandler.Smooth (vectorPath, smoothType);
	}

	#endregion

	#region Registry

	public void Register (Entity e)
	{
		FungusNode fn = e as FungusNode;
		if (fn) {
			if (!nodes.Contains (fn)) {
				nodes.Add (fn);
				fn.transform.parent = entityHolder;
				if (triggerCollection) {
					triggerCollection.EvaluateForNode (fn.transform.position);
				}
			}
			return;
		}
		Human en = e as Human;
		if (en) {
			if (!humans.Contains (en)) {
				humans.Add (en);
				en.transform.parent = entityHolder;
			}
			return;
		}
		PoliceCar pc = e as PoliceCar;
		if (pc) {
			if (!policeCars.Contains (pc)) {
				policeCars.Add (pc);
				pc.transform.parent = entityHolder;
			}
		}
		FungusCore fc = e as FungusCore;
		if (fc) {
			if (core != null) {
				if (core == fc) {
					return;
				}
				Debug.LogError ("Multiple cores in Level!");
#if UNITY_EDITOR
				UnityEditor.EditorApplication.isPaused = true;
#endif
			} else {
				core = fc;
				fc.transform.parent = entityHolder;
			}
		}
		PoliceStation ps = e as PoliceStation;
		if (ps) {
			if (!policeStations.Contains (ps)) {
				policeStations.Add (ps);
			}
		}
	}

	public void Unregister (Entity e)
	{
		FungusNode fn = e as FungusNode;
		if (fn) {
			nodes.Remove (fn);
			CleanupAfterNodes ();
			return;
		}
		Human en = e as Human;
		if (en) {
			humans.Remove (en);
			return;
		}
		PoliceCar pc = e as PoliceCar;
		if (pc) {
			policeCars.Remove (pc);
			return;
		}
		FungusCore fc = e as FungusCore;
		if (fc) {
			if (core == fc) {
				core = null;
			}
			return;
		}
		PoliceStation ps = e as PoliceStation;
		if (ps) {
			policeStations.Remove (ps);
		}
	}

	#endregion

	#region Queries

	public bool HasLineOfSight (Entity source, Entity target)
	{
		return !AstarPath.active.astarData.gridGraph.Linecast (source.transform.position, target.transform.position);
	}

	public void BroadcastToHumans (Message message, Vector3 position, float radius = float.PositiveInfinity)
	{    
		for (int i = 0; i < humans.Count; i++) {
			if (humans [i] == message.sender) {
				continue;
			}
			if (AstarMath.SqrMagnitudeXZ (humans [i].transform.position, position) <= radius * radius) {
				humans [i].ReceiveBroadcast (message);
			}
		}
		if (OnMessageBroadcast != null) {
			OnMessageBroadcast (message);
		}
	}

	public void BroadcastToNodes (Message message, Vector3 position, float radius = float.PositiveInfinity)
	{
		for (int i = 0; i < nodes.Count; i++) {
			if (nodes [i] == message.sender) {
				continue;
			}
			if (AstarMath.SqrMagnitudeXZ (nodes [i].transform.position, position) <= radius * radius) {
				nodes [i].ReceiveBroadcast (message);
			}
		}
	}

	public void BroadcastToPoliceStations (Message message, Vector3 position, float radius = float.PositiveInfinity)
	{
		for (int i = 0; i < policeStations.Count; i++) {
			if (policeStations [i] == message.sender) {
				continue;
			}
			if (AstarMath.SqrMagnitudeXZ (policeStations [i].transform.position, position) <= radius * radius) {
				policeStations [i].ReceiveBroadcast (message);
			}
		}
	}

	public List<Human> GetHumans (Vector3 position, float radius, Human except = null)
	{
		var rangeQuery = new List<Human> ();
		for (var i = 0; i < humans.Count; i++) {
			if (except == humans [i]) {
				continue;
			}
			if (AstarMath.SqrMagnitudeXZ (humans [i].transform.position, position) <= radius * radius) {
				rangeQuery.Add (humans [i]);
			}
		}
		return rangeQuery;
	}

	public List<Human> GetHumans (Vector3 position, float radius, IntelligenceType filter)
	{
		var rangeQuery = new List<Human> ();
		for (var i = 0; i < humans.Count; i++) {
			if (!humans [i].Behaviour) {
				continue;
			}
			var humanClassification = humans [i].Behaviour.Classification;
			if (filter == IntelligenceType.Human) {
				if (humanClassification != IntelligenceType.Human & humanClassification != IntelligenceType.Citizen &
				                humanClassification != IntelligenceType.Police) {
					continue;
				}
			} else {
				if (humanClassification != filter) {
					continue;
				}
			}
			if (AstarMath.SqrMagnitudeXZ (humans [i].transform.position, position) <= radius * radius) {
				rangeQuery.Add (humans [i]);
			}
		}
		return rangeQuery;
	}

	public Human GetNearestHuman (Vector3 position)
	{
		if (humans.Count == 0) {
			return null;
		}
		Human nearest = humans [0];
		float dist = AstarMath.SqrMagnitudeXZ (nearest.transform.position, position);
		for (int i = 1; i < humans.Count; i++) {
			float curDist = AstarMath.SqrMagnitudeXZ (humans [i].transform.position, position);
			if (curDist < dist) {
				nearest = humans [i];
				dist = curDist;
			}
		}
		return nearest;
	}

	public Human GetNearestHuman (Vector3 position, IntelligenceType typeFilter)
	{
		if (typeFilter == IntelligenceType.Undefined) {
			return GetNearestHuman (position);
		}
		if (humans.Count == 0) {
			return null;
		}
		var nearest = humans [0];
		var dist = float.PositiveInfinity;
		bool found = false;
		for (var i = 1; i < humans.Count; i++) {
			if (!humans [i].Behaviour)
				continue;
			if (typeFilter == IntelligenceType.Human) {
				IntelligenceType ht = humans [i].Behaviour.Classification;
				if (ht != IntelligenceType.Citizen & ht != IntelligenceType.Human & ht != IntelligenceType.Police) {
					continue;
				}
			} else {
				if (humans [i].Behaviour.Classification != typeFilter) {
					continue;
				}
			}
			var curDist = AstarMath.SqrMagnitudeXZ (humans [i].transform.position, position);
			if (curDist < dist) {
				nearest = humans [i];
				dist = curDist;
				found = true;
			}
		}
		if (!found) {
			return null;
		}
		return nearest;
	}

	public PoliceStation GetNearestPoliceStation (Vector3 position)
	{
		if (policeStations.Count == 0) {
			return null;
		}
		PoliceStation nearest = policeStations [0];
		float dist = AstarMath.SqrMagnitudeXZ (nearest.transform.position, position);
		for (int i = 1; i < policeStations.Count; i++) {
			float curDist = AstarMath.SqrMagnitudeXZ (policeStations [i].transform.position, position);
			if (curDist < dist) {
				nearest = policeStations [i];
				dist = curDist;
			}
		}
		return nearest;
	}

	public List<FungusNode> GetFungusNodes (Vector3 position, float radius, FungusNode except = null)
	{
		List<FungusNode> rangeQuery = new List<FungusNode> ();
		for (int i = 0; i < nodes.Count; i++) {
			if (except == nodes [i]) {
				continue;
			}
			if (AstarMath.SqrMagnitudeXZ (nodes [i].transform.position, position) <= radius * radius) {
				rangeQuery.Add (nodes [i]);
			}
		}
		return rangeQuery;
	}

	public FungusNode GetNearestFungusNode (Vector3 position)
	{
		if (nodes.Count == 0) {
			return null;
		}
		FungusNode nearest = nodes [0];
		float dist = AstarMath.SqrMagnitudeXZ (nearest.transform.position, position);
		for (int i = 1; i < nodes.Count; i++) {
			float curDist = AstarMath.SqrMagnitudeXZ (nodes [i].transform.position, position);
			if (curDist < dist) {
				nearest = nodes [i];
				dist = curDist;
			}
		}
		return nearest;
	}

	public FungusCore CoreInRange (Vector3 _position, float _coreInputRange)
	{
		if (AstarMath.SqrMagnitudeXZ (core.transform.position, _position) < _coreInputRange) {
			return core;
		}

		return null;
	}

	public Vector3 GetDirectionToRandomFungusNode (Vector3 sourcePoint)
	{
		FungusNode node = nodes [UnityEngine.Random.Range (0, nodes.Count - 1)];
		return (node.transform.position - sourcePoint).normalized;
	}

	public List<PoliceCar> GetPoliceCars (Vector3 position, float radius)
	{
		var rangeQuery = new List<PoliceCar> ();
		for (int i = 0; i < policeCars.Count; i++) {
			if (AstarMath.SqrMagnitudeXZ (policeCars [i].transform.position, position) <= radius * radius) {
				rangeQuery.Add (policeCars [i]);
			}
		}
		return rangeQuery;
	}

	#endregion

	#region Slime

	void CleanupAfterNodes ()
	{
		if (nodes.Count == 0) {
			slimeHandler.ClearAllConnections ();
			RemoveAllSlimeTags ();
		}
	}

	public void SetPositionIsSlime (Vector3 point, float size, bool state)
	{
		if (gameShuttingDown) {
			return;
		}
		slimeHandler.QueueSlimeUpdate (point, size, state);
	}

	public bool GetPositionIsSlime (Vector3 point, float toleranceRadius)
	{
		if (gameShuttingDown) {
			return false;
		}
		return slimeHandler.GetPositionIsSlime (point, toleranceRadius);
	}

	public void RemoveAllSlimeTags ()
	{
		if (gameShuttingDown) {
			return;
		}
		AstarPath.active.FlushGraphUpdates ();
		GraphUpdateObject guo = new GraphUpdateObject (new Bounds (Vector3.zero, Vector3.one * 10000f));
		guo.modifyTag = true;
		guo.updatePhysics = false;
		guo.modifyWalkability = false;
		guo.setTag = 0;
		AstarPath.active.UpdateGraphs (guo);
	}

	/// <summary>
	/// untested, may crash
	/// </summary>
	public bool IsConnectedToBrain (FungusNode node)
	{
		FungusNode nearestToBrain = GetNearestFungusNode (core.transform.position); //TODO: replace with better guess (Vector Project + distance to projection (slimeradius))
		HashSet<FungusNode> traversed = new HashSet<FungusNode> ();
		return RecurseIsConnectedToBrain (node, nearestToBrain, traversed);
	}

	bool RecurseIsConnectedToBrain (FungusNode searchNode, FungusNode currentNode, HashSet<FungusNode> traversed)
	{
		if (searchNode == currentNode) {
			return true;
		}
		for (int i = 0; i < currentNode.Connections.Count; i++) {
			if (!traversed.Add (currentNode.Connections [i])) {
				continue;
			}
			if (RecurseIsConnectedToBrain (searchNode, currentNode.Connections [i], traversed)) {
				return true;
			}
		}
		return false;
	}

	#endregion

	#region EventHandler

	public void OnNodeConnectionInitiated (FungusNode a, FungusNode b, List<Vector3> path)
	{
		slimeHandler.AddConnection (new SlimePath (a, b, path));
	}

	public void OnNodeWasDisconnected (FungusNode node)
	{
		if (destroyDisconnectedNodes && nodes.Count > 1) {
			Destroy (node.gameObject);
		}
	}

	public void OnFungusNodeWasKilled (FungusNode node)
	{
		if (OnNodeDestroyed != null) {
			OnNodeDestroyed (node);
		}
		Destroy (node.gameObject);
	}

	public void OnHumanWasKilled (Human human)
	{
		//TODO handle resources
		Destroy (human.gameObject);
	}

	bool deathEventCalled = false;

	public void OnCoreLostGrounding (FungusCore core)
	{
		if (!deathEventCalled) {
			if (OnCoreKilled != null) {
				OnCoreKilled (core);
			}
			Debug.Log ("Core ungrounded");
			eventDispatcher.FireEvent (LevelEventType.Death);
			deathEventCalled = true;
		}
#if UNITY_EDITOR
//        UnityEditor.EditorApplication.isPaused = true;
#endif
	}

	public void OnCoreWasKilled (FungusCore core)
	{
		Debug.Log ("Core killed");
		eventDispatcher.FireEvent (LevelEventType.Death);
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPaused = true;
#endif
	}

    bool goalReachedCalled;
	public void OnCoreTouchedGoal (LevelGoal goal)
	{
	    if (!goalReachedCalled)
	    {
	        if (goal.unlockedAbility)
	        {
	            PlayerRecord.UnlockAbility(goal.unlockedAbility);
	            Debug.Log("Goal reached, ability unlocked: " + goal.unlockedAbility.name);
	        }
	        else
	        {
	            Debug.Log("Goal reached");
	        }
	        eventDispatcher.FireEvent(LevelEventType.Goal);
	        goalReachedCalled = true;
	    }
	    //TODO handle new level etc
#if UNITY_EDITOR
		//UnityEditor.EditorApplication.isPaused = true;
#endif
	}

	#endregion
}
