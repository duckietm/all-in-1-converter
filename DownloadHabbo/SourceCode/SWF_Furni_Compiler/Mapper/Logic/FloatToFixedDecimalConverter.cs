using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class FloatToFixedDecimalConverter : JsonConverter<float>
{
    public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetSingle();
    }

    public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
    {
        if (value % 1 == 0)
        {
            writer.WriteRawValue(((int)value).ToString());
        }
        else if (Math.Abs(value) < 0.0001f)
        {
            writer.WriteRawValue(value.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteRawValue(value.ToString("G", System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
