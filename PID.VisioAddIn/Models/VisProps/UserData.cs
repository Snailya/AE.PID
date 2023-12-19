using AE.PID.Interfaces;

namespace AE.PID.Models.VisProps;

public class UserData(string name, string value, string prompt = "") : ValueProp(name, "User", value), IUserData
{
    public string Prompt { get; set; } = prompt;
}