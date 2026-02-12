using System.Text.Json;
using System.Text.Json.Serialization;
using ReisingerIntelliApp_V4.Models;

namespace ReisingerIntelliApp_V4.Helpers;

/// <summary>
/// Source-generated JSON serializer context for trimming/AOT safety in Release builds.
/// IMPORTANT: Only register API models here that have explicit [JsonPropertyName] attributes
/// and do NOT use [ObservableProperty]. The MVVM Toolkit source generator and the JSON
/// source generator run in parallel and cannot see each other's output — so
/// [ObservableProperty]-generated properties are invisible to the JSON source generator.
/// Building/Floor/Device models use reflection-based JSON + TrimMode=partial instead.
/// </summary>
[JsonSerializable(typeof(IntellidriveVersionResponse))]
[JsonSerializable(typeof(IntellidriveVersionContent))]
[JsonSerializable(typeof(IntellidriveApiEnvelope))]
[JsonSerializable(typeof(IntellidriveParametersResponse))]
[JsonSerializable(typeof(IntellidriveParameterValue))]
[JsonSerializable(typeof(IntellidriveMinValuesResponse))]
[JsonSerializable(typeof(IntellidriveMaxValuesResponse))]
[JsonSerializable(typeof(IntellidriveSetParametersRequest))]
[JsonSerializable(typeof(IntellidriveParameterSetValue))]
[JsonSerializable(typeof(IntellidriveSetParametersResponse))]
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true)]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
