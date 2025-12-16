using UnityEngine;
using ExcelImporter;

public class Client : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ExcelDataManager.Initialize();

        ExcelDataManager.GetPlayerById(1).Id.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
