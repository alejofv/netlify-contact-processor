using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace AlejoF.Netlify.Contact
{
    public class ContactFunction
    {
        private readonly IMediator _mediator;

        public ContactFunction(IMediator mediator)
        {
            this._mediator = mediator;
        }

        [FunctionName("process-contact")]
        public async Task Run(
            [QueueTrigger(Constants.ContactQueueName)]Models.SubmissionData queueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {queueItem.Id}");

            var request = new Handlers.ProcessContact.Request { Data = queueItem };
            await _mediator.Send(request);
        }
    }
}
