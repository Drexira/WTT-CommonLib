namespace WTTClientCommonLib.Models;

public class CustomQuestZone
{
    public string ZoneId { get; set; }
    public string ZoneName { get; set; }
    public string ZoneLocation { get; set; }
    public string ZoneType { get; set; }
    public string FlareType { get; set; }
    public ZoneTransform Position { get; set; }
    public ZoneTransform Rotation { get; set; }
    public ZoneTransform Scale { get; set; }
}