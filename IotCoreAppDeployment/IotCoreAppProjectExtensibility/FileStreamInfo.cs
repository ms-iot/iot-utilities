// Copyright (c) Microsoft. All rights reserved.

using System.IO;

namespace Microsoft.Iot.IotCoreAppProjectExtensibility
{
    public class FileStreamInfo
    {
        public string AppxRelativePath { get; set; }
        public Stream Stream { get; set; }

        public bool Apply(string rootFolder)
        {
            if (Stream == null)
            {
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(rootFolder + @"\" + AppxRelativePath));
            using (var fileStream = File.Create(rootFolder + @"\" + AppxRelativePath))
            {
                Stream.Seek(0, SeekOrigin.Begin);
                Stream.CopyTo(fileStream);
                return true;
            }
        }
    }
}
