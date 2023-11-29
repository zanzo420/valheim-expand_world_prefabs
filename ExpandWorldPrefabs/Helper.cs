

using System.Linq;
using ExpandWorldData;

namespace ExpandWorld.Prefab;

public class Helper2
{

  public static Range<string> Range(string arg)
  {
    var range = arg.Split('-').ToList();
    if (range.Count > 1 && range[0] == "")
    {
      range[0] = "-" + range[1];
      range.RemoveAt(1);
    }
    if (range.Count > 2 && range[1] == "")
    {
      range[1] = "-" + range[2];
      range.RemoveAt(2);
    }
    if (range.Count == 1) return new(range[0]);
    else return new(range[0], range[1]);

  }
  public static Range<int> IntRange(string arg)
  {
    var range = Range(arg);
    return new(int.Parse(range.Min), int.Parse(range.Max));
  }
}