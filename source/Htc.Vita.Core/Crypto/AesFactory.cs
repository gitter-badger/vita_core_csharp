﻿using System;
using System.Collections.Generic;
using Htc.Vita.Core.Log;

namespace Htc.Vita.Core.Crypto
{
    public abstract class AesFactory
    {
        private static Dictionary<string, AesFactory> Instances { get; } = new Dictionary<string, AesFactory>();
        private static Type _defaultType = typeof(DefaultAesFactory);

        public static void Register<T>() where T : AesFactory
        {
            _defaultType = typeof(T);
            Logger.GetInstance(typeof(AesFactory)).Info("Registered default aes factory type to " + _defaultType);
        }

        public static AesFactory GetInstance()
        {
            AesFactory instance;
            try
            {
                instance = DoGetInstance(_defaultType);
            }
            catch (Exception e)
            {
                Logger.GetInstance(typeof(AesFactory)).Fatal("Instance initialization error: " + e);
                Logger.GetInstance(typeof(AesFactory)).Info("Initializing " + typeof(DefaultAesFactory).FullName + "...");
                instance = new DefaultAesFactory();
            }
            return instance;
        }

        private static AesFactory DoGetInstance(Type type)
        {
            if (type == null)
            {
                throw new ArgumentException("Invalid arguments to get aes instance");
            }

            var key = type.FullName + "_";
            AesFactory instance = null;
            if (Instances.ContainsKey(key))
            {
                instance = Instances[key];
            }
            if (instance == null)
            {
                Logger.GetInstance(typeof(AesFactory)).Info("Initializing " + key + "...");
                var constructor = type.GetConstructor(new Type[] { });
                if (constructor != null)
                {
                    instance = (AesFactory)constructor.Invoke(new object[] { });
                }
            }
            if (instance == null)
            {
                Logger.GetInstance(typeof(AesFactory)).Info("Initializing " + typeof(DefaultAesFactory).FullName + "...");
                instance = new DefaultAesFactory();
            }
            if (!Instances.ContainsKey(key))
            {
                Instances.Add(key, instance);
            }
            return instance;
        }

        public Aes Get()
        {
            return Get(Aes.CipherMode.Cbc, Aes.PaddingMode.Pkcs7);
        }

        public Aes Get(Aes.CipherMode cipherMode, Aes.PaddingMode paddingMode)
        {
            Aes result = null;
            try
            {
                result = OnGet(cipherMode, paddingMode);
            }
            catch (Exception e)
            {
                Logger.GetInstance(typeof(AesFactory)).Error(e.ToString());
            }
            return result;
        }

        protected abstract Aes OnGet(Aes.CipherMode cipherMode, Aes.PaddingMode paddingMode);
    }
}