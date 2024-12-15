using Google.Cloud.Firestore;
using ImageCore;

namespace ImageApi
{
    public class FirestoreImageLayerConverter : IFirestoreConverter<ImageLayer>
    {
        public ImageLayer FromFirestore(object value)
        {
            if (value is Dictionary<string, object> dict)
            {
                var imageLayer = new ImageLayer
                {
                    Position = new Position
                    {
                        X = dict.TryGetValue("x", out var x) ? Convert.ToInt32(x) : 0,
                        Y = dict.TryGetValue("y", out var y) ? Convert.ToInt32(y) : 0
                    },
                    Size = new Size
                    {
                        Width = dict.TryGetValue("width", out var width) ? Convert.ToInt32(width) : 0,
                        Height = dict.TryGetValue("height", out var height) ? Convert.ToInt32(height) : 0
                    },
                    Opacity = dict.TryGetValue("opacity", out var opacity) ? Convert.ToSingle(opacity) : 1.0f,
                    Id = dict.TryGetValue("id", out var id) ? id as string : null,
                    Name = dict.TryGetValue("name", out var name) ? name as string : null,
                    ZIndex = dict.TryGetValue("zIndex", out var zIndex) ? Convert.ToInt32(zIndex) : 0
                };

                if (dict.TryGetValue("imageData", out var imageDataObj) && imageDataObj is Dictionary<string, object> imageData)
                {
                    imageLayer.ImageData = new ImageData
                    {
                        Path = imageData.TryGetValue("path", out var path) ? path as string : null
                    };
                }

                return imageLayer;
            }

            throw new ArgumentException("Value must be a dictionary");
        }

        public object ToFirestore(ImageLayer layer)
        {
            var result = new Dictionary<string, object>
            {
                { "x", layer.Position.X },
                { "y", layer.Position.Y },
                { "width", layer.Size.Width },
                { "height", layer.Size.Height },
                { "opacity", layer.Opacity },
                { "id", layer.Id},
                { "name", layer.Name},
                { "zIndex", layer.ZIndex}
            };

            if (layer.ImageData != null)
            {
                result["imageData"] = new Dictionary<string, object>
                {
                    { "path", layer.ImageData.Path }
                };
            }

            return result;
        }
    }

    public class FirestoreTextLayerConverter : IFirestoreConverter<TextLayer>
    {
        public TextLayer FromFirestore(object value)
        {
            if (value is Dictionary<string, object> dict)
            {
                var textLayer = new TextLayer
                {
                    Position = new Position
                    {
                        X = dict.TryGetValue("x", out var x) ? Convert.ToInt32(x) : 0,
                        Y = dict.TryGetValue("y", out var y) ? Convert.ToInt32(y) : 0
                    },
                    Size = new Size
                    {
                        Width = dict.TryGetValue("width", out var width) ? Convert.ToInt32(width) : 0,
                        Height = dict.TryGetValue("height", out var height) ? Convert.ToInt32(height) : 0
                    },
                    Opacity = dict.TryGetValue("opacity", out var opacity) ? Convert.ToSingle(opacity) : 1.0f,
                    Id = dict.TryGetValue("id", out var id) ? id as string : null,
                    Name = dict.TryGetValue("name", out var name) ? name as string : null,
                    ZIndex = dict.TryGetValue("zIndex", out var zIndex) ? Convert.ToInt32(zIndex) : 0
                };

                if (dict.TryGetValue("textData", out var textDataObj) && textDataObj is Dictionary<string, object> textData)
                {
                    textLayer.TextData = new TextData
                    {
                        Content = textData.TryGetValue("content", out var content) ? content as string : null,
                        Font = textData.TryGetValue("font", out var fontObj) && fontObj is Dictionary<string, object> font
                            ? new Font
                            {
                                Family = font.TryGetValue("family", out var family) ? family as string : null,
                                Size = font.TryGetValue("size", out var fontSize) ? Convert.ToInt32(fontSize) : 12,
                                Color = font.TryGetValue("color", out var color) ? color as string : "#000000",
                                IsBold = font.TryGetValue("isBold", out var isBold) && Convert.ToBoolean(isBold),
                                IsItalic = font.TryGetValue("isItalic", out var isItalic) && Convert.ToBoolean(isItalic)
                            }
                            : null
                    };
                }

                return textLayer;
            }

            throw new ArgumentException("Value must be a dictionary");
        }

