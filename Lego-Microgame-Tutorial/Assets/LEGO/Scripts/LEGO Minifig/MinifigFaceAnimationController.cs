using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.LEGO.Minifig
{

    public class MinifigFaceAnimationController : MonoBehaviour
    {
        public enum FaceAnimation
        {
            Accept,
            Blink,
            BlinkTwice,
            Complain,
            Cool,
            Dissatisfied,
            Doubtful,
            Frustrated,
            Laugh,
            Mad,
            Sleepy,
            Smile,
            Surprised,
            Wink
        }

        [Serializable]
        class AnimationData
        {
            public Texture2D[] textures;
        }

        [SerializeField]
        Transform face;
        [SerializeField]
        Texture2D defaultTexture;
        [SerializeField]
        List<FaceAnimation> animations = new List<FaceAnimation>();
        [SerializeField]
        List<AnimationData> animationData = new List<AnimationData>();

        Material faceMaterial;

        bool playing;
        AnimationData currentAnimationData;
        float currentFrame;
        int showingFrame;
        float framesPerSecond;

        int shaderTextureId;

        public void Init(Transform face, Texture2D defaultTexture)
        {
            this.face = face;
            this.defaultTexture = defaultTexture;
        }

        public void AddAnimation(FaceAnimation animation, Texture2D[] textures)
        {
            if (!animations.Contains(animation))
            {
                animations.Add(animation);
                var animationData = new AnimationData();
                animationData.textures = textures;
                this.animationData.Add(animationData);
            }
            else
            {
                Debug.LogErrorFormat("Face animation controller already contains animation {0}", animation);
            }
        }

        public bool HasAnimation(FaceAnimation animation)
        {
            return animations.IndexOf(animation) >= 0;
        }

        public void PlayAnimation(FaceAnimation animation, float framesPerSecond = 24.0f)
        {
            var animationIndex = animations.IndexOf(animation);
            if (animationIndex < 0)
            {
                Debug.LogErrorFormat("Face animation controller does not contatin animation {0}", animation);
                return;
            }

            if (framesPerSecond <= 0.0f)
            {
                Debug.LogError("Frames per second must be positive");
                return;
            }

            playing = true;
            currentAnimationData = animationData[animationIndex];
            currentFrame = 0.0f;
            showingFrame = -1;
            this.framesPerSecond = framesPerSecond;

        }

        void Start()
        {
            faceMaterial = face.GetComponent<Renderer>().material;
            shaderTextureId = Shader.PropertyToID("_BaseMap");
        }

        // Update is called once per frame
        void Update()
        {
            if (playing)
            {
                currentFrame += Time.deltaTime * framesPerSecond;

                var wholeFrame = Mathf.FloorToInt(currentFrame);
                if (wholeFrame != showingFrame)
                {
                    if (wholeFrame >= currentAnimationData.textures.Length)
                    {
                        faceMaterial.SetTexture(shaderTextureId, defaultTexture);
                        playing = false;
                    }
                    else
                    {
                        faceMaterial.SetTexture(shaderTextureId, currentAnimationData.textures[wholeFrame]);
                        showingFrame = wholeFrame;
                    }
                }

            }
        }
    }

}