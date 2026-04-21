using System.Collections.Generic;
using UnityEngine;

namespace RW.MonumentValley
{
    public class CollectibleSpawner : MonoBehaviour
    {
        [Header("Nodes to spawn collectibles on")]
        public List<Node> spawnNodes;

        [Header("Visual Settings")]
        public float spawnHeightOffset = 1.0f;
        public float sphereScale = 0.4f;
        public Material customMaterial; // Optional material assignment

        private void Start()
        {
            SpawnCollectibles();
        }

        private void SpawnCollectibles()
        {
            if (spawnNodes == null || spawnNodes.Count == 0) return;

            foreach (Node node in spawnNodes)
            {
                if (node == null) continue;

                // 1. Create primitive sphere
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                
                // 2. Position it above the node
                sphere.transform.position = node.transform.position + (Vector3.up * spawnHeightOffset);
                sphere.transform.localScale = new Vector3(sphereScale, sphereScale, sphereScale);
                
                // Remove the collider so it doesn't block player clicks or raycasts
                Destroy(sphere.GetComponent<Collider>());

                // Assign material if provided
                if (customMaterial != null)
                {
                    Renderer rend = sphere.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        rend.material = customMaterial;
                    }
                }

                // 3. Attach Collectible logic
                Collectible collectible = sphere.AddComponent<Collectible>();
                
                // 4. Link the collection to the Node's gameEvent
                // Now, when PlayerController invokes node.gameEvent, it will trigger Collect!
                if (node.gameEvent == null)
                {
                    node.gameEvent = new UnityEngine.Events.UnityEvent();
                }
                node.gameEvent.AddListener(collectible.Collect);
            }
        }
    }
}
