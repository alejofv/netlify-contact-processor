# Netlify Contact Processor

This repo contains the source code for an Azure functions App that complements the azure-netlify-processor app.

The app is aimed to process routed submissions from various sites, using common processes such as sending an email or saving a contact information.

## Functions

**EmailFunction**

This functions gets the data from the submission and sends an email using SendGrid. By default, the sendgrid api call includes all the submission fields as substitutions and can (optionally) use an specific template.

To, From addresses and the template Id can be set up in an Azure Storage Table, using the following structure:

* Table name: NetlifyMappings
* PK: `"email-settings"`
* RK: `{site url}-{form name}`
* FromAddress: sender email address
* FromAddress: sender name
* ToAddress: target email address.
* TemplateId: SendGrid template ID (optional)
* TextContent: Text to use when no template is specified
* Subject: Email subject when no template is specified

Some fields support "tokens" surrounded by brackets (i.e.: `{email}`) to use the value coming from the specified submission field.
Special tokens for `{SiteUrl}` and `{FormName}` are also available.