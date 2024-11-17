using Newtonsoft.Json;

namespace ImageWorker;
public class LayerConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(Layer).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var jsonObject = Newtonsoft.Json.Linq.JObject.Load(reader);
        var type = jsonObject["type"]?.ToString();

        Layer layer = type switch
        {
            "image" => new ImageLayer(),
            "text" => new TextLayer(),
            _ => throw new JsonSerializationException($"Unknown layer type: {type}")
        };

        serializer.Populate(jsonObject.CreateReader(), layer);
        return layer;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }
}