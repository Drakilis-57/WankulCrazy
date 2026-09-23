using System;

namespace WankulCrazyPlugin.utils
{
    public static class RandomUtils
    {
        private static readonly Random sysRandom = new Random();

        public static float Range(float min, float max)
        {
            try
            {
                return UnityEngine.Random.Range(min, max);
            }
            catch (Exception)
            {
                return (float)(min + (sysRandom.NextDouble() * (max - min)));
            }
        }

        public static int Range(int min, int max)
        {
            try
            {
                return UnityEngine.Random.Range(min, max);
            }
            catch (Exception)
            {
                return sysRandom.Next(min, max);
            }
        }
    }
}
