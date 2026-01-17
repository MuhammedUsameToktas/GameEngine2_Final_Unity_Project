using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WorldSaveData
{
    [System.Serializable]
    public class SaveObjectData
    {
        public string id;
        public string dataJson;
        public string dataType;
    }

    public List<SaveObjectData> savedObjects = new List<SaveObjectData>();

    /// <summary>
    /// Store an object's state
    /// </summary>
    public void StoreObject(string id, object state)
    {
        if (state == null) return;

        // Remove existing entry if present
        savedObjects.RemoveAll(x => x.id == id);

        // Create new entry
        SaveObjectData saveData = new SaveObjectData
        {
            id = id,
            dataJson = JsonUtility.ToJson(state),
            dataType = state.GetType().AssemblyQualifiedName
        };

        savedObjects.Add(saveData);
    }

    /// <summary>
    /// Retrieve an object's state
    /// </summary>
    public T RetrieveObject<T>(string id) where T : class
    {
        SaveObjectData saveData = savedObjects.Find(x => x.id == id);
        if (saveData == null) return null;

        try
        {
            return JsonUtility.FromJson<T>(saveData.dataJson);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to deserialize object {id}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Check if an object exists in save data
    /// </summary>
    public bool HasObject(string id)
    {
        return savedObjects.Exists(x => x.id == id);
    }
}
