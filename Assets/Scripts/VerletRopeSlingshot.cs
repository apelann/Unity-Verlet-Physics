using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class VerletRopeSlingshot : MonoBehaviour
{
    [Header("Fixed Points")]
    [SerializeField] private Transform _ropeInitialPoint;
    [SerializeField] private Transform _ropeEndPoint;

    [Header("Properties")]
    [SerializeField] private float _ropeWidth = 0.1f;
    [SerializeField] private int _ropeSegmentAmount = 10;
    [SerializeField] private float _ropeSegmentLength = 0.2f;

    [Header("Physical Forces")]
    [SerializeField] private Vector2 _gravitationalForce = Vector2.down;
    [SerializeField] private float _dragForce = 0.98f;

    [Header("Constraints")]
    [SerializeField] private int _constraintAmountPerFrame = 50;

    private LineRenderer _lineRenderer;
    private RopeSegment[] _ropeSegments;
    private Vector3[] _ropeSegmentPositions;
    private int _grabbedSegmentIndex;
    private Vector2 _lastMousePosition;

    private void Awake()
    {
        if (!_ropeInitialPoint) _ropeInitialPoint = transform;
        if (!_ropeEndPoint) _ropeEndPoint = transform;

        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.widthMultiplier = _ropeWidth;
        _lineRenderer.positionCount = _ropeSegmentAmount;

        _ropeSegments = new RopeSegment[_ropeSegmentAmount];
        for (int i = 0; i < _ropeSegmentAmount; i++)
        {
            Vector2 initialSegmentPosition = _ropeInitialPoint.position + (Vector3.down * (i * _ropeSegmentLength));
            _ropeSegments[i] = new RopeSegment(initialSegmentPosition);
        }

        _ropeSegmentPositions = new Vector3[_ropeSegmentAmount];
    }
    private void FixedUpdate()
    {
        ApplyPhysics();
        for (int i = 0; i < _constraintAmountPerFrame; i++)
        {
            ApplyConstraints();
        }
    }
    private void LateUpdate()
    {
        DrawRope();
    }

    private void DrawRope()
    {
        for (int i = 0; i < _ropeSegmentAmount; i++)
        {
            _ropeSegmentPositions[i] = _ropeSegments[i].CurrentPosition;
        }

        _lineRenderer.SetPositions(_ropeSegmentPositions);
    }
    private void ApplyPhysics()
    {
        Vector2 currentGravitationalForce = _gravitationalForce * Time.fixedDeltaTime;

        for (int i = 1; i < _ropeSegmentAmount; i++)
        {
            Vector2 currentVelocityToApply = _ropeSegments[i].CalculateVelocity() * _dragForce;

            _ropeSegments[i].LastPosition = _ropeSegments[i].CurrentPosition;
            _ropeSegments[i].CurrentPosition += currentVelocityToApply + currentGravitationalForce;
        }
    }
    private void ApplyConstraints()
    {
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector2 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);

        if (Mouse.current.leftButton.isPressed)
        {
            if (_grabbedSegmentIndex == -1)
            {
                _grabbedSegmentIndex = GetIntersectingSegmentIndex(_lastMousePosition, mouseWorldPosition);
            }

            if (_grabbedSegmentIndex != -1)
            {
                Vector2 ropeVector = _ropeEndPoint.position - _ropeInitialPoint.position;
                Vector2 mouseVector = mouseWorldPosition - (Vector2)_ropeInitialPoint.position;

                float projectedT = Vector3.Dot(mouseVector, ropeVector.normalized) / ropeVector.magnitude;

                int targetIndex = Mathf.RoundToInt(projectedT * (_ropeSegmentAmount - 1));

                _grabbedSegmentIndex = Mathf.Clamp(targetIndex, 1, _ropeSegmentAmount - 2);

                _ropeSegments[_grabbedSegmentIndex].CurrentPosition = mouseWorldPosition;
                _ropeSegments[_grabbedSegmentIndex].LastPosition = mouseWorldPosition;
            }
        }
        else
        {
            _grabbedSegmentIndex = -1;
        }
        
        _lastMousePosition = mouseWorldPosition;

        Vector2 correctionVector = CalculateCorrectionVector(0);
        _ropeSegments[0].CurrentPosition = _ropeInitialPoint.position;
        if (_grabbedSegmentIndex != 1)
            _ropeSegments[1].CurrentPosition += correctionVector;
        
        for (int i = 1; i < _ropeSegmentAmount - 2; i++)
        {
            correctionVector = CalculateCorrectionVector(i);

            if (i != _grabbedSegmentIndex)
                _ropeSegments[i].CurrentPosition -= correctionVector * 0.5f;

            if ((i + 1) != _grabbedSegmentIndex)
                _ropeSegments[i + 1].CurrentPosition += correctionVector * 0.5f;
        }

        int lastIndex = _ropeSegmentAmount - 1;
        if (_grabbedSegmentIndex != lastIndex - 1)
            _ropeSegments[lastIndex - 1].CurrentPosition -= CalculateCorrectionVector(lastIndex - 1);

        _ropeSegments[lastIndex].CurrentPosition = _ropeEndPoint.position;
    }

    private Vector2 CalculateCorrectionVector(int ropeSegmentIndex)
    {
        RopeSegment currentSegment = _ropeSegments[ropeSegmentIndex];
        RopeSegment nextSegment = _ropeSegments[ropeSegmentIndex + 1];

        Vector2 differenceVector = currentSegment.CurrentPosition - nextSegment.CurrentPosition;
        float error = differenceVector.magnitude - _ropeSegmentLength;

        return differenceVector.normalized * error;
    }
    public int GetIntersectingSegmentIndex(Vector2 mousePrevPos, Vector2 mouseCurrPos)
    {
        for (int i = 1; i < _ropeSegments.Length - 2; i++)
        {
            Vector2 currentSegmentPos = _ropeSegments[i].CurrentPosition;
            Vector2 nextSegmentPos = _ropeSegments[i + 1].CurrentPosition;
            if (CheckIntersection(mousePrevPos, mouseCurrPos, currentSegmentPos, nextSegmentPos, out Vector2 intersectionPoint))
            {
                float distToCurrent = Vector2.Distance(intersectionPoint, currentSegmentPos);
                float distToNext = Vector2.Distance(intersectionPoint, nextSegmentPos);

                return (distToCurrent < distToNext) ? i : i + 1;
            }
        }

        return -1;
    }
    private bool CheckIntersection(Vector2 line1Start, Vector2 line1End, Vector2 line2Start, Vector2 line2End, out Vector2 intersectionPoint)
    {
        intersectionPoint = Vector2.zero;

        Vector2 line1Direction = line1End - line1Start;
        Vector2 line2Direction = line2End - line2Start;

        float determinant = (line1Direction.x * line2Direction.y) - (line1Direction.y * line2Direction.x);

        if (Mathf.Abs(determinant) < 0.0001f)
        {
            return false;
        }

        Vector2 startPointDifference = line2Start - line1Start;

        float line1Percentage = ((startPointDifference.x * line2Direction.y) - (startPointDifference.y * line2Direction.x)) / determinant;
        float line2Percentage = ((startPointDifference.x * line1Direction.x) - (startPointDifference.y * line1Direction.x)) / determinant;

        if (line1Percentage >= 0 && line1Percentage <= 1 && line2Percentage >= 0 && line2Percentage <= 1)
        {
            intersectionPoint = line1Start + (line1Percentage * line1Direction);
            return true;
        }

        return false;
    }

    public struct RopeSegment
    {
        public Vector2 CurrentPosition { get; set; }
        public Vector2 LastPosition { get; set; }

        public RopeSegment(Vector2 initialPosition)
        {
            CurrentPosition = initialPosition;
            LastPosition = initialPosition;
        }

        public Vector2 CalculateVelocity()
        {
            return CurrentPosition - LastPosition;
        }
    }
}