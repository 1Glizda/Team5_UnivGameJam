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
        
        [Tooltip("Check this if the object is an imported Blender FBX. This swaps the Y and Z axes for squashing and puddles.")]
        public bool isBlenderFBX = false;
        
        [Tooltip("If true, a puddle spawns on the top face of the block and rides it down as it melts. Turn OFF for open/hollow meshes!")]
        public bool spawnTopPuddle = false;

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
            
            Collider col = GetComponentInChildren<Collider>();
            if (col == null)
            {
                Debug.LogWarning("[BlockMelter] No Collider found! Cannot calculate bounds for melting.");
                yield break;
            }

            // Get absolute world space metrics! This completely ignores local scale weirdness (46, 180, etc)
            Vector3 worldBoundsSize = col.bounds.size;
            float startBottomY = col.bounds.min.y;
            float pivotOffset = startPos.y - startBottomY; // The distance from the object's pivot to its bottom face
            
            // 1. Detect Stack Position
            List<BlockMelter> nextMelters = new List<BlockMelter>();
            List<Transform> blocksBeneath = new List<Transform>();
            bool isTopBlock = true;
            bool isBottomBlock = true;
            
            // BoxCast needs world space extents!
            Vector3 halfExtents = worldBoundsSize / 2.1f;
            halfExtents.y = 0.1f; 
            
            // Check below
            foreach (RaycastHit hit in Physics.BoxCastAll(col.bounds.center, halfExtents, Vector3.down, Quaternion.identity, 1.5f))
            {
                if (hit.transform == t || hit.transform.IsChildOf(t)) continue;
                
                isBottomBlock = false;
                if (!blocksBeneath.Contains(hit.transform)) blocksBeneath.Add(hit.transform);

                BlockMelter melter = hit.collider.GetComponentInParent<BlockMelter>() ?? hit.collider.GetComponentInChildren<BlockMelter>();
                if (melter != null && melter != this && !nextMelters.Contains(melter)) nextMelters.Add(melter);
            }

            // Check above
            foreach (RaycastHit hit in Physics.BoxCastAll(col.bounds.center, halfExtents, Vector3.up, Quaternion.identity, 1.5f))
            {
                if (hit.transform == t || hit.transform.IsChildOf(t)) continue;
                isTopBlock = false;
                break;
            }

            // 2. Spawn Visuals
            SpawnParticles(startPos + Vector3.up * 0.6f, t);

            if (acidPuddlePrefab != null)
            {
                // Because worldBoundsSize is ALWAYS in true world space, we don't need to swap Y and Z! Width is X, Depth is Z.
                Vector3 puddleScale = new Vector3(worldBoundsSize.x, worldBoundsSize.z, 1f);

                if (spawnTopPuddle)
                {
                    // Spawn a puddle on the TOP face of this block
                    Vector3 topPuddlePos = new Vector3(startPos.x, col.bounds.max.y + 0.01f, startPos.z);
                    SpawnPuddle(topPuddlePos, this.transform, puddleScale, false);
                }

                // Bottom Puddles (permanent)
                if (isBottomBlock || !fillUnderlyingSurfaces)
                {
                    SpawnPuddle(new Vector3(startPos.x, startBottomY - 0.02f, startPos.z), null, puddleScale, true);
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
                        if (uCol != null)
                        {
                            Vector3 targetFootprint = new Vector3(uCol.bounds.size.x, uCol.bounds.size.z, 1f);
                            SpawnPuddle(pos, uBlock, targetFootprint, true);
                        }
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

                float scaleFactor = 1f;

                if (isBlenderFBX)
                {
                    // Squash Z (which points UP due to Blender rotation) down to 5% of its ORIGINAL scale
                    float targetZ = startScale.z * 0.05f;
                    float curZ = Mathf.Lerp(startScale.z, targetZ, easedP);
                    t.localScale = new Vector3(startScale.x, startScale.y, curZ);
                    scaleFactor = curZ / startScale.z;
                }
                else
                {
                    // Squash Y down to 5% of its ORIGINAL scale
                    float targetY = startScale.y * 0.05f;
                    float curY = Mathf.Lerp(startScale.y, targetY, easedP);
                    t.localScale = new Vector3(startScale.x, curY, startScale.z);
                    scaleFactor = curY / startScale.y;
                }

                // Perfectly lock the bottom face by mathematically keeping the pivot relative to the scale
                t.position = new Vector3(startPos.x, startBottomY + (pivotOffset * scaleFactor), startPos.z);

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

        private void SpawnPuddle(Vector3 pos, Transform parent, Vector3 directScale, bool includeParticles = false)
        {
            // Instantiate unparented first to establish global size
            GameObject puddle = Instantiate(acidPuddlePrefab, pos, Quaternion.Euler(90, 0, 0));
            puddle.transform.localScale = directScale;
            
            // Put the puddle on the "Ignore Raycast" layer so it never blocks the player's mouse clicks!
            int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
            puddle.layer = ignoreRaycastLayer;
            foreach (Transform child in puddle.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = ignoreRaycastLayer;
            }

            // Add the fader script so it dries up when the Special State ends!
            PuddleFader fader = puddle.AddComponent<PuddleFader>();
            fader.fadeDuration = 2f;

            // Parent it now, keeping the global size exactly as we set it
            if (parent != null) puddle.transform.SetParent(parent, true);

            if (includeParticles) SpawnParticles(pos + Vector3.up * 0.1f, puddle.transform);
        }
    }

    // Helper script attached to puddles to make them dry up and disappear
    public class PuddleFader : MonoBehaviour
    {
        public float fadeDuration = 2f;
        private PlayerController player;
        private bool isFading = false;

        private void Start()
        {
#if UNITY_2023_1_OR_NEWER
            player = FindFirstObjectByType<PlayerController>();
#else
            player = FindObjectOfType<PlayerController>();
#endif
            if (player != null)
            {
                player.onSpecialStateToggled.AddListener(OnSpecialStateChanged);
                
                // If spawned while state is ALREADY OFF (edge case), fade immediately
                if (!player.isInSpecialState) StartCoroutine(FadeOutRoutine());
            }
        }

        private void OnDestroy()
        {
            if (player != null)
            {
                player.onSpecialStateToggled.RemoveListener(OnSpecialStateChanged);
            }
        }

        private void OnSpecialStateChanged(bool isSpecialStateActive)
        {
            if (!isSpecialStateActive && !isFading)
            {
                StartCoroutine(FadeOutRoutine());
            }
        }

        private IEnumerator FadeOutRoutine()
        {
            isFading = true;

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            List<Material> mats = new List<Material>();
            foreach (Renderer r in renderers)
            {
                if (r != null) mats.Add(r.material);
            }

            Vector3 startScale = transform.localScale;
            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float p = timer / fadeDuration;
                
                // Shrink it down so it looks like it's drying up
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, p);
                
                // Try to fade the alpha too, just in case the shader supports transparency
                foreach (Material m in mats)
                {
                    if (m != null)
                    {
                        if (m.HasProperty("_Color"))
                        {
                            Color c = m.color;
                            c.a = Mathf.Lerp(1f, 0f, p);
                            m.color = c;
                        }
                        else if (m.HasProperty("_BaseColor"))
                        {
                            Color c = m.GetColor("_BaseColor");
                            c.a = Mathf.Lerp(1f, 0f, p);
                            m.SetColor("_BaseColor", c);
                        }
                    }
                }
                
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
