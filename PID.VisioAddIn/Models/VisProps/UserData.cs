using AE.PID.Interfaces;

namespace PID.VisioAddIn.Props;

public class UserData(string name, string value, string prompt = "") : ValueProp(name, "User", value), IUserData
{
    public string Prompt { get; set; } = prompt;
}