using UnityEngine;
using UnityEditor;
using System.IO;

public class AnimationExtractor : MonoBehaviour
{
    [MenuItem("Assets/Extract Animations")]
    static void ExtractAnimations()
    {
        // Récupère l'objet sélectionné
        Object[] selectedAssets = Selection.objects;
        
        foreach (Object asset in selectedAssets)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            
            // Récupère toutes les animations du FBX
            Object[] clips = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            
            foreach (Object clip in clips)
            {
                if (clip is AnimationClip)
                {
                    AnimationClip animClip = clip as AnimationClip;
                    
                    // Crée un dossier "Extracted Animations" s'il n'existe pas
                    string extractPath = "Assets/Extracted Animations";
                    if (!Directory.Exists(extractPath))
                    {
                        Directory.CreateDirectory(extractPath);
                    }
                    
                    // Crée une copie de l'animation
                    AnimationClip newClip = new AnimationClip();
                    EditorUtility.CopySerialized(animClip, newClip);
                    
                    // Sauvegarde l'animation
                    string savePath = $"{extractPath}/{animClip.name}.anim";
                    AssetDatabase.CreateAsset(newClip, savePath);
                    
                    Debug.Log($"Animation extraite : {animClip.name}");
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Extraction terminée !");
    }
}
