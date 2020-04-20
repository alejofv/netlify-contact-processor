using System;
using System.Linq;

namespace AlejoF.Netlify.Contact.Models
{
    public class SubmissionData
    {
        public string Id { get; set; }
        public string SiteUrl { get; set; }
        public string FormName { get; set; }
        public DateTime CreatedAt { get; set; }
        public SubmissionField[] Fields { get; set; }

        public string ValueOf(string fieldName) => Fields.FirstOrDefault(f => f.Name == fieldName)?.Value;
    }

    public class SubmissionField
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
