using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class VerletRopeConnection : MonoBehaviour
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
        Vector2 correctionVector = CalculateCorrectionVector(0);
        _ropeSegments[0].CurrentPosition = _ropeInitialPoint.position;
        _ropeSegments[1].CurrentPosition += correctionVector;

        for (int i = 1; i < _ropeSegmentAmount - 2; i++)
        {
            correctionVector = CalculateCorrectionVector(i);

            _ropeSegments[i].CurrentPosition -= correctionVector * 0.5f;
            _ropeSegments[i + 1].CurrentPosition += correctionVector * 0.5f;
        }

        _ropeSegments[_ropeSegmentAmount - 2].CurrentPosition -= CalculateCorrectionVector(_ropeSegmentAmount - 2);
        _ropeSegments[_ropeSegmentAmount - 1].CurrentPosition = _ropeEndPoint.position;
    }

    private Vector2 CalculateCorrectionVector(int ropeSegmentIndex)
    {
        RopeSegment currentSegment = _ropeSegments[ropeSegmentIndex];
        RopeSegment nextSegment = _ropeSegments[ropeSegmentIndex + 1];

        Vector2 differenceVector = currentSegment.CurrentPosition - nextSegment.CurrentPosition;
        float error = differenceVector.magnitude - _ropeSegmentLength;

        return differenceVector.normalized * error;
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