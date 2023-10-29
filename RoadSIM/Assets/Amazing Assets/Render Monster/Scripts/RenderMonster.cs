using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Device;

#if ENABLE_INPUT_SYSTEM
    using UnityEngine.InputSystem;
using UnityEngine.Networking;
using static UnityEngine.Rendering.DebugUI;
#endif


namespace AmazingAssets 
{ 
    namespace RenderMonster 
    {
        [RequireComponent(typeof(Camera))]
        [AddComponentMenu("Amazing Assets/Render Monster")]
        public class RenderMonster : MonoBehaviour 
        {
            public enum BEGIN_RECORDING { OnStart, ByHotkey, Manually }
            public enum STOP_RECORDING { ByHotkey, AfterNFrame, AfterNSec, Manually }


            public string outputPath;
            [SerializeField]
            public string base_URL;
            public string oldpath;
            public string prompt;
            public string filePrefix;
            public GameObject car, panel;
            public int superSize = 1;  

            public BEGIN_RECORDING beginRecordingMode = BEGIN_RECORDING.ByHotkey;
            public STOP_RECORDING stopRecordingMode = STOP_RECORDING.ByHotkey;

#if ENABLE_INPUT_SYSTEM
            public Key recordingHotkey = Key.F12;
#else
            public KeyCode recordingHotkey = KeyCode.F12;
#endif

            public int nFrame = 300;
            public int nSec = 10;
            public int fPS = 30;

#if ENABLE_INPUT_SYSTEM
            public Key screenshotHotkey = Key.F5;
#else
            public KeyCode screenshotHotkey = KeyCode.F5;
#endif

            bool isRecording;
            int oldFPS;
            int nFrameCounter;

            string lastSavedFileName;



            void Start()
            {
                if (beginRecordingMode == BEGIN_RECORDING.OnStart)
                    BeginRecording();
                oldpath = outputPath;
                panel = GameObject.Find("SliderCanvas");
                //car = GameObject.Find("Car/SpeedGear");
                //car.SetActive(false);
                prompt = "vibrant road, traffic, environment, indian roads, best quality, ultra high res, photorealistic, realistic weather, sunny, dry roads realistic vehicles, accurate backgrounds, high definition backgrounds, Clear roads";
            }

            void OnDestroy()
            {

            }
             
            void Update()
            {
                CaptureImageSequence();

                if (IsScreenShotHotKeyDown())
                    CaptureScreenshot();
            }

            public void BeginRecording()
            {
                if(string.IsNullOrEmpty(outputPath))
                {
                    Debug.LogError("Render Monster: Can not capture image sequence. Output directory is not defined.\n");
                    return;
                }

                if (isRecording == false)
                    isRecording = true;
                else
                    return;
                panel.SetActive(false);
                DateTime currentTime = DateTime.UtcNow;
                oldpath = outputPath;
                long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();
                outputPath = Path.Combine(outputPath, unixTime.ToString());
                if (Directory.Exists(outputPath) == false)
                    Directory.CreateDirectory(outputPath);

                if(Directory.Exists(outputPath) == false)
                {
                    Debug.Log("Render Monster: Can not capture image sequence. Directory '" + outputPath + "' does not exist.\n");

                    isRecording = false;
                    return;
                }

                Debug.Log("Render Monster: Begin Recording.\n");
#if UNITY_EDITOR
                //Repaint editor to highlight buttons
                //UnityEditor.EditorUtility.SetDirty(UnityEditor.Selection.activeGameObject);
#endif


                superSize = Mathf.Clamp(superSize, 1, 32);

                nFrameCounter = 0;


                //Set playback framerate
                oldFPS = Time.captureFramerate;
                Time.captureFramerate = fPS;
            }

            public void StopRecording()
            {
                if (isRecording == true)
                    isRecording = false;
                else
                    return;


                Debug.Log("Render Monster: Stop Recording. (" + nFrameCounter + ") frames captured.\n");
#if UNITY_EDITOR
                //Repaint editor to highlight buttons
                //UnityEditor.EditorUtility.SetDirty(UnityEditor.Selection.activeGameObject);
#endif


                nFrameCounter = 0;
                Request_Image_processing(outputPath);
                outputPath = oldpath;
                panel.SetActive(true);
                //Restore playback framerate
                Time.captureFramerate = oldFPS;
                
            }

