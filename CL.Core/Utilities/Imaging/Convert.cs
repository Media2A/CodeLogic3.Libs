using System;
using System.IO;
using ImageMagick;

namespace CL.Core.Utilities
{
    /// <summary>
    /// Imaging conversion utilities backed by ImageMagick.
    /// </summary>
    public partial class CLU_Imaging
    {
        /// <summary>
        /// Loads an image file and returns its raw bytes as a Base64 string.
        /// </summary>
        /// <param name="imagePath">Path to the image file.</param>
        /// <returns>Base64-encoded image data.</returns>
        public static string ImageToBase64(string imagePath)
        {
            using (var image = new MagickImage(imagePath))
            {
                using (var memoryStream = new MemoryStream())
                {
                    image.Write(memoryStream);
                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
        }

        /// <summary>
        /// Resizes an image and writes the result to a new file.
        /// </summary>
        /// <param name="inputPath">Path to the source image.</param>
        /// <param name="outputPath">Path to write the resized image.</param>
        /// <param name="width">Target width in pixels.</param>
        /// <param name="height">Target height in pixels.</param>
        /// <param name="maintainAspectRatio">Whether to preserve aspect ratio.</param>
        public static void ResizeImage(string inputPath, string outputPath, int width, int height, bool maintainAspectRatio = true)
        {
            using (var image = new MagickImage(inputPath))
            {
                uint newWidth = (uint)width;
                uint newHeight = (uint)height;

                if (maintainAspectRatio)
                {
                    image.Resize(newWidth, newHeight);
                }
                else
                {
                    image.Scale(newWidth, newHeight);
                }

                image.Write(outputPath);
            }
        }

        /// <summary>
        /// Converts an image file to a different format.
        /// </summary>
        /// <param name="inputPath">Path to the source image.</param>
        /// <param name="outputPath">Path to write the converted image.</param>
        /// <param name="format">Target image format.</param>
        public static void ConvertImageFormat(string inputPath, string outputPath, MagickFormat format)
        {
            using (var image = new MagickImage(inputPath))
            {
                image.Format = format;
                image.Write(outputPath);
            }
        }

        /// <summary>
        /// Creates a square-bounded thumbnail while preserving aspect ratio.
        /// </summary>
        /// <param name="inputPath">Path to the source image.</param>
        /// <param name="outputPath">Path to write the thumbnail image.</param>
        /// <param name="thumbnailSize">Maximum size of the thumbnail in pixels.</param>
        public static void CreateThumbnail(string inputPath, string outputPath, int thumbnailSize = 100)
        {
            using (var image = new MagickImage(inputPath))
            {
                uint newWidth, newHeight;
                if (image.Width > image.Height)
                {
                    newWidth = (uint)thumbnailSize;
                    newHeight = (uint)(image.Height * thumbnailSize / (float)image.Width);
                }
                else
                {
                    newHeight = (uint)thumbnailSize;
                    newWidth = (uint)(image.Width * thumbnailSize / (float)image.Height);
                }

                image.Resize(newWidth, newHeight);
                image.Write(outputPath);
            }
        }

        /// <summary>
        /// Gets the dimensions of an image.
        /// </summary>
        /// <param name="imagePath">Path to the image file.</param>
        /// <returns>Image width and height in pixels.</returns>
        public static (int width, int height) GetImageDimensions(string imagePath)
        {
            using (var image = new MagickImage(imagePath))
            {
                // Convert uint to int explicitly
                int width = (int)image.Width;
                int height = (int)image.Height;

                return (width, height);
            }
        }
    }
}
