using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Video;
using System;
using System.IO;

#pragma warning disable 0414

namespace Panorama180ToPinhole
{
    [RequireComponent(typeof(Camera))]
    public class Panorama180ToPinhole : MonoBehaviour
    {
        // カメラの種類.
        public enum VideoEyesType {
            OneEye,
            TwoEyes
        }

        // レンズの種類.
        public enum VideoLensType {
            Equirectangular,
            FishEye
        }

        // カメラのFOVプリセット.
        public enum CameraFOVPresetType {
            None,                   // 未使用.
            Custom,                 // カスタム.
            GoPro7_4x3_wide,        // 4x3 広角(ズーム0) 122.6 x 94.4
            GoPro7_16x9_wide,       // 16x9 広角(ズーム0) 118.2 x 69.5
            GoPro12_4x3_wide_HyperSmooth_on,       // 4x3 広角, HyperSmooth On (113 x 87)
            GoPro12_4x3_wide_HyperSmooth_off,      // 4x3 広角, HyperSmooth Off (121 x 93)
            GoPro12_16x9_wide_HyperSmooth_on,      // 16x9 広角, HyperSmooth On (109 x 63)
            GoPro12_16x9_wide_HyperSmooth_off,     // 16x9 広角, HyperSmooth Off (118 x 69)
            GoPro12_8x7_wide_HyperSmooth_on,       // 8x7 広角, HyperSmooth On (113 x 100)
            GoPro12_8x7_wide_HyperSmooth_off,      // 8x7 広角, HyperSmooth Off (122 x 108)
        }

        // 動画を貼り付ける背景のRenderTextureのサイズ.
        public enum BackgroundTextureSize {
            TextureSize_1024,
            TextureSize_2048,
            TextureSize_4096,
            TextureSize_8192
        }

        [SerializeField] [HideInInspector] VideoClip PanoramaVideoClip;   // パノラマ180または魚眼のmp4を指定.
        [SerializeField] [HideInInspector] bool StopVideo = false;

        // カメラのパラメータ.
        [SerializeField] [HideInInspector] bool CameraParam_foldout = true;        // Cameraグループの表示.
        [SerializeField] [HideInInspector] VideoEyesType CameraEyesType = VideoEyesType.TwoEyes;   // 単眼か2眼か.
        [SerializeField] [HideInInspector] VideoLensType CameraLensType = VideoLensType.Equirectangular;   // パノラマ180か魚眼か.
        [SerializeField] [HideInInspector] CameraFOVPresetType CameraPresetType = CameraFOVPresetType.None;   // カメラのFOVプリセット.
        [SerializeField] [HideInInspector] float CameraFOVH = 180.0f;       // カメラの視野角度(H)
        [SerializeField] [HideInInspector] float CameraFOVV = 180.0f;       // カメラの視野角度(V)

        // キャプチャのパラメータ.
        [SerializeField] [HideInInspector] bool CaptureParam_foldout = true;   // Captureグループの表示.
        [SerializeField] [HideInInspector] BackgroundTextureSize CaptureBackgroundTextureSize = BackgroundTextureSize.TextureSize_4096;   // 背景として描画するRenderTextureのサイズ.
        [SerializeField] [HideInInspector] float CaptureCameraFOV = 60.0f;   // 視野角度.
        [SerializeField] [HideInInspector] float CaptureCameraTiltH = 30.0f;   // 各カメラの傾き(水平).
        [SerializeField] [HideInInspector] float CaptureCameraTiltV = 20.0f;   // 各カメラの傾き（垂直）.

        // 出力関連のパラメータ.
        [SerializeField] [HideInInspector] bool OutputParam_foldout = true;   // Outputグループの表示.

        [SerializeField] [HideInInspector] Vector2Int OutputTextureSize = new Vector2Int(800, 600);   // テクスチャサイズ.
        [SerializeField] [HideInInspector] double OutputCaptureFPS = 2.0;   // キャプチャのfps.
        [SerializeField] [HideInInspector] bool OutputFiles = true;   // ファイルを出力するか.
        [SerializeField] [HideInInspector] string OutputPath = "Output";  // 出力パス.
        [SerializeField] [HideInInspector] bool OutputSpecifyRange = false;   // 範囲を指定.
        [SerializeField] [HideInInspector] float OutputStartTimeSec = 0.0f;   // 開始時間（秒）.
        [SerializeField] [HideInInspector] float OutputEndTimeSec = 0.0f;   // 終了時間（秒）.


