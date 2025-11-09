namespace WaterCoolerCLI.Api
{
    public class PumpApi
    {
        public static Dictionary<int, int[]> dc240_280_360 = new Dictionary<int, int[]>
    {
        {
            0,
            new int[4] { 2600, 2600, 2600, 3150 }
        },
        {
            1,
            new int[4] { 2800, 2800, 2800, 2800 }
        },
        {
            4,
            new int[4] { 3150, 3150, 3150, 3150 }
        }
    };

        public static Dictionary<int, int[]> dcX240_280_360 = new Dictionary<int, int[]>
    {
        {
            0,
            new int[4] { 2600, 2600, 2600, 3150 }
        },
        {
            1,
            new int[4] { 2800, 2800, 2800, 2800 }
        },
        {
            4,
            new int[4] { 3150, 3150, 3150, 3150 }
        }
    };

        public static Dictionary<int, int[]> dcXII240_360_360I = new Dictionary<int, int[]>
    {
        {
            0,
            new int[4] { 2500, 2500, 2500, 3000 }
        },
        {
            1,
            new int[4] { 2800, 2800, 2800, 2800 }
        },
        {
            4,
            new int[4] { 3000, 3000, 3000, 3000 }
        }
    };
    }
}
