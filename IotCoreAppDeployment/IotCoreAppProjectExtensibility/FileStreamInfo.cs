using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotCoreAppProjectExtensibility
{
    public class FileStreamInfo
    {
        public String AppxRelativePath { get; set; }
        public Stream Stream { get; set; }

        public void Apply(String rootFolder)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(rootFolder + @"\" + AppxRelativePath));
            using (var fileStream = File.Create(rootFolder + @"\" + AppxRelativePath))
            {
                Stream.Seek(0, SeekOrigin.Begin);
                Stream.CopyTo(fileStream);
            }
        }
    }
}
