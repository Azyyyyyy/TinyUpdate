using System.Text.Json;
using System.Text.Json.Serialization;
using SemVersion;

namespace TinyUpdate.Azure;

public class SemanticVersionConverter : JsonConverter<SemanticVersion>
{
    public override SemanticVersion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return SemanticVersion.Parse(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, SemanticVersion value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}