            public bool IsRecording()
            {
                return isRecording;
            }
            public byte[] ReadToEnd(System.IO.Stream stream)
            {
                long originalPosition = 0;

                if (stream.CanSeek)
                {
                    originalPosition = stream.Position;
                    stream.Position = 0;
                }

                try
                {
                    byte[] readBuffer = new byte[4096];

                    int totalBytesRead = 0;
                    int bytesRead;

                    while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                    {
                        totalBytesRead += bytesRead;

                        if (totalBytesRead == readBuffer.Length)
                        {
                            int nextByte = stream.ReadByte();
                            if (nextByte != -1)
                            {
                                byte[] temp = new byte[readBuffer.Length * 2];
                                Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                                Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                                readBuffer = temp;
                                totalBytesRead++;
                            }
                        }
                    }

                    byte[] buffer = readBuffer;
                    if (readBuffer.Length != totalBytesRead)
                    {
                        buffer = new byte[totalBytesRead];
                        Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                    }
                    return buffer;
                }
                finally
                {
                    if (stream.CanSeek)
                    {
                        stream.Position = originalPosition;
                    }
                }
            }
            void Request_Image_processing(string path)
            {
                if (Directory.Exists(path))
                {
                    string worldsFolder = path;
                    DateTime currentTime = DateTime.UtcNow;
                    long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();
                    DirectoryInfo d = new DirectoryInfo(path);
                    var files_len = d.GetFiles("*.png").Length;
                    int mod = (int)(files_len / 5);
                    var count = 1;
                    WWWForm form = new WWWForm();
                    var random_num = unixTime.ToString();
                    form.AddField("video_name", "Images_Generation_" + random_num);
                    UnityWebRequest oldreq = UnityWebRequest.Post(base_URL + "create_folder", form);
                    StartCoroutine(request(oldreq));
                    List<string> files = new List<string>();

                    foreach (var file in d.GetFiles("*.png"))
                    {
                        Debug.Log(file);
                        if (count% mod == 0)
                        {
                            files.Add(file.FullName);
                            //var im = File.Open(file.FullName, FileMode.Open);
                            //byte[] m_Bytes = ReadToEnd(im);
                            //form = new WWWForm();
                            //form.AddField("prompt", "vibrant road, traffic, environment, best quality, ultra high res, photorealistic, realistic weather, rain, realistic vehicles, accurate backgrounds, high definition backgrounds, Clear roads");
                            //form.AddField("negative_prompt", "paintings, sketches, worst quality, low quality, normal quality, lowres, normal quality, monochrome, grayscale,unnecssar shadows, added details,white lines, white streaks, main vehicle in the image");
                            //form.AddField("folder_name", "Images_Generation_" + random_num);
                            //form.AddField("count", count.ToString());
                            //form.AddBinaryData("image", m_Bytes);
                            //UnityWebRequest req = UnityWebRequest.Post(base_URL + "process_image_individual", form);
                        }
                        count += 1;
                    }
                    StartCoroutine(requestRecursive(base_URL + "process_image_individual", files, random_num, 1));
                }
                else
                {
                    return;
                }
            }
            public IEnumerator request(UnityWebRequest req)
            {
                Debug.Log(req.uri);
                using (req)
                {
                    Debug.Log(req);
                    yield return req.SendWebRequest();

                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        Debug.Log(req.error);

                    }
                    else
                    {
                        Debug.Log("Form upload complete!");

                    }
                }
            }
            public IEnumerator requestRecursive(string url,List<string> files,string random_num,int count)
            {
                if (files.Count==1)
                {
                    var file = files[0];

                    var im = File.Open(file, FileMode.Open);
                    byte[] m_Bytes = ReadToEnd(im);
                    var form = new WWWForm();
                    form.AddField("prompt", prompt);
                    form.AddField("negative_prompt", "paintings, sketches, worst quality, low quality, normal quality, lowres, normal quality, monochrome, grayscale,unnecssar shadows, added details,white lines, white streaks, smaller vehicles, background vehicles, main vehicle in the image");
                    form.AddField("folder_name", "Images_Generation_" + random_num);
                    form.AddField("count", count.ToString());
                    form.AddBinaryData("image", m_Bytes);
                    UnityWebRequest req = UnityWebRequest.Post(base_URL + "process_image_individual", form);
                    Debug.Log(req.uri);
                    using (req)
                    {
                        Debug.Log(req);
                        yield return req.SendWebRequest();

                        if (req.result != UnityWebRequest.Result.Success)
                        {
                            Debug.Log(req.error);

                        }
                        else
                        {
                            Debug.Log("Form upload completed!");
                            var nform= new WWWForm();
                            nform.AddField("folder_name", "Images_Generation_" + random_num);
                            using (UnityWebRequest www = UnityWebRequest.Post(base_URL + "download_dataset", nform))
                            {
                                yield return www.Send();
                                if (www.isNetworkError || www.isHttpError)
                                {
                                    Debug.Log(www.error);
                                }
                                else
                                {
                                    string savePath = string.Format("{0}/{1}.zip", oldpath, "Images_Generation_" + random_num+".zip");
                                    System.IO.File.WriteAllBytes(savePath, www.downloadHandler.data);
                                }
                            }
                        }
                    }
                }
                else
                {
                    var file = files[0];

                    var im = File.Open(file, FileMode.Open);
                    byte[] m_Bytes = ReadToEnd(im);
                    var form = new WWWForm();
                    form.AddField("prompt",prompt );
                    form.AddField("negative_prompt", "paintings, sketches, worst quality, low quality, normal quality, lowres, normal quality, monochrome, grayscale,unnecssar shadows, added details,white lines, white streaks,smaller vehicles, background vehicles, main vehicle in the image");
                    form.AddField("folder_name", "Images_Generation_" + random_num);
                    form.AddField("count", count.ToString());
                    form.AddBinaryData("image", m_Bytes);
                    UnityWebRequest req = UnityWebRequest.Post(base_URL + "process_image_individual", form);
                    Debug.Log(req.uri);
                    using (req)
                    {
                        Debug.Log(req);
                        yield return req.SendWebRequest();

                        if (req.result != UnityWebRequest.Result.Success)
                        {
                            Debug.Log(req.error);
                            
                        }
                        else
                        {
                            Debug.Log("Form upload complete for "+ file);
                            files.Remove(file);
                            yield return new WaitForSeconds(0.1f);
                            StartCoroutine(requestRecursive(url,files,random_num,count+1));
                        }
                    }
                }

            }

