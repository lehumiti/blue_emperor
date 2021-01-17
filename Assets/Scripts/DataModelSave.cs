using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public abstract class DataModelSave<T, S> : MonoSingleton<T> where T : DataModelSave<T,S> where S : BaseDataModel
{
    public S dataModel;

    protected string path = "";
    public bool isHaveData;
    public abstract void OnInit();
    private void Start()
    {
        isHaveData = false;
        OnInit();
    }
    public void Save()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(Application.persistentDataPath + "/" + path, FileMode.OpenOrCreate);
        bf.Serialize(file, dataModel);
        file.Close();
    }

    public void Load()
    {
        if (File.Exists(Application.persistentDataPath + "/" + path))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/" + path, FileMode.Open);
            dataModel = (S)bf.Deserialize(file);
            file.Close();
            isHaveData = true;
        }
        else
        {
            dataModel.OnInitLoad();
            Save();
            isHaveData = true;
        }
    }
}
