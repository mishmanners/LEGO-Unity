// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System;
using System.Collections.Generic;
using UnityEngine;

namespace LEGOModelImporter
{
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey> keys = new List<TKey>();
     
        [SerializeField]
        private List<TValue> values = new List<TValue>();
        public SerializableDictionary(SerializableDictionary<TKey, TValue> otherDictionary) : base(otherDictionary)
        {
            keys = new List<TKey>();
            values = new List<TValue>();
        }

        public SerializableDictionary(int capacity) : base(capacity)
        {
            keys = new List<TKey>();
            values = new List<TValue>();
        }

        public SerializableDictionary() : base()
        {
            keys = new List<TKey>();
            values = new List<TValue>();
        }

        // save the dictionary to lists
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach(KeyValuePair<TKey, TValue> pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }
     
        // load dictionary from lists
        public void OnAfterDeserialize()
        {
            Clear();
 
            if(keys.Count != values.Count)
                throw new System.Exception(string.Format("there are {0} keys and {1} values after deserialization. Make sure that both key and value types are serializable.", keys.Count, values.Count));
 
            for(int i = 0; i < keys.Count; i++)
                Add(keys[i], values[i]);
        }
    }

    [Serializable]
    public class DictionaryIntToModelGroupImportSettings : SerializableDictionary<int, ModelGroupImportSettings>
    {
        public DictionaryIntToModelGroupImportSettings(DictionaryIntToModelGroupImportSettings dictionary) : base(dictionary)
        {
            
        }

        public DictionaryIntToModelGroupImportSettings(int capacity) : base(capacity)
        {
            
        }

        public DictionaryIntToModelGroupImportSettings() : base()
        {}
    }
}