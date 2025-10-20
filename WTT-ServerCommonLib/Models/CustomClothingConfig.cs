using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace WTTServerCommonLib.Models;

public class CustomClothingConfig
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("suiteId")]
    public string? SuiteId { get; set; }

    [JsonPropertyName("outfitId")]
    public string? OutfitId { get; set; }

    [JsonPropertyName("topId")]
    public string? TopId { get; set; }

    [JsonPropertyName("handsId")]
    public string? HandsId { get; set; }

    [JsonPropertyName("bottomId")]
    public string? BottomId { get; set; }

    [JsonPropertyName("side")]
    public List<string>? Side { get; set; }

    [JsonPropertyName("locales")]
    public Dictionary<string, string>? Locales { get; set; }

    [JsonPropertyName("topBundlePath")]
    public string? TopBundlePath { get; set; }

    [JsonPropertyName("handsBundlePath")]
    public string? HandsBundlePath { get; set; }

    [JsonPropertyName("bottomBundlePath")]
    public string? BottomBundlePath { get; set; }

    [JsonPropertyName("traderId")]
    public string TraderId { get; set; } = string.Empty;

    [JsonPropertyName("loyaltyLevel")]
    public int LoyaltyLevel { get; set; }

    [JsonPropertyName("profileLevel")]
    public int ProfileLevel { get; set; }

    [JsonPropertyName("standing")]
    public double Standing { get; set; }

    [JsonPropertyName("currencyId")]
    public string CurrencyId { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public int Price { get; set; }

    [JsonPropertyName("watchPrefab")]
    public Prefab? WatchPrefab { get; set; }

    [JsonPropertyName("watchPosition")]
    public XYZ? WatchPosition { get; set; }

    [JsonPropertyName("watchRotation")]
    public XYZ? WatchRotation { get; set; }
}