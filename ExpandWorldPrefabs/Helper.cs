using System;

namespace ExpandWorld.Prefab;

public class Helper2
{

  public static string ReplaceParameters(string str, string prefab, string parameter)
  {
    var split = parameter.Split(' ');
    return str.Replace("<prefab>", prefab)
      .Replace("<par0>", split.Length > 0 ? split[0] : "")
      .Replace("<par1>", split.Length > 1 ? split[1] : "")
      .Replace("<par2>", split.Length > 2 ? split[2] : "")
      .Replace("<par3>", split.Length > 3 ? split[3] : "")
      .Replace("<par4>", split.Length > 4 ? split[4] : "")
      .Replace("<par>", parameter);
  }
  public static bool CheckWild(string wild, string str)
  {
    if (wild == "*")
      return true;
    if (wild[0] == '*' && wild[wild.Length - 1] == '*')
      return str.ToLowerInvariant().Contains(wild.Substring(1, wild.Length - 2).ToLowerInvariant());
    if (wild[0] == '*')
      return str.EndsWith(wild.Substring(1), StringComparison.OrdinalIgnoreCase);
    else if (wild[wild.Length - 1] == '*')
      return str.StartsWith(wild.Substring(0, wild.Length - 1), StringComparison.OrdinalIgnoreCase);
    else
      return str.Equals(wild, StringComparison.OrdinalIgnoreCase);
  }
}