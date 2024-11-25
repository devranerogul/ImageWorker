using Newtonsoft.Json;
using SkiaSharp;
using ImageCore;

namespace ImageWorker;

public static class ImageGenerator
{
    // Create a funciton to generate image
    public static ImageGenResult GenerateImage(string taskFile, string taskId)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();

        Console.WriteLine($"Starting image generation at {DateTime.Now}");

        watch.Start();
// set options for deserializing
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        };
        ImageTask task = JsonConvert.DeserializeObject<ImageTask>(taskFile);
        
        task.TaskId = taskId;

        Console.WriteLine($"Read task data in {watch.ElapsedMilliseconds} ms");
        Console.WriteLine(task.BaseImage.Path);
        using (var baseImage = SKBitmap.Decode(task.BaseImage.Path))
        {
            Console.WriteLine($"Read base image in {watch.ElapsedMilliseconds} ms");

            using (var surface = SKSurface.Create(new SKImageInfo(baseImage.Width, baseImage.Height)))
            {
                var canvas = surface.Canvas;

                canvas.Clear(SKColors.Transparent);
                canvas.DrawBitmap(baseImage, 0, 0);

                foreach (var layer in task.Layers)
                {
                    if (layer is ImageLayer imageLayer)
                    {
                        using (var image = SKBitmap.Decode(imageLayer.ImageData.Path))
                        {
                            var destRect = new SKRect(imageLayer.ImageData.Position.X, imageLayer.ImageData.Position.Y,
                                imageLayer.ImageData.Position.X + imageLayer.ImageData.Size.Width,
                                imageLayer.ImageData.Position.Y + imageLayer.ImageData.Size.Height);
                            var paint = new SKPaint
                            {
                                Color = new SKColor(255, 255, 255, (byte)(255 * imageLayer.ImageData.Opacity))
                            };
                            canvas.DrawBitmap(image, destRect, paint);
                        }

                        Console.WriteLine($"Image layer replaced. Elapsed {watch.ElapsedMilliseconds} ms");
                    }
                    else if (layer is TextLayer textLayer)
                    {
                        var paint = new SKPaint
                        {
                            Color = SKColor.Parse(textLayer.TextData.Font.Color),
                            TextSize = textLayer.TextData.Font.Size,
                            Typeface = SKTypeface.FromFamilyName(textLayer.TextData.Font.Name, SKFontStyleWeight.Normal,
                                SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                        };

                        canvas.DrawText(textLayer.TextData.Content, textLayer.TextData.Position.X,
                            textLayer.TextData.Position.Y, paint);

                        Console.WriteLine($"Text layer replaced. Elapsed {watch.ElapsedMilliseconds} ms");
                    }
                }

                var renderingTimeStart = watch.ElapsedMilliseconds;
                Console.WriteLine($"Rendering final image. Started from {renderingTimeStart} ms");
                using (var image = surface.Snapshot())
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                using (var stream = File.OpenWrite($"{task.TaskId}.png"))
                {
                    data.SaveTo(stream);
                    Console.WriteLine(
                        $"Final image saved. Elapsed {watch.ElapsedMilliseconds - renderingTimeStart} ms");
                }
            }

            Console.WriteLine($"Task completed in {watch.ElapsedMilliseconds} ms");
            return new ImageGenResult
            {
                Success = true,
                TaskId = task.TaskId,
                ImagePath = $"{task.TaskId}.png",
                ElapsedMilliseconds = watch.ElapsedMilliseconds
            };
        }
    }
}

public class ImageGenResult
{
    public string TaskId { get; set; }
    public string ImagePath { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public string ErrorMessage { get; set; }
    public bool Success { get; set; }
}