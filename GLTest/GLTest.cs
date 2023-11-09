using Newtonsoft.Json;
using Orbis;
using OrbisGL;
using OrbisGL.Controls;
using OrbisGL.Debug;
using OrbisGL.GL;
using OrbisGL.GL2D;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Permissions;
using System.Windows.Forms;
using static OrbisGL.GL2D.Coordinates2D;
using Button = OrbisGL.Controls.Button;
using Panel = OrbisGL.Controls.Panel;
using TextBox = OrbisGL.Controls.TextBox;
using Orbis.Game;
using System.Drawing;
using BCnEncoder.Encoder;
using ICSharpCode.SharpZipLib.Zip;
using System.Diagnostics;
using Orbis.Scene;
using System.Windows.Markup;
using Orbis.Interfaces;
using System.Runtime.InteropServices;

namespace GLTest
{
    public partial class GLTest : Form
    {
#if !ORBIS
        GLControl GLControl;
        public GLTest()
        {
            InitializeComponent();

            GLControl = new GLControl(1280, 720);
            Controls.Add(GLControl);
        }
#endif
const string Vertex =
@"
attribute vec3 Position;

void main(void) {
    gl_Position = vec4(Position, 1.0);
}
";
const string VertexMat4 =
@"
attribute vec3 Position;
uniform mat4 Transformation;

void main(void) {
    gl_Position = Transformation * vec4(Position, 1.0);
}
";
const string VertexOffset =
@"
attribute vec3 Position;
uniform vec2 Offset;

void main(void) {
    gl_Position = vec4(Position + Offset, 1.0);
}
";
        const string UVVertex =
@"
attribute vec3 Position;
attribute vec2 uv;
 
varying lowp vec2 UV;

void main(void) {
    gl_Position = vec4(Position, 1.0);
    UV = uv;
}
";
const string FragmentColor =
@"
uniform lowp vec4 Color;
 
void main(void) {
    gl_FragColor = Color;
}
";
const string FragmentTexture =
@"
varying lowp vec2 UV;

uniform sampler2D Texture;
 
void main(void) {
    gl_FragColor = texture2D(Texture, UV);
}
";


        Random Rand = new Random();

        private void button1_Click(object sender, EventArgs e)
        {
#if !ORBIS
            TiledTexture2D tiledTexture = new TiledTexture2D();

            Texture Texture00 = new Texture(true);
            Texture Texture01 = new Texture(true);
            Texture Texture10 = new Texture(true);
            Texture Texture11 = new Texture(true);

            Texture00.SetImage(File.ReadAllBytes("t1.png"), PixelFormat.RGBA, false);
            Texture10.SetImage(File.ReadAllBytes("t2.png"), PixelFormat.RGBA, false);
            Texture01.SetImage(File.ReadAllBytes("t3.png"), PixelFormat.RGBA, false);
            Texture11.SetImage(File.ReadAllBytes("t4.png"), PixelFormat.RGBA, false);

            tiledTexture.SetTexture(Texture00, null, Texture01, null);


            //tiledTexture.Texture = Texture00;
            //tiledTexture.RefreshVertex();

            GLControl.GLApplication.AddObject(tiledTexture);
#endif
        }

        private void button2_Click(object sender, EventArgs e)
        {
#if !ORBIS
            var Objs = GLControl.GLApplication.Objects.ToArray();
            GLControl.GLApplication.RemoveObjects();

            foreach (var Obj in Objs)
            {
                Obj.Dispose();
            }
#endif
        }

