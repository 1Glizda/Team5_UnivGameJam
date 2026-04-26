using System.Collections.Generic;
using UnityEngine;

namespace RW.MonumentValley
{
    [System.Serializable]
    public class CollectibleSpawnData
    {
        public Node node;
        [Tooltip("Optional: Specific blocks to melt. If left empty, it tries to melt the block underneath it.")]
        public List<BlockMelter> targetMelters;
    }

    public class CollectibleSpawner : MonoBehaviour
    {
        [Header("Nodes to spawn collectibles on")]
        public List<CollectibleSpawnData> spawnPoints;

        [Header("Visual Settings")]
        public float spawnHeightOffset = 1.0f;
        public float sphereScale = 0.4f;
        public Material customMaterial; // Optional material assignment

        private void Start()
        {
            // At runtime, the spheres already exist because OnValidate created them persistently!
            // We just need to link them to their Node's gameEvent.
            if (spawnPoints == null) return;

            foreach (CollectibleSpawnData data in spawnPoints)
            {
                if (data == null || data.node == null) continue;

                string expectedName = "Collectible_" + data.node.name;
                Transform childSphere = transform.Find(expectedName);
                
                if (childSphere != null)
                {
                    Collectible collectible = childSphere.GetComponent<Collectible>();
                    if (collectible != null)
                    {
                        if (data.node.gameEvent == null)
                        {
                            data.node.gameEvent = new UnityEngine.Events.UnityEvent();
                        }
                        data.node.gameEvent.AddListener(collectible.Collect);
                    }
                }
            }
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            // delayCall is crucial here because Unity forbids Destroying/Creating GameObjects directly inside OnValidate
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return; // Prevent errors if the spawner itself was deleted
                if (Application.isPlaying) return; // CRITICAL: Do not overwrite runtime objects!

                if (spawnPoints == null) return;

                List<GameObject> activeSpheres = new List<GameObject>();

                // Procedurally draw and update spheres directly in the Scene, and keep them persistent!
                foreach (CollectibleSpawnData data in spawnPoints)
                {
                    if (data == null || data.node == null) continue;

                    string expectedName = "Collectible_" + data.node.name;
                    Transform existingSphere = transform.Find(expectedName);
                    GameObject sphere;

                    if (existingSphere != null)
                    {
                        sphere = existingSphere.gameObject;
                    }
                    else
                    {
                        // Create a brand new persistent sphere
                        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        sphere.name = expectedName;
                        sphere.transform.parent = this.transform; 
                        DestroyImmediate(sphere.GetComponent<Collider>()); // No colliders needed
                        
                        // Add the Collectible script IN THE EDITOR so the user can click it and configure it!
                        sphere.AddComponent<Collectible>();
                    }

                    Collectible collectible = sphere.GetComponent<Collectible>();
                    if (collectible != null)
                    {
                        // ALWAYS update the linked melter, even if the sphere already existed!
                        if (data.targetMelters != null && data.targetMelters.Count > 0)
                        {
                            collectible.linkedMelter = data.targetMelters[0];
                        }
                        else
                        {
                            BlockMelter melter = data.node.GetComponentInChildren<BlockMelter>();
                            if (melter == null) melter = data.node.GetComponentInParent<BlockMelter>();
                            collectible.linkedMelter = melter;
                        }
                    }

                    activeSpheres.Add(sphere);

                    // Update position and scale
                    sphere.transform.position = data.node.transform.position + (Vector3.up * spawnHeightOffset);
                    sphere.transform.localScale = new Vector3(sphereScale, sphereScale, sphereScale);

                    // Update material
                    if (customMaterial != null)
                    {
                        Renderer rend = sphere.GetComponent<Renderer>();
                        if (rend != null) rend.sharedMaterial = customMaterial;
                    }
                }

                // Destroy old spheres that are no longer in the spawn list
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = transform.GetChild(i);
                    if (!activeSpheres.Contains(child.gameObject))
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            };
#endif
        }
    }
}
