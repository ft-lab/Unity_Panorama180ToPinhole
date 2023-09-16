using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Video;
using System;
using System.IO;

namespace PanoramaToPinhole
{
    [RequireComponent(typeof(Camera))]
    public class VR180PanoramaToPinhole : MonoBehaviour
    {
        [SerializeField] float CameraFOV = 60.0f;   // 視野角度.
        [SerializeField] float CameraTilt = 40.0f;   // 各カメラの傾き.
        [SerializeField] Vector2Int TextureSize = new Vector2Int(800, 600);   // テクスチャサイズ.
        [SerializeField] double CaptureFPS = 2.0;   // キャプチャのfps.
        [SerializeField] string OutputPath = "Output";  // 出力パス.

        private List<GameObject> m_camerasList = null;              // Pinhole投影を行うカメラ.
        private List<RenderTexture> m_renderTextureList = null;     // RenderTexture.

        private GameObject m_videoG;        // VideoPlayerのGameObject.
        private VideoPlayer m_videoPlayer;  // Video Player.

        private double m_curTime = 0.0;     // 動画のカレント時間.
        private int m_counter = 0;          // 連番のカウンタ.
        private bool m_outputBusy = false;  // Coroutine実行中の場合はtrue.

        private Texture2D m_tex = null;     // 作業用のテクスチャ.
        private bool m_finished = false;    // 完了したらtrue.

        // Start is called before the first frame update
        void Start()
        {
            // カメラを作成.
            CreatePinholeCameras();

            // Videoの初期化.
            InitVideo();

            if (m_videoPlayer != null) {
                m_videoPlayer.Play();
            }
            m_outputBusy = false;
            m_curTime = 0.0;
            m_counter = 0;
            m_finished = false;
        }

        // Update is called once per frame
        void Update()
        {
            // 一定間隔でキャプチャして出力.
            if (!m_outputBusy)
            {
                if (m_videoPlayer != null && m_videoPlayer.isPlaying) {
                    double delta = 1.0 / Math.Max(CaptureFPS, 0.0001);
                    double t = m_videoPlayer.time;
                    if (t - m_curTime >= delta)
                    {
                        m_videoPlayer.Pause();
                        m_curTime = t;
                        m_outputBusy = true;
                        StartCoroutine(OutputStillImagesCo());
                    }
                }
            }
        }

        IEnumerator OutputStillImagesCo ()
        {
            if (m_videoPlayer == null) yield break;
            if (!Directory.Exists(OutputPath)) Directory.CreateDirectory(OutputPath);

            yield return new WaitForEndOfFrame();

            for (int i = 0; i < 5; ++i)
            {
                RenderTexture rt = m_renderTextureList[i];

                // ファイル出力.
                string filePath = $"{OutputPath}/image_" + string.Format("{0:D5}", m_counter) + ".jpg";
                SaveRenderTextureToFile(rt, filePath);

                m_counter++;
            }

            m_outputBusy = false;

            {
                int persV = (int)((m_curTime * 100.0) / m_videoPlayer.length);
                Debug.Log($"Process : {persV} %");
            }

            // ビデオを再開.
            m_videoPlayer.Play();

            {
                double delta = 1.0 / Math.Max(CaptureFPS, 0.0001);
                if (m_curTime + delta >= m_videoPlayer.length) {
                    m_finished = true;
                    Debug.Log("Finished!");
                }
            }
        }

        void SaveRenderTextureToFile(RenderTexture rt, string fileName)
        {
            if (rt == null) return;

            if (m_tex == null) {
                m_tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            }

            RenderTexture.active = rt;
            m_tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            m_tex.Apply();

            int jpegQuality = 95;
            byte[] bytes = m_tex.EncodeToJPG(jpegQuality);
            File.WriteAllBytes(fileName, bytes);
        }

        /**
         * Pinholeのカメラを作成.
         * 以下を生成する.
         * RenderTexture (Asset).
         * Material (Asset).
         * Camera
         */
        void CreatePinholeCameras()
        {
            // RenderTextureを作成.
            CreateRenderTextures(TextureSize.x, TextureSize.y);

            // カメラを作成.
            m_camerasList = new List<GameObject>();
            for (int i = 0; i < 5; ++i) {
                GameObject g = new GameObject($"camera_{i}");
                g.transform.parent = this.gameObject.transform;
                g.transform.localPosition = Vector3.zero;
                g.transform.localRotation = Quaternion.identity;
                g.transform.localScale    = Vector3.one;

                Camera c = g.AddComponent<Camera>();
                c.nearClipPlane = 0.1f;
                c.farClipPlane = 100000.0f;

                if (m_renderTextureList != null) {
                    RenderTexture rt = m_renderTextureList[i];
                    c.targetTexture = rt;
                }

                m_camerasList.Add(g);
            }

            // カメラのパラメータを変更.
            UpdateCameraParameters();
        }

        /**
         * RenderTextureを作成.
         */
         void CreateRenderTextures(int width = 800, int height = 600)
         {
            if (m_renderTextureList != null) return;

            m_renderTextureList = new List<RenderTexture>();
            for (int i = 0; i < 5; ++i) {
                RenderTexture rt = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);
                rt.Create();
                rt.name = $"renderTexture_{i}";
                m_renderTextureList.Add(rt);
            }
         }

        /**
         * カメラのパラメータを変更.
         */
        void UpdateCameraParameters()
        {
            if (m_camerasList == null) return;

            for (int i = 0; i < 5; ++i) {
                GameObject g = m_camerasList[i];
                Camera c = g.GetComponent<Camera>();
                if (c == null) continue;

                c.fieldOfView = CameraFOV;

                Vector3 cameraRot = new Vector3();
                switch (i)
                {
                case 0:
                    break;
                case 1:
                    cameraRot.y = -CameraTilt;
                    break;
                case 2:
                    cameraRot.y = +CameraTilt;
                    break;
                case 3:
                    cameraRot.x = -CameraTilt;
                    break;
                case 4:
                    cameraRot.x = +CameraTilt;
                    break;
                }
                g.transform.localRotation = Quaternion.Euler(cameraRot.x, cameraRot.y, cameraRot.z);
            }
        }

        /**
         * Video Playerの再生を初期化（停止）.
         */
        void InitVideo()
        {
            m_videoG = GameObject.Find("/Video Player");
            if (m_videoG == null) return;

            m_videoPlayer = m_videoG.GetComponent<VideoPlayer>();
            m_videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            m_videoPlayer.isLooping = false;
            m_videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            m_videoPlayer.playOnAwake       = false;
            m_videoPlayer.waitForFirstFrame = true;     // ソースVideoの最初のフレームが表示される状態になるまで待機する.
            m_videoPlayer.skipOnDrop        = false;    // 同期のためのフレームスキップの有効化.

            m_videoPlayer.Stop();
        }

    }
}
