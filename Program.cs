
using chmodPermissions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
		   .ConfigureAppConfiguration((hostingContext, config) =>
		   {
			   config.SetBasePath("E:\\HomeAssigmnets\\chmodPermissions")
				   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				   .AddEnvironmentVariables()
				   .AddCommandLine(args);
		   })
		   .ConfigureServices((hostContext, services) =>
		   {
			   services.Configure<FilePermissionsSettings>(hostContext.Configuration.GetSection("FilePermissions"));
			   services.AddScoped<IRegister, ChmodPermissionManager>();
		   });

var host = builder.Build();

var manager = host.Services.GetRequiredService<IRegister>();

int handle1 = manager.Register("secret.txt", true, false);
int handle2 = manager.Register("secret.txt", false, true);
manager.Unregister(handle1);
manager.Unregister(handle2);
int handle3 = manager.Register("secret.txt", true, true);
int handle4 = manager.Register("secret.txt", true, false);
manager.Unregister(handle3);
manager.Unregister(handle4);
int handle5 = manager.Register("xxxxxx.txt", true, true);

host.Run(); 