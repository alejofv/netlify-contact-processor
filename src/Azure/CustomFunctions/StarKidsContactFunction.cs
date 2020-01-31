using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;

namespace AlejoF.Contacts
{
    public class StarKidsContactFunction
    {
        private readonly IMediator _mediator;

        public StarKidsContactFunction(IMediator mediator)
        {
            this._mediator = mediator;
        }

        [FunctionName("ProcessStarKidsContact")]
        public async Task Run(
            [QueueTrigger("starkids-contact-form")]Models.SubmissionData queueItem, ILogger log,
            [Queue(Constants.DefaultQueueName)]IAsyncCollector<Models.SubmissionData> collector)
        {
            log.LogInformation($"C# Queue trigger function processed: {queueItem.Id}");

            var request = new Handlers.StarKidsContacts.Request { Data = queueItem };
            var result = await _mediator.Send(request);

            await collector.AddAsync(result.Data);
        }
    }
}
