using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Video;
using System;
using System.IO;

namespace Panorama180ToPinhole
{
    [RequireComponent(typeof(Camera))]
    public class Panorama180ToPinhole : MonoBehaviour
    {
        [SerializeField] VideoClip VR180VideoClip;   // VR180のmp4を指定.

        [SerializeField] float CameraFOV = 60.0f;   // 視野角度.
        [SerializeField] float CameraTilt = 40.0f;   // 各カメラの傾き.
        [SerializeField] Vector2Int TextureSize = new Vector2Int(800, 600);   // テクスチャサイズ.
        [SerializeField] double CaptureFPS = 2.0;   // キャプチャのfps.
        [SerializeField] string OutputPath = "Output";  // 出力パス.

        private List<GameObject> m_camerasList = null;              // Pinhole投影を行うカメラ.
        private List<RenderTexture> m_renderTextureList = null;     // RenderTexture.

        private GameObject m_HalfSphere = null;     // 半球型の動画を貼り付けるGameObject.

        private GameObject m_videoG;        // VideoPlayerのGameObject.
        private VideoPlayer m_videoPlayer;  // Video Player.

        private double m_curTime = 0.0;     // 動画のカレント時間.
        private int m_counter = 0;          // 連番のカウンタ.
        private bool m_outputBusy = false;  // Coroutine実行中の場合はtrue.

        private Texture2D m_tex = null;     // 作業用のテクスチャ.

        // Start is called before the first frame update
        void Start()
        {
            // メインカメラのパラメータを変更.
            UpdateMainCameraParameters();

            // 半球を作成.
            LoadHalfSphere();

            // カメラを作成.
            CreatePinholeCameras();

            // Video Playerを作成.
            CreateVideoPlayer();

            // Videoの初期化.
            InitVideo();

            // Videoを再生.
            if (m_videoPlayer != null) {
                m_videoPlayer.Play();
            }

            m_outputBusy = false;
            m_curTime = 0.0;
            m_counter = 0;
        }

        void OnDestroy()
        {
            if (m_HalfSphere != null) Destroy(m_HalfSphere);
            if (m_tex != null) Destroy(m_tex);
            if (m_videoG != null) Destroy(m_videoG);
            for (int i = 0; i < m_renderTextureList.Count; ++i) {
                if (m_renderTextureList[i] != null) Destroy(m_renderTextureList[i]);
            }
            m_HalfSphere = null;
            m_tex = null;
            m_renderTextureList = null;
            m_videoG = null;
        }

        /**
         * メインカメラのパラメータを変更.
         */
        void UpdateMainCameraParameters()
        {
            Camera c = gameObject.GetComponent<Camera>();
            if (c != null) {
                c.farClipPlane = Math.Max(c.farClipPlane, 100000.0f);
                gameObject.transform.localPosition = Vector3.zero;
                gameObject.transform.localRotation = Quaternion.identity;
                gameObject.transform.localScale    = Vector3.one;
            }
        }

        /**
         * 半球のオブジェクトをロード.
         */
        void LoadHalfSphere(float scaleV = 8000.0f)
        {
            if (m_HalfSphere != null) return;

            GameObject prefab = (GameObject)Resources.Load("Prefabs/HalfSphere");
            if (prefab != null) {
                m_HalfSphere = Instantiate(prefab, Vector3.zero, Quaternion.Euler(0, 180, 0));
                m_HalfSphere.name = "VR180_HalfSphere";
                m_HalfSphere.transform.localScale = new Vector3(scaleV, scaleV, scaleV);
            }
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
                string filePath = $"{OutputPath}/image_{i}" + string.Format("{0:D5}", m_counter) + ".jpg";
                SaveRenderTextureToFile(rt, filePath);
            }
            m_counter++;

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
                    Debug.Log("Finished!");
                }
            }
        }

        /**
         * RenderTextureを画像ファイルとして保存.
         */
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
         *   RenderTexture (Asset).
         *   Material (Asset).
         *   Camera
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
         * Video Playerを作成.
         */
        void CreateVideoPlayer()
        {
            m_videoG = new GameObject("VR180_VideoPlayer");
            m_videoPlayer = m_videoG.AddComponent<VideoPlayer>();
        }

        /**
         * Video Playerの再生を初期化（停止）.
         */
        void InitVideo()
        {
            if (m_videoG == null || m_HalfSphere == null) return;

            MeshRenderer meshR = m_HalfSphere.GetComponent<MeshRenderer>();
            if (meshR == null) return;
            Material mat = meshR.sharedMaterial;

            m_videoPlayer = m_videoG.GetComponent<VideoPlayer>();
            m_videoPlayer.clip = VR180VideoClip;
            m_videoPlayer.renderMode = VideoRenderMode.RenderTexture;

            Texture tex = mat.GetTexture("_MainTex");
            if (tex != null && (tex is RenderTexture)) {
                m_videoPlayer.targetTexture = (RenderTexture)tex;
            }

            m_videoPlayer.isLooping = false;
            m_videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            m_videoPlayer.playOnAwake       = false;
            m_videoPlayer.waitForFirstFrame = true;     // ソースVideoの最初のフレームが表示される状態になるまで待機する.
            m_videoPlayer.skipOnDrop        = false;    // 同期のためのフレームスキップの有効化.

            m_videoPlayer.Stop();
        }

    }
}
