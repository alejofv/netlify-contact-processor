using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AlejoF.Netlify.Contact.Models;
using MediatR;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace AlejoF.Netlify.Contact.Handlers
{
    public class ProcessContact
    {
        public class Request : IRequest
        {
            public Models.SubmissionData Data { get; set; }
        }

        public class Handler : IRequestHandler<Request>
        {
            private readonly CloudTable _settingsTable;
            private readonly CloudTable _contactsTable;

            public Handler(CloudTableClient client)
            {
                this._settingsTable = client.GetTableReference("NetlifyMappings");
                this._contactsTable = client.GetTableReference("NetlifyContacts");
            }

            public async Task<Unit> Handle(Request request, CancellationToken cancellationToken)
            {
                await _settingsTable.CreateIfNotExistsAsync();

                // Get site-specific settings
                var settings = await GetContactSettings(request.Data);
                if (settings == null)
                    throw new Exception("Contact settings not found");

                // Save form contact
                await SaveContact(request.Data, settings);
                return Unit.Value;
            }

            private async Task<ContactSettings> GetContactSettings(SubmissionData submission)
                => await _settingsTable.RetrieveAsync<ContactSettings>("contact-settings", $"{submission.SiteUrl}-{submission.FormName}");

            private async Task SaveContact(SubmissionData submission, ContactSettings settings)
            {
                var nameField = settings.EmailField ?? "name";
                var emailField = settings.EmailField ?? "email";
                var phoneField = settings.EmailField ?? "phone";

                var contact = new Contact
                {
                    PartitionKey = submission.SiteUrl,
                    RowKey = submission.Id,
                    Name = submission.ValueOf(nameField),
                    Email = submission.ValueOf(emailField),
                    Phone = submission.ValueOf(phoneField),
                    Details = JsonConvert.SerializeObject(submission.Fields),
                };

                await _contactsTable.CreateIfNotExistsAsync();
                await _contactsTable.InsertAsync(contact);
            }
        }

        /// <summary>
        /// PK: "contact-settings", RK: {site url}-{form name}
        /// </summary>
        public class ContactSettings : TableEntity
        {
            public string NameField { get; set; }
            public string EmailField { get; set; }
            public string PhoneField { get; set; }
        }

        /// <summary>
        /// PK: "site-url", RK: submission id
        /// </summary>
        public class Contact : TableEntity
        {
            public string Name { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string Details { get; set; }
        }
    }
}