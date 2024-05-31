# AI Positioning System Prototype
I created my own sudo environment query system to get a more dynamic positioning for enemy AI agents. I made it so that I can easily customize and add to get different "positioning behaviors." More Information to come and will be posted on my [website](https://laurencesadler.com/projects/positioning-system-prototype/).

## Preview
<p align="center">
  <img src="https://github.com/SirLorrence/ReadMeImages/blob/main/AI-PositionSystem-Prototype/All-together-gif.gif?raw=true">
</p>

### Code
**Initialization of Points Sample:**
```
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
```



**Query Sample:**
```
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
```
