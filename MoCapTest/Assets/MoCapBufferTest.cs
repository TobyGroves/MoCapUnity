using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MoCapBufferTest : MonoBehaviour
{
    #region jsonHandeller_Structs
    [System.Serializable]
    struct mpuMoveInfo
    {
        public Vector3 accel_data;
        public Vector3 gyro_data;
        public float timeSinceLastPoll;
    }
    [System.Serializable]
    struct DataStruct
    {
        public mpuMoveInfo[] mpuMoveList;
    }
    #endregion

    [SerializeField]
    string url = "http://192.168.0.144:5000/";

    Queue<mpuMoveInfo> movementBuffer;

    // Start is called before the first frame update
    void Start()
    {
        movementBuffer = new Queue<mpuMoveInfo>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void getData()
    {

    }
    IEnumerator getDataCoroutine()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url+"getDataMaxfps"))
        {

            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.isNetworkError)
            {
                Debug.Log("Error: " + webRequest.error);
            }
            else
            {
                setData(JsonUtility.FromJson<DataStruct>(webRequest.downloadHandler.text));

                //dataRecived = true;
            }
        }
    }
    void setData(DataStruct _dataIn)
    {
        for (int i = 0; i < _dataIn.mpuMoveList.Length; i++)
        {
            movementBuffer.Enqueue(_dataIn.mpuMoveList[i]);
        }
    }
    void updatePosition()
    {

    }

    void calibrate()
    {

    }

    IEnumerator calibrateCoroutine()
    {

        yield return null;
    }

}