        // ------------------------------------.

        private RenderTexture m_backgroundRT = null;         // 動画をレンダリングするテクスチャ.
        private RenderTexture m_resultRT = null;         // 結果のテクスチャ.

        private List<GameObject> m_camerasList = null;              // Pinhole投影を行うカメラ.
        private List<RenderTexture> m_renderTextureList = null;     // RenderTexture.

        private GameObject m_HalfSphere = null;     // 半球型の動画を貼り付けるGameObject.

        private GameObject m_videoG;        // VideoPlayerのGameObject.
        private VideoPlayer m_videoPlayer;  // Video Player.

        private double m_curTime = 0.0;     // 動画のカレント時間.
        private int m_counter = 0;          // 連番のカウンタ.
        private bool m_outputBusy = false;  // Coroutine実行中の場合はtrue.

        private Texture2D m_tex = null;     // 作業用のテクスチャ.

        private Material m_FishEyeMat = null;  // 魚眼変換の行列.

        // Start is called before the first frame update
        void Start()
        {
            // メインカメラのパラメータを変更.
            UpdateMainCameraParameters();

            // 半球を作成.
            LoadHalfSphere();

            // カメラを作成.
            CreatePinholeCameras();

            Shader shader = Shader.Find("Hidden/Panorama180ToPinhole/FishEyeToEquirectangular180");
            if (shader != null) {
                m_FishEyeMat = new Material(shader);
            }

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
            if (m_backgroundRT != null) Destroy(m_backgroundRT);
            if (m_resultRT != null) Destroy(m_resultRT);
            if (m_FishEyeMat != null) Destroy(m_FishEyeMat);

            m_HalfSphere = null;
            m_tex = null;
            m_renderTextureList = null;
            m_videoG = null;
            m_backgroundRT = null;
            m_resultRT = null;
            m_FishEyeMat = null;
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

            GameObject prefab = (GameObject)Resources.Load("Prefabs/HalfSphere_full");
            if (prefab != null) {
                m_HalfSphere = Instantiate(prefab, Vector3.zero, Quaternion.Euler(0, 180, 0));
                m_HalfSphere.name = "VR180_HalfSphere";
                m_HalfSphere.transform.localScale = new Vector3(scaleV, scaleV, scaleV);
            }
        }

        /**
         * 背景を描画.
         */
         void UpdateBackgroundTexture()
         {
            if (m_HalfSphere != null && m_backgroundRT != null && m_FishEyeMat != null && m_resultRT != null) {
                m_FishEyeMat.SetTexture("_MainTex", m_backgroundRT);
                m_FishEyeMat.SetVector("_BackgroundColor", new Vector4(0, 0, 0, 1));
                m_FishEyeMat.SetInt("_IsSBS", (CameraEyesType == VideoEyesType.TwoEyes) ? 0 : 1);
                m_FishEyeMat.SetInt("_FishEye", (CameraLensType == VideoLensType.Equirectangular) ? 0 : 1);

                // 元画像のアスペクト比.
                float aspect = (float)m_backgroundRT.width / (float)m_backgroundRT.height;
                m_FishEyeMat.SetFloat("_TextureAspect", aspect);

                // カメラの視野角度.
                m_FishEyeMat.SetFloat("_CameraFOVH", (CameraPresetType == CameraFOVPresetType.None) ? 180.0f : CameraFOVH);
                m_FishEyeMat.SetFloat("_CameraFOVV", (CameraPresetType == CameraFOVPresetType.None) ? 180.0f : CameraFOVV);

                Graphics.Blit(null, m_resultRT, m_FishEyeMat);
            }
         }

