using System;
using AE.PID.Core.Models;

namespace AE.PID.Client.Core;

public class FunctionTypeNotMatchException(FunctionType functionType1, FunctionType functionType2)
    : Exception($"由于{functionType1}与{functionType2}不一致，操作无法被完成。")
{
}

public class MaterialTypeNotMatchException(string materialType1, string materialType2)
    : Exception($"由于{materialType1}与{materialType2}不一致，操作无法被完成。")
{
}