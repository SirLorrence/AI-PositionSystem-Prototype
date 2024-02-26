// //////////////////////////////
// Authors: Laurence (Git: SirLorrence)
// //////////////////////////////

using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class PositionManager : MonoBehaviour {
  private static PositionManager _instance;

  public static PositionManager Instance {
    get {
      if (_instance == null)
        SetupInstance();
      return _instance;
    }
  }

  [SerializeField] private PositionScoring[] AIScoring;


  [SerializeField] private Transform _target;

  [SerializeField] private float rings;
  private const int _initialPositions = 8;
  private float _positions;
  private Vector3 _pos;
  private const float _radiusSpacing = .5f; //radius spacing

  private PositioningPoint[] _points;
  public PositioningPoint[] Points => _points;
  public Transform Target => _target;

  public float Rings => rings;

  public void SetPositionAssignment(int index, bool value) {
    Debug.Log("Called");
    _points[index].AssignedStatus = value;
  }

  private static void SetupInstance() {
    _instance = FindObjectOfType<PositionManager>();
    if (_instance == null) {
      GameObject gameObject = new GameObject();
      gameObject.name = "Position Manager";
      _instance = gameObject.AddComponent<PositionManager>();
      DontDestroyOnLoad(gameObject);
    }
  }

  private void Awake() {
    // Lazy initialize
    if (_instance == null) {
      _instance = this;
      DontDestroyOnLoad(gameObject);
    }
    else Destroy(gameObject);
  }

  private void Start() {
    _points = InitializePoints();
    AIScoring = GameObject.FindObjectsOfType<PositionScoring>();
    if (AIScoring.Length > 0) {
      StartCoroutine(UpdatePawns());
    }
  }

  private void Update() {
    UpdatePointLocation();
  }

  private PositioningPoint[] InitializePoints() {
    // Using a Dynamic Array during the creation of the positions then using a static(sized) array
    List<PositioningPoint> initialSetPosition = new List<PositioningPoint>();

    //starting from 1 so the first ring doesn't spawn into the player
    for (int i = 1; i <= rings; i++) {
      _positions = _initialPositions * i;
      for (int j = 0; j < _positions; j++) {
        float radians = 2 * Mathf.PI / _positions * j;
        Vector3 newPoint = new Vector3(Mathf.Sin(radians), 0, Mathf.Cos(radians));
        float ringSpacing = i + _radiusSpacing;
        Vector3 creationPoint = (newPoint * ringSpacing) + _target.position;
        Vector3 vecAwayFromTarget = creationPoint - _target.position;
        PositioningPoint point = new PositioningPoint {
          CurrentPos = creationPoint,
          OffsetPos = vecAwayFromTarget,
          AssignedStatus = false
        };
        initialSetPosition.Add(point);
      }
    }

    return initialSetPosition.ToArray();
  }

  private void UpdatePointLocation() {
    for (int i = 0; i < _points.Length; i++) {
      Vector3 updateLoc = _points[i].OffsetPos + _target.position;
      updateLoc.y = 0;
      _points[i].CurrentPos = updateLoc;
    }
  }

  //TODO: Make this a job
  private IEnumerator UpdatePawns() {
    while (true) {
      foreach (var pawn in AIScoring) {
        yield return new WaitForSeconds(.25f); // to avoid position collision (data race when selecting a point)
        // Debug.Log($"[Position Manager] Update Pawn Location");
        pawn.Evaluate();
      }
    }
  }


#if DEBUG
  private void OnDrawGizmosSelected() {
    //draw points
    foreach (var positionPoint in _points) {
      var point = positionPoint.CurrentPos;
      Gizmos.color = (positionPoint.AssignedStatus) ? Color.red : Color.green;
      Gizmos.DrawSphere(point, .25f);
    }
  }
#endif
}


public struct PositioningPoint {
  public Vector3 CurrentPos { get; set; }

  /// <summary>
  /// Fixed Position from the target object
  /// </summary>
  public Vector3 OffsetPos { get; set; }

  public bool AssignedStatus { get; set; }
}