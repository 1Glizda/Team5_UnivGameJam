using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RW.MonumentValley
{
    public class BlockMelter : MonoBehaviour
    {
        [Header("Settings")]
        public float meltDuration = 2.5f;
        public GameObject acidSizzleParticles;
        public GameObject acidPuddlePrefab;
        
        [Tooltip("If true, the puddle covers the exact surfaces of blocks underneath. If false, it spawns a single puddle at the bottom of THIS block.")]
        public bool fillUnderlyingSurfaces = true;

        [Header("Material Override")]
        [Tooltip("If assigned, the script will instantly swap the object to this material right before it melts!")]
        public Material meltingMaterialOverride;

        private List<Material> blockMats = new List<Material>();
        private bool isMelting = false;

        private void Start()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[BlockMelter] No Renderers found on {gameObject.name} or its children! Cannot melt visuals.");
            }

            foreach (Renderer rend in renderers)
            {
                if (rend != null)
                {
                    blockMats.Add(rend.material); 
                    
                    if (!rend.material.HasProperty("_MeltAmount"))
                    {
                        Debug.LogWarning($"[BlockMelter] The material '{rend.material.name}' on '{rend.gameObject.name}' DOES NOT have a '_MeltAmount' property! Make sure you assigned the custom melting shader to this FBX!");
                    }
                }
            }
        }

        public void TriggerMelt()
        {
            Debug.Log($"[BlockMelter] TriggerMelt called on {gameObject.name}");
            if (isMelting) return;
            isMelting = true;
            
            // Hot-swap material if an override is provided!
            if (meltingMaterialOverride != null)
            {
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                blockMats.Clear(); // Clear the old standard materials we found in Start
                
                foreach (Renderer rend in renderers)
                {
                    if (rend != null)
                    {
                        // Save the original texture so the new material looks the same!
                        Texture originalTexture = rend.material.mainTexture;
                        
                        rend.material = meltingMaterialOverride;
                        
                        if (originalTexture != null)
                        {
                            if (rend.material.HasProperty("_BaseMap")) rend.material.SetTexture("_BaseMap", originalTexture);
                            else if (rend.material.HasProperty("_MainTex")) rend.material.SetTexture("_MainTex", originalTexture);
                        }

                        blockMats.Add(rend.material);
                    }
                }
            }

            StartCoroutine(MeltRoutine());
        }

        private IEnumerator MeltRoutine()
        {
            Debug.Log("[BlockMelter] MeltRoutine started.");
            Transform t = transform;
            Vector3 startPos = t.position;
            Vector3 startScale = t.localScale;
            
            // 1. Detect Stack Position
            List<BlockMelter> nextMelters = new List<BlockMelter>();
            List<Transform> blocksBeneath = new List<Transform>();
            bool isTopBlock = true;
            bool isBottomBlock = true;
            
            Vector3 halfExtents = startScale / 2.1f;
            halfExtents.y = 0.1f; 
            
            // Check below
            foreach (RaycastHit hit in Physics.BoxCastAll(startPos, halfExtents, Vector3.down, t.rotation, 1.5f))
            {
                if (hit.transform == t || hit.transform.IsChildOf(t)) continue;
                
                isBottomBlock = false;
                if (!blocksBeneath.Contains(hit.transform)) blocksBeneath.Add(hit.transform);

                BlockMelter melter = hit.collider.GetComponentInParent<BlockMelter>() ?? hit.collider.GetComponentInChildren<BlockMelter>();
                if (melter != null && melter != this && !nextMelters.Contains(melter)) nextMelters.Add(melter);
            }

            // Check above
            foreach (RaycastHit hit in Physics.BoxCastAll(startPos, halfExtents, Vector3.up, t.rotation, 1.5f))
            {
                if (hit.transform == t || hit.transform.IsChildOf(t)) continue;
                isTopBlock = false;
                break;
            }

            // 2. Spawn Visuals
            SpawnParticles(startPos + Vector3.up * 0.6f, t);
            
            // Calculate footprint size using actual colliders rather than local scale (which might be wrong for FBXs)
            Collider col = GetComponentInChildren<Collider>();
            Vector3 footprintSize = col != null ? col.bounds.size : startScale;
            Debug.Log($"[BlockMelter] Calculated footprint size: {footprintSize}. Spawning puddles...");

            if (acidPuddlePrefab != null)
            {
                // Spawn a puddle on the TOP face of this block
                Vector3 topPuddlePos = transform.position + (Vector3.up * (footprintSize.y / 2f + 0.01f));
                SpawnPuddle(topPuddlePos, this.transform, footprintSize, false);

                // Bottom Puddles (permanent)
                if (isBottomBlock || !fillUnderlyingSurfaces)
                {
                    // Spawn a single footprint perfectly at the bottom of the melting block
                    SpawnPuddle(startPos - new Vector3(0, footprintSize.y / 2f - 0.02f, 0), null, footprintSize, true);
                }
                else
                {
                    // Filter blocks beneath so we only get the absolute highest block in each X/Z column
                    List<Transform> topBlocksBeneath = new List<Transform>();
                    float highestSurfaceY = -Mathf.Infinity;

                    foreach (Transform uBlock in blocksBeneath)
                    {
                        bool isObscured = false;
                        Collider uCol = uBlock.GetComponentInChildren<Collider>();
                        float uTop = uCol != null ? uCol.bounds.max.y : (uBlock.position.y + uBlock.localScale.y / 2f);

                        foreach (Transform other in blocksBeneath)
                        {
                            if (uBlock == other) continue;
                            Vector2 diff = new Vector2(uBlock.position.x - other.position.x, uBlock.position.z - other.position.z);
                            
                            // If they are in the exact same X/Z column
                            if (diff.sqrMagnitude < 0.1f)
                            {
                                Collider oCol = other.GetComponentInChildren<Collider>();
                                float otherTop = oCol != null ? oCol.bounds.max.y : (other.position.y + other.localScale.y / 2f);
                                
                                // If the other block is physically higher, this one is obscured and shouldn't get a puddle
                                if (otherTop > uTop)
                                {
                                    isObscured = true;
                                    break;
                                }
                            }
                        }
                        
                        if (!isObscured)
                        {
                            topBlocksBeneath.Add(uBlock);
                            if (uTop > highestSurfaceY) highestSurfaceY = uTop;
                        }
                    }

                    // Spawn puddles ONLY for the top blocks, and force ALL of them to hover exactly at the highest block's surface
                    foreach (Transform uBlock in topBlocksBeneath)
                    {
                        // Place exactly at the highest surface + 0.02f to prevent Z-fighting
                        Vector3 pos = new Vector3(uBlock.position.x, highestSurfaceY + 0.02f, uBlock.position.z);
                        
                        Collider uCol = uBlock.GetComponentInChildren<Collider>();
                        Vector3 targetFootprint = uCol != null ? uCol.bounds.size : uBlock.localScale;

                        // Scale the puddle to perfectly match the size of the block it landed on
                        SpawnPuddle(pos, uBlock, targetFootprint, true);
                    }
                }
            }

            Debug.Log("[BlockMelter] Starting Melt Animation Loop...");
            // 3. Melt Animation Loop
            float timer = 0f;
            int meltId = Shader.PropertyToID("_MeltAmount");

            int frameCount = 0;
            while (timer < meltDuration)
            {
                timer += Time.deltaTime;
                float rawP = Mathf.Clamp01(timer / meltDuration);
                
                if (frameCount == 0) Debug.Log("[BlockMelter] First frame of melt loop executing. Squishing localScale...");
                frameCount++;
                
                // Continuous stack easing
                float easedP = rawP;
                if (isTopBlock && isBottomBlock) easedP = rawP * rawP * (3f - 2f * rawP); // SmoothStep
                else if (isTopBlock) easedP = rawP * rawP; // Ease In
                else if (isBottomBlock) easedP = 1f - (1f - rawP) * (1f - rawP); // Ease Out

                foreach (Material mat in blockMats)
                {
                    if (mat != null && mat.HasProperty(meltId)) mat.SetFloat(meltId, easedP);
                }

                // Squash Y and offset position to lock bottom face
                float curY = Mathf.Lerp(startScale.y, 0.05f, easedP);
                t.localScale = new Vector3(startScale.x, curY, startScale.z);
                t.position = startPos - new Vector3(0, (startScale.y - curY) / 2f, 0);

                yield return null;
            }

            Debug.Log("[BlockMelter] Melt loop finished! Destroying object...");
            // 4. Cleanup & Cascade
            foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;

#if UNITY_2023_1_OR_NEWER
            Graph mapGraph = FindFirstObjectByType<Graph>();
#else
            Graph mapGraph = FindObjectOfType<Graph>();
#endif
            if (mapGraph != null) mapGraph.RebuildGraph();

            foreach (BlockMelter nm in nextMelters) if (nm != null) nm.TriggerMelt();

            Destroy(gameObject);
        }

        private void SpawnParticles(Vector3 pos, Transform parent)
        {
            if (acidSizzleParticles == null) return;
            GameObject p = Instantiate(acidSizzleParticles, pos, Quaternion.identity, parent);
            if (parent == transform) Destroy(p, meltDuration + 2f); // Temporary particles
        }

        private void SpawnPuddle(Vector3 pos, Transform parent, Vector3 sourceFootprintSize, bool includeParticles = false)
        {
            // Instantiate unparented first to establish global size
            GameObject puddle = Instantiate(acidPuddlePrefab, pos, Quaternion.Euler(90, 0, 0));
            
            // sourceFootprintSize is global bounding box size. 
            // Puddle is rotated 90 on X, so its local X maps to global X, and local Y maps to global Z!
            puddle.transform.localScale = new Vector3(sourceFootprintSize.x, sourceFootprintSize.z, 1f); 
            
            // Put the puddle on the "Ignore Raycast" layer so it never blocks the player's mouse clicks!
            int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
            puddle.layer = ignoreRaycastLayer;
            foreach (Transform child in puddle.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = ignoreRaycastLayer;
            }

            // Parent it now, keeping the global size exactly as we set it
            if (parent != null) puddle.transform.SetParent(parent, true);

            if (includeParticles) SpawnParticles(pos + Vector3.up * 0.1f, puddle.transform);
        }
    }
}