            void CaptureImageSequence()
            {
                if (isRecording)
                {
                    if ((stopRecordingMode == STOP_RECORDING.ByHotkey && IsRecordingHotKeyDown()) ||
                        (stopRecordingMode == STOP_RECORDING.AfterNFrame && nFrameCounter > nFrame) ||
                        (stopRecordingMode == STOP_RECORDING.AfterNSec && nFrameCounter > nSec * fPS))
                    {
                        StopRecording();                        
                    }
                }
                else 
                {
                    if (beginRecordingMode == BEGIN_RECORDING.ByHotkey && IsRecordingHotKeyDown())
                        BeginRecording();
                }


                if (isRecording == false)
                    return;

                ++nFrameCounter;
                ScreenCapture.CaptureScreenshot(GetSaveFileName(outputPath), superSize);                
            }

            public void CaptureScreenshot()
            { 
                if(string.IsNullOrEmpty(outputPath))
                {
                    Debug.LogError("Render Monster: Can not capture screenshot. Output directory is not defined.\n");
                    return;
                }


                string saveFolder = Path.Combine(outputPath, "Screenshot");
                if (Directory.Exists(saveFolder) == false)
                    Directory.CreateDirectory(saveFolder);

                if (Directory.Exists(saveFolder))
                {
                    string fileName = GetSaveFileName(saveFolder);
                    ScreenCapture.CaptureScreenshot(fileName, superSize);

                    Debug.Log("Render Monster: Screenshot saved at path.\n" + fileName + "\n");
                }
                else
                {
                    Debug.LogError("Render Monster: Can not capture screenshot. Directory '" + outputPath + "' does not exist.\n");
                }
            }

            bool IsRecordingHotKeyDown()
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current[recordingHotkey].wasPressedThisFrame;
#else
                return Input.GetKeyDown(recordingHotkey);
#endif
            }

            bool IsScreenShotHotKeyDown()
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current[screenshotHotkey].wasPressedThisFrame;
#else
                return Input.GetKeyDown(screenshotHotkey);
#endif
            }

            string GetSaveFileName(string path)
            {
                lastSavedFileName = Path.Combine(path, (string.IsNullOrEmpty(filePrefix) ? string.Empty : (filePrefix + "_")) + Time.frameCount + ".png");

                return lastSavedFileName;
            }


#if UNITY_EDITOR
            [ContextMenu("Open Save Folder")]
            public void OpenSaveFolder()
            {
                if (string.IsNullOrEmpty(outputPath) == false && Directory.Exists(outputPath))
                {
                    System.Diagnostics.Process[] localByName = System.Diagnostics.Process.GetProcessesByName(outputPath);

                    if (localByName == null || localByName.Length == 0)
                        System.Diagnostics.Process.Start(outputPath);
                }
                else
                {
                    Debug.LogError("Render Monster: Directory " + (string.IsNullOrEmpty(outputPath) ? string.Empty : ("'" + outputPath + "' ")) + "does not exist.\n");
                }
            }
#endif
        }
    }
}