        public object ToFirestore(TextLayer layer)
        {
            var result = new Dictionary<string, object>
            {
                { "id", layer.Id },
                { "name", layer.Name },
                { "x", layer.Position.X },
                { "y", layer.Position.Y },
                { "width", layer.Size.Width },
                { "height", layer.Size.Height },
                { "opacity", layer.Opacity },
                { "zIndex", layer.ZIndex }
            };

            if (layer.TextData != null)
            {
                var textData = new Dictionary<string, object>
                {
                    { "content", layer.TextData.Content }
                };

                if (layer.TextData.Font != null)
                {
                    textData["font"] = new Dictionary<string, object>
                    {
                        { "family", layer.TextData.Font.Family },
                        { "size", layer.TextData.Font.Size },
                        { "color", layer.TextData.Font.Color },
                        { "isBold", layer.TextData.Font.IsBold },
                        { "isItalic", layer.TextData.Font.IsItalic }
                    };
                }

                result["textData"] = textData;
            }

            return result;
        }
    }
    
    // Add Convertors for TemplateUpdateRequest
    public class FirestoreTemplateUpdateRequestConverter : IFirestoreConverter<TemplateUpdateRequest>
    {
        public TemplateUpdateRequest FromFirestore(object value)
        {
            if (value is Dictionary<string, object> dict)
            {
                var templateUpdateRequest = new TemplateUpdateRequest
                {
                    TemplateName =dict.TryGetValue("templateName", out var templateName) ? templateName as string : null, 
                    Description = dict.TryGetValue("description", out var description) ? description as string : null,
                    CanvasHeight = dict.TryGetValue("canvasHeight", out var canvasHeight) ? Convert.ToInt32(canvasHeight) : 0,
                    CanvasWidth = dict.TryGetValue("canvasWidth", out var canvasWidth) ? Convert.ToInt32(canvasWidth) : 0,
                    Radius = dict.TryGetValue("radius", out var radius) ? Convert.ToInt32(radius) : 0,
                    ImageLayers = dict.TryGetValue("imageLayers", out var imageLayersObj) && imageLayersObj is List<object> imageLayers
                        ? imageLayers.Select(l => l as Dictionary<string, object>).Select(l => new FirestoreImageLayerConverter().FromFirestore(l)).ToList()
                        : new List<ImageLayer>(),
                    TextLayers = dict.TryGetValue("textLayers", out var textLayersObj) && textLayersObj is List<object> textLayers
                        ? textLayers.Select(l => l as Dictionary<string, object>).Select(l => new FirestoreTextLayerConverter().FromFirestore(l)).ToList()
                        : new List<TextLayer>()
                };

                return templateUpdateRequest;
            }

            throw new ArgumentException("Value must be a dictionary");
        }

        public object ToFirestore(TemplateUpdateRequest templateUpdateRequest)
        {
            var result = new Dictionary<string, object>
            {
                { "templateName", templateUpdateRequest.TemplateName },
                { "description", templateUpdateRequest.Description },
                { "canvasWidth", templateUpdateRequest.CanvasWidth },
                { "canvasHeight", templateUpdateRequest.CanvasHeight },
                { "radius", templateUpdateRequest.Radius },
                { "imageLayers", templateUpdateRequest.ImageLayers.Select(l => new FirestoreImageLayerConverter().ToFirestore(l)).ToList() },
                { "textLayers", templateUpdateRequest.TextLayers.Select(l => new FirestoreTextLayerConverter().ToFirestore(l)).ToList() }
            };

            return result;
        }
    }
    
}