using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.WindowsAzure.Storage.Table;
using SendGrid.Helpers.Mail;

namespace AlejoF.Contacts.Handlers
{
    public class FormContacts
    {
        public class Request : IRequest<Response>
        {
            public Models.SubmissionData Data { get; set; }
        }

        public class Response
        {
            public SendGridMessage EmailMessage { get; set; }
        }

        public class Handler : IRequestHandler<Request, Response>
        {
            private readonly CloudTable _contactsTable;
            private readonly CloudTable _settingsTable;

            public Handler(CloudTableClient client)
            {
                this._contactsTable = client.GetTableReference("FormContacts");
                this._settingsTable = client.GetTableReference("NetlifyMappings");
            }

            public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {
                // 1. If submission has policy = true, store it in TableStorage (starkidsco account)
                if (request.Data.ValueOf("save-contact") == bool.TrueString)
                    await SaveContact(request.Data);

                // Get site-specific settings
                var settings = await GetFormSettings(request.Data);
                if (settings == null)
                    return new Response();

                // Send mail message
                var msg = BuildEmailMessage(settings, request.Data);
                return new Response { EmailMessage = msg };
            }

            private async Task SaveContact(Models.SubmissionData submission)
            {
                var contact = new ContactDataEntity
                {
                    PartitionKey = submission.SiteUrl,
                    RowKey = submission.Id,
                    Name = submission.ValueOf("name"),
                    Email = submission.ValueOf("email"),
                    Phone = submission.ValueOf("phone"),
                    Message = submission.ValueOf("message"),
                };

                await _settingsTable.CreateIfNotExistsAsync();
                await _contactsTable.InsertAsync(contact);
            }

            private async Task<ContactSettings> GetFormSettings(Models.SubmissionData submissionData)
            {
                await _settingsTable.CreateIfNotExistsAsync();
                return await _settingsTable.RetrieveAsync<ContactSettings>("contact-form", $"{submissionData.SiteUrl}-{submissionData.FormName}");
            }

            private SendGridMessage BuildEmailMessage(ContactSettings settings, Models.SubmissionData submissionData)
            {
                // 1. Buld SendGrid message with params and substitutions
                var msg = new SendGridMessage();

                msg.SetFrom(new EmailAddress(settings.FromAddress, $"{submissionData.ValueOf("name")} (web)"));
                msg.AddTo(new EmailAddress(settings.ToAddress));

                if (!string.IsNullOrEmpty(settings.TemplateId))
                    msg.SetTemplateId(settings.TemplateId);

                if (string.IsNullOrEmpty(msg.TemplateId))
                {
                    var plainTextContent = submissionData.Fields
                        .Aggregate(
                            new StringBuilder("Se ha recibido un contacto a través de la página web ")
                                .Append(submissionData.SiteUrl)
                                .AppendLine(),
                            (sb, sub) => sb
                                .Append($"- {sub.Name}: {sub.Value}")
                                .AppendLine())
                        .ToString();

                    msg.SetSubject("Nuevo contacto web");
                    msg.AddContent(System.Net.Mime.MediaTypeNames.Text.Plain, plainTextContent);
                }
                else
                {
                    foreach (var s in submissionData.Fields)
                        msg.AddSubstitution(s.Name, s.Value);
                }

                return msg;
            }
        }

        /// <summary>
        /// PK: sitename, RK: SubmissionId
        /// </summary>
        public class ContactDataEntity : TableEntity
        {
            public string Name { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string Message { get; set; }
        }

        /// <summary>
        /// PK: "contact-form", RK: {site url}-{form name}
        /// </summary>
        public class ContactSettings : TableEntity
        {
            public string FromAddress { get; set; }
            public string ToAddress { get; set; }
            public string TemplateId { get; set; }
        }
    }
}