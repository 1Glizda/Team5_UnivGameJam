using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace RW.MonumentValley
{
    public class Collectible : MonoBehaviour
    {
        [Header("Hover Settings")]
        public float hoverSpeed = 2f;
        public float hoverAmplitude = 0.25f;
        public float rotationSpeed = 90f;

        private Vector3 startPos;
        public UnityEvent onCollect;
        
        [HideInInspector]
        public List<BlockMelter> linkedMelters = new List<BlockMelter>();

        private void Start()
        {
            startPos = transform.position;
        }

        private void Update()
        {
            // Hover up and down
            float newY = startPos.y + Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            // Spin
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        public void Collect()
        {
            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null)
            {
                gm.AddCollectible();
            }

            if (onCollect != null)
            {
                onCollect.Invoke();
            }

            if (linkedMelters != null)
            {
                foreach (BlockMelter melter in linkedMelters)
                {
                    if (melter != null)
                    {
                        melter.TriggerMelt();
                    }
                }
            }

            // Destroy the visual object
            Destroy(gameObject);
        }
    }
}
