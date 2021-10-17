using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ImageOrganizer
{
    public class ImageInfo
    {
        public string Hash { get; }
        public string Date { get; }
        public string Album { get; }
        public string Extension { get; }
        public FileInfo File { get; }
        public string UniqueKey { get; }
        public ImageInfo(FileInfo file)
        {
            var dir = new DirectoryInfo(file.DirectoryName);
            Album = dir.Name;
            Extension = file.Extension.ToLower();
            Hash = CalculateMD5(file.FullName);
            Date = GetFileDate(file);
            File = file;
            UniqueKey = Hash + File.Length;
        }
        string GetFileDate(FileInfo file)
        {
            var hyphenatedDate = new Regex("[0-9]{4}-[0-9]{2}-[0-9]{2}");
            var val = GetDateTaken(file);
            if (!val.Equals(""))
            {
                val = val.Replace(':', '-');
                val = hyphenatedDate.Match(val).Value;
                return val;
            }
            if (hyphenatedDate.IsMatch(file.Name))
            {
                return hyphenatedDate.Match(file.Name).Value;
            }
            var numericDate8 = new Regex(".*(20[0-9]{6}).*");
            val = numericDate8.Match(file.Name).Groups[1].Value;
            if (!val.Equals(""))
            {
                return val.Substring(0, 4) + "-" + val.Substring(4, 2) + "-" + val.Substring(6, 2);
            }
            if (hyphenatedDate.IsMatch(Album))
            {
                return hyphenatedDate.Match(Album).Value;
            }
            var year = new Regex("[0-9]{4}");
            if (year.IsMatch(Album))
            {
                return year.Match(Album).Value;
            }
            return "";
        }

        // https://stackoverflow.com/questions/2280948/reading-data-metadata-from-jpeg-xmp-or-exif-in-c-sharp
        string GetDateTaken(FileInfo f)
        {
            try
            {
                IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(f.FullName);
                var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                var dateTime = subIfdDirectory?.GetStringValue(ExifSubIfdDirectory.TagDateTimeOriginal);
                if (dateTime.HasValue && dateTime.Value.Bytes != null)
                {
                    return dateTime.Value.ToString();
                }
                dateTime = subIfdDirectory?.GetStringValue(ExifSubIfdDirectory.TagDateTimeDigitized);
                if (dateTime.HasValue && dateTime.Value.Bytes != null)
                {
                    return dateTime.Value.ToString();
                }
                dateTime = subIfdDirectory?.GetStringValue(ExifSubIfdDirectory.TagDateTime);
                if (dateTime.HasValue && dateTime.Value.Bytes != null)
                {
                    return dateTime.Value.ToString();
                }
                return "";
            } 
            catch (Exception e) 
            {
                return e.Message;
            }   
        }

        // https://stackoverflow.com/questions/10520048/calculate-md5-checksum-for-a-file
        string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = System.IO.File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}
