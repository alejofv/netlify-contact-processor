using System;
using System.Linq;

namespace AlejoF.Contacts.Models
{
    public class SubmissionData
    {
        public string Id { get; set; }
        public string SiteUrl { get; set; }
        public string FormName { get; set; }
        public DateTime CreatedAt { get; set; }
        public SubmissionField[] Fields { get; set; }
        public ContactInfo ContactInfo { get; set; }

        public string ValueOf(string fieldName) => Fields.FirstOrDefault(f => f.Name == fieldName)?.Value;
    }

    public class SubmissionField
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class ContactInfo
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }
}
