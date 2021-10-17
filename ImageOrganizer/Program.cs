using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace ImageOrganizer
{
    class Program
    {
        Dictionary<string, List<ImageInfo>> UniqueImages = new Dictionary<string, List<ImageInfo>>();
        readonly string[] Extensions = new string[] { ".jpg", ".png", ".gif", ".avi", ".3gp", ".3g2", ".mov", ".m4v", ".mp4", ".jpeg", ".heic" };
        readonly string[] IgnoreExtensions = new string[] { ".json" };
        DirectoryInfo Path { get; }
        List<ImageInfo> Images = new List<ImageInfo>();

        Program(DirectoryInfo path)
        {
            Path = path;
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: ImageOrganizer.exe <path_to_photos>");
                return;
            }
            DirectoryInfo path = new DirectoryInfo(args.FirstOrDefault());
            if (!path.Exists)
            {
                Console.WriteLine($"Path not found: {path.FullName}");
                return;
            }
            else
            {
                var program = new Program(path);

                Console.WriteLine();
                Console.WriteLine("-------------- COLLECTING --------------");
                Console.WriteLine();

                program.WalkFolder();

                Console.WriteLine();
                Console.WriteLine("-------------- DEDUPING --------------");
                Console.WriteLine();

                program.Dedupe();

                Console.WriteLine();
                Console.WriteLine("-------------- COPYING --------------");
                Console.WriteLine();

                program.Organize();
            }
        }

        void WalkFolder()
        {
            WalkFolder(Path);
        }

        void WalkFolder(DirectoryInfo path)
        {
            Console.WriteLine(path.FullName);
            foreach (var file in path.GetFiles())
            {
                var ii = new ImageInfo(file);
                if (Extensions.Contains(ii.Extension))
                {
                    Images.Add(ii);
                    Console.WriteLine("\t" + file.Name);
                }
                else if (!IgnoreExtensions.Contains(ii.Extension))
                {
                    Console.WriteLine($"WARNING: Unhandled extension: {ii.Extension}");
                }
            }
            foreach (var dir in path.GetDirectories())
            {
                WalkFolder(dir);
            }
        }

        void Dedupe()
        {
            foreach (var img in Images)
            {   
                var key = img.UniqueKey;
                if (!UniqueImages.ContainsKey(key))
                {
                    UniqueImages.Add(key, new List<ImageInfo>() { img });
                }
                else
                {
                    UniqueImages[key].Add(img);
                }
            }
            Console.WriteLine();
            Console.WriteLine("Initial count: " + Images.Count);
            Images = new List<ImageInfo>();
            foreach (string key in UniqueImages.Keys)
            {
                var imgs = UniqueImages[key];
                imgs.Sort(CompareImageDates);
                var firstFullDate = imgs.Where(i => i.Date.Length > 4).FirstOrDefault();
                if (firstFullDate is null)
                {
                    Images.Add(imgs.First());
                }
                else
                {
                    Images.Add(firstFullDate);
                }
            }
            Console.WriteLine("Final count: " + Images.Count);
            Console.WriteLine();
        }
        private static int CompareImageDates(ImageInfo x, ImageInfo y)
        {
            return x.Date.CompareTo(y.Date);
        }

        void Organize()
        {
            var dir = new DirectoryInfo(Path.Parent.FullName + "\\Photos");
            var Undated = new DirectoryInfo(dir.FullName + "\\Undated");
            if (!dir.Exists)
            {
                dir.Create();
            }
            if (!Undated.Exists)
            {
                Undated.Create();
            }
            foreach (var img in Images)
            {
                var destName = img.File.Name;
                if (!img.Album.StartsWith("Photos from "))
                {
                    destName = img.Album + "_" + destName;
                }
                if (img.Date.Equals(""))
                {
                    img.File.CopyTo(Undated.FullName + "\\" + destName);
                    continue;
                }
                string year = img.Date.Substring(0, 4);
                var subdir = new DirectoryInfo(dir.FullName + "\\" + year);
                if (!subdir.Exists)
                {
                    subdir.Create();
                }
                if (!img.Date.Equals(year))
                {
                    string month = img.Date.Substring(5, 2);
                    subdir = new DirectoryInfo(subdir.FullName + "\\" + month);
                    if (!subdir.Exists)
                    {
                        subdir.Create();
                    }
                }
                var dest = subdir.FullName + "\\" + destName;
                Console.WriteLine(dest);
                img.File.CopyTo(dest);
            }
        }
    }
}
