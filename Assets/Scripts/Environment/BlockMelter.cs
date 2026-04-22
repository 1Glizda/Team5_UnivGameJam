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

        private Material blockMat;
        private bool isMelting = false;

        private void Start()
        {
            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend != null) blockMat = rend.material; 
        }

        public void TriggerMelt()
        {
            if (isMelting) return;
            isMelting = true;
            StartCoroutine(MeltRoutine());
        }

        private IEnumerator MeltRoutine()
        {
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
            
            if (acidPuddlePrefab != null)
            {
                // Spawn a puddle on the TOP face of this block
                Vector3 topPuddlePos = transform.position + (Vector3.up * (startScale.y / 2f + 0.01f));
                SpawnPuddle(topPuddlePos, this.transform, startScale, false);

                // Bottom Puddles (permanent)
                if (isBottomBlock || !fillUnderlyingSurfaces)
                {
                    // Spawn a single footprint perfectly at the bottom of the melting block
                    SpawnPuddle(startPos - new Vector3(0, startScale.y / 2f - 0.02f, 0), null, startScale, true);
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
                        
                        // Scale the puddle to perfectly match the size of the block it landed on
                        SpawnPuddle(pos, uBlock, uBlock.localScale, true);
                    }
                }
            }

            // 3. Melt Animation Loop
            float timer = 0f;
            int meltId = Shader.PropertyToID("_MeltAmount");

            while (timer < meltDuration)
            {
                timer += Time.deltaTime;
                float rawP = Mathf.Clamp01(timer / meltDuration);
                
                // Continuous stack easing
                float easedP = rawP;
                if (isTopBlock && isBottomBlock) easedP = rawP * rawP * (3f - 2f * rawP); // SmoothStep
                else if (isTopBlock) easedP = rawP * rawP; // Ease In
                else if (isBottomBlock) easedP = 1f - (1f - rawP) * (1f - rawP); // Ease Out

                if (blockMat != null && blockMat.HasProperty(meltId)) blockMat.SetFloat(meltId, easedP);

                // Squash Y and offset position to lock bottom face
                float curY = Mathf.Lerp(startScale.y, 0.05f, easedP);
                t.localScale = new Vector3(startScale.x, curY, startScale.z);
                t.position = startPos - new Vector3(0, (startScale.y - curY) / 2f, 0);

                yield return null;
            }

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

        private void SpawnPuddle(Vector3 pos, Transform parent, Vector3 sourceScale, bool includeParticles = false)
        {
            // Instantiate unparented first to establish global size
            GameObject puddle = Instantiate(acidPuddlePrefab, pos, Quaternion.Euler(90, 0, 0));
            puddle.transform.localScale = new Vector3(sourceScale.x, sourceScale.z, sourceScale.y); // Match block X/Z explicitly
            
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
