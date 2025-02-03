using System;

namespace AE.PID.Client.Core;

public class NetworkNotValidException()
    : Exception("无法连接到网络");