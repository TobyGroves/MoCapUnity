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

    [SerializeField]
    Transform targetObject;

    Queue<mpuMoveInfo> movementBuffer;

    [SerializeField] //serialised for debugging deadzones 
    Vector3 accDeadZoneMin = new Vector3(0, 0, 0), accDeadZoneMax = new Vector3(0, 0, 0),
        gyroDeadZoneMin = new Vector3(0, 0, 0), gyroDeadZoneMax = new Vector3(0, 0, 0);


    [SerializeField]
    int gravCalcNumData, deadzoneCalcNumData;

    [SerializeField]
    Vector3 approxGravPerSec;

    // Start is called before the first frame update
    void Start()
    {
        movementBuffer = new Queue<mpuMoveInfo>();
        calibrate();


    }

    // Update is called once per frame
    void Update()
    {
        
       
        // TODO start request loop for live movement 

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
                if (webRequest.downloadHandler.text == "{\"Error\": \"NotRecording\"}")
                {
                    Debug.Log("Your Not Recording");
                } 
                else
                {
                    setData(JsonUtility.FromJson<DataStruct>(webRequest.downloadHandler.text));
                }
            }
        }
    }
    void setData(DataStruct _dataIn)
    {
        for (int i = 0; i < _dataIn.mpuMoveList.Length; i++)
        {
            mpuMoveInfo tempMoveInfo = _dataIn.mpuMoveList[i];

            tempMoveInfo.accel_data -= approxGravPerSec; //needs work just copying from previous test
            //TODO take off gravity 
            if (tempMoveInfo.gyro_data.x >= gyroDeadZoneMin.x && tempMoveInfo.gyro_data.x <= gyroDeadZoneMax.x) tempMoveInfo.gyro_data.x = 0;
            if (tempMoveInfo.gyro_data.y >= gyroDeadZoneMin.y && tempMoveInfo.gyro_data.y <= gyroDeadZoneMax.y) tempMoveInfo.gyro_data.y = 0;
            if (tempMoveInfo.gyro_data.z >= gyroDeadZoneMin.z && tempMoveInfo.gyro_data.z <= gyroDeadZoneMax.z) tempMoveInfo.gyro_data.z = 0;
            if (tempMoveInfo.accel_data.x >= accDeadZoneMin.x && tempMoveInfo.accel_data.x <= accDeadZoneMax.x) tempMoveInfo.accel_data.x = 0;
            if (tempMoveInfo.accel_data.y >= accDeadZoneMin.y && tempMoveInfo.accel_data.y <= accDeadZoneMax.y) tempMoveInfo.accel_data.y = 0;
            if (tempMoveInfo.accel_data.z >= accDeadZoneMin.z && tempMoveInfo.accel_data.z <= accDeadZoneMax.z) tempMoveInfo.accel_data.z = 0;

            movementBuffer.Enqueue(tempMoveInfo);
        }
    }

    void updatePosition()
    {
        while (movementBuffer.Count != 0)
        {
            mpuMoveInfo tempMoveInfo = movementBuffer.Dequeue();
            targetObject.RotateAround(targetObject.position, targetObject.right,
                ((float)tempMoveInfo.gyro_data.x / 131) * tempMoveInfo.timeSinceLastPoll);
            targetObject.RotateAround(targetObject.position, targetObject.up,
                -((float)tempMoveInfo.gyro_data.z / 131) * tempMoveInfo.timeSinceLastPoll);
            targetObject.RotateAround(targetObject.position, targetObject.forward,
                ((float)tempMoveInfo.gyro_data.y / 131) * tempMoveInfo.timeSinceLastPoll);

            //TODO add accel movement like in the original mocap test
            //velocity += (((tempMoveInfo.accel_data / 16384) * 9.8067f) * timeSinceLastPoll) / 2;
        }

    }

    void calibrate()
    {
        StartCoroutine(calibrateCoroutine());
    }

    IEnumerator calibrateCoroutine()
    {
        Debug.Log("starting calibration");
        yield return StartCoroutine(getRequest("calibrate"));

        Debug.Log("pi calibration complete");
        // Calculate gravity 
        yield return StartCoroutine(getRequest("startRecording"));

        Debug.Log("started recording");
        //clear the move buffer
        movementBuffer.Clear();
        //get the mpu data a number of times 
        for (int i = 0; i < gravCalcNumData; i++)
        {
            yield return StartCoroutine(getDataCoroutine());
        }

        Debug.Log("collected data");
        yield return StartCoroutine(getRequest("stopRecording"));

        Debug.Log("stopped recording");
        //calc gravity from the move buffer
        Debug.Log("grav calc start");

        Vector3 totalGravtemp = new Vector3(0, 0, 0);
        float totalGravTime = 0;
        while (movementBuffer.Count != 0)
        {
            mpuMoveInfo tempMoveInfo = movementBuffer.Dequeue();
            totalGravtemp += tempMoveInfo.accel_data * tempMoveInfo.timeSinceLastPoll;
            totalGravTime += tempMoveInfo.timeSinceLastPoll;
        }
        approxGravPerSec = totalGravtemp / totalGravTime; // may need int rounding or something to make useful
        Debug.Log("grav calculated");

        //calculate deadzones
        Debug.Log("recording started");
        yield return StartCoroutine(getRequest("startRecording"));
        //clear the move buffer
        movementBuffer.Clear();

        Debug.Log("starting data collection");
        //get the mpu data a number of times 
        for (int i = 0; i < deadzoneCalcNumData; i++)
        {
            yield return StartCoroutine(getDataCoroutine());
        }

        Debug.Log("finished data collection");
        yield return StartCoroutine(getRequest("stopRecording"));

        Debug.Log("recording stopped");
        // calc deadzones from the move buffer

        Debug.Log("processing deadzones");
        mpuMoveInfo tempDeadzoneMoveInfo = movementBuffer.Dequeue();
        accDeadZoneMin = tempDeadzoneMoveInfo.accel_data;
        accDeadZoneMax = tempDeadzoneMoveInfo.accel_data;
        gyroDeadZoneMin = tempDeadzoneMoveInfo.gyro_data;
        gyroDeadZoneMax = tempDeadzoneMoveInfo.gyro_data;
        while (movementBuffer.Count != 0)
        {
             tempDeadzoneMoveInfo = movementBuffer.Dequeue();
            if (tempDeadzoneMoveInfo.accel_data.x < accDeadZoneMin.x) accDeadZoneMin.x = tempDeadzoneMoveInfo.accel_data.x;
            if (tempDeadzoneMoveInfo.accel_data.y < accDeadZoneMin.y) accDeadZoneMin.y = tempDeadzoneMoveInfo.accel_data.y;
            if (tempDeadzoneMoveInfo.accel_data.z < accDeadZoneMin.z) accDeadZoneMin.z = tempDeadzoneMoveInfo.accel_data.z;
            if (tempDeadzoneMoveInfo.accel_data.x > accDeadZoneMax.x) accDeadZoneMax.x = tempDeadzoneMoveInfo.accel_data.x;
            if (tempDeadzoneMoveInfo.accel_data.y > accDeadZoneMax.y) accDeadZoneMax.y = tempDeadzoneMoveInfo.accel_data.y;
            if (tempDeadzoneMoveInfo.accel_data.z > accDeadZoneMax.z) accDeadZoneMax.z = tempDeadzoneMoveInfo.accel_data.z;
            if (tempDeadzoneMoveInfo.gyro_data.x < gyroDeadZoneMin.x) gyroDeadZoneMin.x = tempDeadzoneMoveInfo.gyro_data.x;
            if (tempDeadzoneMoveInfo.gyro_data.y < gyroDeadZoneMin.y) gyroDeadZoneMin.y = tempDeadzoneMoveInfo.gyro_data.y;
            if (tempDeadzoneMoveInfo.gyro_data.z < gyroDeadZoneMin.z) gyroDeadZoneMin.z = tempDeadzoneMoveInfo.gyro_data.z;
            if (tempDeadzoneMoveInfo.gyro_data.x > gyroDeadZoneMax.x) gyroDeadZoneMax.x = tempDeadzoneMoveInfo.gyro_data.x;
            if (tempDeadzoneMoveInfo.gyro_data.y > gyroDeadZoneMax.y) gyroDeadZoneMax.y = tempDeadzoneMoveInfo.gyro_data.y;
            if (tempDeadzoneMoveInfo.gyro_data.z > gyroDeadZoneMax.z) gyroDeadZoneMax.z = tempDeadzoneMoveInfo.gyro_data.z;
        }

        Debug.Log("deadzones processed");
        Debug.Log("Finished Calibration");
    }




    IEnumerator getRequest(string _urlComponent)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url + _urlComponent))
        {

            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.isNetworkError)
            {
                Debug.Log("Error: " + webRequest.error);
            }
            else
            {
                Debug.Log("Successful");
            }
        }
    }
}
