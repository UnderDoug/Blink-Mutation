using XRL;
using XRL.Rules;
using XRL.World;

namespace XRL.World.ZoneBuilders
{
    public class AddObjectWithUniqueItemBuilder
    {
        public string Object;

        public string X;

        public string Y;

        public bool BuildZone(Zone Z)
        {
            for (int i = 0; i < 500; i++)
            {
                int x = X?.RollCached() ?? Stat.Random(0, Z.Width - 1);
                int y = Y?.RollCached() ?? Stat.Random(0, Z.Height - 1);
                if (Z.GetCell(x, y).IsEmpty())
                {
                    GameObject cachedObject = The.ZoneManager.PullCachedObject(ID: Object, DeepCopy: false);
                    Z.GetCell(x, y).AddObject(cachedObject);
                    return true;
                }
            }
            return true;
        }
    }
}
