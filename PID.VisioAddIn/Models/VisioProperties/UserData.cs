using AE.PID.Interfaces;

namespace AE.PID.Models;

public class UserData(string name, string value, string prompt = "") : ValueProp(name, "User", value), IUserData
{
    public string Prompt { get; set; } = prompt;
}