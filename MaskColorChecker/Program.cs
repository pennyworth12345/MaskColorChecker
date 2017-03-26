using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace MaskColorChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("No arguments were passed to the program, hit enter to exit");
                Console.ReadLine();
                Environment.Exit(1);
            }
            string maskPath = args[0];

            int tileSize = -1;
            int overlap = -1;
            int tilesCount = -1;
            int colorsPerTile = -1;
            int outputTiles = 0;

            //skipping first index of args because that's the path to the mask
            for (int k = 1; k < args.Length; k++)
            {
                string argument = args[k];
                if (argument.Contains("tileSize"))
                {
                    tileSize = int.Parse(argument.Substring(argument.IndexOf('=') + 1));
                }
                else if (argument.Contains("overlap"))
                {
                    overlap = int.Parse(argument.Substring(argument.IndexOf('=') + 1));
                    overlap = overlap / 2; //overlap used in this program is actually half the TB overlap
                }
                else if (argument.Contains("tilesCount"))
                {
                    tilesCount = int.Parse(argument.Substring(argument.IndexOf('=') + 1));
                }
                else if (argument.Contains("colorsPerTile"))
                {
                    colorsPerTile = int.Parse(argument.Substring(argument.IndexOf('=') + 1));
                }
                else if (argument.Contains("outputTiles"))
                {
                    outputTiles = int.Parse(argument.Substring(argument.IndexOf('=') + 1));
                }
                else
                {
                    Console.WriteLine("Unknown argument: {0}", argument);
                }
            }

            //exiting if any of these variables weren't set correctly
            if (tileSize == -1 || overlap == -1 || tilesCount == -1 || colorsPerTile == -1)
            {
                Console.WriteLine("No arguments were passed to the program, hit enter to exit");
                Console.ReadLine();
                System.Environment.Exit(1);
            }

            int spacing = tileSize - (overlap * 2);
            List<string> errors = new List<string>();

            //creating copy of image for the purpose of drawing on the mask if the corresponding outputTiles value is selected
            Image image = Image.FromFile(maskPath);
            Graphics g = Graphics.FromImage(image);
            bool needToSaveMask = false;

            using (Bitmap maskImage = new Bitmap(maskPath))
            {
                for (int x = 0; x < tilesCount; x++)
                {
                    int tileWidth = tileSize;
                    int xPos = (spacing * x) - overlap;
                    //for the first row of tiles we're just ignoring the edges that TB makes
                    if (x == 0)
                    {
                        tileWidth = tileSize - overlap;
                        xPos = 0;
                    }
                    //for the last tile we make it smaller
                    else if (x == tilesCount - 1)
                    {
                        tileWidth = overlap * 3;
                    }
                    for (int y = 0; y < tilesCount; y++)
                    {
                        int tileHeight = tileSize;

                        int yPos = (spacing * y) - overlap;
                        //for the first row of tiles we're just ignoring the edges that TB makes
                        if (y == 0)
                        {
                            tileHeight = tileSize - overlap;
                            yPos = 0;
                        }
                        //for the last tile we make it smaller
                        else if (y == tilesCount - 1)
                        {
                            tileHeight = overlap * 3;
                        }

                        Rectangle tileRect = new Rectangle(xPos, yPos, tileWidth, tileHeight);
                        using (Bitmap generatedTile = maskImage.Clone(tileRect, maskImage.PixelFormat))
                        {
                            //processing pixels in parallel so we need a concurrent dictionary
                            ConcurrentDictionary<Color, byte> colors = new ConcurrentDictionary<Color, byte>();
                            unsafe
                            {
                                BitmapData bitmapData = generatedTile.LockBits(new Rectangle(0, 0, generatedTile.Width, generatedTile.Height), ImageLockMode.ReadWrite, generatedTile.PixelFormat);

                                int bytesPerPixel = Image.GetPixelFormatSize(generatedTile.PixelFormat) / 8;
                                int heightInPixels = bitmapData.Height;
                                int widthInBytes = bitmapData.Width * bytesPerPixel;
                                byte* PtrFirstPixel = (byte*)bitmapData.Scan0;

                                //process the current image's pixels in parallel
                                Parallel.For(0, heightInPixels, yCord =>
                                {
                                    byte* currentPixel = PtrFirstPixel + (yCord * bitmapData.Stride);
                                    for (int xCord = 0; xCord < widthInBytes; xCord = xCord + bytesPerPixel)
                                    {
                                        int blue = currentPixel[xCord];
                                        int green = currentPixel[xCord + 1];
                                        int red = currentPixel[xCord + 2];
                                        Color pixelColor = Color.FromArgb(red, green, blue);
                                        //byte value doesn't matter, we just want a lazy way to add to a container without having to check if it already contains an entry
                                        colors.TryAdd(pixelColor, 0);
                                    }
                                });
                            }
                            
                            //creating tile name string with padded 0s in front
                            string tileName = "m_" + x.ToString("D3") + "_" + y.ToString("D3") + "_lca.png";
                            if (colors.Count > colorsPerTile)
                            {
                                //only needed if the user wants tiles outputted
                                if (outputTiles == 1 || outputTiles == 3)
                                {
                                    //creating directory for bad tiles in the same directory as the original mask
                                    string outputPath = Path.Combine(Path.GetDirectoryName(maskPath), "Bad_Tiles");
                                    Directory.CreateDirectory(outputPath);
                                    generatedTile.Save(Path.Combine(outputPath, tileName));
                                }
                                if (outputTiles == 2 || outputTiles == 3)
                                {
                                    Color customColor = Color.FromArgb(255, Color.Gray);
                                    SolidBrush shadowBrush = new SolidBrush(customColor);
                                    if (tileRect.X > 5)
                                    {
                                        tileRect.X = tileRect.X - 5;
                                        tileRect.Width = tileRect.Width + 10;
                                    }
                                    if (tileRect.Y > 5)
                                    {
                                        tileRect.Y = tileRect.Y - 5;
                                        tileRect.Height = tileRect.Height + 10;
                                    }
                                    Pen myPen = new Pen(Color.FromArgb(255, 0, 0, 0), 10);
                                    g.DrawRectangle(myPen, tileRect);
                                    needToSaveMask = true;
                                }
                                errors.Add(string.Format("{0} had {1} colors", tileName, colors.Count));
                                List<Color> keyList = new List<Color>(colors.Keys);
                                for (int i = 0; i < keyList.Count; i++)
                                {
                                    Color thisColor = keyList[i];
                                    errors.Add(string.Format("\t{0}: ({1}, {2}, {3}, {4})", i, thisColor.R, thisColor.G, thisColor.B, thisColor.A));
                                }
                            }

                            Console.WriteLine("Processed: {0}", tileName);
                        }
                    }
                }
            }

            //only save the duplicate mask if they have the correct outputTiles mode and there were more colors than allowed in a tile
            if (needToSaveMask)
            {
                string imageNewPath = Path.Combine(Path.GetDirectoryName(maskPath), "Bad_Tiles");
                string outputDir = Path.Combine(Path.GetDirectoryName(maskPath), "Bad_Tiles");
                Directory.CreateDirectory(outputDir);
                image.Save(Path.Combine(outputDir, "debugMask.bmp"));
            }

            for (int i = 0; i < errors.Count; i++)
            {
                Console.WriteLine(errors[i]);
            }

            Console.WriteLine("\nFinished, hit enter to exit");
            Console.ReadLine();
        }
    }
}
