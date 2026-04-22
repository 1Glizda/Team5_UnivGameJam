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
            // Clean up any Editor-generated preview visuals before spawning the runtime items
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
            SpawnCollectibles();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            // delayCall is crucial here because Unity forbids Destroying/Creating GameObjects directly inside OnValidate
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return; // Prevent errors if the spawner itself was deleted

                // Destroy old preview spheres
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    if (transform.GetChild(i) != null)
                    {
                        DestroyImmediate(transform.GetChild(i).gameObject);
                    }
                }

                if (spawnNodes == null || spawnNodes.Count == 0) return;

                // Procedurally draw new preview spheres directly in the Scene!
                foreach (Node node in spawnNodes)
                {
                    if (node == null) continue;

                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.transform.parent = this.transform; // Child them to the Spawner
                    sphere.transform.position = node.transform.position + (Vector3.up * spawnHeightOffset);
                    sphere.transform.localScale = new Vector3(sphereScale, sphereScale, sphereScale);
                    DestroyImmediate(sphere.GetComponent<Collider>()); // No colliders needed

                    if (customMaterial != null)
                    {
                        Renderer rend = sphere.GetComponent<Renderer>();
                        if (rend != null) rend.material = customMaterial;
                    }
                }
            };
#endif
        }

        private void SpawnCollectibles()
        {
            if (spawnNodes == null || spawnNodes.Count == 0) return;

            foreach (Node node in spawnNodes)
            {
                if (node == null) continue;

                // 1. Create primitive sphere
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.parent = this.transform;
                
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
