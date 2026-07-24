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

        public bool TryMarkCountedByDropbox()
        {
            if (countedByDropbox || collector != null)
            {
                return false;
            }

            countedByDropbox = true;
            collector = null;
            return true;
        }

        public void ResetForReuse()
        {
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

        protected virtual void OnValidate()
        {
            if (appearance == null)
            {
                appearance = GetComponent<CollectibleAppearance>();
            }
        }
    }
}
