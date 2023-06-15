using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ZeroEditor
{
    /// <summary>
    /// 位图字体创建指令
    /// </summary>
    public class BitmapFontCreateCommand
    {
        public Texture2D[] Textures { get; private set; }
        public Rect[] Rects { get; private set; }
        public char[] Chars { get; private set; }
        public string OutputPath { get; private set; }
        public string FontName { get; private set; }

        public Texture TextureAtlas { get; private set; }

        // 字体文件（Cocos 中使用，Unity 中可用？）
        public string FontFile { get; private set; }

        // 字体设置文件（Unity 中使用）
        public string FontSettingFile { get; private set; }

        // 贴图文件
        public string TextureAtlasFile { get; private set; }

        // 材质文件
        public string MatFile { get; private set; }

        public BitmapFontCreateCommand(Texture2D[] textures, string charContent, string outputPath, string fontName)
            : this(textures, charContent.ToCharArray(), outputPath, fontName)
        {
        }

        public BitmapFontCreateCommand(Texture2D[] textures, char[] chars, string outputPath, string fontName)
        {
            Textures = textures;
            Chars = chars;
            OutputPath = outputPath;
            FontName = fontName;
            var outFileWithoutExt = Path.Combine(outputPath, fontName);


            FontFile = outFileWithoutExt + ".fnt";
            FontSettingFile = outFileWithoutExt + ".fontsettings";
            TextureAtlasFile = outFileWithoutExt + ".png";
            MatFile = outFileWithoutExt + ".mat";
        }

        /// <summary>
        /// 开始执行
        /// </summary>
        public void Execute()
        {
            //删除旧文件
            DeleteOldFiles();
            //合并图集
            BuildTextureAtlas();

            // 创建字体文本文件
            BuildFontTextFormat();

            //创建字体
            BuildFont();


            AssetDatabase.Refresh();
        }

        void DeleteOldFiles()
        {
            if (File.Exists(TextureAtlasFile))
            {
                File.Delete(TextureAtlasFile);
            }

            if (File.Exists(FontFile))
            {
                File.Delete(FontFile);
            }

            if (File.Exists(FontSettingFile))
            {
                File.Delete(FontSettingFile);
            }

            if (File.Exists(MatFile))
            {
                File.Delete(MatFile);
            }
        }

        void BuildFont()
        {
            Material mat = new Material(Shader.Find("GUI/Text Shader"));
            mat.SetTexture("_MainTex", TextureAtlas);
            Font font = new Font();
            font.material = mat;

            CharacterInfo[] characterInfos = new CharacterInfo[Rects.Length];

            float lineSpace = 0.1f;

            for (int i = 0; i < Rects.Length; i++)
            {
                if (Rects[i].height > lineSpace)
                {
                    lineSpace = Rects[i].height;
                }
            }

            for (int i = 0; i < Rects.Length; i++)
            {
                Rect rect = Rects[i];

                CharacterInfo info = new CharacterInfo();
                info.index = Chars[i];

                float pivot = -lineSpace / 2;
                //pivot = 0;
                int offsetY = (int)(pivot + (lineSpace - rect.height) / 2);
                info.uvBottomLeft =
                    new Vector2((float)rect.x / TextureAtlas.width, (float)(rect.y) / TextureAtlas.height);
                info.uvBottomRight = new Vector2((float)(rect.x + rect.width) / TextureAtlas.width,
                    (float)(rect.y) / TextureAtlas.height);
                info.uvTopLeft = new Vector2((float)rect.x / TextureAtlas.width,
                    (float)(rect.y + rect.height) / TextureAtlas.height);
                info.uvTopRight = new Vector2((float)(rect.x + rect.width) / TextureAtlas.width,
                    (float)(rect.y + rect.height) / TextureAtlas.height);
                info.minX = 0;
                info.minY = -(int)rect.height - offsetY;
                info.maxX = (int)rect.width;
                info.maxY = -offsetY;
                info.advance = (int)rect.width;
                characterInfos[i] = info;
            }

            font.characterInfo = characterInfos;

            AssetDatabase.CreateAsset(mat, MatFile);
            AssetDatabase.CreateAsset(font, FontSettingFile);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
        }

        /*
info face="Noto Sans SC Black" size=32 bold=1 italic=0 charset="" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=32 base=26 scaleW=128 scaleH=64 pages=1 packed=0 alphaChnl=1 redChnl=0 greenChnl=0 blueChnl=0
page id=0 file="gift_num_0.png"
chars count=14
char id=48   x=65    y=0     width=12    height=17    xoffset=1     yoffset=9     xadvance=13    page=0  chnl=15
         */
        private void BuildFontTextFormat()
        {
            string fontName = "Custom";
            string textureFilename = TextureAtlasFile;
            int fontSize = 50;

            StringBuilder sb = new StringBuilder();
            sb.Append("info")
                .Append(" face=\"").Append(fontName).Append("\"")
                .Append(" size=").Append(fontSize)
                .Append(" bold=").Append(1)
                .Append(" italic=").Append(0)
                .Append(" charset=\"\"")
                .Append(" unicode=").Append(1)
                .Append(" stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0")
                .Append("\n")
                ;
            sb.Append("common")
                .Append(" lineHeight=").Append(fontSize)
                .Append(" base=26 scaleW=128 scaleH=64 pages=1 packed=0 alphaChnl=1 redChnl=0 greenChnl=0 blueChnl=0")
                .Append("\n")
                ;

            sb.Append("page id=0")
                .Append(" file=\"").Append(textureFilename).Append("\"")
                .Append("\n")
                ;
            sb.Append("chars")
                .Append(" count=").Append(Rects.Length)
                .Append("\n")
                ;
            Debug.LogFormat("Rects:{0} font.characterInfo:{1}"
                , Rects.Length, Chars.Length);
            for (int i = 0; i < Rects.Length && i < Chars.Length; i++)
            {
                Rect rect = Rects[i];
                char chr = Chars[i];

                sb.Append("char")
                    .Append(" id=").Append((int)chr)
                    .Append(" x=").Append(rect.x)
                    // 从左下角计算的 x,y；fmt 中要从左上角计算 x,y；
                    .Append(" y=").Append(TextureAtlas.height - rect.y - rect.height)
                    .Append(" width=").Append(rect.width)
                    .Append(" height=").Append(rect.height)
                    .Append(" xoffset=").Append(0)
                    .Append(" yoffset=").Append(0)
                    .Append(" xadvance=").Append(rect.width)
                    .Append(" page=").Append(0)
                    .Append(" chnl=").Append(0)
                    .Append(" letter=\"").Append(chr).Append("\"")
                    .Append("\n")
                    ;
            }

            string content = sb.ToString();

            File.WriteAllText(FontFile, content);
            Debug.LogFormat("content:\n{0}", content);
        }

        void BuildTextureAtlas()
        {
            foreach (var t in Textures)
            {
                if (t == null)
                {
                    Debug.LogErrorFormat(">>> find null texture, skip");
                    continue;
                }

                var path = AssetDatabase.GetAssetPath(t);
                // Debug.LogFormat(">>>>:[{0}] [{1}]", path, t?.name ?? "NULL");
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;

                var isEdited = false;

                //ti.textureType = TextureImporterType.Sprite;

                if (ti.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    //有些图片压缩格式，没办法进行合并纹理
                    ti.textureCompression = TextureImporterCompression.Uncompressed;
                    isEdited = true;
                }

                if (false == ti.isReadable)
                {
                    //修改为可读写，这样才能进行后面的打包
                    ti.isReadable = true;
                    isEdited = true;
                }

                if (isEdited)
                {
                    ti.SaveAndReimport();
                }
            }

            var textureAtlas = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            Rects = textureAtlas.PackTextures(Textures, 0);

            //Debug.LogFormat("图集合并完成:");

            //将比例关系转换为像素值
            for (var i = 0; i < Rects.Length; i++)
            {
                var rect = Rects[i];
                rect.x = rect.x * textureAtlas.width;
                rect.width = rect.width * textureAtlas.width;
                rect.y = rect.y * textureAtlas.height;
                rect.height = rect.height * textureAtlas.height;
                Rects[i] = rect;

                // Debug.LogFormat("字符: {0}  区域: {1}", Chars[i], rect.ToString());
            }

            if (false == Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }

            //保存图
            var bytes = textureAtlas.EncodeToPNG();
            File.WriteAllBytes(TextureAtlasFile, bytes);
            AssetDatabase.Refresh();

            TextureAtlas = AssetDatabase.LoadAssetAtPath<Texture>(TextureAtlasFile);
        }
    }
}