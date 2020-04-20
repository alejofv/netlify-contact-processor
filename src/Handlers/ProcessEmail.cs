using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AlejoF.Netlify.Contact.Models;
using MediatR;
using Microsoft.WindowsAzure.Storage.Table;
using SendGrid.Helpers.Mail;

namespace AlejoF.Netlify.Contact.Handlers
{
    public class ProcessEmail
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
            private readonly CloudTable _settingsTable;

            public Handler(CloudTableClient client)
            {
                this._settingsTable = client.GetTableReference("NetlifyMappings");
            }

            public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {
                await _settingsTable.CreateIfNotExistsAsync();

                // Get site-specific settings
                var settings = await GetEmailSettings(request.Data);
                if (settings == null)
                    throw new Exception("Email settings not found");

                // Send mail message
                var msg = BuildEmailMessage(settings, request.Data);
                return new Response { EmailMessage = msg };
            }

            private async Task<EmailSettings> GetEmailSettings(SubmissionData submission)
            {
                var settings = await _settingsTable.RetrieveAsync<EmailSettings>("email-settings", $"{submission.SiteUrl}-{submission.FormName}");
                if (settings != null)
                {
                    var fields = submission.Fields
                        .Concat(new [] {
                            new SubmissionField { Name = nameof(SubmissionData.SiteUrl), Value = submission.SiteUrl },
                            new SubmissionField { Name = nameof(SubmissionData.FormName), Value = submission.FormName },
                        });

                    foreach (var field in fields)
                    {
                        var fieldToken = $"{{-{field.Name}-}}"; // i.e.: {-SiteURl-}

                        if (settings.FromAddress?.Contains(fieldToken) == true)
                            settings.FromAddress = settings.FromAddress.Replace(fieldToken, field.Value);
                        
                        if (settings.FromName?.Contains(fieldToken) == true)
                            settings.FromName = settings.FromName.Replace(fieldToken, field.Value);

                        if (settings.ToAddress?.Contains(fieldToken) == true)
                            settings.ToAddress = settings.ToAddress.Replace(fieldToken, field.Value);

                        if (settings.TextContent?.Contains(fieldToken) == true)
                            settings.TextContent = settings.TextContent.Replace(fieldToken, field.Value);

                        if (settings.Subject?.Contains(fieldToken) == true)
                            settings.Subject = settings.Subject.Replace(fieldToken, field.Value);
                    }
                }

                return settings;
            }

            private SendGridMessage BuildEmailMessage(EmailSettings settings, Models.SubmissionData submissionData)
            {
                // 1. Buld SendGrid message with params and substitutions
                var msg = new SendGridMessage();

                msg.SetFrom(new EmailAddress(settings.FromAddress, settings.FromName));
                msg.AddTo(new EmailAddress(settings.ToAddress));

                if (!string.IsNullOrEmpty(settings.TemplateId))
                    msg.SetTemplateId(settings.TemplateId);

                if (string.IsNullOrEmpty(msg.TemplateId))
                {
                    var plainTextContent = settings.TextContent;

                    msg.SetSubject(settings.Subject);
                    msg.AddContent(System.Net.Mime.MediaTypeNames.Text.Plain, plainTextContent);
                }
                else
                {
                    foreach (var s in submissionData.Fields)
                        msg.AddSubstitution($"-{s.Name}-", s.Value);
                }

                return msg;
            }
        }

        /// <summary>
        /// PK: "email-settings", RK: {site url}-{form name}
        /// </summary>
        public class EmailSettings : TableEntity
        {
            public string FromAddress { get; set; }
            public string FromName { get; set; }
            public string ToAddress { get; set; }
            public string TextContent { get; set; }
            public string Subject { get; set; }
            public string TemplateId { get; set; }
        }
    }
}