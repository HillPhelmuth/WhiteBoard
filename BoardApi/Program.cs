using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()/*.ConfigureServices(ConfigureServicesDelegate)*/
    .Build();

host.Run();

//static void ConfigureServicesDelegate(IServiceCollection serviceCollection)
//{
//    serviceCollection.AddH();
//}