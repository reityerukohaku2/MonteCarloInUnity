using UnityEngine;

public class PathTracing : MonoBehaviour
{
    public ComputeShader computeShader;
    public Mesh mesh;
    public Material material;
    public RenderTexture renderTexture;
    private ComputeBuffer triangleBuffer;
    private Camera cam;
    private int kernelHandle;

    struct Triangle
    {
        public Vector3 v0;
        public Vector3 v1;
        public Vector3 v2;
    }

    void Start()
    {
        cam = Camera.main;

        // RenderTextureの設定
        //renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
        //renderTexture.enableRandomWrite = true;
        //renderTexture.Create();

        // Compute Shaderの設定
        computeShader.SetTexture(0, "Result", renderTexture);

        // メッシュの三角形データを設定
        var triangles = mesh.triangles;
        var vertices = mesh.vertices;
        var triangleData = new Triangle[triangles.Length / 3];


        // メッシュの頂点をローカル→ワールド座標系に変換
        Matrix4x4 matrix = transform.localToWorldMatrix;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            triangleData[i / 3] = new Triangle
            {
                v0 = matrix.MultiplyPoint3x4(vertices[triangles[i]]),
                v1 = matrix.MultiplyPoint3x4(vertices[triangles[i + 1]]),
                v2 = matrix.MultiplyPoint3x4(vertices[triangles[i + 2]])
            };
        }

        triangleBuffer = new ComputeBuffer(triangleData.Length, sizeof(float) * 9);
        triangleBuffer.SetData(triangleData);
        computeShader.SetBuffer(0, "triangles", triangleBuffer);

        // カメラデータの設定
        computeShader.SetVector("pos", cam.transform.position);
        computeShader.SetVector("forward", cam.transform.forward);
        computeShader.SetVector("up", cam.transform.up);
        computeShader.SetVector("right", cam.transform.right);
        computeShader.SetVector("screenSize", new Vector2(renderTexture.width, renderTexture.height));
        computeShader.SetFloat("fov", cam.fieldOfView);

        // Compute Shaderの実行
        kernelHandle = computeShader.FindKernel("CSMain");
        computeShader.Dispatch(kernelHandle, renderTexture.width / 8, renderTexture.height / 8, 1);

        // Materialにテクスチャをセット
        material.mainTexture = renderTexture;
    }

    private void Update()
    {
        // Compute Shaderの実行
        computeShader.Dispatch(kernelHandle, renderTexture.width / 8, renderTexture.height / 8, 1);

        // Materialにテクスチャをセット
        material.mainTexture = renderTexture;
    }

    void OnDestroy()
    {
        if (triangleBuffer != null) triangleBuffer.Release();
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        // 描画
        Graphics.Blit(renderTexture, dest, material);
    }
}