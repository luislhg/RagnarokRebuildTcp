﻿using UnityEngine;

namespace Assets.Scripts.Effects
{
    public class ShaderCache : MonoBehaviour
    {
        private static ShaderCache instance;
        public static ShaderCache Instance
        {
            get
            {
                if (instance == null)
                    instance = GameObject.FindObjectOfType<ShaderCache>();

                return instance;
            }
        }

        public Shader SpriteShader;
        public Shader SpriteShaderNoZWrite;
        public Shader SpritePerspectiveShader;
        public Shader SpriteShaderNoZTest;
        public Shader AlphaBlendParticleShader;
        public Shader AlphaBlendNoZTestShader;
        public Shader InvAlphaShader;
        public Shader AdditiveShader;
        public Shader WaterShader;
        public Shader PerspectiveAlphaShader;
        public Shader ProjectorAdditiveShader;

        public bool BillboardSprites = true;
    }
}
