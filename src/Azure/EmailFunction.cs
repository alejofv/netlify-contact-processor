using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;

namespace AlejoF.Netlify.Contact
{
    public class ProcessEmailFunction
    {
        private readonly IMediator _mediator;

        public ProcessEmailFunction(IMediator mediator)
        {
            this._mediator = mediator;
        }

        [FunctionName("process-email")]
        public async Task Run(
            [QueueTrigger(Constants.EmailQueueName, Connection = Storage.ConnectionStringSetting)]Models.SubmissionData queueItem, ILogger log,
            [SendGrid]IAsyncCollector<SendGridMessage> emailCollector)
        {
            log.LogInformation($"C# Queue trigger function processed: {queueItem.Id}");

            var request = new Handlers.ProcessEmail.Request { Data = queueItem };
            var result = await _mediator.Send(request);

            if (result.EmailMessage != null)
                await emailCollector.AddAsync(result.EmailMessage);
        }
    }
}
