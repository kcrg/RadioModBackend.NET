using RadioModBackend.NET.Models;
using System.Text.Json.Serialization;

namespace RadioModBackend.AOT.Models;

[JsonSerializable(typeof(DiscordMessage))]
[JsonSerializable(typeof(DiscordEmbed))]
[JsonSerializable(typeof(DiscordFooter))]
[JsonSerializable(typeof(DownloadResult))]
[JsonSerializable(typeof(SearchResult))]
[JsonSerializable(typeof(QueueRequest))]
[JsonSerializable(typeof(SearchRequest))]
[JsonSerializable(typeof(BaseResponse))]
[JsonSerializable(typeof(QueueResponse))]
[JsonSerializable(typeof(SearchResponse))]
[JsonSerializable(typeof(PlaylistResponse))]
public partial class JsonContext : JsonSerializerContext;
