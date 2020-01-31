using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.WindowsAzure.Storage.Table;
using SendGrid.Helpers.Mail;

namespace AlejoF.Contacts.Handlers
{
    public class StarKidsContacts
    {
        public class Request : IRequest<Response>
        {
            public Models.SubmissionData Data { get; set; }
        }

        public class Response
        {
            public Models.SubmissionData Data { get; set; }
        }

        public class Handler : IRequestHandler<Request, Response>
        {
            public Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {
                var submission = request.Data;
                var fields = submission.Fields.ToList();

                var mappedFields = fields
                    .Where(f => SubstitutionsMapping.ContainsKey(f.Name))
                    .Select(f => new Models.SubmissionField { Name = SubstitutionsMapping[f.Name], Value = f.Value })
                    .ToList();

                if (submission.ValueOf("policy") == "accept")
                {
                    submission.ContactInfo = new Models.ContactInfo
                    {
                        Name = submission.ValueOf("name"),
                        Email = submission.ValueOf("email"),
                        Phone = submission.ValueOf("phone"),
                    };
                }

                // replace fields
                submission.Fields = mappedFields.ToArray();
                return Task.FromResult(new Response { Data = submission });
            }

            private readonly Dictionary<string, string> SubstitutionsMapping = new Dictionary<string, string>
            {
                { "name", "-contact.name-" },
                { "email", "-contact.email-" },
                { "phone", "-contact.phone-" },
                { "message", "-message.text-" },
            };
        }
    }
}