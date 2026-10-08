using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

public class CompactRgbConverter : JsonConverter<List<List<int>>>
{
    private static readonly JsonSerializerOptions Compact = new() { WriteIndented = false };

    public override List<List<int>> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<List<List<int>>>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, List<List<int>> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var rgb in value)
        {
            writer.WriteRawValue(JsonSerializer.Serialize(rgb, Compact));
        }
        writer.WriteEndArray();
    }
}
