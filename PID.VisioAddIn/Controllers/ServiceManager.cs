using System.Net.Http;
using AE.PID.Controllers.Services;

namespace AE.PID.Controllers;

public class ServiceManager(HttpClient httpClient)
{
    public MaterialsService MaterialsService { get; private set; } = new(httpClient);
}