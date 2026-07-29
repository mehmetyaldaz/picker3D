using UnityEngine;

namespace Picker3D.Collectibles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class CollectibleItem : MonoBehaviour
    {
        [SerializeField] private CollectibleShape shape = CollectibleShape.Sphere;
        [SerializeField] private CollectibleAppearance appearance;

        private Rigidbody body;
        private CollectibleCollector collector;
        private bool countedByDropbox;
        private bool guidedReleaseActive;
        private bool guidedReleaseOriginalIsKinematic;
        private bool guidedReleaseOriginalUseGravity;
        private Vector3 guidedReleaseTarget;
        private Vector3 guidedReleaseVelocity;
        private float guidedReleaseSpeed;

        public CollectibleShape Shape => shape;
        public bool IsCollected => collector != null;
        public bool IsCountedByDropbox => countedByDropbox;

        protected virtual void Awake()
        {
            body = GetComponent<Rigidbody>();

            if (appearance == null)
            {
                appearance = GetComponent<CollectibleAppearance>();
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!guidedReleaseActive || body == null)
            {
                return;
            }

            Vector3 currentPosition = body.position;
            Vector3 nextPosition = Vector3.MoveTowards(
                currentPosition,
                guidedReleaseTarget,
                guidedReleaseSpeed *
                Time.fixedDeltaTime);
            Vector3 movementDelta =
                nextPosition - currentPosition;

            if (movementDelta.sqrMagnitude > 0.000001f)
            {
                guidedReleaseVelocity =
                    movementDelta /
                    Time.fixedDeltaTime;
            }

            body.MovePosition(nextPosition);
        }

        public void ApplyColor(Color color)
        {
            if (appearance == null)
            {
                appearance = GetComponent<CollectibleAppearance>();
            }

            if (appearance == null)
            {
                Debug.LogError(
                    $"{nameof(CollectibleItem)} on '{name}' requires a {nameof(CollectibleAppearance)} to change color.",
                    this);
                return;
            }

            appearance.ApplyColor(color);
        }

        public bool TryCollect(CollectibleCollector requestingCollector)
        {
            if (requestingCollector == null ||
                collector != null ||
                countedByDropbox)
            {
                return false;
            }

            collector = requestingCollector;
            return true;
        }

        public bool Release(Vector3 velocityChange)
        {
            if (collector == null)
            {
                return false;
            }

            collector = null;

            if (body != null && !body.isKinematic)
            {
                body.AddForce(velocityChange, ForceMode.VelocityChange);
            }

            return true;
        }

        public bool BeginGuidedRelease(
            Vector3 targetPosition,
            float movementSpeed)
        {
            if (collector == null ||
                countedByDropbox ||
                movementSpeed <= 0f)
            {
                return false;
            }

            collector = null;

            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            guidedReleaseTarget = targetPosition;
            guidedReleaseSpeed = movementSpeed;
            guidedReleaseActive = true;
            guidedReleaseOriginalIsKinematic =
                body.isKinematic;
            guidedReleaseOriginalUseGravity =
                body.useGravity;

            Vector3 initialDirection =
                targetPosition - body.position;
            guidedReleaseVelocity =
                initialDirection.sqrMagnitude > 0.0001f
                    ? initialDirection.normalized *
                      movementSpeed
                    : Vector3.zero;

            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            body.isKinematic = true;
            body.useGravity = false;
            return true;
        }

        public bool TryMarkCountedByDropbox()
        {
            if (countedByDropbox || collector != null)
            {
                return false;
            }

            countedByDropbox = true;
            collector = null;
            StopGuidedRelease(true);
            return true;
        }

        public void ResetForReuse()
        {
            StopGuidedRelease(false);
            collector = null;
            countedByDropbox = false;

            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        private void StopGuidedRelease(
            bool preserveReleaseVelocity)
        {
            if (!guidedReleaseActive || body == null)
            {
                return;
            }

            guidedReleaseActive = false;
            body.isKinematic =
                guidedReleaseOriginalIsKinematic;
            body.useGravity =
                guidedReleaseOriginalUseGravity;

            if (!body.isKinematic)
            {
                body.linearVelocity =
                    preserveReleaseVelocity
                        ? guidedReleaseVelocity
                        : Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        protected virtual void OnValidate()
        {
            if (appearance == null)
            {
                appearance = GetComponent<CollectibleAppearance>();
            }
        }
    }
}
