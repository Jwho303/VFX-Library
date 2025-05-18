using UnityEngine;
using System.Collections.Generic;

namespace Jwho303.VFX
{
    [RequireComponent(typeof(ParticleSystem))]
    public class SpriteSheetParticleElement : ParticleElement
    {
        [Header("Sprite Sheet Settings")]
        [SerializeField] private Texture2D spriteSheet;
        [SerializeField] private int columns = 4;
        [SerializeField] private int rows = 4;
        [SerializeField] private int frameCount = 16;

        [Header("Animation Settings")]
        [Range(1f, 60f)]
        [SerializeField] private float framesPerSecond = 10f;
        [SerializeField] private bool randomStartFrame = false;
        [SerializeField] private bool loopAnimation = true;

        // A key parameter - use frame over time directly
        [SerializeField] private bool useFrameOverTime = true;

        // Cached components
        private ParticleSystemRenderer particleRenderer;

        public override void Initialize()
        {
            // Call parent initialization first
            base.Initialize();

            if (spriteSheet == null)
            {
                Debug.LogError("SpriteSheetParticleElement requires a sprite sheet texture!");
                return;
            }

            // Get the particle system renderer
            particleRenderer = GetComponent<ParticleSystemRenderer>();
            if (particleRenderer == null)
            {
                Debug.LogError("Could not find ParticleSystemRenderer component.");
                return;
            }

            // Apply the sprite sheet texture to the particle material
            Material material = particleRenderer.sharedMaterial;
            if (material != null)
            {
                // Use sharedMaterial in Edit mode, material in Play mode
                if (Application.isPlaying)
                {
                    particleRenderer.material.mainTexture = spriteSheet;
                }
                else
                {
                    particleRenderer.sharedMaterial.mainTexture = spriteSheet;
                }
            }

            // Configure texture sheet animation
            ConfigureTextureSheetAnimation();
        }

        private void ConfigureTextureSheetAnimation()
        {
            // Ensure we have a valid particle system
            if (particleSystem == null) return;

            // Get the texture sheet animation module
            var textureSheetModule = particleSystem.textureSheetAnimation;

            // Enable it
            textureSheetModule.enabled = true;

            // Set the mode and grid size
            textureSheetModule.mode = ParticleSystemAnimationMode.Grid;
            textureSheetModule.numTilesX = columns;
            textureSheetModule.numTilesY = rows;

            // Clamp frame count to valid range
            int maxFrames = columns * rows;
            frameCount = Mathf.Clamp(frameCount, 1, maxFrames);

            // Calculate animation settings based on our mode
            if (useFrameOverTime)
            {
                // Use frameOverTime - this ensures each particle plays the animation over its lifetime
                // First, normalize the total animation duration
                float particleLifetime = particleSystem.main.startLifetime.constant;

                // Calculate the normalized time for complete play through
                float frameInterval = 1.0f / frameCount;

                // Create keyframes that evenly space out the frames
                Keyframe[] keys = new Keyframe[frameCount];
                for (int i = 0; i < frameCount; i++)
                {
                    float time = i / (float)(frameCount - 1); // Normalized time (0 to 1)
                    float frame = i * frameInterval * frameCount; // Normalized frame index

                    keys[i] = new Keyframe(time, frame);
                }

                AnimationCurve frameCurve = new AnimationCurve(keys);
                textureSheetModule.frameOverTime = new ParticleSystem.MinMaxCurve(1.0f, frameCurve);

                // Set to play animation once per lifetime
                textureSheetModule.cycleCount = loopAnimation ? Mathf.Max(1, Mathf.FloorToInt(particleLifetime * framesPerSecond / frameCount)) : 1;

                // When using frameOverTime with custom curve, FPS isn't used directly
                textureSheetModule.fps = 30; // Default value, not actually used
            }
            else
            {
                // Use FPS directly (should work in most cases)
                textureSheetModule.frameOverTime = new ParticleSystem.MinMaxCurve(1.0f);
                textureSheetModule.fps = framesPerSecond;
                textureSheetModule.cycleCount = loopAnimation ? 0 : 1; // 0 = infinite looping
            }

            // Set starting frame
            if (randomStartFrame)
            {
                var startFrame = textureSheetModule.startFrame;
                startFrame.mode = ParticleSystemCurveMode.TwoConstants;
                startFrame.constantMin = 0;
                startFrame.constantMax = frameCount - 1;
                textureSheetModule.startFrame = startFrame;
            }
            else
            {
                var startFrame = textureSheetModule.startFrame;
                startFrame.constant = 0;
                textureSheetModule.startFrame = startFrame;
            }
        }

        public override void Play()
        {
            base.Play();

            // Ensure animation is properly configured before playing
            if (!particleSystem.textureSheetAnimation.enabled)
            {
                ConfigureTextureSheetAnimation();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && isInitialized)
            {
                ConfigureTextureSheetAnimation();
            }
        }
#endif

        // Runtime control methods
        public void SetAnimationSpeed(float newFPS)
        {
            framesPerSecond = Mathf.Clamp(newFPS, 1f, 60f);

            if (isInitialized)
            {
                ConfigureTextureSheetAnimation();
            }
        }

        public void SetSpriteSheet(Texture2D newSpriteSheet, int newColumns, int newRows, int newFrameCount)
        {
            spriteSheet = newSpriteSheet;
            columns = newColumns;
            rows = newRows;
            frameCount = Mathf.Clamp(newFrameCount, 1, newColumns * newRows);

            if (isInitialized)
            {
                // Update material texture
                if (particleRenderer != null)
                {
                    Material material = particleRenderer.material;
                    if (material != null)
                    {
                        material.mainTexture = spriteSheet;
                    }
                }

                ConfigureTextureSheetAnimation();
            }
        }
    }
}