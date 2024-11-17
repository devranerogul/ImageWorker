using Newtonsoft.Json;

namespace ImageWorker;
using System;
using System.Collections.Generic;

public class ImageTask
{
    public required string TaskId { get; set; }
    public required Metadata Metadata { get; set; }
    public required BaseImage BaseImage { get; set; }
    public required List<Layer> Layers { get; set; }
}

public class Metadata
{
    public string Author { get; set; }
    public DateTime CreatedDate { get; set; }
    public string Description { get; set; }
}

public class BaseImage
{
    public string Path { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}


[JsonConverter(typeof(LayerConverter))]
public abstract class Layer
{
    public string Type { get; set; }
}

public class ImageLayer : Layer
{
    public ImageData ImageData { get; set; }
}

public class TextLayer : Layer
{
    public TextData TextData { get; set; }
}

public class ImageData
{
    public string Path { get; set; }
    public Position Position { get; set; }
    public Size Size { get; set; }
    public float Opacity { get; set; }
}

public class TextData
{
    public string Content { get; set; }
    public Position Position { get; set; }
    public Font Font { get; set; }
}

public class Position
{
    public int X { get; set; }
    public int Y { get; set; }
}

public class Size
{
    public int Width { get; set; }
    public int Height { get; set; }
}

public class Font
{
    public string Name { get; set; }
    public int Size { get; set; }
    public string Style { get; set; }
    public string Color { get; set; }
}