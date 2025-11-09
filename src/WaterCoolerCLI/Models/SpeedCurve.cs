namespace WaterCoolerCLI.Models;

public class SpeedCurve
{
    public SpeedType SpeedType;

    public TemperatureSpeed[] TemperatureSpeeds = new TemperatureSpeed[4];

    public SpeedCurve(SpeedType speedType = SpeedType.Fan)
    {
        SpeedType = speedType;
        TemperatureSpeeds[0] = new TemperatureSpeed();
        TemperatureSpeeds[1] = new TemperatureSpeed();
        TemperatureSpeeds[2] = new TemperatureSpeed();
        TemperatureSpeeds[3] = new TemperatureSpeed();
    }
}
