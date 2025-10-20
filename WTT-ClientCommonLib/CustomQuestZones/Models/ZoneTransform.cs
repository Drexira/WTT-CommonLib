namespace WTTClientCommonLib.CustomQuestZones.Models;


public class ZoneTransform
{
    public string X { get; set; }
    public string Y { get; set; }
    public string Z { get; set; }
    public string W { get; set; }

    public ZoneTransform(string x, string y, string z, string w = "0")
    {
        this.X = x; 
        this.Y = y;
        this.Z = z;
        this.W = w;
    }
}
