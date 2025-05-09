using UnityEngine;
using UnityEditor;
using System.IO;

public class RenderTextureSaver : EditorWindow
{
    RenderTexture renderTextureToSave;
    string saveFileName = "BakedTexture.png";
    string saveFolderPath = "Assets/BakedTextures"; // 저장될 폴더 경로

    [MenuItem("Tools/Render Texture Saver")]
    public static void ShowWindow()
    {
        GetWindow<RenderTextureSaver>("Render Texture Saver");
    }

    void OnGUI()
    {
        GUILayout.Label("Render Texture to PNG Saver", EditorStyles.boldLabel);

        renderTextureToSave = (RenderTexture)EditorGUILayout.ObjectField("Render Texture", renderTextureToSave, typeof(RenderTexture), false);
        saveFileName = EditorGUILayout.TextField("File Name", saveFileName);
        saveFolderPath = EditorGUILayout.TextField("Save Folder Path", saveFolderPath);

        if (GUILayout.Button("Save Render Texture to PNG"))
        {
            if (renderTextureToSave == null)
            {
                Debug.LogError("Render Texture not selected!");
                return;
            }
            SaveTexture();
        }
    }

    void SaveTexture()
    {
        RenderTexture prevActive = RenderTexture.active; // 이전 활성 렌더 텍스처 저장
        RenderTexture.active = renderTextureToSave; // 저장할 렌더 텍스처를 활성화

        // 렌더 텍스처의 내용을 읽어올 Texture2D 생성
        Texture2D texture2D = new Texture2D(renderTextureToSave.width, renderTextureToSave.height, TextureFormat.RGBA32, false);
        texture2D.ReadPixels(new Rect(0, 0, renderTextureToSave.width, renderTextureToSave.height), 0, 0);
        texture2D.Apply(); // 변경사항 적용

        RenderTexture.active = prevActive; // 원래 활성 렌더 텍스처로 복원

        byte[] bytes = texture2D.EncodeToPNG();
        Object.DestroyImmediate(texture2D); // 임시 Texture2D 객체 파괴

        // 폴더가 없으면 생성
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
        }

        string fullPath = Path.Combine(saveFolderPath, saveFileName);
        File.WriteAllBytes(fullPath, bytes);
        Debug.Log($"Saved texture to: {fullPath}");

        AssetDatabase.Refresh(); // 프로젝트 창에 새 파일이 보이도록 새로고침
    }
}