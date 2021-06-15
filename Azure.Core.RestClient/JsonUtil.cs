using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Azure.Core
{
    internal static class JsonUtil
    {
        /// <summary>
        /// Reads a token, failing if it can't on the resulting token is of the wrong type,
        /// </summary>
        private static void MustRead(ref Utf8JsonReader r, JsonTokenType jsonTokenType)
        {
            if (!r.Read() || r.TokenType != jsonTokenType)
            {
                throw new InvalidOperationException("unexpected JSON token");
            }
        }

        /// <summary>
        /// Read an array of values from a <see cref="Utf8JsonReader"/>. The entire array is read and
        /// a <see cref="BinaryData"/> is formed over the JSON text for each item in the array.
        /// </summary>
        private static List<ReadOnlyMemory<byte>> ReadArrayItems(ref Utf8JsonReader r, BinaryData jsonDocument)
        {
            ReadOnlyMemory<byte> content = jsonDocument.ToMemory();
            List<ReadOnlyMemory<byte>> values = new List<ReadOnlyMemory<byte>>();

            MustRead(ref r, JsonTokenType.StartArray);
            while (r.Read())
            {
                if (r.TokenType == JsonTokenType.EndArray)
                {
                    return values;
                }
                else if (r.TokenType == JsonTokenType.StartObject || r.TokenType == JsonTokenType.StartArray)
                {
                    int startIdx = (int)r.TokenStartIndex;
                    r.Skip();
                    int endIdx = (int)r.TokenStartIndex;
                    int length = endIdx - startIdx + 1;

                    values.Add(content.Slice(startIdx, length));
                }
                else if (r.TokenType == JsonTokenType.String)
                {
                    values.Add(content.Slice((int)r.TokenStartIndex, r.ValueSpan.Length + 2 /* open and closing quotes are not captured in the value span */));
                }
                else
                {
                    values.Add(content.Slice((int)r.TokenStartIndex, r.ValueSpan.Length));
                }
            }

            throw new InvalidOperationException("invalid JSON");
        }

        /// <summary>
        /// Reads the status value of an operation body.
        /// </summary>
        public static string GetOperationStatusFromJson(BinaryData jsonObject, string propertyName = "status")
        {
            Utf8JsonReader r = new Utf8JsonReader(jsonObject.ToMemory().Span);
            MustRead(ref r, JsonTokenType.StartObject);
            while(r.Read())
            {
                switch (r.TokenType)
                {
                    case JsonTokenType.EndObject:
                        throw new KeyNotFoundException($"Could not find property '{propertyName}' in object");
                    case JsonTokenType.PropertyName:
                        if (r.ValueTextEquals(propertyName))
                        {
                            MustRead(ref r, JsonTokenType.String);
                            return r.GetString();
                        }
                        else
                        {
                            r.Skip();
                            continue;
                        }
                    default:
                        throw new InvalidOperationException("unexpected JSON token");
                }
            }

            throw new InvalidOperationException("invalid JSON");
        }

        /// <summary>
        /// Reads the items and next link from a response for a pageable operation. The values returned are BinaryDatas formed over the underlying content.
        /// </summary>
        public static (List<ReadOnlyMemory<byte>> items, string nextLink) GetItemsAndNextLinkFromJson(BinaryData content, string itemPropertyName = "value", string nextLinkPropertyName = "nextLink")
        {
            string nextLink = null;
            List<ReadOnlyMemory<byte>> items = null;

            Utf8JsonReader r = new Utf8JsonReader(content.ToMemory().Span);
            MustRead(ref r, JsonTokenType.StartObject);
            while (r.Read())
            {
                switch (r.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        if (r.ValueTextEquals(nextLinkPropertyName))
                        {
                            r.Read();
                            nextLink = r.GetString();
                            continue;
                        }
                        else if (r.ValueTextEquals(itemPropertyName))
                        {
                            items = ReadArrayItems(ref r, content);
                        }
                        else
                        {
                            r.Skip();
                        }
                        break;
                    case JsonTokenType.EndObject:
                        break;

                    default:
                        throw new Exception("unknown type in object");
                }
            }

            return (items, nextLink);
        }
    }
}

