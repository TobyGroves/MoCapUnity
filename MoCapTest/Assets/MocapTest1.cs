using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MocapTest1 : MonoBehaviour
{
    int p;
    [SerializeField]
    bool running;

    [SerializeField]
    float pollingInterval;
    float prevPollTime = -100;

    bool dataRecived = true;

    [System.Serializable]
    struct mpu
    {
        public Vector3 accel;
        public Vector3 gyro;
    }

    [System.Serializable]
    struct DataStruct
    {
        public mpu mpu1;
        public mpu mpu2;
    }

    DataStruct curData;

    float startTime;
    string url = "http://192.168.0.144:5000/getData";



    [SerializeField]
    Transform targetObject;
    [SerializeField]
    Vector3 currentGyro;
    [SerializeField]
    Vector3 currentAccel;

    [SerializeField] // deadzone checking 
    Vector3 accDeadZoneMin = new Vector3(0, 0, 0), accDeadZoneMax = new Vector3(0, 0, 0),
        gyroDeadZoneMin = new Vector3(0, 0, 0), gyroDeadZoneMax = new Vector3(0,0,0);
    // Start is called before the first frame update
    Vector3 ObjectRotation;

    [SerializeField]
    Vector3 approxGrav;

    [SerializeField]
    Vector3 targetObjectRotation;
    void Start()
    {
        StartCoroutine(calibrate());
    }

    // ping the rest api to get the data minimum 30 times per second but probably more like 100;

    IEnumerator RequestLoop()
    {
        startTime = Time.time;
        while (running)
        {
            if (dataRecived)
            {
                StartCoroutine(moveObject(Time.time - prevPollTime));
                dataRecived = false;
                prevPollTime = Time.time;
                StartCoroutine(GetData());
            }
            yield return null;
        }
    }

    IEnumerator GetData()
    {

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
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

                dataRecived = true;
            }
        }
    }
    void setData(DataStruct _curData)
    {
        currentGyro = _curData.mpu1.gyro;

        currentAccel = new Vector3((int)(_curData.mpu1.accel.x / 100), (int)(_curData.mpu1.accel.y / 100),
            (int)(_curData.mpu1.accel.z / 100));

        currentAccel  = currentAccel - approxGrav;
        if (currentGyro.x >= gyroDeadZoneMin.x && currentGyro.x <= gyroDeadZoneMax.x) currentGyro.x = 0;
        if (currentGyro.y >= gyroDeadZoneMin.y && currentGyro.y <= gyroDeadZoneMax.y) currentGyro.y = 0;
        if (currentGyro.z >= gyroDeadZoneMin.z && currentGyro.z <= gyroDeadZoneMax.z) currentGyro.z = 0;
        if (currentAccel.x >= accDeadZoneMin.x && currentAccel.x <= accDeadZoneMax.x) currentAccel.x = 0;
        if (currentAccel.y  >= accDeadZoneMin.y && currentAccel.y <= accDeadZoneMax.y) currentAccel.y = 0;
        if (currentAccel.z >= accDeadZoneMin.z && currentAccel.z <= accDeadZoneMax.z) currentAccel.z = 0;

    }
    [SerializeField]
    Vector3 velocity = new Vector3(0,0,0);

    IEnumerator moveObject(float timeSinceLastPoll)
    {

        yield return null;

        ComplementaryGyroFilter(currentGyro, timeSinceLastPoll);

        //targetObject.transform.eulerAngles = new Vector3(pitch, 0, 0);

        targetObject.transform.RotateAround(targetObject.transform.position, targetObject.transform.right, xRotNode);
        targetObject.transform.RotateAround(targetObject.transform.position, targetObject.transform.up, -zRotNode);
        targetObject.transform.RotateAround(targetObject.transform.position, targetObject.transform.forward, yRotNode);


        //targetObject.transform.localEulerAngles += new Vector3(xRotNode, -yRotNode, zRotNode);

        // x is right
        // y is up
        // z is forward
        //Debug.DrawRay(targetObject.transform.position, targetObject.transform.right * 10f, Color.red);
        //Debug.DrawRay(targetObject.transform.position, targetObject.transform.up * 10f, Color.green);
        //Debug.DrawRay(targetObject.transform.position, targetObject.transform.forward * 10f, Color.blue);


        /*Vector3 xRotVector = new Vector3 (targetObject.transform.right.x * xRotNode,
            targetObject.transform.right.y * xRotNode, targetObject.transform.right.z * xRotNode);

        Vector3 yRotVector = new Vector3(targetObject.transform.up.x * -zRotNode, 
            targetObject.transform.up.y * -zRotNode, targetObject.transform.up.z * -zRotNode);

        Vector3 zRotVector = new Vector3(targetObject.transform.forward.x * yRotNode, 
            targetObject.transform.forward.y * yRotNode, targetObject.transform.forward.z * yRotNode);*/

        //targetObjectRotation = (xRotVector + yRotVector + zRotVector);

        //targetObject.transform.eulerAngles = targetObject.transform.eulerAngles + targetObjectRotation;
        //Debug.Log(xRotVector); // seems ok 
        //Debug.Log(targetObject.transform.localRotation.eulerAngles.x); // clamps at 270 WHYYYYY ?

        /*targetObject.transform.eulerAngles = 
            new Vector3((targetObject.transform.eulerAngles.x + targetObjectRotation.x)%360, 
            (targetObject.transform.eulerAngles.y + targetObjectRotation.y) % 360, 
            (targetObject.transform.eulerAngles.z + targetObjectRotation.z) % 360);//% 360f;*/

        //targetObject.transform.eulerAngles += targetObjectRotation;
        //targetObject.transform.Rotate(targetObjectRotation);
        //targetObject.transform.localRotation = Quaternion.Euler((targetObject.transform.localRotation.eulerAngles +targetObjectRotation));
        //+= targetObjectRotation;




        //targetObject.transform.localEulerAngles = new Vector3(xRotNode,-zRotNode, yRotNode);
        // current accel divided by 1g raw to convert into g's then times by 9.8067 to get it in meters per 
        // second squared
        velocity += ((((currentAccel*100) / 16384) * 9.8067f) * timeSinceLastPoll) / 2;


    }

    private void Update()
    {
        Debug.DrawRay(targetObject.transform.position, targetObject.transform.right * 10f, Color.red);
        Debug.DrawRay(targetObject.transform.position, targetObject.transform.up * 10f, Color.green);
        Debug.DrawRay(targetObject.transform.position, targetObject.transform.forward * 10f, Color.blue);
        //targetObject.transform.localPosition += velocity * Time.deltaTime;
        if (Input.GetKeyDown("space"))
        {
            roll = 0;
            pitch = 0;
            yaw = 0;
            targetObject.transform.localEulerAngles = new Vector3(0, 0, 0);

        }
    }

    float GYROSCOPE_SENSITIVITY = 131f;
    float ACCELEROMETER_SENSITIVITY = 16384f; 
    [SerializeField]
    float pitch, roll, yaw,xRotNode,yRotNode,zRotNode;
    float M_PI = 3.14159265359f;
    void ComplementaryGyroFilter(Vector3 _gyroData, float _dt)
    {

        pitch += ((float)_gyroData.x / 131) * _dt;
        roll += ((float)_gyroData.y / 131) * _dt;
        yaw += ((float)_gyroData.z / 131) * _dt;


        xRotNode = ((float)_gyroData.x / 131) * _dt; // clamps at 270 ? 
        yRotNode = ((float)_gyroData.y / 131) * _dt;
        zRotNode = ((float)_gyroData.z / 131) * _dt;

    }

    IEnumerator calibrate()
    {
        Debug.Log("Calibrating Sensors");
        using (UnityWebRequest webRequest = UnityWebRequest.Get("http://192.168.0.144:5000/calibrate"))
        {

            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.isNetworkError)
            {
                Debug.Log("Error: " + webRequest.error);
            }
            else
            {
                Debug.Log("Sensor calibration complete");
                StartCoroutine(calcualteGravity(calcualteDeadzones()));
            }
        }
    }
    
    IEnumerator calcualteGravity(IEnumerator next)
    {
        Debug.Log("Calculating approx gravity direction");
        Vector3 approxGravtemp = new Vector3(0, 0, 0);
        int count = 0;
        while (count <= 300)
        {
            if (dataRecived)
            {
                approxGravtemp += currentAccel; // * (Time.time - prevPollTime)
                dataRecived = false;
                prevPollTime = Time.time;
                StartCoroutine(GetData());
                count++;
            }
            yield return null;
        }
        approxGrav = new Vector3((int)(approxGravtemp.x / 300), (int)(approxGravtemp.y / 300),
            (int)( approxGravtemp.z / 300));
        Debug.Log("approx gravity direction calculated");

        StartCoroutine(next);

        yield return null;
    }
    [SerializeField]
    int countdz = 0;
    IEnumerator calcualteDeadzones()
    {
        Debug.Log("Calculating deadzones");
        while (countdz <= 2000)
        {
            if (dataRecived)
            {
                if(countdz == 0)
                {
                    accDeadZoneMin = currentAccel;
                    accDeadZoneMax = currentAccel;
                    gyroDeadZoneMin = currentGyro;
                    gyroDeadZoneMax = currentGyro;
                }
                else
                {
                    if (currentAccel.x < accDeadZoneMin.x) accDeadZoneMin.x = currentAccel.x;
                    if (currentAccel.y < accDeadZoneMin.y) accDeadZoneMin.y = currentAccel.y;
                    if (currentAccel.z < accDeadZoneMin.z) accDeadZoneMin.z = currentAccel.z;
                    if (currentAccel.x > accDeadZoneMax.x) accDeadZoneMax.x = currentAccel.x;
                    if (currentAccel.y > accDeadZoneMax.y) accDeadZoneMax.y = currentAccel.y;
                    if (currentAccel.z > accDeadZoneMax.z) accDeadZoneMax.z = currentAccel.z;
                    if (currentGyro.x < gyroDeadZoneMin.x) gyroDeadZoneMin.x = currentGyro.x;
                    if (currentGyro.y < gyroDeadZoneMin.y) gyroDeadZoneMin.y = currentGyro.y;
                    if (currentGyro.z < gyroDeadZoneMin.z) gyroDeadZoneMin.z = currentGyro.z;
                    if (currentGyro.x > gyroDeadZoneMax.x) gyroDeadZoneMax.x = currentGyro.x;
                    if (currentGyro.y > gyroDeadZoneMax.y) gyroDeadZoneMax.y = currentGyro.y;
                    if (currentGyro.z > gyroDeadZoneMax.z) gyroDeadZoneMax.z = currentGyro.z;
                }
                dataRecived = false;
                prevPollTime = Time.time;
                StartCoroutine(GetData());
                countdz++;
            }
            yield return null;
        }
        Debug.Log("deadzones calculated");
        StartCoroutine(RequestLoop());

        yield return null;
    }
}
