﻿using System.Collections.Generic;
using Engine.Map;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Weather
{
    public static class Rain
    {
        private static readonly List<RainDrop> RainDrops = new List<RainDrop>();
        private static bool _isRaining;
        private static Texture2D _texture;
        private static float _elapsedMilliSeconds;
        private const float MaxGeneratMilliSeconds = 2000f;
        private static SoundEffectInstance _rainSound;
        private static SoundEffectInstance _thunderSound;
        private static readonly Color RainMapColor = Color.Gray;
        private const float FlashMilliSeconds = 100f;
        private static bool _isInFlash;

        public static bool IsRaining { get { return _isRaining; } }

        private static void Initlize()
        {
            if (_texture == null)
            {
                _texture = TextureGenerator.GetRaniDrop();
            }
            _rainSound = Utils.GetSoundEffect("背-下雨.wav")?.CreateInstance();
            _thunderSound = Utils.GetSoundEffect("背-打雷.wav")?.CreateInstance();
        }
        private static void GenerateRainDrops()
        {
            Initlize();
            RainDrops.Clear();
            for (var w = Globals.TheRandom.Next(2, 10);
                w < Globals.WindowWidth; 
                w += Globals.TheRandom.Next(2, 10))
            {
                for (var h = Globals.TheRandom.Next(10, 100);
                    h < Globals.WindowHeight; 
                    h += Globals.TheRandom.Next(10, 100))
                {
                    RainDrops.Add(new RainDrop(new Vector2(w, h), _texture));
                }
            }
        }

        public static void Raining(bool isRain)
        {
            _isRaining = isRain;
            GenerateRainDrops();
            if (_isRaining)
            {
                Sprite.DrawColor = MapBase.DrawColor = RainMapColor;
                if (_rainSound != null)
                {
                    _rainSound.IsLooped = true;
                    _rainSound.Play();
                }
                
            }
            else
            {
                if (_rainSound != null)
                {
                    _rainSound.Stop();
                }
            }
        }

        public static void Update(GameTime gameTime)
        {
            if(!_isRaining) return;
            //_elapsedMilliSeconds += (float) gameTime.ElapsedGameTime.TotalMilliseconds;
            //if (_elapsedMilliSeconds >= MaxGeneratMilliSeconds)
            //{
            //    _elapsedMilliSeconds = 0;
            //    GenerateRainDrops();
            //}
            foreach (var rainDrop in RainDrops)
            {
                rainDrop.Update();
            }
            Sprite.DrawColor = MapBase.DrawColor = RainMapColor;
            if (_thunderSound != null &&
                Globals.TheRandom.Next(0, 300) == 50 && 
                _thunderSound.State == SoundState.Stopped)
            {
                _isInFlash = true;
                Sprite.DrawColor = MapBase.DrawColor = Color.White;
                _thunderSound.Play();
            }

            if (_isInFlash)
            {
                _elapsedMilliSeconds += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                if (_elapsedMilliSeconds >= FlashMilliSeconds)
                {
                    _elapsedMilliSeconds = 0;
                    _isInFlash = false;
                }
            }
        }

        public static void Draw(SpriteBatch spriteBatch)
        {
            if (!_isRaining) return;
            foreach (var rainDrop in RainDrops)
            {
                rainDrop.Draw(spriteBatch);
            }
        }
    }
}