﻿using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ZeroEditor
{
    /// <summary>
    /// 位图字体创建，菜单
    /// </summary>
    public class BitmapFontCreaterMenu
    {
        /// <summary>
        /// 直接用当前目录下的资源创建字体
        /// 文件名
        /// </summary>
        [MenuItem("Assets/Zero/Create Bitmap Font (Direct)/Use「PNG File Name」", false, 0)]
        static void CreateBitmapFontUsePNGFileName()
        {
            if (Selection.objects.Length != 1)
            {
                Debug.LogError("仅针对文件夹进行操作!");
                return;
            }

            var dirObj = Selection.objects[0];
            var path = AssetDatabase.GetAssetPath(dirObj);
            if (false == Directory.Exists(path))
            {
                Debug.LogError("选中的并不是一个文件夹!");
                return;
            }

            //找到所有的图片
            var files = Directory.GetFiles(path, "*.png", SearchOption.TopDirectoryOnly);

            List<Texture2D> textures = new List<Texture2D>();
            List<char> chars = new List<char>();
            for (var i = 0; i < files.Length; i++)
            {
                var nameChars = Path.GetFileNameWithoutExtension(files[i]).ToCharArray();
                // Debug.LogFormat("文件[{0}] name[{1}]", files[i], nameChars[0]);
                if (nameChars.Length != 1)
                {
                    Debug.LogErrorFormat("文件[{0}]被跳过，因为他的文件名不是单字符", files[i]);
                    continue;
                }

                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(files[i]);

                if (texture == null)
                {
                    // 如果文件名是 "..png"，则 AssetDatabase.LoadAssetAtPath 会加载不到图片资源。
                    Debug.LogErrorFormat("加载图片 [{0}] 失败, 可能是文件名不合法", files[i]);

                    string oldPath = files[i];
                    string oldFilename = Path.GetFileName(oldPath);
                    string newFilename = "_" + oldFilename;
                    string newPath = oldPath.Replace(oldFilename, newFilename);
                    Debug.LogWarningFormat("复制图片 [{0}] -> [{1}]...", oldPath, newPath);

                    if (File.Exists(newPath))
                    {
                        Debug.LogErrorFormat("发现已有图片 [{0}]，先删除文件，再进行复制", newPath);
                        File.Delete(newPath);
                    }

                    File.Copy(oldPath, newPath);
                    AssetDatabase.Refresh();
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>(newPath);

                    if (texture == null)
                    {
                        Debug.LogErrorFormat("经过处理，加载图片 [{0}] 仍旧失败，跳过该文件", files[i]);
                        continue;
                    }
                }

                textures.Add(texture);

                chars.Add(nameChars[0]);
            }

            new BitmapFontCreateCommand(textures.ToArray(), chars.ToArray(), path, dirObj.name).Execute();
        }

        /// <summary>
        /// 直接用当前目录下的资源创建字体
        /// </summary>
        [MenuItem("Assets/Zero/Create Bitmap Font (Direct)/Use「chars.txt」", false, 0)]
        static void CreateBitmapFontUseCharsTxt()
        {
            if (Selection.objects.Length != 1)
            {
                Debug.LogError("仅针对文件夹进行操作!");
                return;
            }

            var dirObj = Selection.objects[0];
            var path = AssetDatabase.GetAssetPath(dirObj);
            if (false == Directory.Exists(path))
            {
                Debug.LogError("选中的并不是一个文件夹!");
                return;
            }

            var charsTxtFile = Path.Combine(path, "chars.txt");
            if (false == File.Exists(charsTxtFile))
            {
                Debug.LogErrorFormat("文件[{0}]不存在!", charsTxtFile);
                return;
            }

            string charsContent = File.ReadAllText(charsTxtFile);
            char[] chars = charsContent.ToCharArray();

            //找到所有的图片
            var files = Directory.GetFiles(path, "*.png", SearchOption.TopDirectoryOnly);

            if (chars.Length != files.Length)
            {
                Debug.LogErrorFormat("PNG文件数量({0})和字符数量({1})不一致，请确定两者一致避免出错!", files.Length, chars.Length);
                return;
            }

            Texture2D[] textures = new Texture2D[files.Length];

            for (var i = 0; i < files.Length; i++)
            {
                textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(files[i]);
            }

            new BitmapFontCreateCommand(textures, chars, path, dirObj.name).Execute();
        }

        /// <summary>
        /// 直接
        /// </summary>
        [MenuItem("Assets/Zero/Create Bitmap Font (GUI)", false, 1)]
        static void CreateBitmapFontGUI()
        {
            var editorWin = BitmapFontCreateEditorWindow.Open();

            if (Selection.objects.Length > 1 || Selection.objects[0] is Texture2D)
            {
                foreach (var obj in Selection.objects)
                {
                    if (obj is Texture2D)
                    {
                        editorWin.textures.Add(obj as Texture2D);
                    }
                }
            }
            else if (Selection.objects.Length == 1)
            {
                var obj = Selection.objects[0];
                var path = AssetDatabase.GetAssetPath(obj);
                if (Directory.Exists(path))
                {
                    editorWin.outputPath = path;
                    editorWin.fontName = obj.name;
                    //找到所有的图片
                    var files = Directory.GetFiles(path, "*.png", SearchOption.TopDirectoryOnly);
                    List<char> chars = new List<char>();
                    for (var i = 0; i < files.Length; i++)
                    {
                        var nameChars = Path.GetFileNameWithoutExtension(files[i]).ToCharArray();
                        if (nameChars.Length == 1)
                        {
                            chars.Add(nameChars[0]);
                        }

                        editorWin.textures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(files[i]));
                    }


                    var charsTxtFile = Path.Combine(path, "chars.txt");
                    if (File.Exists(charsTxtFile))
                    {
                        //如果目录中有「chars.txt」文件，则提取字符填入   
                        string charsContent = File.ReadAllText(charsTxtFile);
                        editorWin.charContent = charsContent;
                    }
                    else
                    {
                        editorWin.charContent = new string(chars.ToArray());
                    }
                }
            }
        }

        [MenuItem("Tools/Zero/Create Bitmap Font", false, 1)]
        static void CreateBitmapFontGUITools()
        {
            BitmapFontCreateEditorWindow.Open();
        }
    }
}