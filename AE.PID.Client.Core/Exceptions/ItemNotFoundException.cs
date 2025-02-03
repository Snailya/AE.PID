using System;

namespace AE.PID.Client.Core;

public class ItemNotFoundException(string key) : Exception($"未找到满足条件{key}的对象。")
{
}