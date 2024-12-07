using Newtonsoft.Json;

namespace ImageCore;
using System;
using System.Collections.Generic;

public class ImageTask
{
    public required string TaskId { get; set; }
    public required Metadata Metadata { get; set; }
    public required BaseImage BaseImage { get; set; }
    public List<ImageLayer> ImageLayers { get; set; } = new();
    public List<TextLayer> TextLayers { get; set; } = new();
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

public class ImageLayer
{
    public string Id { get; set; }
    
    public string Name { get; set; }
    public ImageData ImageData { get; set; }
    public Position Position { get; set; }
    public Size Size { get; set; }
    public float Opacity { get; set; }
}

public class TextLayer
{
    public string Id { get; set; }
    
    public string Name { get; set; }
    public TextData TextData { get; set; }
    public Position Position { get; set; }
    public Size Size { get; set; }
    public float Opacity { get; set; }
}

public class ImageData
{
    public string Path { get; set; }
}

public class TextData
{
    public string Content { get; set; }
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
    public string Family { get; set; }
    public int Size { get; set; }
    public string Color { get; set; }
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
}