using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace Houmao.Models
{
    public class Attachment
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public AttachmentType Type { get; set; }
        public string Base64Data { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        
        public static Attachment FromFile(string filePath)
        {
            var attachment = new Attachment
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                Type = GetAttachmentType(filePath),
                MimeType = GetMimeType(filePath)
            };
            
            // 读取文件并转换为 Base64
            var bytes = File.ReadAllBytes(filePath);
            attachment.Base64Data = Convert.ToBase64String(bytes);
            
            return attachment;
        }
        
        public static Attachment? FromBitmapSource(BitmapSource bitmap)
        {
            try
            {
                // 转换为 PNG 格式
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                
                using var stream = new MemoryStream();
                encoder.Save(stream);
                var bytes = stream.ToArray();
                
                // 生成唯一文件名
                var fileName = $"clipboard_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                var tempPath = Path.Combine(Path.GetTempPath(), "houmao", fileName);
                
                // 确保目录存在
                var dir = Path.GetDirectoryName(tempPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                
                // 保存临时文件
                File.WriteAllBytes(tempPath, bytes);
                
                return new Attachment
                {
                    FileName = fileName,
                    FilePath = tempPath,
                    Type = AttachmentType.Image,
                    MimeType = "image/png",
                    Base64Data = Convert.ToBase64String(bytes)
                };
            }
            catch
            {
                return null;
            }
        }
        
        private static AttachmentType GetAttachmentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            if (IsImageExtension(extension))
                return AttachmentType.Image;
            
            if (IsAudioExtension(extension))
                return AttachmentType.Audio;
            
            return AttachmentType.File;
        }
        
        private static bool IsImageExtension(string extension)
        {
            return extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" => true,
                _ => false
            };
        }
        
        private static bool IsAudioExtension(string extension)
        {
            return extension switch
            {
                ".mp3" or ".wav" or ".ogg" or ".flac" or ".m4a" or ".aac" => true,
                _ => false
            };
        }
        
        private static string GetMimeType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".ogg" => "audio/ogg",
                ".flac" => "audio/flac",
                ".m4a" => "audio/mp4",
                ".aac" => "audio/aac",
                _ => "application/octet-stream"
            };
        }
        
        public string ToDataUri()
        {
            return $"data:{MimeType};base64,{Base64Data}";
        }
    }

    public enum AttachmentType
    {
        Image,
        Audio,
        File
    }
}