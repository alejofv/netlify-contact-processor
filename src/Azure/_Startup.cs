using System;
using MediatR;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(AlejoF.Contacts.Azure.Startup))]

namespace AlejoF.Contacts.Azure
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddTableStorage();
            
            // Function dependencies
            builder.Services.AddMediatR(typeof(Startup));
        }
    }
}
