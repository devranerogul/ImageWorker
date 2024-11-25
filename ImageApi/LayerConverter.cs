using System.Text.Json;
using System.Text.Json.Serialization;
using ImageCore;

namespace ImageApi;

public class LayerConverter : JsonConverter<Layer>
{
    public override Layer? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions? options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            var jsonObject = doc.RootElement;
            var type = jsonObject.GetProperty("type").GetString();

            Layer? target;
            switch (type)
            {
                case "image": // Replace with actual type discriminator
                    target = JsonSerializer.Deserialize<ImageLayer>(jsonObject.GetRawText(), options);
                    break;
                case "text": // Replace with actual type discriminator
                    target = JsonSerializer.Deserialize<TextLayer>(jsonObject.GetRawText(), options);
                    break;
                default:
                    throw new NotSupportedException($"Type {type} is not supported.");
            }
            // Deserialize the remaining properties
            return target;
        }
    }

    public override void Write(Utf8JsonWriter writer, Layer value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}