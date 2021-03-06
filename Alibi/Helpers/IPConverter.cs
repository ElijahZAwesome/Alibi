﻿using System;
using System.Net;
using Newtonsoft.Json;

namespace Alibi.Helpers
{
    public class IpConverter : JsonConverter<IPAddress>
    {
        public override void WriteJson(JsonWriter writer, IPAddress value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override IPAddress ReadJson(JsonReader reader, Type objectType, IPAddress existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            var s = (string) reader.Value;
            return IPAddress.Parse(s);
        }
    }
}