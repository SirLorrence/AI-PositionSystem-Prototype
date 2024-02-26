// //////////////////////////////
// Authors: Laurence (Git: SirLorrence)
// //////////////////////////////


using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class PositionScoring : MonoBehaviour {
  // ----------------- Properties
  public Vector3 GetUberPosition {
    get => _posManager.Points[m_currentPositionIndex].CurrentPos;
  }


  // ----------------- accessible in the Unity editor
  [SerializeField] private int frameDelay;
  [Range(0, 1)] [SerializeField] private float preferredDist; // used for distance query
  [Range(0, 2f)] [SerializeField] private float m_distanceQueryInfluence = 1;
  [Range(-0.5f, 1f)] [SerializeField] private float angleTolerance = 0; // used for dot product query
  [Range(0, 1f)] [SerializeField] private float m_angleQueryInfluence = 1;

  [Header("Position Query Parameters")] [SerializeField]
  private bool enableDistanceParam = true;

  [SerializeField] private bool enableFacingDirectionParam = true;

  //TODO: Add Flanking state 
  /*[SerializeField]*/
  private bool invertDirection = false;
  [SerializeField] private bool m_debug;

  // ---------------
  private PositionManager _posManager;
  private Transform _target;
  private PositioningPoint _uberPosition;
  private NavMeshAgent m_navAgent;
  private int _uberIndex;

  private int _pointDataSize;
  private float _frameCount;
  private float[] _positionScores;
  private bool m_reposition;
  private bool m_initFlag;


  private Int32 m_currentPositionIndex;

  private void Start() {
    _posManager = PositionManager.Instance;
    m_navAgent = GetComponent<NavMeshAgent>();
    m_initFlag = false;
    InitializeVariables();
  }

  private void Update() {
    // if (Mathf.FloorToInt(_frameCount) >= frameDelay) {
    //   if (m_debug) {
    //     Evaluate();
    //   }
    //
    //   _frameCount = 0.0f;
    // }
    //
    // _frameCount += Time.deltaTime;

    if (m_navAgent != null && m_initFlag) {
        m_navAgent.SetDestination(_posManager.Points[m_currentPositionIndex].CurrentPos);
    }
  }

  private void InitializeVariables() {
    if (_posManager != null) {
      _target = _posManager.Target;
      if (_posManager.Points != null && _posManager.Points.Length > 0) {
        _pointDataSize = _posManager.Points.Length;
        _positionScores = new float[_pointDataSize];
        _frameCount = frameDelay;
        m_currentPositionIndex = 0;
        m_reposition = true;
        m_initFlag = true;
        m_initPoint = true;
      }
    }
  }

  private void SetPositionLocation(int incomingIndex) {
    if (_posManager.Points[incomingIndex].AssignedStatus) {
      incomingIndex = HighestWeight();
    }

    // assignments
    _posManager.SetPositionAssignment(incomingIndex, true);
    if (!m_initPoint) {
      _posManager.SetPositionAssignment(m_currentPositionIndex, false);
    }

    m_currentPositionIndex = incomingIndex;
    m_reposition = false;
    m_initPoint = false;
    // TODO: Set nav mesh location
    // if (m_navAgent != null && m_initFlag) {
    //   m_navAgent.SetDestination(_posManager.Points[m_currentPositionIndex].CurrentPos);
    // }
  }

  private int HighestWeight() {
    int highest = Random.Range(0, _positionScores.Length);
    List<int> matchedWeights = new List<int>();
    for (int i = 0; i < _positionScores.Length; i++) {
      if (_positionScores[i] == _positionScores[highest] && !_posManager.Points[i].AssignedStatus) {
        matchedWeights.Add(i);
        highest = i;
      }
      else if (_positionScores[i] > _positionScores[highest] && !_posManager.Points[i].AssignedStatus) {
        highest = i;
      }
    }

    if (matchedWeights.Count > 0) {
      highest = matchedWeights[Random.Range(0, matchedWeights.Count)];
    }

    return highest;
  }

  #region Score Calculation

  public void Evaluate() {
    if (!m_initFlag) {
      InitializeVariables();
    }
    // if (_positionScores == null || _positionScores.Length == 0) {
    //   InitializeVariables(); // misses when multiple actors are spawned. need to re-initialize  
    //   // return;
    // }


    ResetScores();
    AddScores(DistanceFromTarget(), enableDistanceParam, m_distanceQueryInfluence);
    AddScores(AnglePreferenceForTarget(invertDirection), enableFacingDirectionParam, m_angleQueryInfluence);

    _positionScores = _positionScores.Select(x => (float)Math.Round(x, 2)).ToArray();
    int incomingPositionIndex = HighestWeight();
    if (_positionScores[incomingPositionIndex] > _positionScores[m_currentPositionIndex] || m_reposition) {
      SetPositionLocation(incomingPositionIndex);
    }
  }

  private void AddScores(float[] queryArrays, bool isEnabled = true, float modifier = 1) {
    if (!isEnabled) return;
    _positionScores = _positionScores.Zip(queryArrays, (score, query) => score + (query * modifier)).ToArray();
  }

  private void ResetScores() => _positionScores = _positionScores.Select(x => 0.0f).ToArray();

  #endregion


  #region Poisition Queries

  private float[] DistanceFromTarget() {
    if (_posManager == null) return null;
    // calculate the distance from each point
    float[] distArray = new float[_posManager.Points.Length];
    for (int i = 0; i < _pointDataSize; i++) {
      distArray[i] = Vector3.Distance(_target.position, _posManager.Points[i].CurrentPos);
    }

    // Normalize each point between the min and max desired points
    float[] normalizedArray = new float[distArray.Length];
    float min = distArray[0];
    float max = distArray[distArray.GetUpperBound(0)];

    min += preferredDist * (_posManager.Rings - 1);
    max += preferredDist * (_posManager.Rings - 1);

    for (int i = 0; i < _pointDataSize; i++) {
      var nDistance = 1 - Mathf.Abs((distArray[i] - min) / (max - min));
      nDistance = Mathf.Clamp01(nDistance);
      normalizedArray[i] = nDistance;
    }

    return normalizedArray;
  }

  // Can't think of a good name, so have to explain...
  /// <summary>
  /// The angle of approach which the actor will attempt to move from either in-front or behind the target.
  /// By default its set you approaching the target from the front.
  /// </summary>
  /// <param name="inverse">Flip the angle of approach, can be left null</param>
  /// <returns></returns>
  private float[] AnglePreferenceForTarget(bool inverse = false) {
    if (_posManager == null) return null;
    Vector2 pointA = new Vector2(_target.position.x - transform.position.x, _target.position.z - transform.position.z);
    float[] tempArray = new float[_pointDataSize];
    for (int i = 0; i < _pointDataSize; i++) {
      var p = _posManager.Points[i].CurrentPos;
      Vector2 pointB = new Vector2(_target.position.x - p.x, _target.position.z - p.z);
      tempArray[i] = (Vector2.Dot(pointA.normalized, pointB.normalized) * ((inverse) ? -1 : 1)) + angleTolerance;
    }

    return tempArray;
  }

  #endregion


#if DEBUG
  [SerializeField] private Gradient gradientDistance;
  [SerializeField] private Transform debugTarget;
  [SerializeField] private bool enableText = true;
  [SerializeField] private bool m_renderInWireframe = true;
  private GUIStyle _style;
  private bool m_initPoint;

  private void OnValidate() {
    _style = new GUIStyle {
      normal = {
        textColor = Color.red,
      }
    };
  }

  private void OnDrawGizmosSelected() {
    void PositionLocationText(Vector3 pos, float val, int index) {
      if (enableText)
        Handles.Label(pos + Vector3.up,
          $"Pos: {index}, Weight: {val}, Stat: {Convert.ToByte(_posManager.Points[index].AssignedStatus)}",
          _style);
    }

    if (debugTarget != null) {
      Gizmos.DrawLine(transform.position, debugTarget.position);
    }

    for (int i = 0; i < _pointDataSize; i++) {
      var pointVec = _posManager.Points[i].CurrentPos;
      var dist = _positionScores[i];


      Gizmos.color = gradientDistance.Evaluate(dist);
      if (i == m_currentPositionIndex) {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(pointVec, .25f);
        PositionLocationText(pointVec, dist, i);
        continue;
      }

      if (m_renderInWireframe) {
        Gizmos.DrawWireSphere(pointVec, .25f);
      }
      else {
        Gizmos.DrawSphere(pointVec, .25f);
      }

      // Handles.Label(pointVec + Vector3.up, $"index: {i}, Weight: {dist}");
      // if (enableText) Handles.Label(pointVec + Vector3.up, $"Pos: {i}, Weight: {dist}", _style);
      if (enableText)
        Handles.Label(pointVec + Vector3.up,
          $"Pos: {i}, Weight: {dist}, Stat: {Convert.ToByte(_posManager.Points[i].AssignedStatus)}", _style);
    }
  }
#endif
}