        // Update is called once per frame
        void Update()
        {
            // カメラのパラメータを変更.
            UpdateCameraParameters();

            // VideoClipからRenderTextureに反映.
            UpdateBackgroundTexture();

            if (!m_outputBusy)
            {
                if (StopVideo) {
                    m_videoPlayer.Pause();
                    return;
                } else {
                    m_videoPlayer.Play();
                }
            }

            // 一定間隔でキャプチャして出力.
            if (!m_outputBusy)
            {
                if (m_videoPlayer != null && m_videoPlayer.isPlaying) {
                    double delta = 1.0 / Math.Max(OutputCaptureFPS, 0.0001);
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
                if (OutputFiles)
                {
                    string filePath = $"{OutputPath}/image_{i}" + string.Format("{0:D5}", m_counter) + ".jpg";
                    SaveRenderTextureToFile(rt, filePath);
                }
            }
            m_counter++;

            m_outputBusy = false;

            if (OutputFiles) {
                int persV = (int)((m_curTime * 100.0) / m_videoPlayer.length);
                Debug.Log($"Process : {persV} %");
            }

            // ビデオを再開.
            m_videoPlayer.Play();

            if (OutputFiles) {
                double delta = 1.0 / Math.Max(OutputCaptureFPS, 0.0001);
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
            CreateRenderTextures(OutputTextureSize.x, OutputTextureSize.y);

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
                //c.targetDisplay = 1 + i;

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

                c.fieldOfView = CaptureCameraFOV;

                Vector3 cameraRot = new Vector3();
                switch (i)
                {
                case 0:
                    break;
                case 1:
                    cameraRot.y = -CaptureCameraTiltH;
                    break;
                case 2:
                    cameraRot.y = +CaptureCameraTiltH;
                    break;
                case 3:
                    cameraRot.x = -CaptureCameraTiltV;
                    break;
                case 4:
                    cameraRot.x = +CaptureCameraTiltV;
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
         * 背景のテクスチャサイズを数値で取得.
         */
        int GetTextureSize(BackgroundTextureSize bTexSize)
        {
            if (bTexSize == BackgroundTextureSize.TextureSize_1024) return 1024;
            else if (bTexSize == BackgroundTextureSize.TextureSize_2048) return 2048;
            else if (bTexSize == BackgroundTextureSize.TextureSize_4096) return 4096;
            else if (bTexSize == BackgroundTextureSize.TextureSize_8192) return 8192;
            return 4096;
        }

        /**
         * Video Playerの再生を初期化（停止）.
         */
        void InitVideo()
        {
            if (m_videoG == null || m_HalfSphere == null) return;
            if (PanoramaVideoClip == null) return;

            MeshRenderer meshR = m_HalfSphere.GetComponent<MeshRenderer>();
            if (meshR == null) return;
            Material mat = meshR.sharedMaterial;

            m_videoPlayer = m_videoG.GetComponent<VideoPlayer>();
            m_videoPlayer.clip = PanoramaVideoClip;
            m_videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            m_videoPlayer.aspectRatio = VideoAspectRatio.Stretch;

            if (m_backgroundRT == null) {
                int texWidth  = (int)PanoramaVideoClip.width;
                int texHeight = (int)PanoramaVideoClip.height;
                m_backgroundRT = new RenderTexture(texWidth, texHeight, 16, RenderTextureFormat.ARGB32);
                m_backgroundRT.Create();
                m_backgroundRT.name = "backgroundRenderTexture";
            }
            if (m_resultRT == null) {
                int texWidth  = GetTextureSize(CaptureBackgroundTextureSize);
                int texHeight = texWidth;
                m_resultRT = new RenderTexture(texWidth, texHeight, 16, RenderTextureFormat.ARGB32);
                m_resultRT.Create();
                m_resultRT.name = "resultRenderTexture";
            }

            if (m_backgroundRT != null) {
                m_videoPlayer.targetTexture = m_backgroundRT;
            }
            mat.SetTexture("_MainTex", m_resultRT);

            m_videoPlayer.isLooping = false;
            m_videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            m_videoPlayer.playOnAwake       = false;
            m_videoPlayer.waitForFirstFrame = true;     // ソースVideoの最初のフレームが表示される状態になるまで待機する.
            m_videoPlayer.skipOnDrop        = false;    // 同期のためのフレームスキップの有効化.

            m_videoPlayer.Stop();
        }

    }
}