        private unsafe void button3_Click(object sender, EventArgs e)
        {
            var FS = new FolderBrowserDialog();
            FS.SelectedPath = AppDomain.CurrentDomain.BaseDirectory;
            if (FS.ShowDialog() != DialogResult.OK)
                return;

            var Images = Directory.GetFiles(FS.SelectedPath, "*.png", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(FS.SelectedPath, ".bmp", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(FS.SelectedPath, ".jpg", SearchOption.AllDirectories)).ToArray();

            for (int i = 0; i < Images.Length; i++)
            {
                var ImgFile = Images[i];
                using (var ImgOri = new Bitmap(ImgFile))
                {
                    if (ImgOri.Width * ImgOri.Height <= Constants.ORBIS_MAX_TEXTURE_SIZE * Constants.ORBIS_MAX_TEXTURE_SIZE)
                        continue;

                    using (var Img = Trim(ImgOri))
                    {
                        int TileWidth = Img.Width / 2;
                        int TileHeight = Img.Height / 2;

                        TileWidth += 4 - (TileWidth % 4);
                        TileHeight += 4 - (TileHeight % 4);

                        if (TileWidth * TileHeight > Constants.ORBIS_MAX_TEXTURE_SIZE * Constants.ORBIS_MAX_TEXTURE_SIZE)
                            throw new Exception("Dear god, a fucking giant texture");


                        var OutFile = Path.Combine(Path.GetDirectoryName(ImgFile), Path.GetFileNameWithoutExtension(ImgFile) + "_t{0}" + Path.GetExtension(ImgFile));

                        using (Bitmap UL = new Bitmap(TileWidth, TileHeight))
                        using (Bitmap UR = new Bitmap(Img.Width - TileWidth, TileHeight))
                        using (Bitmap BL = new Bitmap(TileWidth, Img.Height - TileHeight))
                        using (Bitmap BR = new Bitmap(Img.Width - TileWidth, Img.Height - TileHeight))
                        {
                            using (Graphics g = Graphics.FromImage(UL))
                            {
                                g.DrawImage(Img, new System.Drawing.Rectangle(0, 0, UL.Width, UL.Height), 0, 0, UL.Width, UL.Height, GraphicsUnit.Pixel);
                                g.Flush();
                            }

                            using (Graphics g = Graphics.FromImage(UR))
                            {
                                g.DrawImage(Img, new System.Drawing.Rectangle(0, 0, UR.Width, UR.Height), TileWidth, 0, UR.Width, UR.Height, GraphicsUnit.Pixel);
                                g.Flush();
                            }

                            using (Graphics g = Graphics.FromImage(BL))
                            {
                                g.DrawImage(Img, new System.Drawing.Rectangle(0, 0, BL.Width, BL.Height), 0, TileHeight, BL.Width, BL.Height, GraphicsUnit.Pixel);
                                g.Flush();
                            }

                            using (Graphics g = Graphics.FromImage(BR))
                            {
                                g.DrawImage(Img, new System.Drawing.Rectangle(0, 0, BR.Width, BR.Height), TileWidth, TileHeight, BR.Width, BR.Height, GraphicsUnit.Pixel);
                                g.Flush();
                            }

                            UL.Save(string.Format(OutFile, "UL"));
                            UR.Save(string.Format(OutFile, "UR"));
                            BL.Save(string.Format(OutFile, "BL"));
                            BR.Save(string.Format(OutFile, "BR"));
                        }
                    }
                }
            }

            for (int i = 0; i < Images.Length; i++)
            {
                var ImgFile = Images[i];
                var OutImg = Path.Combine(Path.GetDirectoryName(ImgFile), Path.GetFileNameWithoutExtension(ImgFile) + ".dds");
                using (var Img = new Bitmap(ImgFile))
                {
                    var MustResizeX = (Img.Width % 4) != 0;
                    var MustResizeY = (Img.Height % 4) != 0;

                    Bitmap Output = Img;

                    if (MustResizeX || MustResizeY)
                    {
                        int NewWidth = Img.Width;
                        int NewHeight = Img.Height;

                        if (MustResizeX)
                            NewWidth += 4 - (Img.Width % 4);

                        if (MustResizeY)
                            NewHeight += 4 - (Img.Height % 4);

                        Output = new Bitmap(NewWidth, NewHeight);

                        using (Graphics g = Graphics.FromImage(Output))
                        {
                            g.DrawImage(Img, 0, 0, Img.Width, Img.Height);
                        }
                    }

                    bool Alpha = false;
                    switch (Img.PixelFormat)
                    {
                        case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                            Alpha = HasAlpha(Img);
                            break;
                    }

                    var Format = Alpha ? BCnEncoder.Shared.CompressionFormat.BC3 : BCnEncoder.Shared.CompressionFormat.BC1;

                    BcEncoder Encoder = new BcEncoder(Format);
                    Encoder.Options.multiThreaded = true;
                    Encoder.OutputOptions.quality = CompressionQuality.BestQuality;

                    var Locker = Output.LockBits(new System.Drawing.Rectangle(0, 0, Output.Width, Output.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    try
                    {
                        int SizeInBytes = Locker.Stride * Locker.Height;
                        int SizeInDW = SizeInBytes / sizeof(uint);

                        uint* pPixels = (uint*)Locker.Scan0.ToPointer();
                        for (int x = 0; x < SizeInDW; x++)
                        {
                            const uint PickA = 0xFF000000;
                            const uint PickR = 0x00FF0000;
                            const uint PickG = 0x0000FF00;
                            const uint PickB = 0x000000FF;

                            const int MoveOneByte = 8;
                            const int MoveTwoByte = 8 * 2;
                            const int MoveThreeByte = 8 * 3;

                            var ARGB = pPixels[x];
                            var ABGR = (ARGB & PickA) | ((ARGB & PickR) >> MoveTwoByte) | (ARGB & PickG) | ((ARGB & PickB) << MoveTwoByte);
                            pPixels[x] = ABGR;
                        }


                        Span<byte> bytes = new Span<byte>(Locker.Scan0.ToPointer(), SizeInBytes);
                        var DDS = Encoder.EncodeToDds(bytes.ToArray(), Output.Width, Output.Height);
                        using (var Stream = File.Create(OutImg))
                            DDS.Write(Stream);
                    }
                    finally
                    {
                        Output.UnlockBits(Locker);
                    }

                    Output?.Dispose();

                    Text = "Generating Textures " + i + "/" + Images.Length;
                    System.Windows.Forms.Application.DoEvents();
                }
            }

            MessageBox.Show("Finished!");
        }
        public Bitmap Trim(Bitmap bmp)
        {
            int w = bmp.Width;
            int h = bmp.Height;

            Func<int, bool> EmptyRow = row =>
            {
                for (int i = 0; i < w; ++i)
                    if (bmp.GetPixel(i, row).A != 0)
                        return false;
                return true;
            };

            Func<int, bool> EmptyColumn = col =>
            {
                for (int i = 0; i < h; ++i)
                    if (bmp.GetPixel(col, i).A != 0)
                        return false;
                return true;
            };


            int bottommost = 0;
            for (int row = h - 1; row >= 0; --row)
            {
                if (EmptyRow(row))
                    bottommost = row;
                else break;
            }
            int rightmost = 0;

            for (int col = w - 1; col >= 0; --col)
            {
                if (EmptyColumn(col))
                    rightmost = col;
                else
                    break;
            }

            if (rightmost == 0) rightmost = w; // As reached left
            if (bottommost == 0) bottommost = h; // As reached top.



            int topmost = 0;
            int leftmost = 0;

            int croppedWidth = rightmost - leftmost;
            int croppedHeight = bottommost - topmost;

            if (croppedWidth == 0) // No border on left or right
            {
                leftmost = 0;
                croppedWidth = w;
            }

            if (croppedHeight == 0) // No border on top or bottom
            {
                topmost = 0;
                croppedHeight = h;
            }

            try
            {
                var target = new Bitmap(croppedWidth, croppedHeight);
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(bmp,
                      new RectangleF(0, 0, croppedWidth, croppedHeight),
                      new RectangleF(leftmost, topmost, croppedWidth, croppedHeight),
                      GraphicsUnit.Pixel);
                }
                return target;
            }
            catch (Exception ex)
            {
                throw new Exception(
                  string.Format("Values are topmost={0} btm={1} left={2} right={3} croppedWidth={4} croppedHeight={5}", topmost, bottommost, leftmost, rightmost, croppedWidth, croppedHeight),
                  ex);
            }
        }

        private unsafe bool HasAlpha(Bitmap img)
        {
            var lck = img.LockBits(new System.Drawing.Rectangle(0, 0, img.Width, img.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            bool HasTransparecy = false;

            try
            {                
                int SizeInBytes = lck.Stride * lck.Height;


                uint* pPixels = (uint*)lck.Scan0.ToPointer();
                for (int i = 0; i < SizeInBytes; i++)
                {
                    var ARGB = pPixels[i];
                    if ((ARGB & 0xFF000000) != 255)
                    {
                        HasTransparecy = true;
                        break;
                    }
                }
            }
            finally
            {
                img.UnlockBits(lck);
            }
            return HasTransparecy;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var AssetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets\\");

            var ImgExts = new string[] { ".png", ".jpg", ".bmp" };
            var SndExts = new string[] { ".mp3", ".acc" };

            using (var Stream = File.Create("Assets.zip"))
            {
                var Archive = ZipFile.Create(Stream);

                Archive.BeginUpdate();

                var Files = Directory.GetFiles(AssetsDir, "*.*", SearchOption.AllDirectories);

                foreach (var File in Files)
                {
                    if (File.EndsWith("assets.zip", StringComparison.CurrentCultureIgnoreCase))
                        continue;

                    if (ImgExts.Contains(Path.GetExtension(File).ToLowerInvariant()))
                    {
                        if (Files.Contains(Path.ChangeExtension(File, ".dds")))
                            continue;
                    }

                    var SplitFile = Path.Combine(Path.GetDirectoryName(File), Path.GetFileNameWithoutExtension(File) + "{0}" + Path.GetExtension(File));

                    if (Files.Contains(string.Format(SplitFile, "_tUL")))
                        continue;

                    if (SndExts.Contains(Path.GetExtension(File).ToLowerInvariant()))
                    {
                        if (Files.Contains(Path.ChangeExtension(File, ".wav")))
                            continue;
                    }

                    var AudioConv = Path.Combine(Path.GetDirectoryName(File), Path.GetFileNameWithoutExtension(File) + "_48khz.wav");
                    if (Files.Contains(AudioConv))
                        continue;

                    StaticDiskDataSource DataSource = new StaticDiskDataSource(File);

                    Archive.Add(DataSource, File.Substring(AssetsDir.Length), CompressionMethod.Deflated);
                }

                Archive.CommitUpdate();
            }

            MessageBox.Show("Assets Archive Created", "GLTest");
        }

        private void button5_Click(object sender, EventArgs e)
        {
#if !ORBIS
            var Rect = new Rectangle2D(1280, 720, true, ResLoader.GetResource("ThemeFrag"));
            //Rect.Offset = new Vector3(XOffset * Rand.Next(0, GLControl.Width), YOffset * Rand.Next(GLControl.Height), 1);

            Rect.Program.SetUniform("Resolution", 1280f, 720f);

            GLControl.GLApplication.AddObject(Rect);
#endif
        }

        private void button6_Click(object sender, EventArgs e)
        {
#if !ORBIS
            var Rect = new RoundedRectangle2D(250, 100, Rand.Next(0, 2) == 1);
            Rect.Position = new Vector2(Rand.Next(0, GLControl.Width - 250), Rand.Next(GLControl.Height - 100));
            Rect.Color = new RGBColor((byte)Rand.Next(0, 255), (byte)Rand.Next(0, 255), (byte)Rand.Next(0, 255));
            //Rect.Transparecy = (byte)Rand.Next(0, 255);

            Rect.Rotate = Rand.Next(0, 3600) / 10f;

            Rect.RoundLevel = Rand.Next(0, 100) / 100f;
            GLControl.GLApplication.AddObject(Rect);
#endif
        }

        private void button7_Click(object sender, EventArgs e)
        {
#if !ORBIS
            var Rect = new Elipse2D(200, 200, Rand.Next(0, 2) == 1);
            Rect.Position = new Vector2(Rand.Next(0, GLControl.Width - 200), Rand.Next(GLControl.Height - 200));
            Rect.Color = new RGBColor((byte)Rand.Next(0, 255), (byte)Rand.Next(0, 255), (byte)Rand.Next(0, 255));
            Rect.Opacity = (byte)Rand.Next(0, 255);

            GLControl.GLApplication.AddObject(Rect);
#endif
        }

        private void button8_Click(object sender, EventArgs e)
        {
#if !ORBIS
            var Rect = new PartialElipse2D(200, 200, Rand.Next(0, 2) == 1);
            Rect.Position = new Vector2(Rand.Next(0, GLControl.Width - 200), Rand.Next(GLControl.Height - 200));
            Rect.Color = new RGBColor((byte)Rand.Next(0, 255), (byte)Rand.Next(0, 255), (byte)Rand.Next(0, 255));
            Rect.StartAngle = Rand.Next(-314, 314) / 100;
            Rect.EndAngle = Rand.Next(-314, 314) / 100; 
            Rect.Thickness = Rand.Next(0, 100) / 100;


            //Rect.Transparency = (byte)Rand.Next(0, 255);

            GLControl.GLApplication.AddObject(Rect);
#endif
        }

        private void button9_Click(object sender, EventArgs e)
        {
#if !ORBIS
            var Font = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "*.ttf").First();
            var Text = new Text2D(24, Font);
            Text.SetText("Hello World");
            Text.Position = new Vector2(Rand.Next(0, GLControl.Width - 200), Rand.Next(GLControl.Height - 200));
            Text.Color = new RGBColor((byte)Rand.Next(0, 255), (byte)Rand.Next(0, 255), (byte)Rand.Next(0, 255));
            GLControl.GLApplication.AddObject(Text);
#endif
        }

        private void button10_Click(object sender, EventArgs e)
        {
#if !ORBIS
            var Rect = new Elipse2D(200, 200, true);
            Rect.Position = new Vector2(Rand.Next(0, GLControl.Width - 200), Rand.Next(GLControl.Height - 200));
            Rect.Color = new RGBColor((byte)Rand.Next(0, 255), (byte)Rand.Next(0, 255), (byte)Rand.Next(0, 255));


            var Rect2 = new Rectangle2D(20, 300, true);
            Rect2.Position = new Vector2(0, 0);
            Rect2.Color = new RGBColor((byte)Rand.Next(0, 255), (byte)Rand.Next(0, 255), (byte)Rand.Next(0, 255));

            Rect.AddChild(Rect2);

            Rect.SetVisibleRectangle(0, 0, 200, 100);

            GLControl.GLApplication.AddObject(Rect);
#endif
        }

        private void button11_Click(object sender, EventArgs e)
        {
#if !ORBIS
            var BG = new OrbisGL.Controls.RowView(GLControl.Size.Width, GLControl.Size.Height);


            var Button = new OrbisGL.Controls.Button(50, 25, 18);
            Button.Text = "Hello World";
            Button.Primary = Rand.Next(0, 2) == 1;

            Button.Position = new Vector2(Rand.Next(0, GLControl.Width - 200), Rand.Next(GLControl.Height - 200));

            BG.AddChild(Button);

            GLControl.GLApplication.AddObject(BG);
#endif
        }

        private void button12_Click(object sender, EventArgs e)
        {
#if !ORBIS
            var BG = new OrbisGL.Controls.RowView(GLControl.Size.Width, GLControl.Size.Height);

            var RT2D = new RichText2D(28, RGBColor.Black, null);

            RT2D.SetRichText("<align=horizontal>Hello <backcolor=f00><color=0f0>World</color></backcolor></align>\n<align=vertical>Testing<size=48> simple </size><font=Inkfree.ttf>rich</font> <color=F00>text</color></align>");

            foreach (var Glyph in RT2D.GlyphsSpace)
            {
                var Box = new Rectangle2D(Glyph.Area, false);
                Box.Color = RGBColor.Red;
                RT2D.AddChild(Box);
            }

            GLControl.MouseUp += (This, Args) =>
            {
                foreach (var Glyph in RT2D.GlyphsSpace)
                {
                    if (Glyph.Area.IsInBounds(Args.X, Args.Y))
                    {
                        MessageBox.Show($"{Glyph.Char} Clicked, Index: {Glyph.Index}, RichIndex: {RT2D.RichText[Glyph.Index]}");
                    }
                }
            };

            GLControl.GLApplication.AddObject(BG);
            GLControl.GLApplication.AddObject(RT2D);
#endif
        }

        private void button13_Click(object sender, EventArgs e)
        {
            var FS = new FolderBrowserDialog();
            FS.SelectedPath = AppDomain.CurrentDomain.BaseDirectory;
            if (FS.ShowDialog() != DialogResult.OK)
                return;

            var Audios = Directory.GetFiles(FS.SelectedPath, "*.ogg", SearchOption.AllDirectories);

            foreach (var Audio in Audios)
            {
                var OutFile = Path.Combine(Path.GetDirectoryName(Audio), Path.GetFileNameWithoutExtension(Audio) + "_48khz.wav");
                if (Audio.Contains("_48khz") || File.Exists(OutFile))
                    continue;

                ConvertOgg(Audio, OutFile);
            }

            MessageBox.Show("Converted!", "GLTEST");
        }

        private void ConvertOgg(string inputFile, string outputFile)
        {
            string arguments = $"-i \"{inputFile}\" -ar 48000 \"{outputFile}\"";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = new Process
            {
                StartInfo = startInfo
            };

            process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
            process.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();
        }

        private void button14_Click(object sender, EventArgs e)
        {

#if !ORBIS

            var BG = new OrbisGL.Controls.RowView(GLControl.Size.Width, GLControl.Size.Height);
            BG.BackgroundColor = RGBColor.ReallyLightBlue;

            var TB = new OrbisGL.Controls.TextBox(200, 18);
            TB.Position = new Vector2(10, 10);

            TB.Text = "Debug texbox test";
            TB.SelectionStart = Rand.Next(0, TB.Text.Length);

            BG.AddChild(TB);

            GLControl.GLApplication.AddObject(BG);
            GLControl.Focus();
#endif
        }

        private void button15_Click(object sender, EventArgs e)
        {
#if !ORBIS
            var BG = new OrbisGL.Controls.RowView(GLControl.Size.Width, GLControl.Size.Height);
            BG.BackgroundColor = RGBColor.Red;

            var BG2 = new OrbisGL.Controls.RowView(GLControl.Size.Width / 2, GLControl.Size.Height / 2);
            BG2.BackgroundColor = RGBColor.ReallyLightBlue;
            BG2.Position = new Vector2(0, 10);
            BG2.AllowScroll = true;

            var BG3 = new OrbisGL.Controls.RowView(GLControl.Size.Width - 100, GLControl.Size.Height / 3); BG2.BackgroundColor = RGBColor.ReallyLightBlue;
            BG3.BackgroundColor = RGBColor.Yellowish;
            BG3.Position = new Vector2(50, 100);
            BG3.AllowScroll = true;


            var TB = new OrbisGL.Controls.TextBox(200, 18);
            TB.Position = new Vector2(10, 10);

            var TB2 = new OrbisGL.Controls.TextBox(200, 18);
            TB2.Position = new Vector2(10, 400);
            TB2.Text = "Debug texbox test 2";

            TB.Text = "Debug texbox test";

            BG2.AddChild(TB);
            BG2.AddChild(TB2);

            BG3.AddChild(BG2);

            BG.AddChild(BG3);

            GLControl.GLApplication.AddObject(BG);
            GLControl.Focus();
#endif
        }

        private void button16_Click(object sender, EventArgs e)
        {
#if !ORBIS
            var Rect = new Triangle2D(200, 200);
            Rect.Position = new Vector2(Rand.Next(0, GLControl.Width - 200), Rand.Next(GLControl.Height - 200));
            Rect.Color = new RGBColor((byte)Rand.Next(0, 255), (byte)Rand.Next(0, 255), (byte)Rand.Next(0, 255));
            Rect.RoundLevel = (float)Rand.NextDouble() * 0.3f;

            switch (Rand.Next(0, 8))
            {
                case 0:
                    Rect.Rotation = Triangle2D.Degrees.Degree0;
                    break;
                case 1:
                    Rect.Rotation = Triangle2D.Degrees.Degree45; 
                    break;
                case 2:
                    Rect.Rotation = Triangle2D.Degrees.Degree90;
                    break;
                case 3:
                    Rect.Rotation = Triangle2D.Degrees.Degree135;
                    break;
                case 4:
                    Rect.Rotation = Triangle2D.Degrees.Degree180;
                    break;
                case 5:
                    Rect.Rotation = Triangle2D.Degrees.Degree225;
                    break;
                case 6:
                    Rect.Rotation = Triangle2D.Degrees.Degree270;
                    break;
                case 7:
                    Rect.Rotation = Triangle2D.Degrees.Degree315;
                    break;
            }


            //Rect.Transparency = (byte)Rand.Next(10, 255);

            GLControl.GLApplication.AddObject(Rect);
#endif
        }

        private void button17_Click(object sender, EventArgs e)
        {
#if !ORBIS
            var BG = new OrbisGL.Controls.RowView(GLControl.Size.Width, GLControl.Size.Height);
            BG.BackgroundColor = RGBColor.White;


            var CK = new Checkbox(20);
            CK.Text = "Hello World";
            CK.Position = new Vector2(10, 10);

            BG.AddChild(CK);

            GLControl.GLApplication.AddObject(BG);
#endif

        }

        private void button18_Click(object sender, EventArgs e)
        {

#if !ORBIS
            var BG = new OrbisGL.Controls.RowView(GLControl.Size.Width, GLControl.Size.Height);
            BG.BackgroundColor = RGBColor.White;

            var List = new RowView(300, 500);
            List.Position = new Vector2(100, 100);
            List.BackgroundColor = RGBColor.ReallyLightBlue;

            for (int i = 0; i < 15; i++)
            {
                var RB = new Radiobutton(20);

                RB.Text = "Hello World " + i;
                List.AddChild(RB);
            }

            BG.AddChild(List);

            GLControl.GLApplication.AddObject(BG);
#endif
        }

        private void button19_Click(object sender, EventArgs e)
        {
#if !ORBIS
            var BG = new Panel(GLControl.Size.Width, GLControl.Size.Height);
            BG.BackgroundColor = RGBColor.White;

            var Inspect = new Inspector(600, 600);
            Inspect.Position = new Vector2(400, 0);

            var List = new RowView(300, 600);
            List.Position = new Vector2(0, 0);
            List.BackgroundColor = RGBColor.ReallyLightBlue;

            var RB = new Radiobutton(28);
            RB.Text = "Hello World";
            RB.OnMouseClick += (s, a) => { Inspect.Target = (OrbisGL.Controls.Control)s; };

            var BTN = new Button(200, 20, 28);
            BTN.Text = "Hello World";
            BTN.OnMouseClick += (s, a) => { Inspect.Target = (OrbisGL.Controls.Control)s; };

            var TB = new TextBox(200, 28);
            TB.Text = "Hello World";
            TB.OnMouseClick += (s, a) => { Inspect.Target = (OrbisGL.Controls.Control)s; };

            List.AddChild(RB);
            List.AddChild(BTN);
            List.AddChild(TB);

            BG.AddChild(List);
            BG.AddChild(Inspect);

            GLControl.GLApplication.AddObject(BG);
#endif
        }

        private void button20_Click(object sender, EventArgs e)
        {
#if !ORBIS
            var BG = new Panel(GLControl.Size.Width, GLControl.Size.Height);
            BG.BackgroundColor = RGBColor.ReallyLightBlue;

            var DropButton = new DropDownButton(200);

            DropButton.Items.Add("Option A");
            DropButton.Items.Add("Option B");
            DropButton.Items.Add("Option C");
            DropButton.Items.Add("Option D");
            DropButton.Items.Add("Option E");
            DropButton.Items.Add("Option F");
            DropButton.Items.Add("Option G");
            DropButton.Items.Add("Option H");
            DropButton.Items.Add("Option I");
            DropButton.Items.Add("Option J");
            DropButton.Items.Add("Option K");
            DropButton.Items.Add("Option L");
            DropButton.Items.Add("Option M");
            DropButton.Items.Add("Option N");
            DropButton.Items.Add("Option O");

            DropButton.Text = "Select an Option";

            DropButton.Position = new Vector2(100, 20);

            BG.AddChild(DropButton);

            GLControl.GLApplication.AddObject(BG);
#endif

        }

        private void button21_Click(object sender, EventArgs e)
        {
#if !ORBIS
            var BG = new Panel(GLControl.Size.Width, GLControl.Size.Height);
            BG.BackgroundColor = RGBColor.White;

            var Cat = ResLoader.GetResourceData(Assembly.GetExecutingAssembly(), "cat_sprite");

            Texture Tex = new Texture(true);
            Tex.SetImage(Cat, PixelFormat.RGBA, true);

            Texture2D TexObj = new Texture2D();
            TexObj.Texture = Tex;

            Sprite2D Spriter = new Sprite2D(TexObj);
            Spriter.Width = 221;
            Spriter.Height = 154;

            Spriter.ComputeAllFrames(8);
            Spriter.FrameDelay = 50;

            GLControl.GLApplication.AddObject(BG);
            GLControl.GLApplication.AddObject(Spriter);
#endif
        }

        private void button22_Click(object sender, EventArgs e)
        {
#if !ORBIS
            var Zoom = Rand.Next(10, 100) / 100f;
            foreach (var Obj in GLControl.GLApplication.Objects)
            {
                if (Obj is GLObject2D Obj2D)
                {
                    Obj2D.SetZoom(Zoom);
                }
            }
#endif
        }

#if !ORBIS
        ILoadable SM;
#endif

        CharacterAnim Anim;
        TiledSpriteAtlas2D Sprite;
        private void button23_Click(object sender, EventArgs e)
        {
#if !ORBIS
            
            FormBorderStyle = FormBorderStyle.None;
            Size = new Size(1920, 1080);

            WindowState = FormWindowState.Maximized;

            GLControl.SetSize(Size.Width, Size.Height);

            panel1.Visible = false;
            
            SM = new IntroScene();

            SM.Load(i =>
            {
                if (SM.Loaded)
                    GLControl.GLApplication.AddObject(SM);
            });
            
            /*
            var XML = Util.GetXML(Character.GirlfriendAssets);

            Sprite = new TiledSpriteAtlas2D(XML, Util.CopyFileToMemory, true);
            Anim = new CharacterAnim("gf");

            Switch = true;
            Sprite.SetActiveAnimation(Anim.DANCING);

            Offset = Anim.GetAnimOffset(Sprite.CurrentSprite);

            Sprite.Position = new Vector2(100, 100);
            Sprite.FrameDelay = 20;
            Sprite.Position -= Offset;

            GLControl.GLApplication.AddObject(Sprite);
            */
#endif
        }
        private void button24_Click(object sender, EventArgs e)
        {
#if DEBUG
            /*
            var Param = new OrbisSaveDataDialogParam();

            Param.baseParam.size = 48;
            Param.baseParam.magic = 1;
            Param.baseParam.reserved = new byte[24];
            Param.dispType = OrbisSaveDataDialogType.ORBIS_SAVE_DATA_DIALOG_TYPE_SAVE;
            Param.mode = OrbisSaveDataDialogMode.ORBIS_SAVE_DATA_DIALOG_MODE_SYSTEM_MSG;
            Param.size = 0x98;

            Param.items = new OrbisSaveDataDialogItems()
            {
                userId = 0x11223344,
                newItem = new OrbisSaveDataDialogNewItem()
                {
                    title = IntPtr.Zero
                }
            };

            using (var stream = new MemoryStream())
            {
                var Addr = Param.CopyTo(stream).ToArray();
                File.WriteAllBytes("rst.dbg", stream.ToArray());
            }
            */
#endif
        }
    }
}